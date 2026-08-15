using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// When using <see cref="TransformSyncModes.Batched"/> mode, this manages the jobs to detect changes in
    /// or apply changes to the transform assigned to each <see cref="NetworkTransform"/> instance.
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

        /// <summary>
        /// The maximum number of measurements an interpolator instance's buffer can hold.
        /// </summary>
        /// <remarks>
        /// The managed interpolator's queue is unbounded and only bounded in practice by
        /// <see cref="NativeInterpolator.BufferCountLimit"/>, which is the point at which it gives up and
        /// teleports. In practice a queue never holds more than the tick latency plus a couple of entries, so
        /// this is sized for that with headroom rather than for the panic threshold. Overflow drops the oldest
        /// measurement, which is the same value the interpolator would have consumed and discarded next.
        /// </remarks>
        private const int k_InterpolatorBufferCapacity = 32;

        /// <summary>
        /// Position, rotation and scale.
        /// </summary>
        private const int k_InterpolatorsPerInstance = 3;

        private const int k_ItemsPerInstance = k_InterpolatorBufferCapacity * k_InterpolatorsPerInstance;

        /// <summary>
        /// The registered non-authority instances, parallel to <see cref="InterpolationEntries"/>.
        /// </summary>
        private readonly List<NetworkTransform> m_NonAuthorityInstances = new List<NetworkTransform>(k_InitialCapacity);

        /// <summary>
        /// The interpolation state per registered non-authority instance.
        /// </summary>
        internal NativeList<InterpolationEntry> InterpolationEntries;

        /// <summary>
        /// The native list, where states are stored, that is used like a ring buffer.
        /// </summary>
        /// <remarks>
        /// An instance at index <c>i</c> owns the range starting at <c>i * k_ItemsPerInstance</c>, which is
        /// what lets the interpolation job write into one shared array without the indices aliasing.
        /// </remarks>
        internal NativeList<BufferedItemNative> BufferedItems;

        /// <summary>
        /// The bandwidth friendly transform identifiers used to uniquely identify each transform to its managed
        /// <see cref="NetworkTransform"/> component.
        /// </summary>
        /// <remarks>
        /// Managed only:
        /// Unlike the native collections, it is available without anything having registered.
        /// A handle is assigned to every synchronized instance, whether or not that instance is eligible for
        /// batched jobs or not now.
        /// </remarks>
        internal readonly TransformHandleAllocator Handles = new TransformHandleAllocator();

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
            InterpolationEntries = new NativeList<InterpolationEntry>(k_InitialCapacity, Allocator.Persistent);
            BufferedItems = new NativeList<BufferedItemNative>(k_InitialCapacity * k_ItemsPerInstance, Allocator.Persistent);
            m_Created = true;
        }

        /// <summary>
        /// Registers a non-authority <see cref="NetworkTransform"/> so its interpolation runs within a job.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Register"/> because the two run at different points (authority on the
        /// network tick, non-authority every frame) and need different data. An instance is only ever one or
        /// the other, and a change of ownership re-runs
        /// <see cref="NetworkTransform.InternalInitialization"/>, which moves it between the two.
        /// </remarks>
        internal void RegisterForInterpolation(NetworkTransform networkTransform)
        {
            if (m_Disposed || networkTransform.InterpolatorIndex >= 0)
            {
                return;
            }

            EnsureCreated();

            var index = InterpolationEntries.Length;
            networkTransform.InterpolatorIndex = index;
            m_NonAuthorityInstances.Add(networkTransform);

            // Give this instance its own slice of the shared measurement storage.
            BufferedItems.Length = (index + 1) * k_ItemsPerInstance;
            var offset = index * k_ItemsPerInstance;

            InterpolationEntries.Add(new InterpolationEntry()
            {
                Position = CreateInterpolatorState(offset, InterpolatorValueKind.Vector3),
                Rotation = CreateInterpolatorState(offset + k_InterpolatorBufferCapacity, InterpolatorValueKind.Quaternion),
                Scale = CreateInterpolatorState(offset + k_InterpolatorBufferCapacity * 2, InterpolatorValueKind.Vector3),
            });
        }

        private static NativeInterpolatorState CreateInterpolatorState(int bufferOffset, InterpolatorValueKind valueKind)
        {
            return new NativeInterpolatorState()
            {
                BufferOffset = bufferOffset,
                BufferCapacity = k_InterpolatorBufferCapacity,
                ValueKind = valueKind,
            };
        }

        /// <summary>
        /// Removes a <see cref="NetworkTransform"/> from native interpolation.
        /// </summary>
        internal void DeregisterFromInterpolation(NetworkTransform networkTransform)
        {
            var index = networkTransform.InterpolatorIndex;
            if (m_Disposed || index < 0)
            {
                return;
            }

            networkTransform.InterpolatorIndex = -1;

            var lastIndex = m_NonAuthorityInstances.Count - 1;
            var moved = m_NonAuthorityInstances[lastIndex];

            if (index != lastIndex)
            {
                // The buffer offsets are derived from the index, so the instance being swapped into this slot
                // has to have its measurements moved into this slot's range as well.
                var destination = index * k_ItemsPerInstance;
                var source = lastIndex * k_ItemsPerInstance;
                for (int i = 0; i < k_ItemsPerInstance; i++)
                {
                    BufferedItems[destination + i] = BufferedItems[source + i];
                }

                var movedEntry = InterpolationEntries[lastIndex];
                movedEntry.Position.BufferOffset = destination;
                movedEntry.Rotation.BufferOffset = destination + k_InterpolatorBufferCapacity;
                movedEntry.Scale.BufferOffset = destination + k_InterpolatorBufferCapacity * 2;
                InterpolationEntries[lastIndex] = movedEntry;

                moved.InterpolatorIndex = index;
            }

            InterpolationEntries.RemoveAtSwapBack(index);
            m_NonAuthorityInstances[index] = moved;
            m_NonAuthorityInstances.RemoveAt(lastIndex);
            BufferedItems.Length = m_NonAuthorityInstances.Count * k_ItemsPerInstance;
        }

        /// <summary>
        /// Advances the interpolators for every registered non-authority instance.
        /// </summary>
        /// <remarks>
        /// Invoked once per update stage in place of each instance interpolating itself. Only the buffer
        /// consumption and interpolation math run within the job; the results are applied to the transforms on
        /// the main thread afterwards by each instance's normal apply path.
        /// </remarks>
        internal void RunInterpolation()
        {
            var count = m_NonAuthorityInstances.Count;
            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var entry = InterpolationEntries[i];
                m_NonAuthorityInstances[i].PrepareInterpolationEntry(ref entry);
                InterpolationEntries[i] = entry;
            }

            var job = new InterpolateTransformJob()
            {
                Entries = InterpolationEntries.AsArray(),
                BufferedItems = BufferedItems.AsArray(),
            };
            // Explicitly qualified: UnityEngine.Jobs is in scope for TransformAccessArray, and its Schedule
            // extension would otherwise be preferred over the IJobParallelFor one.
            Jobs.IJobParallelForExtensions.Schedule(job, count, 16).Complete();
        }

        /// <summary>
        /// A state update waiting to go out in this tick's batch.
        /// </summary>
        /// <remarks>
        /// The state is captured rather than read back from the instance later, because committing a state
        /// update clears the teleport and explicit set flags immediately afterwards. Reading it at send time
        /// would transmit the already cleared version.
        /// </remarks>
        private struct PendingStateUpdate
        {
            internal NetworkTransform Instance;
            internal NetworkTransform.NetworkTransformState State;
        }

        private readonly List<PendingStateUpdate> m_PendingBatch = new List<PendingStateUpdate>(k_InitialCapacity);
        private NetworkTransformBatchMessage m_BatchMessage = new NetworkTransformBatchMessage();

        /// <summary>
        /// Queues a detected state update for this tick's batch instead of sending it on its own.
        /// </summary>
        internal void QueueForBatch(NetworkTransform networkTransform, in NetworkTransform.NetworkTransformState state)
        {
            m_PendingBatch.Add(new PendingStateUpdate()
            {
                Instance = networkTransform,
                State = state,
            });
        }

        /// <summary>
        /// Sends everything queued this tick, one message per observing client.
        /// </summary>
        /// <remarks>
        /// Assembled per client rather than once for everyone because observer sets differ between clients.
        /// </remarks>
        internal void SendBatchedStateUpdates(NetworkManager networkManager)
        {
            if (m_PendingBatch.Count == 0)
            {
                return;
            }

            // Only the server registers instances for batching, so a non-server should never have anything
            // queued. Kept as a safety net rather than an assumption: silently dropping is still better than
            // a client attempting a send it cannot address, but it should not be reachable.
            if (networkManager.ShutdownInProgress || !networkManager.IsServer)
            {
                m_PendingBatch.Clear();
                return;
            }

            m_BatchMessage.Manager = this;

            var connectedClients = networkManager.ConnectionManager.ConnectedClientsList;
            for (int i = 0; i < connectedClients.Count; i++)
            {
                var clientId = connectedClients[i].ClientId;
                if (clientId == NetworkManager.ServerClientId)
                {
                    continue;
                }

                if (!HasAnythingFor(clientId))
                {
                    continue;
                }

                m_BatchMessage.TargetClientId = clientId;
                networkManager.MessageManager.SendMessage(ref m_BatchMessage, NetworkDelivery.ReliableFragmentedSequenced, clientId);
            }

            m_PendingBatch.Clear();
        }

        /// <summary>
        /// Whether any queued state update is observed by the given client.
        /// </summary>
        /// <remarks>
        /// Checked before sending so a client that observes none of this tick's updates gets no message at all
        /// rather than one containing a count of zero.
        /// </remarks>
        private bool HasAnythingFor(ulong clientId)
        {
            for (int i = 0; i < m_PendingBatch.Count; i++)
            {
                var instance = m_PendingBatch[i].Instance;
                if (instance != null && instance.NetworkObject != null && instance.NetworkObject.Observers.Contains(clientId))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Writes the queued state updates the given client observes.
        /// </summary>
        /// <remarks>
        /// The count is backfilled once the entries are written, since it is not known until the observer
        /// filtering has run.
        /// </remarks>
        internal void WriteBatch(FastBufferWriter writer, ulong targetClientId)
        {
            // Written at a fixed width rather than bit packed. The value is not known until the observer
            // filtering below has run, and a bit packed placeholder that later needs more bytes would overrun
            // the first entry when seeking back. One byte is not worth that failure mode.
            var count = (ushort)0;
            var countPosition = writer.Position;
            writer.WriteValueSafe(count);

            for (int i = 0; i < m_PendingBatch.Count; i++)
            {
                var pending = m_PendingBatch[i];
                var instance = pending.Instance;
                if (instance == null || instance.NetworkObject == null || !instance.NetworkObject.Observers.Contains(targetClientId))
                {
                    continue;
                }

                BytePacker.WriteValueBitPacked(writer, instance.TransformHandle);
                writer.WriteNetworkSerializable(pending.State);
                count++;
            }

            var tailPosition = writer.Position;
            writer.Seek(countPosition);
            writer.WriteValueSafe(count);
            writer.Seek(tailPosition);
        }

        /// <summary>
        /// Which of an instance's three interpolators an operation applies to.
        /// </summary>
        internal enum InterpolatorTarget
        {
            Position,
            Rotation,
            Scale,
        }

        /// <summary>
        /// The native equivalent of <see cref="BufferedLinearInterpolator{T}.AddMeasurement(T, double)"/>.
        /// </summary>
        internal void AddMeasurement(int index, InterpolatorTarget target, float4 value, double time)
        {
            var entry = InterpolationEntries[index];
            var items = BufferedItems.AsArray();
            switch (target)
            {
                case InterpolatorTarget.Position:
                    NativeInterpolator.AddMeasurement(ref entry.Position, ref items, value, time);
                    break;
                case InterpolatorTarget.Rotation:
                    NativeInterpolator.AddMeasurement(ref entry.Rotation, ref items, value, time);
                    break;
                default:
                    NativeInterpolator.AddMeasurement(ref entry.Scale, ref items, value, time);
                    break;
            }
            InterpolationEntries[index] = entry;
        }

        /// <summary>
        /// The native equivalent of <see cref="BufferedLinearInterpolator{T}.ResetTo(T, double)"/>.
        /// </summary>
        internal void ResetTo(int index, InterpolatorTarget target, float4 value, double time)
        {
            var entry = InterpolationEntries[index];
            var items = BufferedItems.AsArray();
            switch (target)
            {
                case InterpolatorTarget.Position:
                    NativeInterpolator.ResetTo(ref entry.Position, ref items, value, time);
                    entry.InterpolatedPosition = value;
                    break;
                case InterpolatorTarget.Rotation:
                    NativeInterpolator.ResetTo(ref entry.Rotation, ref items, value, time);
                    entry.InterpolatedRotation = value;
                    break;
                default:
                    NativeInterpolator.ResetTo(ref entry.Scale, ref items, value, time);
                    entry.InterpolatedScale = value;
                    break;
            }
            InterpolationEntries[index] = entry;
        }

        /// <summary>
        /// The native equivalent of clearing all three of an instance's interpolators.
        /// </summary>
        internal void ClearInterpolators(int index)
        {
            var entry = InterpolationEntries[index];
            NativeInterpolator.Clear(ref entry.Position);
            NativeInterpolator.Clear(ref entry.Rotation);
            NativeInterpolator.Clear(ref entry.Scale);
            InterpolationEntries[index] = entry;
        }

        /// <summary>
        /// Re-expresses an instance's buffered measurements and in flight values in a different space.
        /// </summary>
        /// <remarks>
        /// Scale is deliberately not converted: it is a local scale, so it is already parent relative and
        /// means the same thing under either parent. The managed interpolator does not convert it either.
        /// </remarks>
        internal void ConvertInterpolationSpace(int index, in float4x4 pointTransform, in quaternion rotationTransform)
        {
            var entry = InterpolationEntries[index];
            var items = BufferedItems.AsArray();

            NativeInterpolator.ConvertSpace(ref entry.Position, ref items, pointTransform, rotationTransform);
            NativeInterpolator.ConvertSpace(ref entry.Rotation, ref items, pointTransform, rotationTransform);

            // The most recently produced results are converted as well, otherwise the value applied on the
            // frame of the reparent would still be in the old space.
            entry.InterpolatedPosition = new float4(math.transform(pointTransform, entry.InterpolatedPosition.xyz), 0.0f);
            entry.InterpolatedRotation = math.mul(rotationTransform, new quaternion(entry.InterpolatedRotation)).value;

            InterpolationEntries[index] = entry;
        }

        /// <summary>
        /// Diagnostics for an instance's position interpolator: how many measurements are buffered, whether it
        /// has a target, and what the job last produced.
        /// </summary>
        /// <remarks>
        /// Separates "no state is arriving" from "state is arriving but not being advanced or applied", which
        /// are otherwise indistinguishable from the outside.
        /// </remarks>
        internal string DescribePositionInterpolator(int index)
        {
            if (index < 0 || !m_Created || index >= InterpolationEntries.Length)
            {
                return "not registered";
            }
            var entry = InterpolationEntries[index];
            var position = entry.Position;
            var items = BufferedItems.AsArray();
            var oldest = position.BufferCount > 0
                ? items[position.BufferOffset + position.BufferHead].TimeSent.ToString("F4")
                : "none";
            return $"buffered={position.BufferCount} hasTarget={position.HasTarget} " +
                $"target={(position.HasTarget ? position.Target.Item.xyz.ToString() : "none")} " +
                $"targetStamp={(position.HasTarget ? position.Target.TimeSent.ToString("F4") : "none")} " +
                $"oldestStamp={oldest} " +
                $"current={position.CurrentValue.xyz} result={entry.InterpolatedPosition.xyz} " +
                $"received={position.BufferCounter} syncPos={entry.SynchronizePosition}";
        }

        /// <summary>
        /// The native equivalent of <see cref="BufferedLinearInterpolator{T}.GetInterpolatedValue"/>.
        /// </summary>
        internal float4 GetInterpolatedValue(int index, InterpolatorTarget target)
        {
            var entry = InterpolationEntries[index];
            switch (target)
            {
                case InterpolatorTarget.Position:
                    return entry.InterpolatedPosition;
                case InterpolatorTarget.Rotation:
                    return entry.InterpolatedRotation;
                default:
                    return entry.InterpolatedScale;
            }
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

            for (int i = 0; i < m_NonAuthorityInstances.Count; i++)
            {
                if (m_NonAuthorityInstances[i] != null)
                {
                    m_NonAuthorityInstances[i].InterpolatorIndex = -1;
                }
            }
            m_NonAuthorityInstances.Clear();
            Handles.Clear();

            if (m_Created)
            {
                if (InterpolationEntries.IsCreated)
                {
                    InterpolationEntries.Dispose();
                }
                if (BufferedItems.IsCreated)
                {
                    BufferedItems.Dispose();
                }
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
