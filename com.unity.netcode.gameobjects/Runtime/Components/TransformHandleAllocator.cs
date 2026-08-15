using System.Collections.Generic;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A compressed/bandwidth-friendly identifer allocation system that is used when <see cref="TransformSyncModes.Batched"/>
    /// mode is set. This helps to reduce the identifier from a bitpacked ulong and uint down to a ushort.
    /// </summary>
    /// <remarks>
    /// A batched state update identifies its instance by this handle rather than by a
    /// <see cref="NetworkObject.NetworkObjectId"/> and <see cref="NetworkBehaviour.NetworkBehaviourId"/> pair.
    /// The pair costs two to four bytes once bit packed and grows as object ids climb, where a "dense handle"
    /// ranges between one to two bytes for the lifetime of a session.<br />
    /// <br />
    /// Only the instance writing synchronization data (the server, or the session owner in a distributed
    /// authority topology) allocates. Everyone else is told the handle at spawn. That is deliberate: allocating
    /// on the owner would reassign the handle on every change of ownership, and the identity has to outlive
    /// ownership.<br />
    /// <br />
    /// Freed handles are not re-issued immediately.<br />
    /// An unreliable state update naming a handle can still be in flight when the instance it referred to despawns,
    /// and reissuing straight away would let that packet apply to whichever instance picked the handle up next.
    /// Holding to-be-released handles for <see cref="k_RecycleDelaySeconds"/> has no impact/cost and avoids running
    /// into this scenario.
    /// </remarks>
    internal class TransformHandleAllocator
    {
        /// <summary>
        /// Reserved to mean "no handle assigned".
        /// </summary>
        internal const ushort InvalidHandle = 0;

        /// <summary>
        /// How long a freed handle is held before it can be reissued. Comfortably longer than any state
        /// update can remain in flight.
        /// </summary>
        private const double k_RecycleDelaySeconds = 5.0;

        private struct PendingHandle
        {
            internal ushort Handle;
            internal double ReusableAtTime;
        }

        private ushort m_NextHandle = 1;

        /// <summary>
        /// Freed handles in the order they were released, which is also the order they become reusable.
        /// </summary>
        private readonly Queue<PendingHandle> m_PendingRecycle = new Queue<PendingHandle>();

        /// <summary>
        /// Resolves a handle back to its instance when a batched state update is applied.
        /// </summary>
        private readonly Dictionary<ushort, NetworkTransform> m_ByHandle = new Dictionary<ushort, NetworkTransform>();

        /// <summary>
        /// Issues a handle, reusing a previously freed one once it has been held long enough.
        /// </summary>
        /// <param name="currentTime">The current network time, used to age freed handles.</param>
        internal ushort Allocate(double currentTime)
        {
            if (m_PendingRecycle.Count > 0 && m_PendingRecycle.Peek().ReusableAtTime <= currentTime)
            {
                return m_PendingRecycle.Dequeue().Handle;
            }

            if (m_NextHandle != ushort.MaxValue)
            {
                return m_NextHandle++;
            }

            // Every handle is in use or still cooling down. Reusing the oldest one is the only way to keep
            // going, and it is the least likely to still be named by anything in flight.
            if (m_PendingRecycle.Count > 0)
            {
                NetworkLog.LogWarning($"[{nameof(NetworkTransform)}] Ran out of transform handles and had to reuse one before its hold expired. " +
                    "A state update still in flight for the previous instance could be applied to the new one.");
                return m_PendingRecycle.Dequeue().Handle;
            }

            NetworkLog.LogError($"[{nameof(NetworkTransform)}] Exhausted all {ushort.MaxValue - 1} transform handles. " +
                "Any further instances cannot be synchronized.");
            return InvalidHandle;
        }

        /// <summary>
        /// Releases a handle that only becomes reusable when the k_RecycleDelaySeconds
        /// delay period has expired.
        /// </summary>
        internal void Release(ushort handle, double currentTime)
        {
            if (handle == InvalidHandle)
            {
                return;
            }
            m_ByHandle.Remove(handle);
            m_PendingRecycle.Enqueue(new PendingHandle()
            {
                Handle = handle,
                ReusableAtTime = currentTime + k_RecycleDelaySeconds,
            });
        }

        /// <summary>
        /// Associates a handle with the instance it addresses, on both the sending and receiving sides.
        /// </summary>
        internal void Register(ushort handle, NetworkTransform networkTransform)
        {
            if (handle == InvalidHandle)
            {
                return;
            }
            m_ByHandle[handle] = networkTransform;
        }

        /// <summary>
        /// Removes the association without making the handle reusable for the non-authoritative
        /// instances.
        /// </summary>
        internal void Unregister(ushort handle)
        {
            if (handle == InvalidHandle)
            {
                return;
            }
            m_ByHandle.Remove(handle);
        }

        internal bool TryGet(ushort handle, out NetworkTransform networkTransform)
        {
            return m_ByHandle.TryGetValue(handle, out networkTransform);
        }

        internal int GetRegisteredCount()
        {
            return m_ByHandle.Count;
        }

        internal void Clear()
        {
            m_ByHandle.Clear();
            m_PendingRecycle.Clear();
            m_NextHandle = 1;
        }
    }
}
