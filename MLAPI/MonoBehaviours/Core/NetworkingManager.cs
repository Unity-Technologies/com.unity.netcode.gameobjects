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
using MLAPI.Collections;
using MLAPI.Components;
using MLAPI.Configuration;
using MLAPI.Cryptography;
using MLAPI.Internal;
using MLAPI.Profiling;
using MLAPI.Serialization;
using MLAPI.Transports;
using MLAPI.Transports.UNET;
using BitStream = MLAPI.Serialization.BitStream;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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
        /// <summary>
        /// The singleton instance of the NetworkingManager
        /// </summary>
        public static NetworkingManager singleton { get; private set; }
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
                if (isServer) return NetworkConfig.NetworkTransport.ServerClientId;
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
        internal readonly HashSet<uint> connectionPendingClients = new HashSet<uint>();
        internal readonly HashSet<uint> hailPendingClients = new HashSet<uint>();
        /// <summary>
        /// Gets wheter or not a server is running
        /// </summary>
        public bool isServer { get; internal set; }
        /// <summary>
        /// Gets wheter or not a client is running
        /// </summary>
        public bool isClient { get; internal set; }
        /// <summary>
        /// Gets if we are running as host
        /// </summary>
        public bool isHost => isServer && isClient;
        /// <summary>
        /// Gets wheter or not we are listening for connections
        /// </summary>
        public bool isListening { get; internal set; }
        private byte[] messageBuffer;
        /// <summary>
        /// Gets if we are connected as a client
        /// </summary>
        public bool isConnectedClients { get; internal set; }
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
        public delegate void ConnectionApprovedDelegate(uint clientId, int prefabId, bool approved, Vector3 position, Quaternion rotation);
        /// <summary>
        /// The callback to invoke during connection approval
        /// </summary>
        public Action<byte[], uint, ConnectionApprovedDelegate> ConnectionApprovalCallback = null;
        /// <summary>
        /// The current NetworkingConfiguration
        /// </summary>
        public NetworkConfig NetworkConfig;
        /// <summary>
        /// Delegate used for incomming custom messages
        /// </summary>
        /// <param name="clientId">The clientId that sent the message</param>
        /// <param name="stream">The stream containing the message data</param>
        public delegate void CustomMessageDelegete(uint clientId, Stream stream);
        /// <summary>
        /// Event invoked when custom messages arrive
        /// </summary>
        public event CustomMessageDelegete OnIncommingCustomMessage;
        public string ConnectedHostname { get; private set; }
        internal static event Action OnSingletonReady;

        internal void InvokeOnIncommingCustomMessage(uint clientId, Stream stream)
        {
            if (OnIncommingCustomMessage != null) OnIncommingCustomMessage(clientId, stream);
        }

        /// <summary>
        /// Sends custom message to a list of clients
        /// </summary>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="channel">The channel to send the data on</param>
        public void SendCustomMessage(List<uint> clientIds, Stream stream, string channel = "MLAPI_DEFAULT_MESSAGE")
        {
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogWarning("Can not send custom message to multiple users as a client");
                return;
            }
            if (clientIds == null)
            {
                for (int i = 0; i < ConnectedClientsList.Count; i++)
                {
                    InternalMessageHandler.Send(ConnectedClientsList[i].ClientId, MLAPIConstants.MLAPI_CUSTOM_MESSAGE, channel, stream, new InternalSecuritySendOptions(false, false));
                }
            }
            else
            {
                for (int i = 0; i < clientIds.Count; i++)
                {
                    InternalMessageHandler.Send(clientIds[i], MLAPIConstants.MLAPI_CUSTOM_MESSAGE, channel, stream, new InternalSecuritySendOptions(false, false));
                }
            }
        }

        /// <summary>
        /// Sends a custom message to a specific client
        /// </summary>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="channel">The channel tos end the data on</param>
        public void SendCustomMessage(uint clientId, Stream stream, string channel = "MLAPI_DEFAULT_MESSAGE")
        {
            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CUSTOM_MESSAGE, channel, stream, new InternalSecuritySendOptions(false, false));
        }


#if !DISABLE_CRYPTOGRAPHY
        internal readonly Dictionary<uint, EllipticDiffieHellman> pendingKeyExchanges = new Dictionary<uint, EllipticDiffieHellman>();
        internal byte[] clientAesKey;
        internal readonly Dictionary<uint, byte[]> pendingClientAesKeys = new Dictionary<uint, byte[]>();
#endif

        /// <summary>
        /// An inspector bool that acts as a Trigger for regenerating RSA keys. Should not be used outside Unity editor.
        /// </summary>
        public bool RegenerateRSAKeys = false;

        private void OnValidate()
        {
            if (NetworkConfig == null)
                return; //May occur when the component is added            

            //Sort lists
            if (NetworkConfig.Channels != null)
                NetworkConfig.Channels = NetworkConfig.Channels.OrderBy(x => x.Name).ToList();
            if (NetworkConfig.NetworkedPrefabs != null)
                NetworkConfig.NetworkedPrefabs = NetworkConfig.NetworkedPrefabs.OrderBy(x => x.name).ToList(); 
            if (NetworkConfig.RegisteredScenes != null)
                NetworkConfig.RegisteredScenes.Sort();

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

            /*
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
            */
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
            connectionPendingClients.Clear();
            ConnectedClients.Clear();
            ConnectedClientsList.Clear();
            messageBuffer = new byte[NetworkConfig.MessageBufferSize];
#if !DISABLE_CRYPTOGRAPHY
            pendingKeyExchanges.Clear();
#endif
            MessageManager.channels.Clear();
            MessageManager.reverseChannels.Clear();
            SpawnManager.SpawnedObjects.Clear();
            SpawnManager.SpawnedObjectsList.Clear();
            SpawnManager.releasedNetworkObjectIds.Clear();
            NetworkPoolManager.Pools.Clear();
            NetworkPoolManager.PoolNamesToIndexes.Clear();
            NetworkSceneManager.registeredSceneNames.Clear();
            NetworkSceneManager.sceneIndexToString.Clear();
            NetworkSceneManager.sceneNameToIndex.Clear();

            if (server) NetworkConfig.ServerX509Certificate = new X509Certificate2(NetworkConfig.ServerCertificatePfx);

            if (NetworkConfig.Transport == DefaultTransport.UNET)
                NetworkConfig.NetworkTransport = new UnetTransport();
            else if (NetworkConfig.Transport == DefaultTransport.MLAPI_Relay)
                NetworkConfig.NetworkTransport = new RelayedTransport();
            else if (NetworkConfig.Transport == DefaultTransport.Custom && NetworkConfig.NetworkTransport == null)
                throw new NullReferenceException("The current NetworkTransport is null");

            object settings = NetworkConfig.NetworkTransport.GetSettings(); //Gets a new "settings" object for the transport currently used.

            if (NetworkConfig.HandleObjectSpawning)
            {
                NetworkConfig.NetworkPrefabIds = new Dictionary<string, int>();
                NetworkConfig.NetworkPrefabNames = new Dictionary<int, string>();
                NetworkConfig.NetworkedPrefabs = NetworkConfig.NetworkedPrefabs.OrderBy(x => x.name).ToList();
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
                if (isServer && NetworkConfig.HandleObjectSpawning)
                {
                    NetworkedObject[] networkedObjects = FindObjectsOfType<NetworkedObject>();
                    for (int i = 0; i < networkedObjects.Length; i++)
                    {
                        if (networkedObjects[i].sceneObject == null || networkedObjects[i].sceneObject == true)
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

            isServer = true;
            isClient = false;
            isListening = true;

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
            if (isServer || isClient)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot start client while an instance is already running");
                return;
            }

            object settings = Init(false);
            byte error;
            ConnectedHostname = NetworkConfig.ConnectAddress;
            NetworkConfig.NetworkTransport.Connect(NetworkConfig.ConnectAddress, NetworkConfig.ConnectPort, settings, out error);
            isServer = false;
            isClient = true;
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
            foreach (uint clientId in connectionPendingClients)
            {
                if (!disconnectedIds.Contains(clientId))
                {
                    disconnectedIds.Add(clientId);
					if (clientId == NetworkConfig.NetworkTransport.ServerClientId)
                        continue;
					
                    NetworkConfig.NetworkTransport.DisconnectClient(clientId);
                }
            }
            isServer = false;
            Shutdown();
        }

        /// <summary>
        /// Stops the running host
        /// </summary>
        public void StopHost()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopHost()");
            isServer = false;
            isClient = false;
            StopServer();
            //We don't stop client since we dont actually have a transport connection to our own host. We just handle host messages directly in the MLAPI
        }

        /// <summary>
        /// Stops the running client
        /// </summary>
        public void StopClient()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("StopClient()");
            isClient = false;
            NetworkConfig.NetworkTransport.DisconnectFromServer();
            isConnectedClients = false;
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

            isServer = true;
            isClient = true;
            isListening = true;

			uint hostClientId = NetworkConfig.NetworkTransport.ServerClientId;
            ConnectedClients.Add(hostClientId, new NetworkedClient()
            {
                ClientId = hostClientId
            });
            ConnectedClientsList.Add(ConnectedClients[hostClientId]);

            if (NetworkConfig.HandleObjectSpawning)
            {
                prefabId = prefabId == -1 ? NetworkConfig.NetworkPrefabIds[NetworkConfig.PlayerPrefabName] : prefabId;
                SpawnManager.CreateSpawnedObject(prefabId, 0, hostClientId, true, pos.GetValueOrDefault(), rot.GetValueOrDefault(), null, false, false);
            }

            SpawnSceneObjects();

            if (OnServerStarted != null)
                OnServerStarted.Invoke();
        }

        private void OnEnable()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                singleton = this;
                if (OnSingletonReady != null) OnSingletonReady();
                if (DontDestroy)
                    DontDestroyOnLoad(gameObject);
                if (RunInBackground)
                    Application.runInBackground = true;
            }
        }
        
        private void OnDestroy()
        {
            if (singleton != null && singleton == this)
            {
                singleton = null;
                Shutdown();  
            }
        }

        private void Shutdown()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Shutdown()");
            NetworkProfiler.Stop();
            isListening = false;
            isServer = false;
            isClient = false;
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
                        eventType = NetworkConfig.NetworkTransport.PollReceive(out clientId, out channelId, ref messageBuffer, messageBuffer.Length, out receivedSize, out error);

                        switch (eventType)
                        {
                            case NetEventType.Connect:
                                NetworkProfiler.StartEvent(TickType.Receive, (uint)receivedSize, MessageManager.reverseChannels[channelId], "TRANSPORT_CONNECT");
                                if (isServer)
                                {
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Client Connected");
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
                                                pendingKeyExchanges.Add(clientId, diffieHellman);

                                                if (NetworkConfig.SignKeyExchange)
                                                {
                                                    // Write public part signature (signed by certificate private)
                                                    X509Certificate2 certificate = NetworkConfig.ServerX509Certificate;
                                                    if (!certificate.HasPrivateKey) throw new CryptographicException("[MLAPI] No private key was found in server certificate. Unable to sign key exchange");
                                                    RSACryptoServiceProvider rsa = certificate.PrivateKey as RSACryptoServiceProvider;
                                                    DSACryptoServiceProvider dsa = certificate.PrivateKey as DSACryptoServiceProvider;

                                                    if (rsa != null)
                                                    {
                                                        hailWriter.WriteByteArray(rsa.SignData(diffieHellmanPublicPart, new SHA256Managed()));
                                                    }
                                                    else if (dsa != null)
                                                    {
                                                        using (SHA256Managed sha = new SHA256Managed())
                                                        {
                                                            hailWriter.WriteByteArray(dsa.CreateSignature(sha.ComputeHash(diffieHellmanPublicPart)));
                                                        }
                                                    }
                                                }
                                            }
                                            // Send the hail
                                            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CERTIFICATE_HAIL, "MLAPI_INTERNAL", hailStream, new InternalSecuritySendOptions(false, false), true);
                                        }
                                        hailPendingClients.Add(clientId);
                                    }
                                    else
                                    {
                                        connectionPendingClients.Add(clientId);
                                    }

                                    StartCoroutine(ApprovalTimeout(clientId));
                                }
                                else
                                {
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Connected");
                                    SendConnectionRequest();
                                }
                                NetworkProfiler.EndEvent();
                                break;
                            case NetEventType.Data:
                                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo($"Incomming Data From {clientId} : {receivedSize} bytes");

                                HandleIncomingData(clientId, messageBuffer, channelId, receivedSize);
                                break;
                            case NetEventType.Disconnect:
                                NetworkProfiler.StartEvent(TickType.Receive, 0, "NONE", "TRANSPORT_DISCONNECT");
                                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Disconnect Event From " + clientId);

                                if (isServer)
                                    OnClientDisconnectFromServer(clientId);
                                else
                                {
                                    isConnectedClients = false;
                                    StopClient();
                                }

                                if (OnClientDisconnectCallback != null)
                                    OnClientDisconnectCallback.Invoke(clientId);
                                NetworkProfiler.EndEvent();
                                break;
                        }
                        // Only do another iteration if: there are no more messages AND (there is no limit to max events or we have processed less than the maximum)
                    } while (isListening && (eventType != NetEventType.Nothing && (NetworkConfig.MaxReceiveEventsPerTickRate <= 0 || processedEvents < NetworkConfig.MaxReceiveEventsPerTickRate)));
                    lastReceiveTickTime = NetworkTime;
                    NetworkProfiler.EndTick();
                }

                if (isServer && ((NetworkTime - lastEventTickTime >= (1f / NetworkConfig.EventTickrate))))
                {
                    NetworkProfiler.StartTick(TickType.Event);
                    eventOvershootCounter += ((NetworkTime - lastEventTickTime) - (1f / NetworkConfig.EventTickrate));
                    LagCompensationManager.AddFrames();
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

                if (isServer && NetworkConfig.EnableTimeResync && NetworkTime - lastTimeSyncTime >= 30)
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

                InternalMessageHandler.Send(ServerClientId, MLAPIConstants.MLAPI_CONNECTION_REQUEST, "MLAPI_INTERNAL", stream, new InternalSecuritySendOptions(true, false), true);
            }
        }

        private IEnumerator ApprovalTimeout(uint clientId)
        {
            float timeStarted = NetworkTime;
            //We yield every frame incase a pending client disconnects and someone else gets its connection id
            while (NetworkTime - timeStarted < NetworkConfig.ClientConnectionBufferTimeout && connectionPendingClients.Contains(clientId))
            {
                yield return null;
            }
            if(connectionPendingClients.Contains(clientId) && !ConnectedClients.ContainsKey(clientId))
            {
                //Timeout
                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Client " + clientId + " Handshake Timed Out");
                DisconnectClient(clientId);
            }
        }

        private readonly byte[] IVBuffer = new byte[16];
        private readonly byte[] HMACBuffer = new byte[32];
        private void HandleIncomingData(uint clientId, byte[] data, int channelId, int totalSize)
        {
            using (BitStream bitStream = new BitStream(data))
            {
                RijndaelManaged rijndael = null;
                Stream stream = bitStream;
                try
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Unwrapping Data Header");
                    bitStream.SetLength(totalSize);
                    using (PooledBitReader headerReader = PooledBitReader.Get(bitStream))
                    {
                        byte messageType;
                        bool encrypted = headerReader.ReadBit();
                        bool authenticated = headerReader.ReadBit();
                        if (encrypted && NetworkConfig.EnableEncryption)
                        {
                            headerReader.SkipPadBits();
                            headerReader.ReadByteArray(IVBuffer, 16);
                            rijndael = new RijndaelManaged();
                            rijndael.Key = isServer ? ConnectedClients[clientId].AesKey : clientAesKey;
                            rijndael.IV = IVBuffer;
                            stream = new CryptoStream(bitStream, rijndael.CreateDecryptor(), CryptoStreamMode.Read);
                            using (PooledBitReader reader = PooledBitReader.Get(stream))
                            {
                                messageType = reader.ReadByteDirect();
                            }
                        }
                        else if (authenticated && NetworkConfig.EnableEncryption)
                        {
                            headerReader.SkipPadBits();
                            using (HMACSHA256 hmac = new HMACSHA256(isServer ? ConnectedClients[clientId].AesKey : clientAesKey))
                            {
                                // 1 is the size of the header. 32 is the size of the hmac
                                headerReader.ReadByteArray(HMACBuffer, 32);
                                byte[] hmacBytes = hmac.ComputeHash(bitStream.GetBuffer(), 1 + 32, totalSize - (1 + 32));
                                for (int i = 0; i < hmacBytes.Length; i++)
                                {
                                    if (hmacBytes[i] != HMACBuffer[i])
                                    {
                                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("HMAC authentication code did not match");
                                        return;
                                    }
                                }
                            }
                            messageType = headerReader.ReadByteDirect();
                        }
                        else
                        {
                            messageType = headerReader.ReadByteBits(6);
                        }

                        uint headerByteSize = (uint) Arithmetic.VarIntSize(messageType);
                        NetworkProfiler.StartEvent(TickType.Receive, (uint) (totalSize - headerByteSize), channelId, messageType);

                        if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Data Header: messageType=" + messageType);

                        //Client tried to send a network message that was not the connection request before he was accepted.
                        if (isServer && (NetworkConfig.EnableEncryption && hailPendingClients.Contains(clientId) && messageType != MLAPIConstants.MLAPI_CERTIFICATE_HAIL_RESPONSE) ||
                                        (connectionPendingClients.Contains(clientId) && messageType != MLAPIConstants.MLAPI_CONNECTION_REQUEST))
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Message recieved from clientId " + clientId + " before it has been accepted");
                            return;
                        }

                        #region INTERNAL MESSAGE

                        switch (messageType)
                        {
                            case MLAPIConstants.MLAPI_CONNECTION_REQUEST:
                                if (isServer)
                                    InternalMessageHandler.HandleConnectionRequest(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_CONNECTION_APPROVED:
                                if (isClient)
                                    InternalMessageHandler.HandleConnectionApproved(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_ADD_OBJECT:
                                if (isClient) InternalMessageHandler.HandleAddObject(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_CLIENT_DISCONNECT:
                                if (isClient)
                                    InternalMessageHandler.HandleClientDisconnect(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_DESTROY_OBJECT:
                                if (isClient) InternalMessageHandler.HandleDestroyObject(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_SWITCH_SCENE:
                                if (isClient) InternalMessageHandler.HandleSwitchScene(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_SPAWN_POOL_OBJECT:
                                if (isClient) InternalMessageHandler.HandleSpawnPoolObject(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_DESTROY_POOL_OBJECT:
                                if (isClient)
                                    InternalMessageHandler.HandleDestroyPoolObject(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_CHANGE_OWNER:
                                if (isClient) InternalMessageHandler.HandleChangeOwner(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_ADD_OBJECTS:
                                if (isClient) InternalMessageHandler.HandleAddObjects(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_TIME_SYNC:
                                if (isClient) InternalMessageHandler.HandleTimeSync(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_NETWORKED_VAR_DELTA:
                                InternalMessageHandler.HandleNetworkedVarDelta(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_NETWORKED_VAR_UPDATE:
                                InternalMessageHandler.HandleNetworkedVarUpdate(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_SERVER_RPC:
                                if (isServer) InternalMessageHandler.HandleServerRPC(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_CLIENT_RPC:
                                if (isClient) InternalMessageHandler.HandleClientRPC(clientId, stream, channelId);
                                break;
                            case MLAPIConstants.MLAPI_CUSTOM_MESSAGE:
                                InternalMessageHandler.HandleCustomMessage(clientId, stream, channelId);
                                break;
                        }

                        #endregion

                        NetworkProfiler.EndEvent();
                    }
                }
                finally
                {
                    if (stream != bitStream) stream.Dispose();
                    if (rijndael != null) rijndael.Clear();
                }
            }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal void DisconnectClient(uint clientId)
        {
            if (!isServer)
                return;

            if (connectionPendingClients.Contains(clientId))
                connectionPendingClients.Remove(clientId);

            if (ConnectedClients.ContainsKey(clientId))
                ConnectedClients.Remove(clientId);

            for (int i = ConnectedClientsList.Count - 1; i > -1; i--)
            {
                if (ConnectedClientsList[i].ClientId == clientId)
                    ConnectedClientsList.RemoveAt(i);
            }

#if !DISABLE_CRYPTOGRAPHY
            if (pendingKeyExchanges.ContainsKey(clientId))
                pendingKeyExchanges.Remove(clientId);
#endif

            NetworkConfig.NetworkTransport.DisconnectClient(clientId);
        }

        internal void OnClientDisconnectFromServer(uint clientId)
        {
            if (connectionPendingClients.Contains(clientId))
                connectionPendingClients.Remove(clientId);
            if (ConnectedClients.ContainsKey(clientId))
            {
                if(NetworkConfig.HandleObjectSpawning)
                {
                    if (ConnectedClients[clientId].PlayerObject != null)
                        Destroy(ConnectedClients[clientId].PlayerObject.gameObject);
                    for (int i = 0; i < ConnectedClients[clientId].OwnedObjects.Count; i++)
                    {
                        if (ConnectedClients[clientId].OwnedObjects[i] != null)
                            Destroy(ConnectedClients[clientId].OwnedObjects[i].gameObject);
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

            if (isServer)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt32Packed(clientId);
                        InternalMessageHandler.Send(MLAPIConstants.MLAPI_CLIENT_DISCONNECT, "MLAPI_INTERNAL", clientId, stream, new InternalSecuritySendOptions(false, false));
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
                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_TIME_SYNC, "MLAPI_TIME_SYNC", stream, new InternalSecuritySendOptions(false, false));
                }
            }
        }

        internal void HandleApproval(uint clientId, int prefabId, bool approved, Vector3 position, Quaternion rotation)
        {
            if(approved)
            {
                //Inform new client it got approved
                if (connectionPendingClients.Contains(clientId))
                    connectionPendingClients.Remove(clientId);

                byte[] aesKey = pendingClientAesKeys.ContainsKey(clientId) ? pendingClientAesKeys[clientId] : null;
                pendingClientAesKeys.Remove(clientId);
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
                    prefabId = prefabId == -1 ? NetworkConfig.NetworkPrefabIds[NetworkConfig.PlayerPrefabName] : prefabId;
                    netObject = SpawnManager.CreateSpawnedObject(prefabId, 0, clientId, true, position, rotation, null, false, false);
                    ConnectedClients[clientId].PlayerObject = netObject;
                }

                int amountOfObjectsToSend = SpawnManager.SpawnedObjects.Values.Count;

                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt32Packed(clientId);
                        if (NetworkConfig.EnableSceneSwitching)
                            writer.WriteUInt32Packed(NetworkSceneManager.CurrentSceneIndex);

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
                                writer.WriteBit(pair.Value.isPlayerObject);
                                writer.WriteUInt32Packed(pair.Value.NetworkId);
                                writer.WriteUInt32Packed(pair.Value.OwnerClientId);
                                writer.WriteInt32Packed(NetworkConfig.NetworkPrefabIds[pair.Value.NetworkedPrefabName]);
                                writer.WriteBit(pair.Value.gameObject.activeInHierarchy);
                                writer.WriteBit(pair.Value.sceneObject == null ? true : pair.Value.sceneObject.Value);

                                writer.WriteSinglePacked(pair.Value.transform.position.x);
                                writer.WriteSinglePacked(pair.Value.transform.position.y);
                                writer.WriteSinglePacked(pair.Value.transform.position.z);

                                writer.WriteSinglePacked(pair.Value.transform.rotation.eulerAngles.x);
                                writer.WriteSinglePacked(pair.Value.transform.rotation.eulerAngles.y);
                                writer.WriteSinglePacked(pair.Value.transform.rotation.eulerAngles.z);

                                pair.Value.WriteNetworkedVarData(stream, clientId);
                            }
                        }

                        InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CONNECTION_APPROVED, "MLAPI_INTERNAL", stream, new InternalSecuritySendOptions(true, false), true);

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
                                writer.WriteBit(true);
                                writer.WriteUInt32Packed(ConnectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().NetworkId);
                                writer.WriteUInt32Packed(clientId);
                                writer.WriteInt32Packed(prefabId);
                                writer.WriteBit(false);

                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.x);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.y);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.z);

                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.x);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.y);
                                writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.z);

                                writer.WriteBit(false); //No payload data

                                ConnectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().WriteNetworkedVarData(stream, clientPair.Key);
                            }
                            else
                            {
                                writer.WriteUInt32Packed(clientId);
                            }
                            InternalMessageHandler.Send(clientPair.Key, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream, new InternalSecuritySendOptions(false, false));
                        }
                    }
                }
            }
            else
            {
                if (connectionPendingClients.Contains(clientId))
                    connectionPendingClients.Remove(clientId);

#if !DISABLE_CRYPTOGRAPHY
                if (pendingKeyExchanges.ContainsKey(clientId))
                    pendingKeyExchanges.Remove(clientId);
#endif

                NetworkConfig.NetworkTransport.DisconnectClient(clientId);
            }
        }
    }
}
