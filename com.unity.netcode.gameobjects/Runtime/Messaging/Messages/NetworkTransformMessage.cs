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
        private const string k_Name = "NetworkTransformMessage";

        internal NetworkTransform NetworkTransform;

        // Only used for DAHost
        internal NetworkTransform.NetworkTransformState State;
        private FastBufferReader m_CurrentReader;

        internal int BytesWritten;

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
                BytesWritten = NetworkTransform.SerializeMessage(writer, targetVersion);
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
            var networkObjectId = (ulong)0;
            var networkBehaviourId = 0;

            ByteUnpacker.ReadValueBitPacked(reader, out networkObjectId);
            var isSpawnedLocally = networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId);

            // Only defer if the NetworkObject is not spawned yet and the local NetworkManager is not running as a DAHost.
            if (!isSpawnedLocally && !networkManager.DAHost)
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, networkObjectId, reader, ref context, k_Name);
                return false;
            }

            // While the below check and assignment might seem out of place, this is specific to running in DAHost mode when a NetworkObject is
            // hidden from the DAHost but is visible to other clients. Since the DAHost needs to forward updates to the clients, we ignore processing
            // this message locally
            var networkObject = (NetworkObject)null;
            var isServerAuthoritative = false;
            var ownerAuthoritativeServerSide = false;

            // Get the behaviour index
            ByteUnpacker.ReadValueBitPacked(reader, out networkBehaviourId);

            if (isSpawnedLocally)
            {
                networkObject = networkManager.SpawnManager.SpawnedObjects[networkObjectId];
                if (networkObject.ChildNetworkBehaviours.Count <= networkBehaviourId || networkObject.ChildNetworkBehaviours[networkBehaviourId] == null)
                {
                    Debug.LogError($"[{nameof(NetworkTransformMessage)}][Invalid] Targeted {nameof(NetworkTransform)}, {nameof(NetworkBehaviour.NetworkBehaviourId)} ({networkBehaviourId}), does not exist! Make sure you are not spawning {nameof(NetworkObject)}s with disabled {nameof(GameObject)}s that have {nameof(NetworkBehaviour)} components on them.");
                    return false;
                }

                // Get the target NetworkTransform
                var transform = networkObject.ChildNetworkBehaviours[networkBehaviourId] as NetworkTransform;
                if (transform == null)
                {
                    Debug.LogError($"[{nameof(NetworkTransformMessage)}][Invalid] Targeted {nameof(NetworkTransform)}, {nameof(NetworkBehaviour.NetworkBehaviourId)} ({networkBehaviourId}), does not exist! Make sure you are not spawning {nameof(NetworkObject)}s with disabled {nameof(GameObject)}s that have {nameof(NetworkBehaviour)} components on them.");
                    return false;
                }

                NetworkTransform = transform;
                isServerAuthoritative = NetworkTransform.IsServerAuthoritative();
                ownerAuthoritativeServerSide = !isServerAuthoritative && networkManager.IsServer;

                reader.ReadNetworkSerializableInPlace(ref NetworkTransform.InboundState);
                NetworkTransform.InboundState.LastSerializedSize = reader.Position - currentPosition;
            }
            else
            {
                ownerAuthoritativeServerSide = networkManager.DAHost;
                // If we are the DAHost and the NetworkObject is hidden from the host we still need to forward this message.
                if (ownerAuthoritativeServerSide)
                {
                    // We need to deserialize the state to our local State property so we can extract the reliability used.
                    reader.ReadNetworkSerializableInPlace(ref State);
                    // Fall through to act like a proxy for this message.
                }
                else
                {
                    // Otherwise we can error out because we either shouldn't be receiving this message.
                    Debug.LogError($"[{nameof(NetworkTransformMessage)}][Invalid] Target NetworkObject ({networkObjectId}) does not exist!");
                    return false;
                }
            }

            unsafe
            {
                if (ownerAuthoritativeServerSide)
                {
                    var targetCount = 1;

                    if (networkManager.DistributedAuthorityMode && networkManager.DAHost)
                    {
                        ByteUnpacker.ReadValueBitPacked(reader, out targetCount);
                    }

                    var targetIds = stackalloc ulong[targetCount];

                    if (networkManager.DistributedAuthorityMode && networkManager.DAHost)
                    {
                        var targetId = (ulong)0;
                        for (int i = 0; i < targetCount; i++)
                        {
                            ByteUnpacker.ReadValueBitPacked(reader, out targetId);
                            targetIds[i] = targetId;
                        }
                    }

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

                    // Depending upon whether it is spawned locally or not, get the deserialized state
                    var stateToUse = NetworkTransform != null ? NetworkTransform.InboundState : State;
                    // Determine the reliability used to send the message
                    var networkDelivery = stateToUse.IsReliableStateUpdate() ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced;

                    // Forward the state update if there are any remote clients to foward it to
                    if (networkManager.ConnectionManager.ConnectedClientsList.Count > (networkManager.IsHost ? 2 : 1))
                    {
                        var clientCount = networkManager.DistributedAuthorityMode ? targetCount : networkManager.ConnectionManager.ConnectedClientsList.Count;
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
                            var clientId = networkManager.DistributedAuthorityMode ? targetIds[i] : networkManager.ConnectionManager.ConnectedClientsList[i].ClientId;
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
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = context.SystemOwner as NetworkManager;
            // Only if the local NetworkManager instance is running as the DAHost we just exit if there is no local
            // NetworkTransform component to apply the state update to (i.e. it is hidden from the DAHost and it
            // just forwarded the state update to any other connected client)
            if (networkManager.DAHost && NetworkTransform == null)
            {
                return;
            }

            if (NetworkTransform == null)
            {
                Debug.LogError($"[{nameof(NetworkTransformMessage)}][Dropped] Reciever {nameof(NetworkTransform)} was not set!");
                return;
            }
            NetworkTransform.TransformStateUpdate(context.SenderId);
        }
    }
}
