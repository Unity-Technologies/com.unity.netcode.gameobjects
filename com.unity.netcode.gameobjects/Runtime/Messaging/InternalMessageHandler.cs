using System.IO;
using UnityEngine;


namespace Unity.Netcode
{
    internal class InternalMessageHandler : IInternalMessageHandler
    {
        public NetworkManager NetworkManager => m_NetworkManager;
        private NetworkManager m_NetworkManager;

        public InternalMessageHandler(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public void HandleConnectionRequest(ulong clientId, Stream stream)
        {
            if (NetworkManager.PendingClients.TryGetValue(clientId, out PendingClient client))
            {
                // Set to pending approval to prevent future connection requests from being approved
                client.ConnectionState = PendingClient.State.PendingApproval;
            }

            using var reader = PooledNetworkReader.Get(stream);
            ulong configHash = reader.ReadUInt64Packed();
            if (!NetworkManager.NetworkConfig.CompareConfig(configHash))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                }

                // Treat this similar to a client that is not approved (remove from pending and disconnect at transport layer)
                NetworkManager.PendingClients.Remove(clientId);
                NetworkManager.NetworkConfig.NetworkTransport.DisconnectRemoteClient(clientId);
                return;
            }

            if (NetworkManager.NetworkConfig.ConnectionApproval)
            {
                byte[] connectionBuffer = reader.ReadByteArray();
                NetworkManager.InvokeConnectionApproval(connectionBuffer, clientId,
                    (createPlayerObject, playerPrefabHash, approved, position, rotation) =>
                        NetworkManager.HandleApproval(clientId, createPlayerObject, playerPrefabHash, approved, position, rotation));
            }
            else
            {
                NetworkManager.HandleApproval(clientId, NetworkManager.NetworkConfig.PlayerPrefab != null, null, true, null, null);
            }
        }

        /// <summary>
        /// Client Side: handles the connection approved message
        /// </summary>
        /// <param name="clientId">transport derived client identifier (currently not used)</param>
        /// <param name="stream">incoming stream</param>
        /// <param name="receiveTime">time this message was received (currently not used)</param>
        public void HandleConnectionApproved(ulong clientId, Stream stream, float receiveTime)
        {
            using var reader = PooledNetworkReader.Get(stream);
            NetworkManager.LocalClientId = reader.ReadUInt64Packed();

            int tick = reader.ReadInt32Packed();
            var time = new NetworkTime(NetworkManager.NetworkTickSystem.TickRate, tick);
            NetworkManager.NetworkTimeSystem.Reset(time.Time, 0.15f); // Start with a constant RTT of 150 until we receive values from the transport.

            NetworkManager.ConnectedClients.Add(NetworkManager.LocalClientId, new NetworkClient { ClientId = NetworkManager.LocalClientId });

            // Only if scene management is disabled do we handle NetworkObject synchronization at this point
            if (!NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                NetworkManager.SpawnManager.DestroySceneObjects();

                // is not packed!
                var objectCount = reader.ReadUInt16();
                for (ushort i = 0; i < objectCount; i++)
                {
                    NetworkObject.DeserializeSceneObject(reader.GetStream() as NetworkBuffer, reader, m_NetworkManager);
                }

                // Mark the client being connected
                m_NetworkManager.IsConnectedClient = true;
                // When scene management is disabled we notify after everything is synchronized
                m_NetworkManager.InvokeOnClientConnectedCallback(clientId);
            }
        }

        public void HandleAddObject(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            var isPlayerObject = reader.ReadBool();
            var networkId = reader.ReadUInt64Packed();
            var ownerClientId = reader.ReadUInt64Packed();
            var hasParent = reader.ReadBool();
            ulong? parentNetworkId = null;

            if (hasParent)
            {
                parentNetworkId = reader.ReadUInt64Packed();
            }

            var softSync = reader.ReadBool();
            var prefabHash = reader.ReadUInt32Packed();

            Vector3? pos = null;
            Quaternion? rot = null;
            if (reader.ReadBool())
            {
                pos = new Vector3(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                rot = Quaternion.Euler(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
            }

            var (isReparented, latestParent) = NetworkObject.ReadNetworkParenting(reader);

            var networkObject = NetworkManager.SpawnManager.CreateLocalNetworkObject(softSync, prefabHash, ownerClientId, parentNetworkId, pos, rot, isReparented);
            networkObject.SetNetworkParenting(isReparented, latestParent);
            NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, networkId, softSync, isPlayerObject, ownerClientId, stream, true, false);
            var bytesReported = NetworkManager.LocalClientId == clientId
                ? 0
                : stream.Length;
            m_NetworkManager.NetworkMetrics.TrackObjectSpawnReceived(clientId, networkObject.NetworkObjectId, networkObject.name, bytesReported);
        }

        public void HandleDestroyObject(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ulong networkObjectId = reader.ReadUInt64Packed();

            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
            {
                // This is the same check and log message that happens inside OnDespawnObject, but we have to do it here
                // while we still have access to the network ID, otherwise the log message will be less useful.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Trying to destroy {nameof(NetworkObject)} #{networkObjectId} but it does not exist in {nameof(NetworkSpawnManager.SpawnedObjects)} anymore!");
                }

                return;
            }

            var bytesReported = NetworkManager.LocalClientId == clientId
                ? 0
                : stream.Length;
            m_NetworkManager.NetworkMetrics.TrackObjectDestroyReceived(clientId, networkObjectId, networkObject.name, bytesReported);
            NetworkManager.SpawnManager.OnDespawnObject(networkObject, true);
        }

        /// <summary>
        /// Called for all Scene Management related events
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="stream"></param>
        public void HandleSceneEvent(ulong clientId, Stream stream)
        {
            NetworkManager.SceneManager.HandleSceneEvent(clientId, stream);
        }

        public void HandleChangeOwner(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ulong networkObjectId = reader.ReadUInt64Packed();
            ulong ownerClientId = reader.ReadUInt64Packed();

            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Trying to handle owner change but {nameof(NetworkObject)} #{networkObjectId} does not exist in {nameof(NetworkSpawnManager.SpawnedObjects)} anymore!");
                }

                return;
            }

            if (networkObject.OwnerClientId == NetworkManager.LocalClientId)
            {
                //We are current owner.
                networkObject.InvokeBehaviourOnLostOwnership();
            }

            networkObject.OwnerClientId = ownerClientId;

            if (ownerClientId == NetworkManager.LocalClientId)
            {
                //We are new owner.
                networkObject.InvokeBehaviourOnGainedOwnership();
            }

            var bytesReported = NetworkManager.LocalClientId == clientId
                ? 0
                : stream.Length;
            NetworkManager.NetworkMetrics.TrackOwnershipChangeReceived(clientId, networkObject.NetworkObjectId, networkObject.name, bytesReported);
        }

        public void HandleTimeSync(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            int tick = reader.ReadInt32Packed();
            var time = new NetworkTime(NetworkManager.NetworkTickSystem.TickRate, tick);
            NetworkManager.NetworkTimeSystem.Sync(time.Time, NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId) / 1000d);
        }

        public void HandleNetworkVariableDelta(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ulong networkObjectId = reader.ReadUInt64Packed();
            ushort networkBehaviourIndex = reader.ReadUInt16Packed();

            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                NetworkBehaviour behaviour = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourIndex);

                if (behaviour == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Network variable delta message received for a non-existent behaviour. {nameof(networkObjectId)}: {networkObjectId}, {nameof(networkBehaviourIndex)}: {networkBehaviourIndex}");
                    }
                }
                else
                {
                    behaviour.HandleNetworkVariableDeltas(stream, clientId);
                }
            }
            else if (NetworkManager.IsServer)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Network variable delta message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta was lost.");
                }
            }
        }

        /// <summary>
        /// Converts the stream to a PerformanceQueueItem and adds it to the receive queue
        /// </summary>
        public void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType)
        {
            if (NetworkManager.IsServer && clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            if (messageType == MessageQueueContainer.MessageType.None)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogError($"Message header contained an invalid type: {((int)messageType).ToString()}");
                }

                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo($"Data Header: {nameof(messageType)}={((int)messageType).ToString()}");
            }

            if (NetworkManager.PendingClients.TryGetValue(clientId, out PendingClient client) && (client.ConnectionState == PendingClient.State.PendingApproval || client.ConnectionState == PendingClient.State.PendingConnection && messageType != MessageQueueContainer.MessageType.ConnectionRequest))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Message received from {nameof(clientId)}={clientId.ToString()} before it has been accepted");
                }

                return;
            }

            var messageQueueContainer = NetworkManager.MessageQueueContainer;
            messageQueueContainer.AddQueueItemToInboundFrame(messageType, receiveTime, clientId, (NetworkBuffer)stream);
        }

        public void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            NetworkManager.CustomMessagingManager.InvokeUnnamedMessage(clientId, stream);
        }

        public void HandleNamedMessage(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ulong hash = reader.ReadUInt64Packed();

            NetworkManager.CustomMessagingManager.InvokeNamedMessage(hash, clientId, stream);
        }

        public void HandleNetworkLog(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            var length = stream.Length;
            var logType = (NetworkLog.LogType)reader.ReadByte();
            var bytesReported = NetworkManager.LocalClientId == clientId
                ? 0
                : length;
            m_NetworkManager.NetworkMetrics.TrackServerLogReceived(clientId, (uint)logType, bytesReported);
            string message = reader.ReadStringPacked();

            switch (logType)
            {
                case NetworkLog.LogType.Info:
                    NetworkLog.LogInfoServerLocal(message, clientId);
                    break;
                case NetworkLog.LogType.Warning:
                    NetworkLog.LogWarningServerLocal(message, clientId);
                    break;
                case NetworkLog.LogType.Error:
                    NetworkLog.LogErrorServerLocal(message, clientId);
                    break;
            }
        }

        public void HandleSnapshot(ulong clientId, Stream messageStream)
        {
            m_NetworkManager.SnapshotSystem.ReadSnapshot(clientId, messageStream);
        }
    }
}
