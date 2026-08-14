using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Jobs;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Owns the native state that <see cref="TransformSyncModes.Batched"/> uses to detect and apply
    /// <see cref="NetworkTransform"/> changes within a job.
    /// </summary>
    /// <remarks>
    /// One instance per <see cref="NetworkManager"/>. It is created the first time an instance registers and
    /// is disposed when the <see cref="NetworkManager"/> shuts down, so a project running in
    /// <see cref="TransformSyncModes.PerInstance"/> never allocates any of this.<br /><br />
    /// The collections it owns are parallel: index <c>i</c> of each refers to the same
    /// <see cref="NetworkTransform"/>. They are only ever mutated through <see cref="Register"/> and
    /// <see cref="Deregister"/>, both of which apply the same swap back to every collection, so they cannot
    /// drift apart. Each registered instance caches its own index in
    /// <see cref="NetworkTransform.StateManagerIndex"/>, which makes deregistration O(1) and removes the need
    /// for a lookup table.
    /// </remarks>
    internal class NetworkTransformStateManager : IDisposable
    {
        private const int k_InitialCapacity = 64;

        /// <summary>
        /// The registered instances, that are aligned in parallel to <see cref="States"/> and <see cref="TransformAccess"/>.
        /// </summary>
        private readonly List<NetworkTransform> m_Instances = new List<NetworkTransform>(k_InitialCapacity);

        /// <summary>
        /// The transform access array for the registered instances.
        /// </summary>
        internal TransformAccessArray TransformAccess;

        /// <summary>
        /// The most recently sent (authority) or received (non-authority) state per registered instance.
        /// </summary>
        internal NativeList<NetworkTransform.TransformDeltaEntry> Entries;

        private bool m_Created;
        private bool m_Disposed;

        /// <summary>
        /// The number of currently registered instances.
        /// </summary>
        /// <remarks>
        /// Kept as the length of the native list as opposed to a separate counter so that it cannot drift.
        /// </remarks>
        internal int GetCount()
        {
            return m_Created ? Entries.Length : 0;
        }

        internal NetworkTransform GetInstance(int index)
        {
            return m_Instances[index];
        }

        private void EnsureCreated()
        {
            if (m_Created)
            {
                return;
            }
            TransformAccess = new TransformAccessArray(k_InitialCapacity);
            Entries = new NativeList<NetworkTransform.TransformDeltaEntry>(k_InitialCapacity, Allocator.Persistent);
            m_Created = true;
        }

        /// <summary>
        /// Authority Only: <br />
        /// Registers a <see cref="NetworkTransform"/> so its state is tracked natively.
        /// </summary>
        /// <remarks>
        /// Invoked whenever an instance becomes an authority, which includes ownership changes since
        /// <see cref="NetworkTransform.InternalInitialization"/> runs again on each change of ownership.
        /// Registering an instance that is already registered does nothing.
        /// </remarks>
        internal void Register(NetworkTransform networkTransform)
        {
            if (m_Disposed || networkTransform.StateManagerIndex >= 0)
            {
                return;
            }

            EnsureCreated();

            networkTransform.StateManagerIndex = Entries.Length;
            m_Instances.Add(networkTransform);
            TransformAccess.Add(networkTransform.transform);
            // Seed with whatever the instance has already established so the first delta check compares
            // against a real state as opposed to a default one.
            Entries.Add(new NetworkTransform.TransformDeltaEntry()
            {
                State = networkTransform.LocalAuthoritativeNetworkState,
            });
        }

        /// <summary>
        /// Authority Only: <br />
        /// Deregisers a <see cref="NetworkTransform"/> instance from having its transform deltas tracked.
        /// </summary>
        /// <remarks>
        /// Invoked on despawn, destroy, and whenever an instance stops being an authority.
        /// </remarks>
        internal void Deregister(NetworkTransform networkTransform)
        {
            var index = networkTransform.StateManagerIndex;
            if (m_Disposed || index < 0)
            {
                return;
            }

            networkTransform.StateManagerIndex = -1;

            var lastIndex = m_Instances.Count - 1;
            var moved = m_Instances[lastIndex];

            // Every collection has to receive the same swap back or they stop referring to the same instance.
            Entries.RemoveAtSwapBack(index);
            TransformAccess.RemoveAtSwapBack(index);
            m_Instances[index] = moved;
            m_Instances.RemoveAt(lastIndex);

            // The instance that was swapped into this slot has to be told where it now lives. When the
            // instance being removed was already the last one there is nothing to move.
            if (index != lastIndex)
            {
                moved.StateManagerIndex = index;
            }
        }

        /// <summary>
        /// Runs the delta check job for every registered instance.
        /// </summary>
        /// <remarks>
        /// Invoked once per network tick in place of iterating the instances and having each one check itself.
        /// Each instance contributes what only the main thread can resolve, the job performs the detection in
        /// parallel, and anything that came back dirty then sends its state update on the main thread in the
        /// same order it would have otherwise.<br /><br />
        /// The job is completed within this call as opposed to being left in flight: the state update has to
        /// be sent on the tick it was detected on, so there is nothing to overlap with. 
        /// </remarks>
        internal void RunDeltaCheck()
        {
            var count = GetCount();
            if (count == 0)
            {
                return;
            }

            // Gather what the job cannot resolve for itself.
            for (int i = 0; i < count; i++)
            {
                var instance = m_Instances[i];
                var entry = Entries[i];
                instance.PrepareBatchedDeltaEntry(ref entry);
                Entries[i] = entry;
            }

            var job = new DetectTransformDeltaJob()
            {
                Entries = Entries.AsArray(),
            };
            job.Schedule(TransformAccess).Complete();

            // Apply the results. Iterated by index rather than by instance so that an instance which
            // deregisters as a result of its own state update (a despawn from within a callback) cannot
            // invalidate the iteration.
            for (int i = 0; i < Entries.Length; i++)
            {
                var instance = m_Instances[i];
                var entry = Entries[i];
                instance.ApplyBatchedDeltaEntry(ref entry);
                // The instance may have deregistered while applying, in which case this slot now belongs to a
                // different instance and must not be written back.
                if (instance.StateManagerIndex == i)
                {
                    Entries[i] = entry;
                }
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            m_Disposed = true;

            // Clear the cached index on anything still registered so a late deregister is a no-op as opposed
            // to indexing into a disposed collection.
            for (int i = 0; i < m_Instances.Count; i++)
            {
                if (m_Instances[i] != null)
                {
                    m_Instances[i].StateManagerIndex = -1;
                }
            }
            m_Instances.Clear();

            if (m_Created)
            {
                if (Entries.IsCreated)
                {
                    Entries.Dispose();
                }
                if (TransformAccess.isCreated)
                {
                    TransformAccess.Dispose();
                }
                m_Created = false;
            }
        }
    }
}
