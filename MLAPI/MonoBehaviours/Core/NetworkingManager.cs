using MLAPI.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Security.Cryptography;
using MLAPI.NetworkingManagerComponents.Cryptography;
using MLAPI.NetworkingManagerComponents.Core;
using UnityEngine.SceneManagement;
using MLAPI.NetworkingManagerComponents.Binary;

namespace MLAPI.MonoBehaviours.Core
{
    /// <summary>
    /// The main component of the library
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkingManager", -100)]
    public class NetworkingManager : MonoBehaviour
    {
        /// <summary>
        /// A syncronized time, represents the time in seconds since the server application started. Is replicated across all clients
        /// </summary>
        public float NetworkTime
        {
            get
            {
                return networkTime;
            }
        }
        internal float networkTime;
        /// <summary>
        /// Gets or sets if the NetworkingManager should be marked as DontDestroyOnLoad
        /// </summary>
        public bool DontDestroy = true;
        /// <summary>
        /// Gets or sets if the application should be set to run in background
        /// </summary>
        public bool RunInBackground = true;
        /// <summary>
        /// The singleton instance of the NetworkingManager
        /// </summary>
        public static NetworkingManager singleton
        {
            get
            {
                return _singleton;
            }
        }
        private static NetworkingManager _singleton;
        /// <summary>
        /// The clientId the server calls the local client by, only valid for clients
        /// </summary>
        public uint MyClientId
        {
            get
            {
                return myClientId;
            }
        }
        internal uint myClientId;
        internal Dictionary<uint, NetworkedClient> connectedClients;
        /// <summary>
        /// Gets a dictionary of connected clients
        /// </summary>
        public Dictionary<uint, NetworkedClient> ConnectedClients
        {
            get
            {
                return connectedClients;
            }
        }
        internal HashSet<uint> pendingClients;
        internal bool _isServer;
        internal bool _isClient;
        /// <summary>
        /// Gets if we are running as host
        /// </summary>
        public bool isHost
        {
            get
            {
                return isServer && isClient;
            }
        }
        /// <summary>
        /// Gets wheter or not a client is running
        /// </summary>
        public bool isClient
        {
            get
            {
                return _isClient;
            }
        }

        /// <summary>
        /// Gets wheter or not a server is running
        /// </summary>
        public bool isServer
        {
            get
            {
                return _isServer;
            }
        }

        private bool isListening;
        private byte[] messageBuffer;
        internal int serverConnectionId;
        internal int serverHostId;
        /// <summary>
        /// Gets if we are connected as a client
        /// </summary>
        public bool IsClientConnected
        {
            get
            {
                return _isClientConnected;
            }
        }
        internal bool _isClientConnected;
        /// <summary>
        /// The callback to invoke once a client connects
        /// </summary>
        public Action<uint> OnClientConnectedCallback = null;
        /// <summary>
        /// The callback to invoke when a client disconnects
        /// </summary>
        public Action<uint> OnClientDisconnectCallback = null;
        /// <summary>
        /// The callback to invoke once the server is ready
        /// </summary>
        public Action OnServerStarted = null;
        /// <summary>
        /// The callback to invoke during connection approval
        /// </summary>
        public Action<byte[], uint, Action<uint, bool, Vector3, Quaternion>> ConnectionApprovalCallback = null;
        /// <summary>
        /// The current NetworkingConfiguration
        /// </summary>
        public NetworkConfig NetworkConfig;

        internal EllipticDiffieHellman clientDiffieHellman;
        internal Dictionary<uint, byte[]> diffieHellmanPublicKeys;
        internal byte[] clientAesKey;

        /// <summary>
        /// An inspector bool that acts as a Trigger for regenerating RSA keys. Should not be used outside Unity editor.
        /// </summary>
        public bool RegenerateRSAKeys = false;

        private void OnValidate()
        {
            if (NetworkConfig == null)
                return; //May occur when the component is added

            if(NetworkConfig.EnableSceneSwitching && !NetworkConfig.RegisteredScenes.Contains(SceneManager.GetActiveScene().name))
            {
                Debug.LogWarning("MLAPI: The active scene is not registered as a networked scene. The MLAPI has added it");
                NetworkConfig.RegisteredScenes.Add(SceneManager.GetActiveScene().name);
            }

            if(!NetworkConfig.EnableSceneSwitching && NetworkConfig.HandleObjectSpawning)
            {
                Debug.LogWarning("MLAPI: Please be aware that Scene objects are NOT supported if SceneManagement is turned on, even if HandleObjectSpawning is turned on");
            }

            if(NetworkConfig.HandleObjectSpawning)
            {
                for (int i = 0; i < NetworkConfig.NetworkedPrefabs.Count; i++)
                {
                    if (string.IsNullOrEmpty(NetworkConfig.NetworkedPrefabs[i].name))
                    {
                        Debug.LogWarning("MLAPI: The NetworkedPrefab " + NetworkConfig.NetworkedPrefabs[i].prefab.name + " does not have a NetworkedPrefabName.");
                    }
                }
                int playerPrefabCount = NetworkConfig.NetworkedPrefabs.Count(x => x.playerPrefab == true);
                if (playerPrefabCount == 0)
                {
                    Debug.LogWarning("MLAPI: There is no NetworkedPrefab marked as a PlayerPrefab");
                }
                else if (playerPrefabCount > 1)
                {
                    Debug.LogWarning("MLAPI: Only one networked prefab can be marked as a player prefab");
                }
                else
                    NetworkConfig.PlayerPrefabName =  NetworkConfig.NetworkedPrefabs.Find(x => x.playerPrefab == true).name;

            }

            if (!NetworkConfig.EnableEncryption)
            {
                RegenerateRSAKeys = false;
            }
            else
            {
                if(RegenerateRSAKeys)
                {
                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                    {
                        rsa.PersistKeyInCsp = false;
                        NetworkConfig.RSAPrivateKey = rsa.ToXmlString(true);
                        NetworkConfig.RSAPublicKey = rsa.ToXmlString(false);
                    }
                    RegenerateRSAKeys = false;
                }
            }
        }

        private ConnectionConfig Init(bool server)
        {
            networkTime = 0f;
            lastSendTickTime = 0f;
            lastEventTickTime = 0f;
            lastReceiveTickTime = 0f;
            eventOvershootCounter = 0f;
            pendingClients = new HashSet<uint>();
            connectedClients = new Dictionary<uint, NetworkedClient>();
            messageBuffer = new byte[NetworkConfig.MessageBufferSize];
            diffieHellmanPublicKeys = new Dictionary<uint, byte[]>();
            Data.Cache.messageAttributeHashes = new Dictionary<string, ulong>();
            Data.Cache.messageAttributeNames = new Dictionary<ulong, string>();
            MessageManager.channels = new Dictionary<string, int>();
            MessageManager.messageTypes = new Dictionary<string, ushort>();
            MessageManager.messageCallbacks = new Dictionary<ushort, Dictionary<int, Action<uint, byte[]>>>();
            MessageManager.messageHandlerCounter = new Dictionary<ushort, int>();
            MessageManager.releasedMessageHandlerCounters = new Dictionary<ushort, Stack<int>>();
            MessageManager.reverseChannels = new Dictionary<int, string>();
            MessageManager.reverseMessageTypes = new Dictionary<ushort, string>();
            SpawnManager.spawnedObjects = new Dictionary<uint, NetworkedObject>();
            SpawnManager.releasedNetworkObjectIds = new Stack<uint>();
            NetworkPoolManager.Pools = new Dictionary<ushort, Data.NetworkPool>();
            NetworkPoolManager.PoolNamesToIndexes = new Dictionary<string, ushort>();
            NetworkSceneManager.registeredSceneNames = new HashSet<string>();
            NetworkSceneManager.sceneIndexToString = new Dictionary<uint, string>();
            NetworkSceneManager.sceneNameToIndex = new Dictionary<string, uint>();
            InternalMessageHandler.FinalMessageBuffer = new byte[NetworkConfig.MessageBufferSize];

            if(NetworkConfig.HandleObjectSpawning)
            {
                NetworkConfig.NetworkPrefabIds = new Dictionary<string, int>();
                NetworkConfig.NetworkPrefabNames = new Dictionary<int, string>();
                NetworkConfig.NetworkedPrefabs.OrderBy(x => x.name);
                HashSet<string> networkedPrefabName = new HashSet<string>();
                for (int i = 0; i < NetworkConfig.NetworkedPrefabs.Count; i++)
                {
                    if (networkedPrefabName.Contains(NetworkConfig.NetworkedPrefabs[i].name))
                    {
                        Debug.LogWarning("MLAPI: Duplicate NetworkedPrefabName " + NetworkConfig.NetworkedPrefabs[i].name);
                        continue;
                    }
                    NetworkConfig.NetworkPrefabIds.Add(NetworkConfig.NetworkedPrefabs[i].name, i);
                    NetworkConfig.NetworkPrefabNames.Add(i, NetworkConfig.NetworkedPrefabs[i].name);
                    networkedPrefabName.Add(NetworkConfig.NetworkedPrefabs[i].name);
                }
                if (NetworkConfig.EnableSceneSwitching)
                {
                    SpawnManager.MarkSceneObjects();
                    if (NetworkConfig.HandleObjectSpawning)
                    {
                        if (server)
                        {
                            bool isServerState = _isServer;
                            _isServer = true;
                            NetworkedObject[] networkedObjects = FindObjectsOfType<NetworkedObject>();
                            for (int i = 0; i < networkedObjects.Length; i++)
                            {
                                if (networkedObjects[i].sceneObject == null || networkedObjects[i].sceneObject == true)
                                    networkedObjects[i].Spawn();
                            }
                            _isServer = isServerState;
                        }
                    }
                }
            }

            NetworkTransport.Init();
            ConnectionConfig cConfig = new ConnectionConfig()
            {
                SendDelay = 0
            };

            //MLAPI channels and messageTypes
            List<Channel> internalChannels = new List<Channel>
            {
                new Channel()
                {
                    Name = "MLAPI_INTERNAL",
                    Type = QosType.ReliableFragmentedSequenced
                },
                new Channel()
                {
                    Name = "MLAPI_POSITION_UPDATE",
                    Type = QosType.StateUpdate
                },
                new Channel()
                {
                    Name = "MLAPI_ANIMATION_UPDATE",
                    Type = QosType.ReliableSequenced
                },
                new Channel()
                {
                    Name = "MLAPI_NAV_AGENT_STATE",
                    Type = QosType.ReliableSequenced
                },
                new Channel()
                {
                    Name = "MLAPI_NAV_AGENT_CORRECTION",
                    Type = QosType.StateUpdate
                },
                new Channel()
                {
                    Name = "MLAPI_TIME_SYNC",
                    Type = QosType.Unreliable
                }
            };

            if (NetworkConfig.EnableEncryption)
            {
                HashSet<string> addedEncryptedChannels = new HashSet<string>();
                for (int i = 0; i < NetworkConfig.Channels.Count; i++)
                {
                    if (addedEncryptedChannels.Contains(NetworkConfig.Channels[i].Name))
                    {
                        Debug.LogWarning("MLAPI: Duplicate encrypted channel name " + NetworkConfig.Channels[i].Name);
                        continue;
                    }
                    if (NetworkConfig.Channels[i].Encrypted)
                    {
                        NetworkConfig.EncryptedChannels.Add(NetworkConfig.Channels[i].Name);
                        NetworkConfig.EncryptedChannelsHashSet.Add(NetworkConfig.Channels[i].Name);
                    }
                    addedEncryptedChannels.Add(NetworkConfig.Channels[i].Name);
                }
            }

            if (NetworkConfig.AllowPassthroughMessages)
            {
                HashSet<string> addedPassthroughMessages = new HashSet<string>();
                for (int i = 0; i < NetworkConfig.MessageTypes.Count; i++)
                {
                    if (addedPassthroughMessages.Contains(NetworkConfig.MessageTypes[i].Name))
                    {
                        Debug.LogWarning("MLAPI: Duplicate passthrough message type " + NetworkConfig.MessageTypes[i].Name);
                        continue;
                    }
                    if (NetworkConfig.MessageTypes[i].Passthrough)
                    {
                        NetworkConfig.PassthroughMessageHashSet.Add(MessageManager.messageTypes[NetworkConfig.MessageTypes[i].Name]);
                        addedPassthroughMessages.Add(NetworkConfig.MessageTypes[i].Name);
                    }
                }
            }

            HashSet<string> channelNames = new HashSet<string>();
            //Register internal channels
            for (int i = 0; i < internalChannels.Count; i++)
            {
                if (channelNames.Contains(internalChannels[i].Name))
                {
                    Debug.LogWarning("MLAPI: Duplicate channel name: " + NetworkConfig.Channels[i].Name);
                    continue;
                }
                int channelId = cConfig.AddChannel(internalChannels[i].Type);
                MessageManager.channels.Add(internalChannels[i].Name, channelId);
                channelNames.Add(internalChannels[i].Name);
                MessageManager.reverseChannels.Add(channelId, internalChannels[i].Name);
            }

            MessageManager.messageTypes.Add("MLAPI_CONNECTION_REQUEST", 0);
            MessageManager.messageTypes.Add("MLAPI_CONNECTION_APPROVED", 1);
            MessageManager.messageTypes.Add("MLAPI_ADD_OBJECT", 2);
            MessageManager.messageTypes.Add("MLAPI_CLIENT_DISCONNECT", 3);
            MessageManager.messageTypes.Add("MLAPI_DESTROY_OBJECT", 4);
            MessageManager.messageTypes.Add("MLAPI_SWITCH_SCENE", 5);
            MessageManager.messageTypes.Add("MLAPI_SPAWN_POOL_OBJECT", 6);
            MessageManager.messageTypes.Add("MLAPI_DESTROY_POOL_OBJECT", 7);
            MessageManager.messageTypes.Add("MLAPI_CHANGE_OWNER", 8);
            MessageManager.messageTypes.Add("MLAPI_SYNC_VAR_UPDATE", 9);
            MessageManager.messageTypes.Add("MLAPI_ADD_OBJECTS", 10);
            MessageManager.messageTypes.Add("MLAPI_TIME_SYNC", 11);
            MessageManager.messageTypes.Add("MLAPI_COMMAND", 12);
            MessageManager.messageTypes.Add("MLAPI_RPC", 13);
            MessageManager.messageTypes.Add("MLAPI_TARGET", 14);

            List<MessageType> messageTypes = new List<MessageType>(NetworkConfig.MessageTypes)
            {
                new MessageType()
                {
                    Name = "MLAPI_OnRecieveTransformFromClient",
                    Passthrough = false
                },
                new MessageType()
                {
                    Name = "MLAPI_OnRecieveTransformFromServer",
                    Passthrough = false
                },
                new MessageType()
                {
                    Name = "MLAPI_HandleAnimationMessage",
                    Passthrough = false
                },
                new MessageType()
                {
                    Name = "MLAPI_HandleAnimationParameterMessage",
                    Passthrough = false
                },
                new MessageType()
                {
                    Name = "MLAPI_HandleAnimationTriggerMessage",
                    Passthrough = false
                },
                new MessageType()
                {
                    Name = "MLAPI_OnNavMeshStateUpdate",
                    Passthrough = false
                },
                new MessageType()
                {
                    Name = "MLAPI_OnNavMeshCorrectionUpdate",
                    Passthrough = false
                }
            };

            if (NetworkConfig.EnableSceneSwitching)
            {
                for (int i = 0; i < NetworkConfig.RegisteredScenes.Count; i++)
                {
                    NetworkSceneManager.registeredSceneNames.Add(NetworkConfig.RegisteredScenes[i]);
                    NetworkSceneManager.sceneIndexToString.Add((uint)i, NetworkConfig.RegisteredScenes[i]);
                    NetworkSceneManager.sceneNameToIndex.Add(NetworkConfig.RegisteredScenes[i], (uint)i);
                }

                NetworkSceneManager.SetCurrentSceneIndex();
            }

            //Register user channels
            for (int i = 0; i < NetworkConfig.Channels.Count; i++)
            {
                if(channelNames.Contains(NetworkConfig.Channels[i].Name))
                {
                    Debug.LogWarning("MLAPI: Duplicate channel name: " + NetworkConfig.Channels[i].Name);
                    continue;
                }
                int channelId = cConfig.AddChannel(NetworkConfig.Channels[i].Type);
                MessageManager.channels.Add(NetworkConfig.Channels[i].Name, channelId);
                channelNames.Add(NetworkConfig.Channels[i].Name);
                MessageManager.reverseChannels.Add(channelId, NetworkConfig.Channels[i].Name);
            }

            //0-32 are reserved for MLAPI messages
            ushort messageId = 32;
            for (ushort i = 0; i < messageTypes.Count; i++)
            {
                MessageManager.messageTypes.Add(messageTypes[i].Name, messageId);
                MessageManager.reverseMessageTypes.Add(messageId, messageTypes[i].Name);
                messageId++;
            }
            return cConfig;
        }

        /// <summary>
        /// Starts a server
        /// </summary>
        public void StartServer()
        {
            if (isServer || isClient)
            {
                Debug.LogWarning("MLAPI: Cannot start server while an instance is already running");
                return;
            }

            ConnectionConfig cConfig = Init(true);
            if (NetworkConfig.ConnectionApproval)
            {
                if (ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            for (int i = 0; i < NetworkConfig.ServerTransports.Count; i++)
            {
                if (NetworkConfig.ServerTransports[i].Websockets)
                    NetworkTransport.AddWebsocketHost(hostTopology, NetworkConfig.ServerTransports[i].Port);
                else
                    NetworkTransport.AddHost(hostTopology, NetworkConfig.ServerTransports[i].Port);
            } 
            
            _isServer = true;
            _isClient = false;
            isListening = true;

            if (OnServerStarted != null)
                OnServerStarted.Invoke();
        }

        /// <summary>
        /// Starts a client
        /// </summary>
        public void StartClient()
        {
            if (isServer || isClient)
            {
                Debug.LogWarning("MLAPI: Cannot start client while an instance is already running");
                return;
            }

            ConnectionConfig cConfig = Init(false);
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            serverHostId = NetworkTransport.AddHost(hostTopology, 0, null);

            _isServer = false;
            _isClient = true;
            isListening = true;
            byte error;
            serverConnectionId = NetworkTransport.Connect(serverHostId, NetworkConfig.ConnectAddress, NetworkConfig.ConnectPort, 0, out error);
        }

        /// <summary>
        /// Starts a client using Websockets
        /// </summary>
        public void StartClientWebsocket()
        {
            if (isServer || isClient)
            {
                Debug.LogWarning("MLAPI: Cannot start client while an instance is already running");
                return;
            }

            ConnectionConfig cConfig = Init(false);
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            serverHostId = NetworkTransport.AddWebsocketHost(hostTopology, 0, null);

            _isServer = false;
            _isClient = true;
            isListening = true;
            byte error;
            serverConnectionId = NetworkTransport.Connect(serverHostId, NetworkConfig.ConnectAddress, NetworkConfig.ConnectPort, 0, out error);
        }

        /// <summary>
        /// Stops the running server
        /// </summary>
        public void StopServer()
        {
            HashSet<uint> disconnectedIds = new HashSet<uint>();
            //Don't know if I have to disconnect the clients. I'm assuming the NetworkTransport does all the cleaning on shtudown. But this way the clients get a disconnect message from server (so long it does't get lost)
            foreach (KeyValuePair<uint, NetworkedClient> pair in connectedClients)
            {
                if(!disconnectedIds.Contains(pair.Key))
                {
                    disconnectedIds.Add(pair.Key);
                    NetId netId = new NetId(pair.Key);
                    if (netId.IsHost())
                        continue;

                    byte error;
                    NetworkTransport.Disconnect(netId.HostId, netId.ConnectionId, out error);
                }
            }
            foreach (uint clientId in pendingClients)
            {
                if (!disconnectedIds.Contains(clientId))
                {
                    disconnectedIds.Add(clientId);
                    NetId netId = new NetId(clientId);
                    if (netId.IsHost())
                        continue;

                    byte error;
                    NetworkTransport.Disconnect(netId.HostId, netId.ConnectionId, out error);
                }
            }
            _isServer = false;
            Shutdown();
        }

        /// <summary>
        /// Stops the running host
        /// </summary>
        public void StopHost()
        {
            _isClient = false;
            _isServer = false;
            StopServer();
            //We don't stop client since we dont actually have a transport connection to our own host. We just handle host messages directly in the MLAPI
        }

        /// <summary>
        /// Stops the running client
        /// </summary>
        public void StopClient()
        {
            _isClient = false;
            byte error;
            NetworkTransport.Disconnect(serverHostId, serverConnectionId, out error);
            Shutdown();
        }

        /// <summary>
        /// Starts a Host
        /// </summary>
        public void StartHost(Vector3? pos = null, Quaternion? rot = null)
        {
            if (isServer || isClient)
            {
                Debug.LogWarning("MLAPI: Cannot start host while an instance is already running");
                return;
            }

            ConnectionConfig cConfig = Init(true);
            if (NetworkConfig.ConnectionApproval)
            {
                if (ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            for (int i = 0; i < NetworkConfig.ServerTransports.Count; i++)
            {
                if (NetworkConfig.ServerTransports[i].Websockets)
                    NetworkTransport.AddWebsocketHost(hostTopology, NetworkConfig.ServerTransports[i].Port);
                else
                    NetworkTransport.AddHost(hostTopology, NetworkConfig.ServerTransports[i].Port);
            }

            _isServer = true;
            _isClient = true;
            isListening = true;

            NetId netId = new NetId(0, 0, true, false);
            connectedClients.Add(netId.GetClientId(), new NetworkedClient()
            {
                ClientId = netId.GetClientId()
            });

            if(NetworkConfig.HandleObjectSpawning)
            {
                SpawnManager.SpawnPlayerObject(netId.GetClientId(), 0, pos.GetValueOrDefault(), rot.GetValueOrDefault());
            }

            if (OnServerStarted != null)
                OnServerStarted.Invoke();
        }

        private void OnEnable()
        {
            if (singleton != null)
            {
                Debug.LogWarning("MLAPI: Multiple NetworkingManagers");
                Destroy(this);
                return;
            }
            _singleton = this;
            if (DontDestroy)
                DontDestroyOnLoad(gameObject);
            if (RunInBackground)
                Application.runInBackground = true;
        }
        
        private void OnDestroy()
        {
            _singleton = null;
            Shutdown();
        }

        private void Shutdown()
        {
            isListening = false;
            _isClient = false;
            _isServer = false;
            SpawnManager.DestroyNonSceneObjects();
            NetworkTransport.Shutdown();
        }

        private float lastReceiveTickTime;
        private float lastSendTickTime;
        private float lastEventTickTime;
        private float eventOvershootCounter;
        private float lastTimeSyncTime;
        private void Update()
        {
            if(isListening)
            {
                if((NetworkTime - lastSendTickTime >= (1f / NetworkConfig.SendTickrate)) || NetworkConfig.SendTickrate <= 0)
                {
                    foreach (KeyValuePair<uint, NetworkedClient> pair in connectedClients)
                    {
                        NetId netId = new NetId(pair.Key);
                        if (netId.IsHost() || netId.IsInvalid())                        
                            continue;   
                        
                        byte error;
                        NetworkTransport.SendQueuedMessages(netId.HostId, netId.ConnectionId, out error);
                    }
                    lastSendTickTime = NetworkTime;
                }
                if((NetworkTime - lastReceiveTickTime >= (1f / NetworkConfig.ReceiveTickrate)) || NetworkConfig.ReceiveTickrate <= 0)
                {
                    NetworkEventType eventType;
                    int processedEvents = 0;
                    do
                    {
                        processedEvents++;
                        int hostId;
                        int connectionId;
                        int channelId;
                        int receivedSize;
                        byte error;
                        eventType = NetworkTransport.Receive(out hostId, out connectionId, out channelId, messageBuffer, messageBuffer.Length, out receivedSize, out error);
                        NetId netId = new NetId((byte)hostId, (ushort)connectionId, false, false);
                        NetworkError networkError = (NetworkError)error;
                        if (networkError == NetworkError.Timeout)
                        {
                            //Client timed out. 
                            if (isServer)
                            {
                                OnClientDisconnect(netId.GetClientId());
                                return;
                            }
                            else
                                _isClientConnected = false;

                            if (OnClientDisconnectCallback != null)
                                OnClientDisconnectCallback.Invoke(netId.GetClientId());
                        }
                        else if (networkError != NetworkError.Ok)
                        {
                            Debug.LogWarning("MLAPI: NetworkTransport receive error: " + networkError.ToString());
                            return;
                        }

                        switch (eventType)
                        {
                            case NetworkEventType.ConnectEvent:
                                if (isServer)
                                {
                                    pendingClients.Add(netId.GetClientId());
                                    StartCoroutine(ApprovalTimeout(netId.GetClientId()));
                                }
                                else
                                {
                                    byte[] diffiePublic = new byte[0];
                                    if(NetworkConfig.EnableEncryption)
                                    {
                                        clientDiffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                                        diffiePublic = clientDiffieHellman.GetPublicKey();
                                    }

                                    using (BitWriter writer = new BitWriter())
                                    {
                                        writer.WriteByteArray(NetworkConfig.GetConfig(), true);

                                        if (NetworkConfig.EnableEncryption)      
                                            writer.WriteByteArray(diffiePublic);

                                        if (NetworkConfig.ConnectionApproval)
                                            writer.WriteByteArray(NetworkConfig.ConnectionData);

                                        InternalMessageHandler.Send(netId.GetClientId(), "MLAPI_CONNECTION_REQUEST", "MLAPI_INTERNAL", writer.Finalize(), null, null, true);
                                    }
                                }
                                break;
                            case NetworkEventType.DataEvent:
                                HandleIncomingData(netId.GetClientId(), messageBuffer, channelId);
                                break;
                            case NetworkEventType.DisconnectEvent:
                                if (isServer)
                                    OnClientDisconnect(netId.GetClientId());
                                else
                                    _isClientConnected = false;

                                if (OnClientDisconnectCallback != null)
                                    OnClientDisconnectCallback.Invoke(netId.GetClientId());
                                break;
                        }
                        // Only do another iteration if: there are no more messages AND (there is no limit to max events or we have processed less than the maximum)
                    } while (eventType != NetworkEventType.Nothing && (NetworkConfig.MaxReceiveEventsPerTickRate <= 0 || processedEvents < NetworkConfig.MaxReceiveEventsPerTickRate));
                    lastReceiveTickTime = NetworkTime;
                }

                if (isServer && ((NetworkTime - lastEventTickTime >= (1f / NetworkConfig.EventTickrate))))
                {
                    eventOvershootCounter += ((NetworkTime - lastEventTickTime) - (1f / NetworkConfig.EventTickrate));
                    LagCompensationManager.AddFrames();
                    NetworkedObject.InvokeSyncvarUpdate();
                    lastEventTickTime = NetworkTime;
                }
                else if (isServer && eventOvershootCounter >= ((1f / NetworkConfig.EventTickrate)))
                {
                    //We run this one to compensate for previous update overshoots.
                    eventOvershootCounter -= (1f / NetworkConfig.EventTickrate);
                    LagCompensationManager.AddFrames();
                }

                if (NetworkConfig.EnableTimeResync && NetworkTime - lastTimeSyncTime >= 30)
                {
                    SyncTime();
                    lastTimeSyncTime = NetworkTime;
                }

                networkTime += Time.unscaledDeltaTime;
            }
        }

        private IEnumerator ApprovalTimeout(uint clientId)
        {
            float timeStarted = NetworkTime;
            //We yield every frame incase a pending client disconnects and someone else gets its connection id
            while (NetworkTime - timeStarted < NetworkConfig.ClientConnectionBufferTimeout && pendingClients.Contains(clientId))
            {
                yield return null;
            }
            if(pendingClients.Contains(clientId) && !connectedClients.ContainsKey(clientId))
            {
                //Timeout
                DisconnectClient(clientId);
            }
        }

        private void HandleIncomingData(uint clientId, byte[] data, int channelId)
        {
            BitReader reader = new BitReader(data);

            ushort messageType = reader.ReadUShort();
            bool targeted = reader.ReadBool();
            uint targetNetworkId = 0;
            ushort networkOrderId = 0;
            if (targeted)
            {
                targetNetworkId = reader.ReadUInt();
                networkOrderId = reader.ReadUShort();
            }
            bool isPassthrough = reader.ReadBool();

            uint passthroughOrigin = 0;
            uint passthroughTarget = 0;

            if (isPassthrough && isServer)
                passthroughTarget = reader.ReadUInt();
            else if (isPassthrough && !isServer)
                passthroughOrigin = reader.ReadUInt();


            //Client tried to send a network message that was not the connection request before he was accepted.
            if (isServer && pendingClients.Contains(clientId) && messageType != 0)
            {
                Debug.LogWarning("MLAPI: Message recieved from clientId " + clientId + " before it has been accepted");
                return;
            }


            //ushort bytesToRead = reader.ReadUShort();
            reader.SkipPadded();

            byte[] incommingData = reader.ReadByteArray();
            if (NetworkConfig.EncryptedChannelsHashSet.Contains(MessageManager.reverseChannels[channelId]))
            {
                //Encrypted message
                if (isServer)
                    incommingData = CryptographyHelper.Decrypt(incommingData, connectedClients[clientId].AesKey);
                else
                    incommingData = CryptographyHelper.Decrypt(incommingData, clientAesKey);
            }

            if (isServer && isPassthrough && !NetworkConfig.PassthroughMessageHashSet.Contains(messageType))
            {
                Debug.LogWarning("MLAPI: Client " + clientId + " tried to send a passthrough message for a messageType not registered as passthrough");
                return;
            }
            else if (isClient && isPassthrough && !NetworkConfig.PassthroughMessageHashSet.Contains(messageType))
            {
                Debug.LogWarning("MLAPI: Server tried to send a passthrough message for a messageType not registered as passthrough");
                return;
            }
            else if (isServer && isPassthrough)
            {
                if (!connectedClients.ContainsKey(passthroughTarget))
                {
                    Debug.LogWarning("MLAPI: Passthrough message was sent with invalid target: " + passthroughTarget + " from client " + clientId);
                    return;
                }
                uint? netIdTarget = null;
                ushort? netOrderId = null;
                if (targeted)
                {
                    netIdTarget = targetNetworkId;
                    netOrderId = networkOrderId;
                }
                InternalMessageHandler.PassthroughSend(passthroughTarget, clientId, messageType, channelId, incommingData, netIdTarget, netOrderId);
                return;
            }

            if (messageType >= 32)
            {
                #region CUSTOM MESSAGE
                //Custom message, invoke all message handlers
                if (targeted)
                {
                    if (!SpawnManager.spawnedObjects.ContainsKey(targetNetworkId))
                    {
                        Debug.LogWarning("MLAPI: No target for message found");
                        return;
                    }
                    else if (!SpawnManager.spawnedObjects[targetNetworkId].targetMessageActions.ContainsKey(networkOrderId))
                    {
                        Debug.LogWarning("MLAPI: No target messageType for message found");
                        return;
                    }
                    else if (!SpawnManager.spawnedObjects[targetNetworkId].targetMessageActions[networkOrderId].ContainsKey(messageType))
                    {
                        Debug.LogWarning("MLAPI: No target found with the given messageType");
                        return;
                    }
                    SpawnManager.spawnedObjects[targetNetworkId].targetMessageActions[networkOrderId][messageType].Invoke(clientId, incommingData);
                }
                else
                {
                    foreach (KeyValuePair<int, Action<uint, byte[]>> pair in MessageManager.messageCallbacks[messageType])
                    {
                        if (isPassthrough)
                            pair.Value(passthroughOrigin, incommingData);
                        else
                            pair.Value(clientId, incommingData);
                    }
                }
                #endregion
            }
            else
            {
                #region INTERNAL MESSAGE
                //MLAPI message
                switch (messageType)
                {
                    case 0: //Client to server > sends connection buffer
                        if (isServer)
                        {
                            InternalMessageHandler.HandleConnectionRequest(clientId, incommingData, channelId);
                        }
                        break;
                    case 1: //Server informs client it has been approved:
                        if (isClient)
                        {
                            InternalMessageHandler.HandleConnectionApproved(clientId, incommingData, channelId);
                        }
                        break;
                    case 2:
                        //Server informs client another client connected
                        //MLAPI_ADD_OBJECT
                        if (isClient)
                        {
                            InternalMessageHandler.HandleAddObject(clientId, incommingData, channelId);
                        }
                        break;
                    case 3:
                        //Server informs client another client disconnected
                        //MLAPI_CLIENT_DISCONNECT
                        if (isClient)
                        {
                            InternalMessageHandler.HandleClientDisconnect(clientId, incommingData, channelId);
                        }
                        break;
                    case 4:
                        //Server infroms clients to destroy an object
                        if (isClient)
                        {
                            InternalMessageHandler.HandleDestroyObject(clientId, incommingData, channelId);
                        }
                        break;
                    case 5:
                        //Scene switch
                        if (isClient)
                        {
                            InternalMessageHandler.HandleSwitchScene(clientId, incommingData, channelId);
                        }
                        break;
                    case 6: //Spawn pool object
                        if (isClient)
                        {
                            InternalMessageHandler.HandleSpawnPoolObject(clientId, incommingData, channelId);
                        }
                        break;
                    case 7: //Destroy pool object
                        if (isClient)
                        {
                            InternalMessageHandler.HandleDestroyPoolObject(clientId, incommingData, channelId);
                        }
                        break;
                    case 8: //Change owner
                        if (isClient)
                        {
                            InternalMessageHandler.HandleChangeOwner(clientId, incommingData, channelId);
                        }
                        break;
                    case 9: //Syncvar
                        if (isClient)
                        {
                            InternalMessageHandler.HandleSyncVarUpdate(clientId, incommingData, channelId);
                        }
                        break;
                    case 10:
                        if (isClient) //MLAPI_ADD_OBJECTS (plural)
                        {
                            InternalMessageHandler.HandleAddObjects(clientId, incommingData, channelId);
                        }
                        break;
                    case 11:
                        if (isClient)
                        {
                            InternalMessageHandler.HandleTimeSync(clientId, incommingData, channelId);
                        }
                        break;
                    case 12:
                        if (isServer)
                        {
                            InternalMessageHandler.HandleCommand(clientId, incommingData, channelId);
                        }
                        break;
                    case 13:
                        if (isClient)
                        {
                            InternalMessageHandler.HandleRpc(clientId, incommingData, channelId);
                        }
                        break;
                    case 14:
                        if (isClient)
                        {
                            InternalMessageHandler.HandleTargetRpc(clientId, incommingData, channelId);
                        }
                        break;
                }
                #endregion
            }
        }

        internal void DisconnectClient(uint clientId)
        {
            if (!isServer)
                return;

            if (pendingClients.Contains(clientId))
                pendingClients.Remove(clientId);

            if (connectedClients.ContainsKey(clientId))
                connectedClients.Remove(clientId);

            if (diffieHellmanPublicKeys.ContainsKey(clientId))
                diffieHellmanPublicKeys.Remove(clientId);

            NetId netId = new NetId(clientId);
            if (netId.IsHost() || netId.IsInvalid())
                return;

            byte error;
            NetworkTransport.Disconnect(netId.HostId, netId.ConnectionId, out error);          
        }

        internal void OnClientDisconnect(uint clientId)
        {
            if (pendingClients.Contains(clientId))
                pendingClients.Remove(clientId);
            if (connectedClients.ContainsKey(clientId))
            {
                if(NetworkConfig.HandleObjectSpawning)
                {
                    if (connectedClients[clientId].PlayerObject != null)
                        Destroy(connectedClients[clientId].PlayerObject);
                    for (int i = 0; i < connectedClients[clientId].OwnedObjects.Count; i++)
                    {
                        if (connectedClients[clientId].OwnedObjects[i] != null)
                            Destroy(connectedClients[clientId].OwnedObjects[i].gameObject);
                    }
                }
                connectedClients.Remove(clientId);
            }

            if (isServer)
            {
                using (BitWriter writer = new BitWriter())
                {
                    writer.WriteUInt(clientId);
                    InternalMessageHandler.Send("MLAPI_CLIENT_DISCONNECT", "MLAPI_INTERNAL", writer.Finalize(), clientId);
                }
            }
        }

        private void SyncTime()
        {
            using (BitWriter writer = new BitWriter())
            {
                writer.WriteFloat(NetworkTime);
                int timestamp = NetworkTransport.GetNetworkTimestamp();
                writer.WriteInt(timestamp);

                byte[] buffer = writer.Finalize();
                foreach (KeyValuePair<uint, NetworkedClient> pair in connectedClients)
                    InternalMessageHandler.Send("MLAPI_TIME_SYNC", "MLAPI_TIME_SYNC", buffer);
            }
        }

        internal void HandleApproval(uint clientId, bool approved, Vector3 position, Quaternion rotation)
        {
            if(approved)
            {
                //Inform new client it got approved
                if (pendingClients.Contains(clientId))
                    pendingClients.Remove(clientId);

                byte[] aesKey = new byte[0];
                byte[] publicKey = new byte[0];
                byte[] publicKeySignature = new byte[0];
                if (NetworkConfig.EnableEncryption)
                {
                    EllipticDiffieHellman diffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                    aesKey = diffieHellman.GetSharedSecret(diffieHellmanPublicKeys[clientId]);
                    publicKey = diffieHellman.GetPublicKey();

                    if (diffieHellmanPublicKeys.ContainsKey(clientId))
                        diffieHellmanPublicKeys.Remove(clientId);

                    if (NetworkConfig.SignKeyExchange)
                    {
                        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                        {
                            rsa.PersistKeyInCsp = false;
                            rsa.FromXmlString(NetworkConfig.RSAPrivateKey);
                            publicKeySignature = rsa.SignData(publicKey, new SHA512CryptoServiceProvider());
                        }
                    }
                }

                NetworkedClient client = new NetworkedClient()
                {
                    ClientId = clientId,
                    AesKey = aesKey
                };
                connectedClients.Add(clientId, client);
                
                if(NetworkConfig.HandleObjectSpawning)
                {
                    uint networkId = SpawnManager.GetNetworkObjectId();
                    GameObject go = SpawnManager.SpawnPlayerObject(clientId, networkId, position, rotation);
                    connectedClients[clientId].PlayerObject = go;
                }

                int amountOfObjectsToSend = SpawnManager.spawnedObjects.Values.Count();

                using (BitWriter writer = new BitWriter())
                {
                    writer.WriteUInt(clientId);
                    if (NetworkConfig.EnableSceneSwitching)
                        writer.WriteUInt(NetworkSceneManager.CurrentSceneIndex);

                    if (NetworkConfig.EnableEncryption)
                    {
                        writer.WriteByteArray(publicKey);
                        if (NetworkConfig.SignKeyExchange)
                            writer.WriteByteArray(publicKeySignature);
                    }

                    writer.WriteFloat(NetworkTime);
                    writer.WriteInt(NetworkTransport.GetNetworkTimestamp());

                    writer.WriteInt(connectedClients.Count - 1);
                    foreach (KeyValuePair<uint, NetworkedClient> item in connectedClients)
                    {
                        //Our own ID. Already added as the first one above
                        if (item.Key == clientId)
                            continue;
                        writer.WriteUInt(item.Key); //ClientId
                    }
                    if (NetworkConfig.HandleObjectSpawning)
                    {
                        writer.WriteInt(amountOfObjectsToSend);

                        foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
                        {
                            writer.WriteBool(pair.Value.isPlayerObject);
                            writer.WriteUInt(pair.Value.NetworkId);
                            writer.WriteUInt(pair.Value.OwnerClientId);
                            writer.WriteInt(NetworkConfig.NetworkPrefabIds[pair.Value.NetworkedPrefabName]);
                            writer.WriteBool(pair.Value.gameObject.activeInHierarchy);
                            writer.WriteBool(pair.Value.sceneObject == null ? true : pair.Value.sceneObject.Value);

                            writer.WriteFloat(pair.Value.transform.position.x);
                            writer.WriteFloat(pair.Value.transform.position.y);
                            writer.WriteFloat(pair.Value.transform.position.z);

                            writer.WriteFloat(pair.Value.transform.rotation.eulerAngles.x);
                            writer.WriteFloat(pair.Value.transform.rotation.eulerAngles.y);
                            writer.WriteFloat(pair.Value.transform.rotation.eulerAngles.z);
                        }
                    }

                    InternalMessageHandler.Send(clientId, "MLAPI_CONNECTION_APPROVED", "MLAPI_INTERNAL", writer.Finalize(), null, null, true);

                    if (OnClientConnectedCallback != null)
                        OnClientConnectedCallback.Invoke(clientId);
                }

                //Inform old clients of the new player

                using (BitWriter writer = new BitWriter())
                {
                    if (NetworkConfig.HandleObjectSpawning)
                    {
                        writer.WriteBool(true);
                        writer.WriteUInt(connectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().NetworkId);
                        writer.WriteUInt(clientId);
                        writer.WriteInt(-1);
                        writer.WriteBool(false);

                        writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.position.x);
                        writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.position.y);
                        writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.position.z);

                        writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.x);
                        writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.y);
                        writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.z);
                    }
                    else
                    {
                        writer.WriteUInt(clientId);
                    }

                    InternalMessageHandler.Send("MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", writer.Finalize(), clientId);
                }
                //Flush syncvars:
                foreach (KeyValuePair<uint, NetworkedObject> networkedObject in SpawnManager.spawnedObjects)
                {
                    networkedObject.Value.FlushToClient(clientId);
                }
            }
            else
            {
                if (pendingClients.Contains(clientId))
                    pendingClients.Remove(clientId);

                if (diffieHellmanPublicKeys.ContainsKey(clientId))
                    diffieHellmanPublicKeys.Remove(clientId);

                NetId netId = new NetId(clientId);

                byte error;
                NetworkTransport.Disconnect(netId.HostId, netId.ConnectionId, out error);
            }
        }
    }
}
