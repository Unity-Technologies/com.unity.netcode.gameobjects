using MLAPI.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Security.Cryptography;
using MLAPI.Logging;
using UnityEngine.SceneManagement;
using System.IO;
using MLAPI.Components;
using MLAPI.Configuration;
#if !DISABLE_CRYPTOGRAPHY
using MLAPI.Cryptography;
#endif
using MLAPI.Internal;
using MLAPI.Profiling;
using MLAPI.Serialization;
using MLAPI.Transports;
using MLAPI.Transports.UNET;
using BitStream = MLAPI.Serialization.BitStream;
using System.Security.Cryptography.X509Certificates;

namespace MLAPI
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
        public float NetworkTime { get; internal set; }
        /// <summary>
        /// Gets or sets if the NetworkingManager should be marked as DontDestroyOnLoad
        /// </summary>
        [HideInInspector]
        public bool DontDestroy = true;
        /// <summary>
        /// Gets or sets if the application should be set to run in background
        /// </summary>
        [HideInInspector]
        public bool RunInBackground = true;
        /// <summary>
        /// The log level to use
        /// </summary>
        [HideInInspector]
        public LogLevel LogLevel = LogLevel.Normal;
        [Obsolete("Use Singleton instead", false)]
        public static NetworkingManager singleton => Singleton;
        /// <summary>
        /// The singleton instance of the NetworkingManager
        /// </summary>
        public static NetworkingManager Singleton { get; private set; }
        /// <summary>
        /// Gets the networkId of the server
        /// </summary>
		public uint ServerClientId => NetworkConfig.NetworkTransport != null ? NetworkConfig.NetworkTransport.ServerClientId : 0;
        /// <summary>
        /// The clientId the server calls the local client by, only valid for clients
        /// </summary>
        public uint LocalClientId 
        {
            get
            {
                if (IsServer) return NetworkConfig.NetworkTransport.ServerClientId;
				else return localClientId;
            }
            internal set
            {
                localClientId = value;
            }
        }
        private uint localClientId;
        /// <summary>
        /// Gets a dictionary of connected clients and their clientId keys
        /// </summary>
        public readonly Dictionary<uint, NetworkedClient> ConnectedClients = new Dictionary<uint, NetworkedClient>();
        /// <summary>
        /// Gets a list of connected clients
        /// </summary>
        public readonly List<NetworkedClient> ConnectedClientsList = new List<NetworkedClient>();
        /// <summary>
        /// Gets a dictionary of the clients that have been accepted by the transport but are still pending by the MLAPI.
        /// </summary>
        public readonly Dictionary<uint, PendingClient> PendingClients = new Dictionary<uint, PendingClient>();
        [Obsolete("Use IsServer instead", false)]
        public bool isServer => IsServer;
        /// <summary>
        /// Gets wheter or not a server is running
        /// </summary>
        public bool IsServer { get; internal set; }
        [Obsolete("Use IsClient instead", false)]
        public bool isClient => IsClient;
        /// <summary>
        /// Gets wheter or not a client is running
        /// </summary>
        public bool IsClient { get; internal set; }
        [Obsolete("Use IsHost instead", false)]
        public bool isHost => IsHost;
        /// <summary>
        /// Gets if we are running as host
        /// </summary>
        public bool IsHost => IsServer && IsClient;
        [Obsolete("Use IsListening instead", false)]
        public bool isListening => IsListening;
        /// <summary>
        /// Gets wheter or not we are listening for connections
        /// </summary>
        public bool IsListening { get; internal set; }
        private byte[] messageBuffer;
        [Obsolete("Use IsConnectedClient instead", false)]
        public bool isConnectedClients => IsConnectedClient;
        /// <summary>
        /// Gets if we are connected as a client
        /// </summary>
        public bool IsConnectedClient { get; internal set; }
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
        /// Delegate type called when connection has been approved
        /// </summary>
        /// <param name="clientId">The clientId of the approved client</param>
        /// <param name="prefabId">The prefabId to use for the client</param>
        /// <param name="approved">Wheter or not the client was approved</param>
        /// <param name="position">The position to spawn the client at</param>
        /// <param name="rotation">The rotation to spawn the client with</param>
        public delegate void ConnectionApprovedDelegate(uint clientId, int prefabId, bool approved, Vector3? position, Quaternion? rotation);
        /// <summary>
        /// The callback to invoke during connection approval
        /// </summary>
        public Action<byte[], uint, ConnectionApprovedDelegate> ConnectionApprovalCallback = null;
        /// <summary>
        /// The current NetworkingConfiguration
        /// </summary>
        public NetworkConfig NetworkConfig;
        /// <summary>
        /// Delegate used for incoming custom messages
        /// </summary>
        /// <param name="clientId">The clientId that sent the message</param>
        /// <param name="stream">The stream containing the message data</param>
        public delegate void CustomMessageDelegete(uint clientId, Stream stream);
        /// <summary>
        /// Event invoked when custom messages arrive
        /// </summary>
        public event CustomMessageDelegete OnIncomingCustomMessage;
        /// <summary>
        /// The current hostname we are connected to, used to validate certificate
        /// </summary>
        public string ConnectedHostname { get; private set; }
        internal byte[] clientAesKey;
        internal static event Action OnSingletonReady;

        internal void InvokeOnIncomingCustomMessage(uint clientId, Stream stream)
        {
            if (OnIncomingCustomMessage != null) OnIncomingCustomMessage(clientId, stream);
        }

        /// <summary>
        /// Sends custom message to a list of clients
        /// </summary>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="channel">The channel to send the data on</param>
        /// <param name="security">The security settings to apply to the message</param>
        public void SendCustomMessage(List<uint> clientIds, BitStream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            if (!IsServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogWarning("Can not send custom message to multiple users as a client");
                return;
            }
            if (clientIds == null)
            {
                for (int i = 0; i < ConnectedClientsList.Count; i++)
                {
                    InternalMessageHandler.Send(ConnectedClientsList[i].ClientId, MLAPIConstants.MLAPI_CUSTOM_MESSAGE, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                }
            }
            else
            {
                for (int i = 0; i < clientIds.Count; i++)
                {
                    InternalMessageHandler.Send(clientIds[i], MLAPIConstants.MLAPI_CUSTOM_MESSAGE, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                }
            }
        }

        /// <summary>
        /// Sends a custom message to a specific client
        /// </summary>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="channel">The channel tos end the data on</param>
        /// <param name="security">The security settings to apply to the message</param>
        public void SendCustomMessage(uint clientId, BitStream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CUSTOM_MESSAGE, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
        }

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
                    if (NetworkConfig.NetworkedPrefabs[i] != null && string.IsNullOrEmpty(NetworkConfig.NetworkedPrefabs[i].name))
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
                else NetworkConfig.PlayerPrefabName = NetworkConfig.NetworkedPrefabs.Find(x => x.playerPrefab == true).name;
            }
        }

        private object Init(bool server)
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Init()");
            LocalClientId = 0;
            NetworkTime = 0f;
            lastSendTickTime = 0f;
            lastEventTickTime = 0f;
            lastReceiveTickTime = 0f;
            eventOvershootCounter = 0f;
            PendingClients.Clear();
            ConnectedClients.Clear();
            ConnectedClientsList.Clear();
            messageBuffer = new byte[NetworkConfig.MessageBufferSize];
            
            ResponseMessageManager.Clear();
            MessageManager.channels.Clear();
            MessageManager.reverseChannels.Clear();
            SpawnManager.SpawnedObjects.Clear();
            SpawnManager.SpawnedObjectsList.Clear();
            SpawnManager.releasedNetworkObjectIds.Clear();
            SpawnManager.PendingSpawnObjects.Clear();
            NetworkPoolManager.Pools.Clear();
            NetworkPoolManager.PoolNamesToIndexes.Clear();
            NetworkSceneManager.registeredSceneNames.Clear();
            NetworkSceneManager.sceneIndexToString.Clear();
            NetworkSceneManager.sceneNameToIndex.Clear();
            NetworkSceneManager.sceneSwitchProgresses.Clear();

            try
            {
                if (server && !string.IsNullOrEmpty(NetworkConfig.ServerBase64PfxCertificate))
                {
                    NetworkConfig.ServerX509Certificate = new X509Certificate2(Convert.FromBase64String(NetworkConfig.ServerBase64PfxCertificate));
                    if (!NetworkConfig.ServerX509Certificate.HasPrivateKey)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The imported PFX file did not have a private key");
                    }
                }
            }
            catch (CryptographicException ex)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Importing of certificate failed: " + ex.ToString());
            }

            if (NetworkConfig.Transport == DefaultTransport.UNET)
                NetworkConfig.NetworkTransport = new UnetTransport();
            else if (NetworkConfig.Transport == DefaultTransport.MLAPI_Relay)
                NetworkConfig.NetworkTransport = new RelayedTransport();
            else if (NetworkConfig.Transport == DefaultTransport.Custom && NetworkConfig.NetworkTransport == null)
                throw new NullReferenceException("The current NetworkTransport is null");

            object settings = NetworkConfig.NetworkTransport.GetSettings(); //Gets a new "settings" object for the transport currently used.

            if (NetworkConfig.HandleObjectSpawning)
            {
                HashSet<string> networkedPrefabName = new HashSet<string>();
                for (int i = 0; i < NetworkConfig.NetworkedPrefabs.Count; i++)
                {
                    if (networkedPrefabName.Contains(NetworkConfig.NetworkedPrefabs[i].name))
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Duplicate NetworkedPrefabName " + NetworkConfig.NetworkedPrefabs[i].name);
                        continue;
                    }
                    networkedPrefabName.Add(NetworkConfig.NetworkedPrefabs[i].name);
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
                    Name = "MLAPI_DEFAULT_MESSAGE",
                    Type = ChannelType.Reliable
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

            NetworkConfig.RegisteredScenes.Sort();
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
            NetworkConfig.Channels = NetworkConfig.Channels.OrderBy(x => x.Name).ToList();
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

            return settings;
        }

        private void SpawnSceneObjects()
        {
            if (NetworkConfig.EnableSceneSwitching)
            {
                SpawnManager.MarkSceneObjects();
                if (IsServer && NetworkConfig.HandleObjectSpawning)
                {
                    NetworkedObject[] networkedObjects = FindObjectsOfType<NetworkedObject>();
                    for (int i = 0; i < networkedObjects.Length; i++)
                    {
                        if (networkedObjects[i].destroyWithScene == null || networkedObjects[i].destroyWithScene == true)
                            networkedObjects[i].Spawn();
                    }
                }
            }
        }

        /// <summary>
        /// Starts a server
        /// </summary>
        public void StartServer()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StartServer()");
            if (IsServer || IsClient)
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

            IsServer = true;
            IsClient = false;
            IsListening = true;

            SpawnSceneObjects();

            if (OnServerStarted != null)
                OnServerStarted.Invoke();
        }

        /// <summary>
        /// Starts a client
        /// </summary>
        public void StartClient()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StartClient()");
            if (IsServer || IsClient)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot start client while an instance is already running");
                return;
            }

            object settings = Init(false);
            byte error;
            ConnectedHostname = NetworkConfig.ConnectAddress;
            NetworkConfig.NetworkTransport.Connect(NetworkConfig.ConnectAddress, NetworkConfig.ConnectPort, settings, out error);
            IsServer = false;
            IsClient = true;
            IsListening = true;
        }

        /// <summary>
        /// Stops the running server
        /// </summary>
        public void StopServer()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopServer()");
            HashSet<uint> disconnectedIds = new HashSet<uint>();
            //Don't know if I have to disconnect the clients. I'm assuming the NetworkTransport does all the cleaning on shtudown. But this way the clients get a disconnect message from server (so long it does't get lost)
            foreach (KeyValuePair<uint, NetworkedClient> pair in ConnectedClients)
            {
                if(!disconnectedIds.Contains(pair.Key))
                {
                    disconnectedIds.Add(pair.Key);
					if (pair.Key == NetworkConfig.NetworkTransport.ServerClientId)
                        continue;

                    NetworkConfig.NetworkTransport.DisconnectClient(pair.Key);
                }
            }
            
            foreach (KeyValuePair<uint, PendingClient> pair in PendingClients)
            {
                if(!disconnectedIds.Contains(pair.Key))
                {
                    disconnectedIds.Add(pair.Key);
                    if (pair.Key == NetworkConfig.NetworkTransport.ServerClientId)
                        continue;

                    NetworkConfig.NetworkTransport.DisconnectClient(pair.Key);
                }
            }
            
            IsServer = false;
            Shutdown();
        }

        /// <summary>
        /// Stops the running host
        /// </summary>
        public void StopHost()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopHost()");
            IsServer = false;
            IsClient = false;
            StopServer();
            //We don't stop client since we dont actually have a transport connection to our own host. We just handle host messages directly in the MLAPI
        }

        /// <summary>
        /// Stops the running client
        /// </summary>
        public void StopClient()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopClient()");
            IsClient = false;
            NetworkConfig.NetworkTransport.DisconnectFromServer();
            IsConnectedClient = false;
            Shutdown();
        }

        /// <summary>
        /// Starts a Host
        /// </summary>
        public void StartHost(Vector3? pos = null, Quaternion? rot = null, int prefabId = -1)
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StartHost()");
            if (IsServer || IsClient)
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

            IsServer = true;
            IsClient = true;
            IsListening = true;

			uint hostClientId = NetworkConfig.NetworkTransport.ServerClientId;
            ConnectedClients.Add(hostClientId, new NetworkedClient()
            {
                ClientId = hostClientId
            });
            ConnectedClientsList.Add(ConnectedClients[hostClientId]);

            if (NetworkConfig.HandleObjectSpawning)
            {
                prefabId = prefabId == -1 ? SpawnManager.GetNetworkedPrefabIndexOfName(NetworkConfig.PlayerPrefabName) : prefabId;
                SpawnManager.CreateSpawnedObject(prefabId, 0, hostClientId, true, NetworkSceneManager.CurrentActiveSceneIndex, false, false, pos.GetValueOrDefault(), rot.GetValueOrDefault(), true, null, false, 0, false);
            }

            SpawnSceneObjects();

            if (OnServerStarted != null)
                OnServerStarted.Invoke();
        }

        private void OnEnable()
        {
            if (Singleton != null && Singleton != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Singleton = this;
                if (OnSingletonReady != null) OnSingletonReady();
                if (DontDestroy)
                    DontDestroyOnLoad(gameObject);
                if (RunInBackground)
                    Application.runInBackground = true;
            }
        }
        
        private void OnDestroy()
        {
            if (Singleton != null && Singleton == this)
            {
                Singleton = null;
                Shutdown();  
            }
        }

        private void Shutdown()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Shutdown()");
            NetworkProfiler.Stop();
            IsListening = false;
            IsServer = false;
            IsClient = false;
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
            if(IsListening)
            {
                if((NetworkTime - lastSendTickTime >= (1f / NetworkConfig.SendTickrate)) || NetworkConfig.SendTickrate <= 0)
                {
                    NetworkedObject.NetworkedVarPrepareSend();
                    foreach (KeyValuePair<uint, NetworkedClient> pair in ConnectedClients)
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
                        byte[] data = messageBuffer;
                        eventType = NetworkConfig.NetworkTransport.PollReceive(out clientId, out channelId, ref data, data.Length, out receivedSize, out error);

                        switch (eventType)
                        {
                            case NetEventType.Connect:
                                NetworkProfiler.StartEvent(TickType.Receive, (uint)receivedSize, MessageManager.reverseChannels[channelId], "TRANSPORT_CONNECT");
                                if (IsServer)
                                {
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Client Connected");
#if !DISABLE_CRYPTOGRAPHY
                                    if (NetworkConfig.EnableEncryption)
                                    {
                                        // This client is required to complete the crypto-hail exchange.
                                        using (PooledBitStream hailStream = PooledBitStream.Get())
                                        {
                                            using (PooledBitWriter hailWriter = PooledBitWriter.Get(hailStream))
                                            {
                                                if (NetworkConfig.SignKeyExchange)
                                                {
                                                    // Write certificate
                                                    hailWriter.WriteByteArray(NetworkConfig.ServerX509CertificateBytes);
                                                }

                                                // Write key exchange public part
                                                EllipticDiffieHellman diffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                                                byte[] diffieHellmanPublicPart = diffieHellman.GetPublicKey();
                                                hailWriter.WriteByteArray(diffieHellmanPublicPart);
                                                PendingClients.Add(clientId, new PendingClient()
                                                {
                                                    ClientId = clientId,
                                                    ConnectionState = PendingClient.State.PendingHail,
                                                    KeyExchange = diffieHellman
                                                });

                                                if (NetworkConfig.SignKeyExchange)
                                                {
                                                    // Write public part signature (signed by certificate private)
                                                    X509Certificate2 certificate = NetworkConfig.ServerX509Certificate;
                                                    if (!certificate.HasPrivateKey) throw new CryptographicException("[MLAPI] No private key was found in server certificate. Unable to sign key exchange");
                                                    RSACryptoServiceProvider rsa = certificate.PrivateKey as RSACryptoServiceProvider;

                                                    if (rsa != null)
                                                    {
                                                        using (SHA256Managed sha = new SHA256Managed())
                                                        {
                                                            hailWriter.WriteByteArray(rsa.SignData(diffieHellmanPublicPart, sha));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        throw new CryptographicException("[MLAPI] Only RSA certificates are supported. No valid RSA key was found");
                                                    }
                                                }
                                            }
                                            // Send the hail
                                            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CERTIFICATE_HAIL, "MLAPI_INTERNAL", hailStream, SecuritySendFlags.None, true);
                                        }
                                    }
                                    else
                                    {
#endif
                                        PendingClients.Add(clientId, new PendingClient()
                                        {
                                            ClientId = clientId,
                                            ConnectionState = PendingClient.State.PendingConnection
                                        });
#if !DISABLE_CRYPTOGRAPHY
                                    }
#endif
                                    StartCoroutine(ApprovalTimeout(clientId));
                                }
                                else
                                {
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Connected");
                                    if (!NetworkConfig.EnableEncryption) SendConnectionRequest();
                                    StartCoroutine(ApprovalTimeout(clientId));
                                }
                                NetworkProfiler.EndEvent();
                                break;
                            case NetEventType.Data:
                                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo($"Incoming Data From {clientId} : {receivedSize} bytes");

                                HandleIncomingData(clientId, data, channelId, receivedSize);
                                break;
                            case NetEventType.Disconnect:
                                NetworkProfiler.StartEvent(TickType.Receive, 0, "NONE", "TRANSPORT_DISCONNECT");
                                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Disconnect Event From " + clientId);

                                if (IsServer)
                                    OnClientDisconnectFromServer(clientId);
                                else
                                {
                                    IsConnectedClient = false;
                                    StopClient();
                                }

                                if (OnClientDisconnectCallback != null)
                                    OnClientDisconnectCallback.Invoke(clientId);
                                NetworkProfiler.EndEvent();
                                break;
                        }
                        // Only do another iteration if: there are no more messages AND (there is no limit to max events or we have processed less than the maximum)
                    } while (IsListening && (eventType != NetEventType.Nothing && (NetworkConfig.MaxReceiveEventsPerTickRate <= 0 || processedEvents < NetworkConfig.MaxReceiveEventsPerTickRate)));
                    lastReceiveTickTime = NetworkTime;
                    NetworkProfiler.EndTick();
                }

                if (IsServer && ((NetworkTime - lastEventTickTime >= (1f / NetworkConfig.EventTickrate))))
                {
                    NetworkProfiler.StartTick(TickType.Event);
                    eventOvershootCounter += ((NetworkTime - lastEventTickTime) - (1f / NetworkConfig.EventTickrate));
                    LagCompensationManager.AddFrames();
                    ResponseMessageManager.CheckTimeouts();
                    lastEventTickTime = NetworkTime;
                    NetworkProfiler.EndTick();
                }
                else if (IsServer && eventOvershootCounter >= ((1f / NetworkConfig.EventTickrate)))
                {
                    NetworkProfiler.StartTick(TickType.Event);
                    //We run this one to compensate for previous update overshoots.
                    eventOvershootCounter -= (1f / NetworkConfig.EventTickrate);
                    LagCompensationManager.AddFrames();
                    NetworkProfiler.EndTick();
                }

                if (IsServer && NetworkConfig.EnableTimeResync && NetworkTime - lastTimeSyncTime >= 30)
                {
                    NetworkProfiler.StartTick(TickType.Event);
                    SyncTime();
                    lastTimeSyncTime = NetworkTime;
                    NetworkProfiler.EndTick();
                }

                NetworkTime += Time.unscaledDeltaTime;
            }
        }

        internal void SendConnectionRequest()
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(NetworkConfig.GetConfig());

                    if (NetworkConfig.ConnectionApproval)
                        writer.WriteByteArray(NetworkConfig.ConnectionData);
                }

                InternalMessageHandler.Send(ServerClientId, MLAPIConstants.MLAPI_CONNECTION_REQUEST, "MLAPI_INTERNAL", stream, SecuritySendFlags.Authenticated | SecuritySendFlags.Encrypted, true);
            }
        }

        private IEnumerator ApprovalTimeout(uint clientId)
        {
            float timeStarted = NetworkTime;
            //We yield every frame incase a pending client disconnects and someone else gets its connection id
            while (NetworkTime - timeStarted < NetworkConfig.ClientConnectionBufferTimeout && PendingClients.ContainsKey(clientId))
            {
                yield return null;
            }
            
            if (PendingClients.ContainsKey(clientId) && !ConnectedClients.ContainsKey(clientId))
            {
                // Timeout
                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Client " + clientId + " Handshake Timed Out");
                DisconnectClient(clientId);
            }
        }

        internal IEnumerator TimeOutSwitchSceneProgress(SceneSwitchProgress switchSceneProgress)
        {
            yield return new WaitForSeconds(this.NetworkConfig.LoadSceneTimeOut);
            switchSceneProgress.SetTimedOut();
        }

        private void HandleIncomingData(uint clientId, byte[] data, int channelId, int totalSize)
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Unwrapping Data Header");

            using (BitStream inputStream = new BitStream(data))
            {
                inputStream.SetLength(totalSize);
                
                using (BitStream messageStream = MessageManager.UnwrapMessage(inputStream, clientId, out byte messageType, out SecuritySendFlags security))
                {
                    if (messageStream == null)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Message unwrap could not be completed. Was the header corrupt? Crypto error?");
                        return;
                    }
                    else if (messageType == MLAPIConstants.INVALID)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Message unwrap read an invalid messageType");
                        return;
                    }

                    uint headerByteSize = (uint)Arithmetic.VarIntSize(messageType);
                    NetworkProfiler.StartEvent(TickType.Receive, (uint)(totalSize - headerByteSize), channelId, messageType);

                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Data Header: messageType=" + messageType);

                    // Client tried to send a network message that was not the connection request before he was accepted.
                    if (IsServer && (NetworkConfig.EnableEncryption && PendingClients.ContainsKey(clientId) && PendingClients[clientId].ConnectionState == PendingClient.State.PendingHail && messageType != MLAPIConstants.MLAPI_CERTIFICATE_HAIL_RESPONSE) ||
                        (PendingClients.ContainsKey(clientId) && PendingClients[clientId].ConnectionState == PendingClient.State.PendingConnection && messageType != MLAPIConstants.MLAPI_CONNECTION_REQUEST))
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Message recieved from clientId " + clientId + " before it has been accepted");
                        return;
                    }

                    #region INTERNAL MESSAGE

                    switch (messageType)
                    {
                        case MLAPIConstants.MLAPI_CONNECTION_REQUEST:
                            if (IsServer)
                                InternalMessageHandler.HandleConnectionRequest(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_CONNECTION_APPROVED:
                            if (IsClient)
                                InternalMessageHandler.HandleConnectionApproved(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_ADD_OBJECT:
                            if (IsClient) InternalMessageHandler.HandleAddObject(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_CLIENT_DISCONNECT:
                            if (IsClient)
                                InternalMessageHandler.HandleClientDisconnect(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_DESTROY_OBJECT:
                            if (IsClient) InternalMessageHandler.HandleDestroyObject(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_SWITCH_SCENE:
                            if (IsClient) InternalMessageHandler.HandleSwitchScene(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_SPAWN_POOL_OBJECT:
                            if (IsClient) InternalMessageHandler.HandleSpawnPoolObject(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_DESTROY_POOL_OBJECT:
                            if (IsClient)
                                InternalMessageHandler.HandleDestroyPoolObject(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_CHANGE_OWNER:
                            if (IsClient) InternalMessageHandler.HandleChangeOwner(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_ADD_OBJECTS:
                            if (IsClient) InternalMessageHandler.HandleAddObjects(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_TIME_SYNC:
                            if (IsClient) InternalMessageHandler.HandleTimeSync(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_NETWORKED_VAR_DELTA:
                            InternalMessageHandler.HandleNetworkedVarDelta(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_NETWORKED_VAR_UPDATE:
                            InternalMessageHandler.HandleNetworkedVarUpdate(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_SERVER_RPC:
                            if (IsServer) InternalMessageHandler.HandleServerRPC(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_SERVER_RPC_REQUEST:
                            if (IsServer) InternalMessageHandler.HandleServerRPCRequest(clientId, messageStream, channelId, security);
                            break;
                        case MLAPIConstants.MLAPI_SERVER_RPC_RESPONSE:
                            if (IsClient) InternalMessageHandler.HandleServerRPCResponse(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_CLIENT_RPC:
                            if (IsClient) InternalMessageHandler.HandleClientRPC(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_CLIENT_RPC_REQUEST:
                            if (IsClient) InternalMessageHandler.HandleClientRPCRequest(clientId, messageStream, channelId, security);
                            break;
                        case MLAPIConstants.MLAPI_CLIENT_RPC_RESPONSE:
                            if (IsServer) InternalMessageHandler.HandleClientRPCResponse(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_CUSTOM_MESSAGE:
                            InternalMessageHandler.HandleCustomMessage(clientId, messageStream, channelId);
                            break;
#if !DISABLE_CRYPTOGRAPHY
                        case MLAPIConstants.MLAPI_CERTIFICATE_HAIL:
                            if (IsClient) InternalMessageHandler.HandleHailRequest(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_CERTIFICATE_HAIL_RESPONSE:
                            if (IsServer) InternalMessageHandler.HandleHailResponse(clientId, messageStream, channelId);
                            break;
                        case MLAPIConstants.MLAPI_GREETINGS:
                            if (IsClient) InternalMessageHandler.HandleGreetings(clientId, messageStream, channelId);
                            break;
    #endif
                        case MLAPIConstants.MLAPI_CLIENT_SWITCH_SCENE_COMPLETED:
                            if (IsServer) InternalMessageHandler.HandleClientSwitchSceneCompleted(clientId, messageStream, channelId);
                            break;
                        default:
                            if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Read unrecognized messageType " + messageType);
                            break;
                    }

                    #endregion

                    NetworkProfiler.EndEvent();
                }
            }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal void DisconnectClient(uint clientId)
        {
            if (!IsServer)
                return;

            if (ConnectedClients.ContainsKey(clientId))
                ConnectedClients.Remove(clientId);

            if (PendingClients.ContainsKey(clientId))
                PendingClients.Remove(clientId);

            for (int i = ConnectedClientsList.Count - 1; i > -1; i--)
            {
                if (ConnectedClientsList[i].ClientId == clientId)
                    ConnectedClientsList.RemoveAt(i);
            }

            NetworkConfig.NetworkTransport.DisconnectClient(clientId);
        }

        internal void OnClientDisconnectFromServer(uint clientId)
        {
            if (PendingClients.ContainsKey(clientId))
                PendingClients.Remove(clientId);
            
            if (ConnectedClients.ContainsKey(clientId))
            {
                if (IsServer && NetworkConfig.HandleObjectSpawning)
                {
                    if (ConnectedClients[clientId].PlayerObject != null)
                        Destroy(ConnectedClients[clientId].PlayerObject.gameObject);
                    
                    for (int i = 0; i < ConnectedClients[clientId].OwnedObjects.Count; i++)
                    {
                        if (ConnectedClients[clientId].OwnedObjects[i] != null)
                        {
                            if (!ConnectedClients[clientId].OwnedObjects[i].DontDestroyWithOwner)
                            {
                                Destroy(ConnectedClients[clientId].OwnedObjects[i].gameObject);
                            }
                            else
                            {
                                ConnectedClients[clientId].OwnedObjects[i].RemoveOwnership();
                            }
                        }
                    }
                }

                for (int i = 0; i < ConnectedClientsList.Count; i++)
                {
                    if (ConnectedClientsList[i].ClientId == clientId)
                    {
                        ConnectedClientsList.RemoveAt(i);
                        break;
                    }
                }
                
                ConnectedClients.Remove(clientId);
            }

            if (IsServer)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt32Packed(clientId);
                        InternalMessageHandler.Send(MLAPIConstants.MLAPI_CLIENT_DISCONNECT, "MLAPI_INTERNAL", clientId, stream, SecuritySendFlags.None);
                    }
                }
            }
        }

        private void SyncTime()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Syncing Time To Clients");
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteSinglePacked(NetworkTime);
                    int timestamp = NetworkConfig.NetworkTransport.GetNetworkTimestamp();
                    writer.WriteInt32Packed(timestamp);
                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_TIME_SYNC, "MLAPI_TIME_SYNC", stream, SecuritySendFlags.None);
                }
            }
        }

        internal void HandleApproval(uint clientId, int prefabId, bool approved, Vector3? position, Quaternion? rotation)
        {
            if(approved)
            {
                // Inform new client it got approved   
                byte[] aesKey = PendingClients.ContainsKey(clientId) ? PendingClients[clientId].AesKey : null;
                if (PendingClients.ContainsKey(clientId))
                    PendingClients.Remove(clientId);
                NetworkedClient client = new NetworkedClient()
                {
                    ClientId = clientId,
#if !DISABLE_CRYPTOGRAPHY
                    AesKey = aesKey
#endif
                };
                ConnectedClients.Add(clientId, client);
                ConnectedClientsList.Add(client);

                NetworkedObject netObject = null;
                if(NetworkConfig.HandleObjectSpawning)
                {
                    prefabId = prefabId == -1 ? SpawnManager.GetNetworkedPrefabIndexOfName(NetworkConfig.PlayerPrefabName) : prefabId;
                    netObject = SpawnManager.CreateSpawnedObject(prefabId, 0, clientId, true, NetworkSceneManager.CurrentActiveSceneIndex, false, false, position, rotation, true, null, false, 0, false);
                    ConnectedClients[clientId].PlayerObject = netObject;
                }

                int amountOfObjectsToSend = SpawnManager.SpawnedObjects.Values.Count;

                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt32Packed(clientId);
                        if (NetworkConfig.EnableSceneSwitching) 
                        {
                            writer.WriteUInt32Packed(NetworkSceneManager.currentSceneIndex);
                            writer.WriteByteArray(NetworkSceneManager.currentSceneSwitchProgressGuid.ToByteArray());
                        }

                        writer.WriteSinglePacked(NetworkTime);
                        writer.WriteInt32Packed(NetworkConfig.NetworkTransport.GetNetworkTimestamp());

                        writer.WriteInt32Packed(ConnectedClients.Count - 1);

                        foreach (KeyValuePair<uint, NetworkedClient> item in ConnectedClients)
                        {
                            //Our own ID. Already added as the first one above
                            if (item.Key == clientId)
                                continue;
                            writer.WriteUInt32Packed(item.Key); //ClientId
                        }
                        if (NetworkConfig.HandleObjectSpawning)
                        {
                            writer.WriteInt32Packed(amountOfObjectsToSend);

                            foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.SpawnedObjects)
                            {
                                writer.WriteBool(pair.Value.IsPlayerObject);
                                writer.WriteUInt32Packed(pair.Value.NetworkId);
                                writer.WriteUInt32Packed(pair.Value.OwnerClientId);
                                writer.WriteUInt64Packed(pair.Value.NetworkedPrefabHash);
                                writer.WriteBool(pair.Value.gameObject.activeInHierarchy);

                                writer.WriteBit(pair.Value.destroyWithScene == null ? true : pair.Value.destroyWithScene.Value);
                                writer.WriteBool(pair.Value.SceneDelayedSpawn);
                                writer.WriteUInt32Packed(pair.Value.sceneSpawnedInIndex);

                                writer.WriteSinglePacked(pair.Value.transform.position.x);
                                writer.WriteSinglePacked(pair.Value.transform.position.y);
                                writer.WriteSinglePacked(pair.Value.transform.position.z);

                                writer.WriteSinglePacked(pair.Value.transform.rotation.eulerAngles.x);
                                writer.WriteSinglePacked(pair.Value.transform.rotation.eulerAngles.y);
                                writer.WriteSinglePacked(pair.Value.transform.rotation.eulerAngles.z);

                                pair.Value.WriteNetworkedVarData(stream, clientId);
                            }
                        }

                        InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CONNECTION_APPROVED, "MLAPI_INTERNAL", stream, SecuritySendFlags.Encrypted | SecuritySendFlags.Authenticated, true);

                        if (OnClientConnectedCallback != null)
                            OnClientConnectedCallback.Invoke(clientId);
                    }
                }

                //Inform old clients of the new player

                foreach (var clientPair in ConnectedClients)
                {
                    if (clientPair.Key == clientId)
                        continue; //The new client.

                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                        {
                            if (NetworkConfig.HandleObjectSpawning)
                            {
                                writer.WriteBool(true);
                                writer.WriteUInt32Packed(ConnectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().NetworkId);
                                writer.WriteUInt32Packed(clientId);
                                writer.WriteUInt64Packed(NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[prefabId].hash);
                                writer.WriteBool(false);

                                writer.WriteBool(ConnectedClients[clientId].PlayerObject.SceneDelayedSpawn);
                                writer.WriteUInt32Packed(ConnectedClients[clientId].PlayerObject.sceneSpawnedInIndex);

                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.x);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.y);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.z);

                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.x);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.y);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.z);

                                writer.WriteBool(false); //No payload data

                                ConnectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().WriteNetworkedVarData(stream, clientPair.Key);
                            }
                            else
                            {
                                writer.WriteUInt32Packed(clientId);
                            }
                            InternalMessageHandler.Send(clientPair.Key, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
                        }
                    }
                }
            }
            else
            {
                if (PendingClients.ContainsKey(clientId))
                    PendingClients.Remove(clientId);

                NetworkConfig.NetworkTransport.DisconnectClient(clientId);
            }
        }
    }
}
