using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// The message that delivers the <see cref="TransformSyncModes.Batched"/> <see cref="NetworkTransform"/>
    /// state updates, per tick, as a single message.
    /// </summary>
    /// <remarks>
    /// - TransformHandle helps reduce bandwidth overhead.
    /// - These messages are always delivered reliably.
    /// </remarks>
    internal struct NetworkTransformBatchMessage : INetworkMessage
    {
        public int Version => 0;
        private const string k_Name = "NetworkTransformBatchMessage";

        /// <summary>
        /// The state manager is set before sending.
        /// </summary>
        internal NetworkTransformStateManager Manager;

        /// <summary>
        /// Only instances this client observes are written.
        /// </summary>
        internal ulong TargetClientId;

        internal int BytesWritten;

        /// <summary>
        /// Placeholder to read an entry whose handle does not resolve locally.
        /// </summary>
        /// <remarks>
        /// For batched transforms, we don't worry about deferring messages for
        /// received state updates since we currently are synchronizing the full
        /// state (i.e. no delta compression).
        /// </remarks>
        private NetworkTransform.NetworkTransformState m_Discarded;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            var startPosition = writer.Position;
            Manager.WriteBatch(writer, TargetClientId);
            BytesWritten = writer.Position - startPosition;
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = context.SystemOwner as NetworkManager;
            if (networkManager == null)
            {
                Debug.LogError($"[{k_Name}] System owner context was not of type {nameof(NetworkManager)}!");
                return false;
            }
            if (networkManager.ShutdownInProgress)
            {
                return false;
            }

            // Fixed width to match the writer cannot be bit packed.
            reader.ReadValueSafe(out ushort count);
            var handles = networkManager.TransformStateManager.Handles;

            for (int i = 0; i < count; i++)
            {
                ByteUnpacker.ReadValueBitPacked(reader, out ushort handle);

                // An entry is applied as it is read rather than being collected and applied in Handle, since
                // holding onto every state would mean allocating the in-bound payload per message.
                if (handles.TryGet(handle, out var networkTransform) && networkTransform != null)
                {
                    var currentPosition = reader.Position;
                    reader.ReadNetworkSerializableInPlace(ref networkTransform.InboundState);
                    networkTransform.InboundState.LastSerializedSize = reader.Position - currentPosition;
                    networkTransform.TransformStateUpdate();
                    continue;
                }

                // If the handle does not resolve locally, which can happen while a spawn is still in flight or just
                // after a despawn, the entry is read and dropped rather than deferring the whole message.
                reader.ReadNetworkSerializableInPlace(ref m_Discarded);
            }

            return true;
        }

        /// <summary>
        /// Since states are applied during deserialization, we have nothing
        /// to "handle" for this message
        /// </summary>
        public void Handle(ref NetworkContext context)
        {
            // NOP
        }
    }
}
