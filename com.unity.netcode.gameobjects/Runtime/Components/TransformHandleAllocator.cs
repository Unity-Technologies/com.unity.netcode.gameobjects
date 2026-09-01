using System.Collections.Generic;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Allocates the ushort handles that identify instances in <see cref="TransformSyncModes.Batched"/> mode.
    /// </summary>
    /// <remarks>
    /// A batched state update names its instance by a handle. The alternative is a
    /// <see cref="NetworkObject.NetworkObjectId"/> and <see cref="NetworkBehaviour.NetworkBehaviourId"/> pair,
    /// which costs more once bit packed and grows as object ids climb.<br />
    /// Only the instance writing synchronization data allocates: the server, or the session owner in a
    /// distributed authority topology. Everyone else is told the handle at spawn.<br />
    /// The owner does not allocate. A handle has to outlive ownership changes.<br />
    /// Freed handles are held for <see cref="k_RecycleDelaySeconds"/> before being reissued. An unreliable
    /// state update naming a handle can still be in flight when its instance despawns.
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
