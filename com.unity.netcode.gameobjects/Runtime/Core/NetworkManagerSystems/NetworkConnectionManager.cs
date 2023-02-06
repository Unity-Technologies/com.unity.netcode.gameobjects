using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools;
#endif

using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace Unity.Netcode
{
    public class NetworkConnectionManager
    {
        private ulong m_NextClientId = 1;
        internal NetworkManager NetworkManager;
        internal NetworkClient LocalClient;

        internal Dictionary<ulong, PendingClient> PendingClients = new Dictionary<ulong, PendingClient>();
        internal Dictionary<ulong, NetworkClient> ConnectedClients = new Dictionary<ulong, NetworkClient>();
        internal Dictionary<ulong, ulong> ClientIdToTransportIdMap = new Dictionary<ulong, ulong>();
        internal Dictionary<ulong, ulong> TransportIdToClientIdMap = new Dictionary<ulong, ulong>();

        internal List<NetworkClient> ConnectedClientsList = new List<NetworkClient>();
        internal List<ulong> ConnectedClientIds = new List<ulong>();


        internal ulong TransportIdToClientId(ulong transportId)
        {
            return transportId == GetServerTransporId() ? NetworkManager.ServerClientId : TransportIdToClientIdMap[transportId];
        }

        internal ulong ClientIdToTransportId(ulong clientId)
        {
            return clientId == NetworkManager.ServerClientId ? GetServerTransporId() : ClientIdToTransportIdMap[clientId];
        }

        /// <summary>
        /// Handles cleaning up the transport id/client id tables after
        /// receiving a disconnect event from transport
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong TransportIdCleanUp(ulong clientId, ulong transportId)
        {
            PendingClients.Remove(clientId);
            // This check is for clients that attempted to connect but failed.
            // When this happens, the client will not have an entry within the
            // m_TransportIdToClientIdMap or m_ClientIdToTransportIdMap lookup
            // tables so we exit early and just return 0 to be used for the
            // disconnect event.
            if (!LocalClient.IsServer && !TransportIdToClientIdMap.ContainsKey(clientId))
            {
                return 0;
            }

            clientId = TransportIdToClientId(clientId);

            TransportIdToClientIdMap.Remove(transportId);
            ClientIdToTransportIdMap.Remove(clientId);

            return clientId;
        }



        /// <summary>
        /// Gets the networkId of the server
        /// </summary>
        internal ulong ServerTransportId => GetServerTransporId();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetServerTransporId()
        {
            if(NetworkManager != null)
            {
                return NetworkManager.NetworkConfig.NetworkTransport?.ServerClientId ?? throw new NullReferenceException($"The transport in the active {nameof(NetworkConfig)} is null");
            }
            throw new Exception($"There is no {nameof(NetworkManager)} assigned to this instance!");
        }

        internal void ConnectedEvent(ulong transportClientId)
        {
            // Assumptions:
            // - When server receives a connection, it *must be* a client
            // - When client receives one, it *must be* the server
            // Client's can't connect to or talk to other clients.
            // Server is a sentinel so only one exists, if we are server, we can't be
            // connecting to it.
            var clientId = transportClientId;
            if (LocalClient.IsServer)
            {
                clientId = m_NextClientId++;
            }
            else
            {
                clientId = NetworkManager.ServerClientId;
            }
            ClientIdToTransportIdMap[clientId] = transportClientId;
            TransportIdToClientIdMap[transportClientId] = clientId;
            NetworkManager.MessagingSystem.ClientConnected(clientId);

            if (LocalClient.IsServer)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo("Client Connected");
                }

                PendingClients.Add(clientId, new PendingClient()
                {
                    ClientId = clientId,
                    ConnectionState = PendingClient.State.PendingConnection
                });

                NetworkManager.StartCoroutine(ApprovalTimeout(clientId));
            }
            else
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo("Connected");
                }

                SendConnectionRequest();
                NetworkManager.StartCoroutine(ApprovalTimeout(clientId));
            }

        }

        private void SendConnectionRequest()
        {
            var message = new ConnectionRequestMessage
            {
                // Since only a remote client will send a connection request,
                // we should always force the rebuilding of the NetworkConfig hash value
                ConfigHash = NetworkManager.NetworkConfig.GetConfig(false),
                ShouldSendConnectionData = NetworkManager.NetworkConfig.ConnectionApproval,
                ConnectionData = NetworkManager.NetworkConfig.ConnectionData
            };

            message.MessageVersions = new NativeArray<MessageVersionData>(NetworkManager.MessagingSystem.MessageHandlers.Length, Allocator.Temp);
            for (int index = 0; index < NetworkManager.MessagingSystem.MessageHandlers.Length; index++)
            {
                if (NetworkManager.MessagingSystem.MessageTypes[index] != null)
                {
                    var type = NetworkManager.MessagingSystem.MessageTypes[index];
                    message.MessageVersions[index] = new MessageVersionData
                    {
                        Hash = XXHash.Hash32(type.FullName),
                        Version = NetworkManager.MessagingSystem.GetLocalVersion(type)
                    };
                }
            }

            NetworkManager.SendMessage(ref message, NetworkDelivery.ReliableSequenced, NetworkManager.ServerClientId);
            message.MessageVersions.Dispose();
        }

        private IEnumerator ApprovalTimeout(ulong clientId)
        {
            var timeStarted = LocalClient.IsServer ? NetworkManager.LocalTime.TimeAsFloat : Time.realtimeSinceStartup;
            var timedOut = false;
            var connectionApproved = false;
            var connectionNotApproved = false;
            var timeoutMarker = timeStarted + NetworkManager.NetworkConfig.ClientConnectionBufferTimeout;

            while (NetworkManager.IsListening && !NetworkManager.ShutdownInProgress && !timedOut && !connectionApproved)
            {
                yield return null;
                // Check if we timed out
                timedOut = timeoutMarker < (LocalClient.IsServer ? NetworkManager.LocalTime.TimeAsFloat : Time.realtimeSinceStartup);

                if (LocalClient.IsServer)
                {
                    // When the client is no longer in the pending clients list and is in the connected clients list
                    // it has been approved
                    connectionApproved = !PendingClients.ContainsKey(clientId) && ConnectedClients.ContainsKey(clientId);

                    // For the server side, if the client is in neither list then it was declined or the client disconnected
                    connectionNotApproved = !PendingClients.ContainsKey(clientId) && !ConnectedClients.ContainsKey(clientId);
                }
                else
                {
                    connectionApproved = NetworkManager.IsApproved;
                }
            }

            // Exit coroutine if we are no longer listening or a shutdown is in progress (client or server)
            if (!NetworkManager.IsListening || NetworkManager.ShutdownInProgress)
            {
                yield break;
            }

            // If the client timed out or was not approved
            if (timedOut || connectionNotApproved)
            {
                // Timeout
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    if (timedOut)
                    {
                        if (LocalClient.IsServer)
                        {
                            // Log a warning that the transport detected a connection but then did not receive a follow up connection request message.
                            // (hacking or something happened to the server's network connection)
                            NetworkLog.LogWarning($"Server detected a transport connection from Client-{clientId}, but timed out waiting for the connection request message.");
                        }
                        else
                        {
                            // We only provide informational logging for the client side
                            NetworkLog.LogInfo("Timed out waiting for the server to approve the connection request.");
                        }
                    }
                    else if (connectionNotApproved)
                    {
                        NetworkLog.LogInfo($"Client-{clientId} was either denied approval or disconnected while being approved.");
                    }
                }

                if (LocalClient.IsServer)
                {
                    NetworkManager.DisconnectClient(clientId);
                }
                else
                {
                    NetworkManager.Shutdown(true);
                }
            }
        }

        /// <summary>
        /// Server Side: Handles the approval of a client
        /// </summary>
        /// <param name="ownerClientId">The Network Id of the client being approved</param>
        /// <param name="response">The response to allow the player in or not, with its parameters</param>
        internal void HandleConnectionApproval(ulong ownerClientId, NetworkManager.ConnectionApprovalResponse response)
        {
            if (response.Approved)
            {
                // Inform new client it got approved
                PendingClients.Remove(ownerClientId);

                var client = AddClient(ownerClientId);

                if (response.CreatePlayerObject)
                {
                    var playerPrefabHash = response.PlayerPrefabHash ?? NetworkManager.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;

                    // Generate a SceneObject for the player object to spawn
                    var sceneObject = new NetworkObject.SceneObject
                    {
                        OwnerClientId = ownerClientId,
                        IsPlayerObject = true,
                        IsSceneObject = false,
                        HasTransform = true,
                        Hash = playerPrefabHash,
                        TargetClientId = ownerClientId,
                        Transform = new NetworkObject.SceneObject.TransformData
                        {
                            Position = response.Position.GetValueOrDefault(),
                            Rotation = response.Rotation.GetValueOrDefault()
                        }
                    };

                    // Create the player NetworkObject locally
                    var networkObject = NetworkManager.SpawnManager.CreateLocalNetworkObject(sceneObject);

                    // Spawn the player NetworkObject locally
                    NetworkManager.SpawnManager.SpawnNetworkObjectLocally(
                        networkObject,
                        NetworkManager.SpawnManager.GetNetworkObjectId(),
                        sceneObject: false,
                        playerObject: true,
                        ownerClientId,
                        destroyWithScene: false);

                    client.AssignPlayerObject(ref networkObject);
                }

                // Server doesn't send itself the connection approved message
                if (ownerClientId != NetworkManager.ServerClientId)
                {
                    var message = new ConnectionApprovedMessage
                    {
                        OwnerClientId = ownerClientId,
                        NetworkTick = NetworkManager.LocalTime.Tick
                    };
                    if (!NetworkManager.NetworkConfig.EnableSceneManagement)
                    {
                        if (NetworkManager.SpawnManager.SpawnedObjectsList.Count != 0)
                        {
                            message.SpawnedObjectsList = NetworkManager.SpawnManager.SpawnedObjectsList;
                        }
                    }

                    message.MessageVersions = new NativeArray<MessageVersionData>(NetworkManager.MessagingSystem.MessageHandlers.Length, Allocator.Temp);
                    for (int index = 0; index < NetworkManager.MessagingSystem.MessageHandlers.Length; index++)
                    {
                        if (NetworkManager.MessagingSystem.MessageTypes[index] != null)
                        {
                            var type = NetworkManager.MessagingSystem.MessageTypes[index];
                            message.MessageVersions[index] = new MessageVersionData
                            {
                                Hash = XXHash.Hash32(type.FullName),
                                Version = NetworkManager.MessagingSystem.GetLocalVersion(type)
                            };
                        }
                    }

                    NetworkManager.SendMessage(ref message, NetworkDelivery.ReliableFragmentedSequenced, ownerClientId);
                    message.MessageVersions.Dispose();

                    // If scene management is enabled, then let NetworkSceneManager handle the initial scene and NetworkObject synchronization
                    if (!NetworkManager.NetworkConfig.EnableSceneManagement)
                    {
                        NetworkManager.InvokeOnClientConnectedCallback(ownerClientId);
                    }
                    else
                    {
                        NetworkManager.SceneManager.SynchronizeNetworkObjects(ownerClientId);
                    }
                }
                else // Server just adds itself as an observer to all spawned NetworkObjects
                {
                    LocalClient = client;
                    NetworkManager.SpawnManager.UpdateObservedNetworkObjects(ownerClientId);
                }

                if (!response.CreatePlayerObject || (response.PlayerPrefabHash == null && NetworkManager.NetworkConfig.PlayerPrefab == null))
                {
                    return;
                }

                // Separating this into a contained function call for potential further future separation of when this notification is sent.
                ApprovedPlayerSpawn(ownerClientId, response.PlayerPrefabHash ?? NetworkManager.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
            }
            else
            {
                if (!string.IsNullOrEmpty(response.Reason))
                {
                    var disconnectReason = new DisconnectReasonMessage();
                    disconnectReason.Reason = response.Reason;
                    NetworkManager.SendMessage(ref disconnectReason, NetworkDelivery.Reliable, ownerClientId);

                    NetworkManager.MessagingSystem.ProcessSendQueues();
                }

                PendingClients.Remove(ownerClientId);
                DisconnectRemoteClient(ownerClientId);
            }
        }

        /// <summary>
        /// Spawns the newly approved player
        /// </summary>
        /// <param name="clientId">new player client identifier</param>
        /// <param name="playerPrefabHash">the prefab GlobalObjectIdHash value for this player</param>
        internal void ApprovedPlayerSpawn(ulong clientId, uint playerPrefabHash)
        {
            foreach (var clientPair in ConnectedClients)
            {
                if (clientPair.Key == clientId ||
                    clientPair.Key == NetworkManager.ServerClientId || // Server already spawned it
                    ConnectedClients[clientId].PlayerObject == null ||
                    !ConnectedClients[clientId].PlayerObject.Observers.Contains(clientPair.Key))
                {
                    continue; //The new client.
                }

                var message = new CreateObjectMessage
                {
                    ObjectInfo = ConnectedClients[clientId].PlayerObject.GetMessageSceneObject(clientPair.Key)
                };
                message.ObjectInfo.Hash = playerPrefabHash;
                message.ObjectInfo.IsSceneObject = false;
                message.ObjectInfo.HasParent = false;
                message.ObjectInfo.IsPlayerObject = true;
                message.ObjectInfo.OwnerClientId = clientId;
                var size = NetworkManager.SendMessage(ref message, NetworkDelivery.ReliableFragmentedSequenced, clientPair.Key);
                NetworkManager.NetworkMetrics.TrackObjectSpawnSent(clientPair.Key, ConnectedClients[clientId].PlayerObject, size);
            }
        }


        internal NetworkClient AddClient(ulong clientId)
        {
            var networkClient = LocalClient;
            if (clientId != NetworkManager.ServerClientId)
            {
                networkClient = new NetworkClient(false, true, clientId);
            }
            ConnectedClients.Add(clientId, networkClient);
            ConnectedClientsList.Add(networkClient);
            ConnectedClientIds.Add(clientId);
            return networkClient;
        }

        internal void Shutdown()
        {
            if (LocalClient.IsServer)
            {
                // make sure all messages are flushed before transport disconnect clients
                if (NetworkManager.MessagingSystem != null)
                {
                    NetworkManager.MessagingSystem.ProcessSendQueues();
                }

                // Build a list of all client ids to be disconnected
                var disconnectedIds = new HashSet<ulong>();

                //Don't know if I have to disconnect the clients. I'm assuming the NetworkTransport does all the cleaning on shutdown. But this way the clients get a disconnect message from server (so long it does't get lost)
                var serverTransportId = NetworkManager.NetworkConfig.NetworkTransport.ServerClientId;
                foreach (KeyValuePair<ulong, NetworkClient> pair in ConnectedClients)
                {
                    if (!disconnectedIds.Contains(pair.Key))
                    {
                        disconnectedIds.Add(pair.Key);

                        if (pair.Key == serverTransportId)
                        {
                            continue;
                        }
                    }
                }

                foreach (KeyValuePair<ulong, PendingClient> pair in PendingClients)
                {
                    if (!disconnectedIds.Contains(pair.Key))
                    {
                        disconnectedIds.Add(pair.Key);
                        if (pair.Key == serverTransportId)
                        {
                            continue;
                        }
                    }
                }

                foreach (var clientId in disconnectedIds)
                {
                    DisconnectRemoteClient(clientId);
                }

            }
            else if (NetworkManager != null && NetworkManager.IsListening)
            {
                // Client only, send disconnect to server
                NetworkManager.NetworkConfig.NetworkTransport.DisconnectLocalClient();
            }
        }

        internal void OnClientDisconnectFromServer(ulong clientId)
        {
            if (!LocalClient.IsServer)
            {
                throw new Exception("[OnClientDisconnectFromServer] Was invoked by non-server instance!");
            }

            // If we are shutting down and this is the server or host disconnecting, then ignore
            // clean up as everything that needs to be destroyed will be during shutdown.
            if (NetworkManager.ShutdownInProgress && clientId == NetworkManager.ServerClientId)
            {
                return;
            }
            if (ConnectedClients.TryGetValue(clientId, out NetworkClient networkClient))
            {
                var playerObject = networkClient.PlayerObject;
                if (playerObject != null)
                {
                    if (!playerObject.DontDestroyWithOwner)
                    {
                        if (NetworkManager.PrefabHandler.ContainsHandler(ConnectedClients[clientId].PlayerObject.GlobalObjectIdHash))
                        {
                            NetworkManager.PrefabHandler.HandleNetworkPrefabDestroy(ConnectedClients[clientId].PlayerObject);
                        }
                        else
                        {
                            // Call despawn to assure NetworkBehaviour.OnNetworkDespawn is invoked
                            // on the server-side (when the client side disconnected).
                            // This prevents the issue (when just destroying the GameObject) where
                            // any NetworkBehaviour component(s) destroyed before the NetworkObject
                            // would not have OnNetworkDespawn invoked.
                            NetworkManager.SpawnManager.DespawnObject(playerObject, true);
                        }
                    }
                    else
                    {
                        playerObject.RemoveOwnership();
                    }
                }

                // Get the NetworkObjects owned by the disconnected client
                var clientOwnedObjects = NetworkManager.SpawnManager.GetClientOwnedObjects(clientId);
                if (clientOwnedObjects == null)
                {
                    // This could happen if a client is never assigned a player object and is disconnected
                    // Only log this in verbose/developer mode
                    if (NetworkManager.LogLevel == LogLevel.Developer)
                    {
                        NetworkLog.LogWarning($"ClientID {clientId} disconnected with (0) zero owned objects!  Was a player prefab not assigned?");
                    }
                }
                else
                {
                    // Handle changing ownership and prefab handlers
                    // TODO-2023: Look into whether in-scene placed NetworkObjects could be destroyed if ownership changes to a client
                    for (int i = clientOwnedObjects.Count - 1; i >= 0; i--)
                    {
                        var ownedObject = clientOwnedObjects[i];
                        if (ownedObject != null)
                        {
                            if (!ownedObject.DontDestroyWithOwner)
                            {
                                if (NetworkManager.PrefabHandler.ContainsHandler(clientOwnedObjects[i].GlobalObjectIdHash))
                                {
                                    NetworkManager.PrefabHandler.HandleNetworkPrefabDestroy(clientOwnedObjects[i]);
                                }
                                else
                                {
                                    Object.Destroy(ownedObject.gameObject);
                                }
                            }
                            else
                            {
                                ownedObject.RemoveOwnership();
                            }
                        }
                    }
                }

                // TODO: Could(should?) be replaced with more memory per client, by storing the visibility
                foreach (var sobj in NetworkManager.SpawnManager.SpawnedObjectsList)
                {
                    sobj.Observers.Remove(clientId);
                }

                if (ConnectedClients.ContainsKey(clientId))
                {
                    ConnectedClientsList.Remove(ConnectedClients[clientId]);
                    ConnectedClients.Remove(clientId);
                }
                ConnectedClientIds.Remove(clientId);

            }
            if (ClientIdToTransportIdMap.ContainsKey(clientId))
            {
                var transportId = ClientIdToTransportId(clientId);

                NetworkManager.NetworkConfig.NetworkTransport.DisconnectRemoteClient(transportId);
            }
            NetworkManager.MessagingSystem.ClientDisconnected(clientId);
            PendingClients.Remove(clientId);
        }


        internal void DisconnectRemoteClient(ulong clientId)
        {
            NetworkManager.MessagingSystem.ProcessSendQueues();
            OnClientDisconnectFromServer(clientId);
        }

        internal void ClearClients()
        {
            PendingClients.Clear();
            ConnectedClients.Clear();
            ConnectedClientsList.Clear();
            ConnectedClientIds.Clear();
            ClientIdToTransportIdMap.Clear();
            TransportIdToClientIdMap.Clear();
        }

        public NetworkConnectionManager()
        {
            LocalClient = new NetworkClient();
        }
    }
}
