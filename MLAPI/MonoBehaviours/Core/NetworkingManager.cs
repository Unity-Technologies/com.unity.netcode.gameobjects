using MLAPI.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Security.Cryptography;
using MLAPI.NetworkingManagerComponents.Cryptography;
using MLAPI.NetworkingManagerComponents.Core;
using UnityEngine.SceneManagement;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.Data.Transports;
using MLAPI.Data.Transports.UNET;
using MLAPI.Data.NetworkProfiler;

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
        /// The log level to use
        /// </summary>
        public LogLevel LogLevel = LogLevel.Normal;
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
        internal readonly Dictionary<uint, NetworkedClient> connectedClients = new Dictionary<uint, NetworkedClient>();
        internal readonly List<NetworkedClient> connectedClientsList = new List<NetworkedClient>();
        /// <summary>
        /// Gets a list of connected clients
        /// </summary>
        public List<NetworkedClient> ConnectedClientsList
        {
            get
            {
                return connectedClientsList;
            }
        }
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
        internal readonly HashSet<uint> pendingClients = new HashSet<uint>();
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
        public delegate void ConnectionApprovedDelegate(uint clientId, int prefabId, bool approved, Vector3 position, Quaternion rotation);
        /// <summary>
        /// The callback to invoke during connection approval
        /// </summary>
        public Action<byte[], uint, ConnectionApprovedDelegate> ConnectionApprovalCallback = null;
        /// <summary>
        /// The current NetworkingConfiguration
        /// </summary>
        public NetworkConfig NetworkConfig;

#if !DISABLE_CRYPTOGRAPHY
        internal EllipticDiffieHellman clientDiffieHellman;
        internal readonly Dictionary<uint, byte[]> diffieHellmanPublicKeys = new Dictionary<uint, byte[]>();
        internal byte[] clientAesKey;
#endif

        /// <summary>
        /// An inspector bool that acts as a Trigger for regenerating RSA keys. Should not be used outside Unity editor.
        /// </summary>
        public bool RegenerateRSAKeys = false;

        private void OnValidate()
        {
            if (NetworkConfig == null)
                return; //May occur when the component is added            

            if (NetworkConfig.EnableSceneSwitching && !NetworkConfig.RegisteredScenes.Contains(SceneManager.GetActiveScene().name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The active scene is not registered as a networked scene. The MLAPI has added it");
                NetworkConfig.RegisteredScenes.Add(SceneManager.GetActiveScene().name);
            }

            if (!NetworkConfig.EnableSceneSwitching && NetworkConfig.HandleObjectSpawning)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Please be aware that Scene objects are NOT supported if SceneManagement is turned off, even if HandleObjectSpawning is turned on");
            }

            if (NetworkConfig.HandleObjectSpawning)
            {
                for (int i = 0; i < NetworkConfig.NetworkedPrefabs.Count; i++)
                {
                    if (string.IsNullOrEmpty(NetworkConfig.NetworkedPrefabs[i].name))
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The NetworkedPrefab " + NetworkConfig.NetworkedPrefabs[i].prefab.name + " does not have a NetworkedPrefabName.");
                    }
                }
                int playerPrefabCount = NetworkConfig.NetworkedPrefabs.Count(x => x.playerPrefab == true);
                if (playerPrefabCount == 0)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("There is no NetworkedPrefab marked as a PlayerPrefab");
                }
                else if (playerPrefabCount > 1)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only one networked prefab can be marked as a player prefab");
                }
                else
                    NetworkConfig.PlayerPrefabName = NetworkConfig.NetworkedPrefabs.Find(x => x.playerPrefab == true).name;

            }

            if (!NetworkConfig.EnableEncryption)
            {
                RegenerateRSAKeys = false;
            }
            else
            {
                if (RegenerateRSAKeys)
                {
#if !DISABLE_CRYPTOGRAPHY
                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                    {
                        rsa.PersistKeyInCsp = false;
                        NetworkConfig.RSAPrivateKey = rsa.ToXmlString(true);
                        NetworkConfig.RSAPublicKey = rsa.ToXmlString(false);
                    }
#endif
                    RegenerateRSAKeys = false;
                }
            }
        }

        private object Init(bool server)
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Init()");
            networkTime = 0f;
            lastSendTickTime = 0f;
            lastEventTickTime = 0f;
            lastReceiveTickTime = 0f;
            eventOvershootCounter = 0f;
            pendingClients.Clear();
            connectedClients.Clear();
            connectedClientsList.Clear();
            messageBuffer = new byte[NetworkConfig.MessageBufferSize];
#if !DISABLE_CRYPTOGRAPHY
            diffieHellmanPublicKeys.Clear();
#endif
            Data.Cache.messageAttributeHashes.Clear();
            Data.Cache.messageAttributeNames.Clear();
            MessageManager.channels.Clear();
            MessageManager.messageTypes.Clear();
            MessageManager.messageCallbacks.Clear();
            MessageManager.messageHandlerCounter.Clear();
            MessageManager.releasedMessageHandlerCounters.Clear();
            MessageManager.reverseChannels.Clear();
            MessageManager.reverseMessageTypes.Clear();
            SpawnManager.spawnedObjects.Clear();
            SpawnManager.releasedNetworkObjectIds.Clear();
            NetworkPoolManager.Pools.Clear();
            NetworkPoolManager.PoolNamesToIndexes.Clear();
            NetworkSceneManager.registeredSceneNames.Clear();
            NetworkSceneManager.sceneIndexToString.Clear();
            NetworkSceneManager.sceneNameToIndex.Clear();
            InternalMessageHandler.FinalMessageBuffer = new byte[NetworkConfig.MessageBufferSize];

            if (NetworkConfig.Transport == DefaultTransport.UNET)
                NetworkConfig.NetworkTransport = new UnetTransport();
            else if (NetworkConfig.Transport == DefaultTransport.Custom && NetworkConfig.NetworkTransport == null)
                throw new NullReferenceException("The current NetworkTransport is null");

            object settings = NetworkConfig.NetworkTransport.GetSettings(); //Gets a new "settings" object for the transport currently used.

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
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Duplicate NetworkedPrefabName " + NetworkConfig.NetworkedPrefabs[i].name);
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

            //MLAPI channels and messageTypes
            List<Channel> internalChannels = new List<Channel>
            {
                new Channel()
                {
                    Name = "MLAPI_INTERNAL",
                    Type = NetworkConfig.NetworkTransport.InternalChannel
                },
                new Channel()
                {
                    Name = "MLAPI_POSITION_UPDATE",
                    Type = ChannelType.StateUpdate
                },
                new Channel()
                {
                    Name = "MLAPI_ANIMATION_UPDATE",
                    Type = ChannelType.ReliableSequenced
                },
                new Channel()
                {
                    Name = "MLAPI_NAV_AGENT_STATE",
                    Type = ChannelType.ReliableSequenced
                },
                new Channel()
                {
                    Name = "MLAPI_NAV_AGENT_CORRECTION",
                    Type = ChannelType.StateUpdate
                },
                new Channel()
                {
                    Name = "MLAPI_TIME_SYNC",
                    Type = ChannelType.Unreliable
                }
            };

            if (NetworkConfig.EnableEncryption)
            {
                HashSet<string> addedEncryptedChannels = new HashSet<string>();
                for (int i = 0; i < NetworkConfig.Channels.Count; i++)
                {
                    if (addedEncryptedChannels.Contains(NetworkConfig.Channels[i].Name))
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Duplicate encrypted channel name " + NetworkConfig.Channels[i].Name);
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
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Duplicate passthrough message type " + NetworkConfig.MessageTypes[i].Name);
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
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Duplicate channel name: " + NetworkConfig.Channels[i].Name);
                    continue;
                }
                int channelId = NetworkConfig.NetworkTransport.AddChannel(internalChannels[i].Type, settings);
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
            MessageManager.messageTypes.Add("MLAPI_SET_VISIBILITY", 15);

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
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Duplicate channel name: " + NetworkConfig.Channels[i].Name);
                    continue;
                }
                int channelId = NetworkConfig.NetworkTransport.AddChannel(NetworkConfig.Channels[i].Type, settings);
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
            return settings;
        }

        /// <summary>
        /// Starts a server
        /// </summary>
        public void StartServer()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StartServer()");
            if (isServer || isClient)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot start server while an instance is already running");
                return;
            }

            if (NetworkConfig.ConnectionApproval)
            {
                if (ConnectionApprovalCallback == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }

            object settings = Init(true);
            NetworkConfig.NetworkTransport.RegisterServerListenSocket(settings);

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
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StartClient()");
            if (isServer || isClient)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot start client while an instance is already running");
                return;
            }

            object settings = Init(false);
            byte error;
            NetworkConfig.NetworkTransport.Connect(NetworkConfig.ConnectAddress, NetworkConfig.ConnectPort, settings, out error);
            _isServer = false;
            _isClient = true;
            isListening = true;
        }

        /// <summary>
        /// Stops the running server
        /// </summary>
        public void StopServer()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopServer()");
            HashSet<uint> disconnectedIds = new HashSet<uint>();
            //Don't know if I have to disconnect the clients. I'm assuming the NetworkTransport does all the cleaning on shtudown. But this way the clients get a disconnect message from server (so long it does't get lost)
            foreach (KeyValuePair<uint, NetworkedClient> pair in connectedClients)
            {
                if(!disconnectedIds.Contains(pair.Key))
                {
                    disconnectedIds.Add(pair.Key);
                    if (pair.Key == NetworkConfig.NetworkTransport.HostDummyId ||
                        pair.Key == NetworkConfig.NetworkTransport.InvalidDummyId)
                        continue;

                    NetworkConfig.NetworkTransport.DisconnectClient(pair.Key);
                }
            }
            foreach (uint clientId in pendingClients)
            {
                if (!disconnectedIds.Contains(clientId))
                {
                    disconnectedIds.Add(clientId);
                    if (clientId == NetworkConfig.NetworkTransport.HostDummyId ||
                        clientId == NetworkConfig.NetworkTransport.InvalidDummyId)
                        continue;
                    NetworkConfig.NetworkTransport.DisconnectClient(clientId);
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
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopHost()");
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
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopClient()");
            _isClient = false;
            NetworkConfig.NetworkTransport.DisconnectFromServer();
            _isClientConnected = false;
            Shutdown();
        }

        /// <summary>
        /// Starts a Host
        /// </summary>
        public void StartHost(Vector3? pos = null, Quaternion? rot = null, int prefabId = -1)
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StartHost()");
            if (isServer || isClient)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot start host while an instance is already running");
                return;
            }

            if (NetworkConfig.ConnectionApproval)
            {
                if (ConnectionApprovalCallback == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }
            object settings = Init(true);
            NetworkConfig.NetworkTransport.RegisterServerListenSocket(settings);

            _isServer = true;
            _isClient = true;
            isListening = true;

            uint hostClientId = NetworkConfig.NetworkTransport.HostDummyId;
            connectedClients.Add(hostClientId, new NetworkedClient()
            {
                ClientId = hostClientId
            });
            connectedClientsList.Add(connectedClients[hostClientId]);

            if (NetworkConfig.HandleObjectSpawning)
            {
                prefabId = prefabId == -1 ? NetworkConfig.NetworkPrefabIds[NetworkConfig.PlayerPrefabName] : prefabId;
                SpawnManager.CreateSpawnedObject(prefabId, 0, hostClientId, true, pos.GetValueOrDefault(), rot.GetValueOrDefault(), null, false, false);
            }

            if (OnServerStarted != null)
                OnServerStarted.Invoke();
        }

        private void OnEnable()
        {
            if (singleton != null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Multiple NetworkingManagers");
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
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Shutdown()");
            NetworkProfiler.Stop();
            isListening = false;
            _isClient = false;
            _isServer = false;
            SpawnManager.DestroyNonSceneObjects();

            if (NetworkConfig != null && NetworkConfig.NetworkTransport != null) //The Transport is set during Init time, thus it is possible for the Transport to be null
                NetworkConfig.NetworkTransport.Shutdown();
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
                        byte error;
                        NetworkConfig.NetworkTransport.SendQueue(pair.Key, out error);
                        if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Send Pending Queue: " + pair.Key);
                    }
                    lastSendTickTime = NetworkTime;
                }
                if((NetworkTime - lastReceiveTickTime >= (1f / NetworkConfig.ReceiveTickrate)) || NetworkConfig.ReceiveTickrate <= 0)
                {
                    NetworkProfiler.StartTick(TickType.Receive);
                    NetEventType eventType;
                    int processedEvents = 0;
                    do
                    {
                        processedEvents++;
                        uint clientId;
                        int channelId;
                        int receivedSize;
                        byte error;
                        eventType = NetworkConfig.NetworkTransport.PollReceive(out clientId, out channelId, ref messageBuffer, messageBuffer.Length, out receivedSize, out error);

                        switch (eventType)
                        {
                            case NetEventType.Connect:
                                NetworkProfiler.StartEvent(TickType.Receive, (uint)receivedSize, MessageManager.reverseChannels[channelId], "TRANSPORT_CONNECT");
                                if (isServer)
                                {
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Client Connected");
                                    pendingClients.Add(clientId);
                                    StartCoroutine(ApprovalTimeout(clientId));
                                }
                                else
                                {
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Connected");
#if !DISABLE_CRYPTOGRAPHY
                                    byte[] diffiePublic = new byte[0];
                                    if(NetworkConfig.EnableEncryption)
                                    {
                                        clientDiffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                                        diffiePublic = clientDiffieHellman.GetPublicKey();
                                    }
#endif

                                    using (BitWriter writer = BitWriter.Get())
                                    {
                                        writer.WriteByteArray(NetworkConfig.GetConfig(), true);
#if !DISABLE_CRYPTOGRAPHY
                                        if (NetworkConfig.EnableEncryption)      
                                            writer.WriteByteArray(diffiePublic);
#endif

                                        if (NetworkConfig.ConnectionApproval)
                                            writer.WriteByteArray(NetworkConfig.ConnectionData);

                                        InternalMessageHandler.Send(clientId, "MLAPI_CONNECTION_REQUEST", "MLAPI_INTERNAL", writer, null, null, null, true);
                                    }
                                }
                                NetworkProfiler.EndEvent();
                                break;
                            case NetEventType.Data:
                                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Incomming Data From " + clientId + " : " + receivedSize + " bytes");

                                HandleIncomingData(clientId, messageBuffer, channelId, (uint)receivedSize);
                                break;
                            case NetEventType.Disconnect:
                                NetworkProfiler.StartEvent(TickType.Receive, 0, "NONE", "TRANSPORT_DISCONNECT");
                                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Disconnect Event From " + clientId);

                                if (isServer)
                                    OnClientDisconnectFromServer(clientId);
                                else
                                {
                                    _isClientConnected = false;
                                    StopClient();
                                }

                                if (OnClientDisconnectCallback != null)
                                    OnClientDisconnectCallback.Invoke(clientId);
                                NetworkProfiler.EndEvent();
                                break;
                        }
                        // Only do another iteration if: there are no more messages AND (there is no limit to max events or we have processed less than the maximum)
                    } while (eventType != NetEventType.Nothing && (NetworkConfig.MaxReceiveEventsPerTickRate <= 0 || processedEvents < NetworkConfig.MaxReceiveEventsPerTickRate));
                    lastReceiveTickTime = NetworkTime;
                    NetworkProfiler.EndTick();
                }

                if (isServer && ((NetworkTime - lastEventTickTime >= (1f / NetworkConfig.EventTickrate))))
                {
                    NetworkProfiler.StartTick(TickType.Event);
                    eventOvershootCounter += ((NetworkTime - lastEventTickTime) - (1f / NetworkConfig.EventTickrate));
                    LagCompensationManager.AddFrames();
                    NetworkedObject.InvokeSyncvarUpdate();
                    lastEventTickTime = NetworkTime;
                    NetworkProfiler.EndTick();
                }
                else if (isServer && eventOvershootCounter >= ((1f / NetworkConfig.EventTickrate)))
                {
                    NetworkProfiler.StartTick(TickType.Event);
                    //We run this one to compensate for previous update overshoots.
                    eventOvershootCounter -= (1f / NetworkConfig.EventTickrate);
                    LagCompensationManager.AddFrames();
                    NetworkProfiler.EndTick();
                }

                if (NetworkConfig.EnableTimeResync && NetworkTime - lastTimeSyncTime >= 30)
                {
                    NetworkProfiler.StartTick(TickType.Event);
                    SyncTime();
                    lastTimeSyncTime = NetworkTime;
                    NetworkProfiler.EndTick();
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
                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Client " + clientId + " Handshake Timed Out");
                DisconnectClient(clientId);
            }
        }

        private void HandleIncomingData(uint clientId, byte[] data, int channelId, uint totalSize)
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Unwrapping Data Header");
            using (BitReader reader = BitReader.Get(data))
            {
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

                long headerBitSize = BitWriter.GetBitCount(messageType) + BitWriter.GetBitCount(targeted);
                if (targeted) headerBitSize += BitWriter.GetBitCount(targetNetworkId) + BitWriter.GetBitCount(networkOrderId);
                headerBitSize += BitWriter.GetBitCount(isPassthrough);
                if (isPassthrough && isServer)
                    headerBitSize += BitWriter.GetBitCount(passthroughTarget);
                else if (isPassthrough && !isServer)
                    headerBitSize += BitWriter.GetBitCount(passthroughOrigin);
                
                uint headerByteSize = (uint)Math.Ceiling(headerBitSize / 8d);
                NetworkProfiler.StartEvent(TickType.Receive, totalSize - headerByteSize, channelId, messageType);

                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Data Header" + 
                    ":messageHeader=" + messageType + 
                    ":targeted=" + targeted + 
                    ":targetNetworkId=" + targetNetworkId + 
                    ":targetBehaviourIndex=" + networkOrderId + 
                    ":passthrough=" + isPassthrough + 
                    ":passthroughOrigin=" + passthroughOrigin +
                    ":passthroughTarget=" + passthroughTarget);

                //Client tried to send a network message that was not the connection request before he was accepted.
                if (isServer && pendingClients.Contains(clientId) && messageType != 0)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Message recieved from clientId " + clientId + " before it has been accepted");
                    return;
                }

                reader.SkipPadded();
#if !DISABLE_CRYPTOGRAPHY
                byte[] readBuffer = null;
                if (NetworkConfig.EncryptedChannelsHashSet.Contains(MessageManager.reverseChannels[channelId]))
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Decrypting message body");
                    //Encrypted message
                    if (isServer)
                        readBuffer = CryptographyHelper.Decrypt(reader.ReadByteArray(), connectedClients[clientId].AesKey);
                    else
                        readBuffer = CryptographyHelper.Decrypt(reader.ReadByteArray(), clientAesKey);
                }

                using (BitReader messageReader = readBuffer == null ? reader : BitReader.Get(readBuffer))
#else
                using (BitReader messageReader = reader)
#endif
                {
                    if (isServer && isPassthrough && !NetworkConfig.PassthroughMessageHashSet.Contains(messageType))
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Client " + clientId + " tried to send a passthrough message for a messageType not registered as passthrough");
                        messageReader.Dispose();
                        return;
                    }
                    else if (isClient && isPassthrough && !NetworkConfig.PassthroughMessageHashSet.Contains(messageType))
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server tried to send a passthrough message for a messageType not registered as passthrough");
                        messageReader.Dispose();
                        return;
                    }
                    else if (isServer && isPassthrough)
                    {
                        if (!connectedClients.ContainsKey(passthroughTarget))
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Passthrough message was sent with invalid target: " + passthroughTarget + " from client " + clientId);
                            messageReader.Dispose();
                            return;
                        }
                        uint? netIdTarget = null;
                        ushort? netOrderId = null;
                        if (targeted)
                        {
                            netIdTarget = targetNetworkId;
                            netOrderId = networkOrderId;
                        }
                        InternalMessageHandler.PassthroughSend(passthroughTarget, clientId, messageType, channelId, messageReader, netIdTarget, netOrderId);
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
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("No target for message found");
                                messageReader.Dispose();
                                return;
                            }
                            else if (!SpawnManager.spawnedObjects[targetNetworkId].targetMessageActions.ContainsKey(networkOrderId))
                            {
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("No target messageType for message found");
                                messageReader.Dispose();
                                return;
                            }
                            else if (!SpawnManager.spawnedObjects[targetNetworkId].targetMessageActions[networkOrderId].ContainsKey(messageType))
                            {
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("No target found with the given messageType");
                                messageReader.Dispose();
                                return;
                            }
                            if (SpawnManager.spawnedObjects[targetNetworkId].targetMessageActions[networkOrderId].ContainsKey(messageType))
                            {
                                SpawnManager.spawnedObjects[targetNetworkId].targetMessageActions[networkOrderId][messageType].Invoke(clientId, messageReader);
                            }
                        }
                        else
                        {
                            foreach (KeyValuePair<int, Action<uint, BitReader>> pair in MessageManager.messageCallbacks[messageType])
                            {
                                if (isPassthrough)
                                    pair.Value(passthroughOrigin, messageReader);
                                else
                                    pair.Value(clientId, messageReader);
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
                                    InternalMessageHandler.HandleConnectionRequest(clientId, messageReader, channelId);
                                break;
                            case 1: //Server informs client it has been approved:
                                if (isClient)
                                    InternalMessageHandler.HandleConnectionApproved(clientId, messageReader, channelId);
                                break;
                            case 2:
                                //Server informs client another client connected
                                //MLAPI_ADD_OBJECT
                                if (isClient)
                                    InternalMessageHandler.HandleAddObject(clientId, messageReader, channelId);
                                break;
                            case 3:
                                //Server informs client another client disconnected
                                //MLAPI_CLIENT_DISCONNECT
                                if (isClient)
                                    InternalMessageHandler.HandleClientDisconnect(clientId, messageReader, channelId);
                                break;
                            case 4:
                                //Server infroms clients to destroy an object
                                if (isClient)
                                    InternalMessageHandler.HandleDestroyObject(clientId, messageReader, channelId);
                                break;
                            case 5:
                                //Scene switch
                                if (isClient)
                                    InternalMessageHandler.HandleSwitchScene(clientId, messageReader, channelId);
                                break;
                            case 6: //Spawn pool object
                                if (isClient)
                                    InternalMessageHandler.HandleSpawnPoolObject(clientId, messageReader, channelId);
                                break;
                            case 7: //Destroy pool object
                                if (isClient)
                                    InternalMessageHandler.HandleDestroyPoolObject(clientId, messageReader, channelId);
                                break;
                            case 8: //Change owner
                                if (isClient)
                                    InternalMessageHandler.HandleChangeOwner(clientId, messageReader, channelId);
                                break;
                            case 9: //Syncvar
                                if (isClient)
                                    InternalMessageHandler.HandleSyncVarUpdate(clientId, messageReader, channelId);
                                break;
                            case 10:
                                if (isClient) //MLAPI_ADD_OBJECTS (plural)
                                    InternalMessageHandler.HandleAddObjects(clientId, messageReader, channelId);
                                break;
                            case 11:
                                if (isClient)
                                    InternalMessageHandler.HandleTimeSync(clientId, messageReader, channelId);
                                break;
                            case 12:
                                if (isServer)
                                    InternalMessageHandler.HandleCommand(clientId, messageReader, channelId);
                                break;
                            case 13:
                                if (isClient)
                                    InternalMessageHandler.HandleRpc(clientId, messageReader, channelId);
                                break;
                            case 14:
                                if (isClient)
                                    InternalMessageHandler.HandleTargetRpc(clientId, messageReader, channelId);
                                break;
                            case 15:
                                if (isClient)
                                    InternalMessageHandler.HandleSetVisibility(clientId, messageReader, channelId);
                                break;
                        }
                        #endregion
                    }
                }
                NetworkProfiler.EndEvent();
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
      
            connectedClientsList.RemoveAll(x => x.ClientId == clientId); // :(

#if !DISABLE_CRYPTOGRAPHY
            if (diffieHellmanPublicKeys.ContainsKey(clientId))
                diffieHellmanPublicKeys.Remove(clientId);
#endif

            foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
                pair.Value.observers.Remove(clientId);

            NetworkConfig.NetworkTransport.DisconnectClient(clientId);
        }

        internal void OnClientDisconnectFromServer(uint clientId)
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
                connectedClientsList.RemoveAll(x => x.ClientId == clientId);
                connectedClients.Remove(clientId);
            }

            if (isServer)
            {
                foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
                    pair.Value.observers.Remove(clientId);

                using (BitWriter writer = BitWriter.Get())
                {
                    writer.WriteUInt(clientId);
                    InternalMessageHandler.Send("MLAPI_CLIENT_DISCONNECT", "MLAPI_INTERNAL", writer, clientId, null);
                }
            }
        }

        private void SyncTime()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Syncing Time To Clients");
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteFloat(NetworkTime);
                int timestamp = NetworkConfig.NetworkTransport.GetNetworkTimestamp();
                writer.WriteInt(timestamp);
                InternalMessageHandler.Send("MLAPI_TIME_SYNC", "MLAPI_TIME_SYNC", writer, null);
            }
        }

        internal void HandleApproval(uint clientId, int prefabId, bool approved, Vector3 position, Quaternion rotation)
        {
            if(approved)
            {
                //Inform new client it got approved
                if (pendingClients.Contains(clientId))
                    pendingClients.Remove(clientId);

#if !DISABLE_CRYPTOGRAPHY
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
#endif

                NetworkedClient client = new NetworkedClient()
                {
                    ClientId = clientId,
#if !DISABLE_CRYPTOGRAPHY
                    AesKey = aesKey
#endif
                };
                connectedClients.Add(clientId, client);
                connectedClientsList.Add(client);

                NetworkedObject netObject = null;
                if(NetworkConfig.HandleObjectSpawning)
                {
                    uint networkId = SpawnManager.GetNetworkObjectId();
                    prefabId = prefabId == -1 ? NetworkConfig.NetworkPrefabIds[NetworkConfig.PlayerPrefabName] : prefabId;
                    netObject = SpawnManager.CreateSpawnedObject(prefabId, networkId, clientId, true, position, rotation, null, false, false);
                    connectedClients[clientId].PlayerObject = netObject;
                }

                int amountOfObjectsToSend = SpawnManager.spawnedObjects.Values.Count;

                using (BitWriter writer = BitWriter.Get())
                {
                    writer.WriteUInt(clientId);
                    if (NetworkConfig.EnableSceneSwitching)
                        writer.WriteUInt(NetworkSceneManager.CurrentSceneIndex);

#if !DISABLE_CRYPTOGRAPHY
                    if (NetworkConfig.EnableEncryption)
                    {
                        writer.WriteByteArray(publicKey);
                        if (NetworkConfig.SignKeyExchange)
                            writer.WriteByteArray(publicKeySignature);
                    }
#endif

                    writer.WriteFloat(NetworkTime);
                    writer.WriteInt(NetworkConfig.NetworkTransport.GetNetworkTimestamp());

                    writer.WriteInt(connectedClients.Count - 1);

                    foreach (KeyValuePair<uint, NetworkedClient> item in connectedClients)
                    {
                        //Our own ID. Already added as the first one above
                        if (item.Key == clientId)
                            continue;
                        writer.WriteUInt(item.Key); //ClientId

                        if (netObject != null)
                            netObject.RebuildObservers(item.Key);
                    }
                    if (NetworkConfig.HandleObjectSpawning)
                    {
                        writer.WriteInt(amountOfObjectsToSend);

                        foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
                        {
                            pair.Value.RebuildObservers(clientId); //Rebuilds observers for the new client

                            writer.WriteBool(pair.Value.isPlayerObject);
                            writer.WriteUInt(pair.Value.NetworkId);
                            writer.WriteUInt(pair.Value.OwnerClientId);
                            writer.WriteInt(NetworkConfig.NetworkPrefabIds[pair.Value.NetworkedPrefabName]);
                            writer.WriteBool(pair.Value.gameObject.activeInHierarchy);
                            writer.WriteBool(pair.Value.sceneObject == null ? true : pair.Value.sceneObject.Value);
                            writer.WriteBool(pair.Value.observers.Contains(clientId));

                            writer.WriteFloat(pair.Value.transform.position.x);
                            writer.WriteFloat(pair.Value.transform.position.y);
                            writer.WriteFloat(pair.Value.transform.position.z);

                            writer.WriteFloat(pair.Value.transform.rotation.eulerAngles.x);
                            writer.WriteFloat(pair.Value.transform.rotation.eulerAngles.y);
                            writer.WriteFloat(pair.Value.transform.rotation.eulerAngles.z);

                            if (pair.Value.observers.Contains(clientId))
                                pair.Value.WriteFormattedSyncedVarData(writer);
                        }
                    }
                    InternalMessageHandler.Send(clientId, "MLAPI_CONNECTION_APPROVED", "MLAPI_INTERNAL", writer, null, null, null, true);

                    if (OnClientConnectedCallback != null)
                        OnClientConnectedCallback.Invoke(clientId);
                }

                //Inform old clients of the new player

                foreach (var clientPair in connectedClients)
                {
                    if (clientPair.Key == clientId)
                        continue; //The new client.

                    using (BitWriter writer = BitWriter.Get())
                    {
                        if (NetworkConfig.HandleObjectSpawning)
                        {
                            writer.WriteBool(true);
                            writer.WriteUInt(connectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().NetworkId);
                            writer.WriteUInt(clientId);
                            writer.WriteInt(prefabId);
                            writer.WriteBool(false);
                            writer.WriteBool(connectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().observers.Contains(clientPair.Key));

                            writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.position.x);
                            writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.position.y);
                            writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.position.z);

                            writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.x);
                            writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.y);
                            writer.WriteFloat(connectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.z);

                            writer.WriteBool(false); //No payload data

                            if (connectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().observers.Contains(clientPair.Key))
                                connectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().WriteFormattedSyncedVarData(writer);
                        }
                        else
                        {
                            writer.WriteUInt(clientId);
                        }
                        InternalMessageHandler.Send(clientPair.Key, "MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", writer, null);
                    }
                }
            }
            else
            {
                if (pendingClients.Contains(clientId))
                    pendingClients.Remove(clientId);

#if !DISABLE_CRYPTOGRAPHY
                if (diffieHellmanPublicKeys.ContainsKey(clientId))
                    diffieHellmanPublicKeys.Remove(clientId);
#endif

                NetworkConfig.NetworkTransport.DisconnectClient(clientId);
            }
        }
#region SEND METHODS
        /// <summary>
        /// Registers a message handler
        /// </summary>
        /// <param name="name">The MessageType to register</param>
        /// <param name="action">The callback to get invoked whenever a message is received</param>
        /// <returns>HandlerId for the messageHandler that can be used to deregister the messageHandler</returns>
        public int RegisterMessageHandler(string name, Action<uint, BitReader> action)
        {
            if (!MessageManager.messageTypes.ContainsKey(name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The messageType " + name + " is not registered");
                return -1;
            }
            return MessageManager.AddIncomingMessageHandler(name, action);
        }

        /// <summary>
        /// Deregisters a given message handler
        /// </summary>
        /// <param name="name">The MessageType to deregister</param>
        /// <param name="counter">The messageHandlerId to deregister</param>
        public void DeregisterMessageHandler(string name, int counter)
        {
            MessageManager.RemoveIncomingMessageHandler(name, counter);
        }

        /// <summary>
        /// Sends a buffer to the server from client
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        public void SendToServer(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server can not send messages to server");
                return;
            }
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, null);
            }
        }

        /// <summary>
        /// Sends a buffer to the server from client
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        public void SendToServer(string messageType, string channelName, BitWriter writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server can not send messages to server");
                return;
            }
            InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, null);
        }

        /// <summary>
        /// Sends a binary serialized class to the server from client 
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="instance">The instance to send</param>
        public void SendToServer<T>(string messageType, string channelName, T instance)
        {
            SendToServer(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        public void SendToClient(uint clientId, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientId, messageType, channelName, writer, null);
            }
        }


        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        public void SendToClient(uint clientId, string messageType, string channelName, BitWriter writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(clientId, messageType, channelName, writer, null);
        }

        /// <summary>
        /// Sends a binary serialized class to a client with a given clientId from Server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>    
        /// <param name="instance">The instance to send</param>
        public void SendToClient<T>(int clientId, string messageType, string channelName, T instance)
        {
            SendToClient(clientId, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        public void SendToClients(uint[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, null);
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        public void SendToClients(uint[] clientIds, string messageType, string channelName, BitWriter writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, null);
        }

        /// <summary>
        /// Sends a binary serialized class to multiple clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>    
        /// <param name="instance">The instance to send</param>
        public void SendToClients<T>(int[] clientIds, string messageType, string channelName, T instance)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        public void SendToClients(List<uint> clientIds, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, null);
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        public void SendToClients(List<uint> clientIds, string messageType, string channelName, BitWriter writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, null);
        }

        /// <summary>
        /// Sends a binary serialized class to multiple clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>    
        /// <param name="instance">The instance to send</param>
        public void SendToClients<T>(List<int> clientIds, string messageType, string channelName, T instance)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        public void SendToClients(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, null);
            }
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        public void SendToClients(string messageType, string channelName, BitWriter writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            InternalMessageHandler.Send(messageType, channelName, writer, null);
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>    
        /// <param name="instance">The instance to send</param>
        public void SendToClients<T>(string messageType, string channelName, T instance)
        {
            SendToClients(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Returns the NetworkedObject with a given NetworkId
        /// </summary>
        /// <param name="networkId">The NetworkId</param>
        /// <returns>Returns the NetworkedObject</returns>
        public NetworkedObject GetNetworkedObject(uint networkId)
        {
            return SpawnManager.spawnedObjects[networkId];
        }
#endregion
    }
}
