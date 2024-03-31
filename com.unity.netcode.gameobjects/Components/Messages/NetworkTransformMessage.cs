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
        // This is only used when serializing but not serialized
        public bool DistributedAuthorityMode;
        // Might get removed
        public ulong[] TargetIds;

        private int GetTargetIdLength()
        {
            if (TargetIds != null)
            {
                return TargetIds.Length;
            }
            return 0;
        }

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
                if (DistributedAuthorityMode)
                {
                    var length = GetTargetIdLength();
                    BytePacker.WriteValuePacked(writer, length);
                    // If no target ids, then just exit early (DAHost specific)
                    if (length == 0)
                    {
                        return;
                    }
                    foreach (var target in TargetIds)
                    {
                        BytePacker.WriteValuePacked(writer, target);
                    }
                }
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
            var isSpawnedLocally = networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId);

            // Only defer if the NetworkObject is not spawned yet and the local NetworkManager is not running as a DAHost.
            if (!isSpawnedLocally && !networkManager.DAHost)
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
                return false;
            }

            // While the below check and assignment might seem out of place, this is specific to running in DAHost mode when a NetworkObject is
            // hidden from the DAHost but is visible to other clients. Since the DAHost needs to forward updates to the clients, we ignore processing
            // this message locally
            var networkObject = (NetworkObject)null;
            var isServerAuthoritative = false;
            var ownerAuthoritativeServerSide = false;

            // Get the behaviour index
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkBehaviourId);

            // Deserialize the state
            reader.ReadNetworkSerializableInPlace(ref State);

            if (networkManager.DistributedAuthorityMode)
            {
                var targetCount = 0;
                ByteUnpacker.ReadValueBitPacked(reader, out targetCount);
                if (targetCount > 0)
                {
                    TargetIds = new ulong[targetCount];
                }
                var targetId = (ulong)0;
                for (int i = 0; i < targetCount; i++)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out targetId);
                    TargetIds[i] = targetId;
                }
            }

            if (isSpawnedLocally)
            {
                networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];
                // Get the target NetworkTransform
                m_ReceiverNetworkTransform = networkObject.ChildNetworkBehaviours[NetworkBehaviourId] as NetworkTransform;
                isServerAuthoritative = m_ReceiverNetworkTransform.IsServerAuthoritative();
                ownerAuthoritativeServerSide = !isServerAuthoritative && networkManager.IsServer;
            }
            else
            {
                // If we are the DAHost and the NetworkObject is hidden from the host we still need to forward this message
                ownerAuthoritativeServerSide = networkManager.DAHost && !isSpawnedLocally;
            }

            if (ownerAuthoritativeServerSide)
            {
                var ownerClientId = (ulong)0;

                if (networkObject != null)
                {
                    ownerClientId = networkObject.OwnerClientId;
                    if (ownerClientId == NetworkManager.ServerClientId)
                    {
                        // Ownership must have changed, ignore any additional pending messages that might have
                        // come from a previous owner client.
                        return true;
                    }
                }
                else if (networkManager.DAHost)
                {
                    // Specific to distributed authority mode, the only sender of state updates will be the owner 
                    ownerClientId = context.SenderId;
                }

                var networkDelivery = State.IsReliableStateUpdate() ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced;

                // Forward the state update if there are any remote clients to foward it to
                if (networkManager.ConnectionManager.ConnectedClientsList.Count > (networkManager.IsHost ? 2 : 1))
                {
                    var clientCount = networkManager.DistributedAuthorityMode ? GetTargetIdLength() : networkManager.ConnectionManager.ConnectedClientsList.Count;
                    if (clientCount == 0)
                    {
                        return true;
                    }

                    // This is only to copy the existing and already serialized struct for forwarding purposes only.
                    // This will not include any changes made to this struct at this particular stage of processing the message.
                    var currentMessage = this;
                    // Create a new reader that replicates this message
                    currentMessage.m_CurrentReader = new FastBufferReader(reader, Collections.Allocator.None);
                    // Rewind the new reader to the beginning of the message's payload
                    currentMessage.m_CurrentReader.Seek(currentPosition);
                    // Forward the message to all connected clients that are observers of the associated NetworkObject

                    for (int i = 0; i < clientCount; i++)
                    {
                        var clientId = networkManager.DistributedAuthorityMode ? TargetIds[i] : networkManager.ConnectionManager.ConnectedClientsList[i].ClientId;
                        if (NetworkManager.ServerClientId == clientId || (!isServerAuthoritative && clientId == ownerClientId) ||
                            (!networkManager.DistributedAuthorityMode && !networkObject.Observers.Contains(clientId)))
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
            var networkManager = context.SystemOwner as NetworkManager;
            // Only if the local NetworkManager instance is running as the DAHost we just exit if there is no local
            // NetworkTransform component to apply the state update to (i.e. it is hidden from the DAHost and it
            // just forwarded the state update to any other connected client)
            if (networkManager.DAHost && m_ReceiverNetworkTransform == null)
            {
                return;
            }

            if (m_ReceiverNetworkTransform == null)
            {
                Debug.LogError($"[{nameof(NetworkTransformMessage)}][Dropped] Reciever {nameof(NetworkTransform)} was not set!");
                return;
            }
            m_ReceiverNetworkTransform.TransformStateUpdate(ref State, context.SenderId);
        }
    }
}
