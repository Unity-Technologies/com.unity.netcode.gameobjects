using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// NetworkTransform State Update Message
    /// </summary>
    internal struct NetworkTransformMessage : INetworkMessage
    {
        public int Version => 0;
        public ulong NetworkObjectId;
        public int NetworkBehaviourId;
        public NetworkTransform.NetworkTransformState State;

        private NetworkTransform m_ReceiverNetworkTransform;
        private FastBufferReader m_CurrentReader;

        private unsafe void CopyPayload(ref FastBufferWriter writer)
        {
            writer.WriteBytesSafe(m_CurrentReader.GetUnsafePtrAtCurrentPosition(), m_CurrentReader.Length - m_CurrentReader.Position);
        }

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            if (m_CurrentReader.IsInitialized)
            {
                CopyPayload(ref writer);
            }
            else
            {
                BytePacker.WriteValueBitPacked(writer, NetworkObjectId);
                BytePacker.WriteValueBitPacked(writer, NetworkBehaviourId);
                writer.WriteNetworkSerializable(State);
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = context.SystemOwner as NetworkManager;
            if (networkManager == null)
            {
                Debug.LogError($"[{nameof(NetworkTransformMessage)}] System owner context was not of type {nameof(NetworkManager)}!");
                return false;
            }
            var currentPosition = reader.Position;
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkObjectId);
            if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
                return false;
            }
            // Get the behaviour index
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkBehaviourId);

            // Deserialize the state
            reader.ReadNetworkSerializable(out State);

            var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];

            // Get the target NetworkTransform
            m_ReceiverNetworkTransform = networkObject.ChildNetworkBehaviours[NetworkBehaviourId] as NetworkTransform;

            var isServerAuthoritative = m_ReceiverNetworkTransform.IsServerAuthoritative();
            var ownerAuthoritativeServerSide = !isServerAuthoritative && networkManager.IsServer;
            if (ownerAuthoritativeServerSide)
            {
                var ownerClientId = networkObject.OwnerClientId;
                if (ownerClientId == NetworkManager.ServerClientId)
                {
                    // Ownership must have changed, ignore any additional pending messages that might have
                    // come from a previous owner client.
                    return true;
                }

                var networkDelivery = State.IsReliableStateUpdate() ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced;

                // Forward the state update if there are any remote clients to foward it to
                if (networkManager.ConnectionManager.ConnectedClientsList.Count > (networkManager.IsHost ? 2 : 1))
                {
                    // This is only to copy the existing and already serialized struct for forwarding purposes only.
                    // This will not include any changes made to this struct at this particular stage of processing the message.
                    var currentMessage = this;
                    // Create a new reader that replicates this message
                    currentMessage.m_CurrentReader = new FastBufferReader(reader, Collections.Allocator.None);
                    // Rewind the new reader to the beginning of the message's payload
                    currentMessage.m_CurrentReader.Seek(currentPosition);
                    // Forward the message to all connected clients that are observers of the associated NetworkObject
                    var clientCount = networkManager.ConnectionManager.ConnectedClientsList.Count;
                    for (int i = 0; i < clientCount; i++)
                    {
                        var clientId = networkManager.ConnectionManager.ConnectedClientsList[i].ClientId;
                        if (NetworkManager.ServerClientId == clientId || (!isServerAuthoritative && clientId == ownerClientId) || !networkObject.Observers.Contains(clientId))
                        {
                            continue;
                        }
                        networkManager.MessageManager.SendMessage(ref currentMessage, networkDelivery, clientId);
                    }
                    // Dispose of the reader used for forwarding
                    currentMessage.m_CurrentReader.Dispose();
                }
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            if (m_ReceiverNetworkTransform == null)
            {
                Debug.LogError($"[{nameof(NetworkTransformMessage)}][Dropped] Reciever {nameof(NetworkTransform)} was not set!");
                return;
            }
            m_ReceiverNetworkTransform.TransformStateUpdate(ref State);
        }
    }
}
