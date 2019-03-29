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
using System.Linq.Expressions;
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
using UnityEngine.Networking;

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
        [Obsolete("Use Singleton instead", false)]
        public static NetworkingManager singleton => Singleton;
        /// <summary>
        /// The singleton instance of the NetworkingManager
        /// </summary>
        public static NetworkingManager Singleton { get; private set; }
        /// <summary>
        /// Gets the networkId of the server
        /// </summary>
		public ulong ServerClientId => NetworkConfig.NetworkTransport != null ? NetworkConfig.NetworkTransport.ServerClientId : throw new NullReferenceException("The transport in the active NetworkConfig is null");
        /// <summary>
        /// The clientId the server calls the local client by, only valid for clients
        /// </summary>
        public ulong LocalClientId 
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
        private ulong localClientId;
        /// <summary>
        /// Gets a dictionary of connected clients and their clientId keys
        /// </summary>
        public readonly Dictionary<ulong, NetworkedClient> ConnectedClients = new Dictionary<ulong, NetworkedClient>();
        /// <summary>
        /// Gets a list of connected clients
        /// </summary>
        public readonly List<NetworkedClient> ConnectedClientsList = new List<NetworkedClient>();
        /// <summary>
        /// Gets a dictionary of the clients that have been accepted by the transport but are still pending by the MLAPI.
        /// </summary>
        public readonly Dictionary<ulong, PendingClient> PendingClients = new Dictionary<ulong, PendingClient>();
        /// <summary>
        /// Gets wheter or not a server is running
        /// </summary>
        [Obsolete("Use IsServer instead", false)]
        public bool isServer => IsServer;
        /// <summary>
        /// Gets wheter or not a server is running
        /// </summary>
        public bool IsServer { get; internal set; }
        /// <summary>
        /// Gets wheter or not a client is running
        /// </summary>
        [Obsolete("Use IsClient instead", false)]
        public bool isClient => IsClient;
        /// <summary>
        /// Gets wheter or not a client is running
        /// </summary>
        public bool IsClient { get; internal set; }
        /// <summary>
        /// Gets if we are running as host
        /// </summary>
        [Obsolete("Use IsHost instead", false)]
        public bool isHost => IsHost;
        /// <summary>
        /// Gets if we are running as host
        /// </summary>
        public bool IsHost => IsServer && IsClient;
        /// <summary>
        /// Gets wheter or not we are listening for connections
        /// </summary>
        [Obsolete("Use IsListening instead", false)]
        public bool isListening => IsListening;
        /// <summary>
        /// Gets wheter or not we are listening for connections
        /// </summary>
        public bool IsListening { get; internal set; }
        /// <summary>
        /// Gets if we are connected as a client
        /// </summary>
        [Obsolete("Use IsConnectedClient instead", false)]
        public bool isConnectedClients => IsConnectedClient;
        /// <summary>
        /// Gets if we are connected as a client
        /// </summary>
        public bool IsConnectedClient { get; internal set; }
        /// <summary>
        /// The callback to invoke once a client connects
        /// </summary>
        public Action<ulong> OnClientConnectedCallback = null;
        /// <summary>
        /// The callback to invoke when a client disconnects
        /// </summary>
        public Action<ulong> OnClientDisconnectCallback = null;
        /// <summary>
        /// The callback to invoke once the server is ready
        /// </summary>
        public Action OnServerStarted = null;
        /// <summary>
        /// Delegate type called when connection has been approved
        /// </summary>
        /// <param name="clientId">The clientId of the approved client</param>
        /// <param name="prefabHash">The prefabHash to use for the client</param>
        /// <param name="approved">Wheter or not the client was approved</param>
        /// <param name="position">The position to spawn the client at</param>
        /// <param name="rotation">The rotation to spawn the client with</param>
        public delegate void ConnectionApprovedDelegate(ulong clientId, ulong? prefabHash, bool approved, Vector3? position, Quaternion? rotation);
        /// <summary>
        /// The callback to invoke during connection approval
        /// </summary>
        public Action<byte[], ulong, ConnectionApprovedDelegate> ConnectionApprovalCallback = null;
        /// <summary>
        /// The current NetworkingConfiguration
        /// </summary>
        [HideInInspector]
        public NetworkConfig NetworkConfig;
        /// <summary>
        /// Delegate used for incoming custom messages
        /// </summary>
        /// <param name="clientId">The clientId that sent the message</param>
        /// <param name="stream">The stream containing the message data</param>
        public delegate void CustomMessageDelegete(ulong clientId, Stream stream);
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

        internal void InvokeOnIncomingCustomMessage(ulong clientId, Stream stream)
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
                    InternalMessageHandler.Send(ConnectedClientsList[i].ClientId, MLAPIConstants.MLAPI_CUSTOM_MESSAGE, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security, null);
                }
            }
            else
            {
                for (int i = 0; i < clientIds.Count; i++)
                {
                    InternalMessageHandler.Send(clientIds[i], MLAPIConstants.MLAPI_CUSTOM_MESSAGE, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security, null);
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
        public void SendCustomMessage(ulong clientId, BitStream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CUSTOM_MESSAGE, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security, null);
        }

        private void OnValidate()
        {
            if (NetworkConfig == null)
                return; //May occur when the component is added

            if (!NetworkConfig.RegisteredScenes.Contains(SceneManager.GetActiveScene().name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The active scene is not registered as a networked scene. The MLAPI has added it");
                NetworkConfig.RegisteredScenes.Add(SceneManager.GetActiveScene().name);
            }
            
            
            // TODO: Show which two prefab generators that collide
            HashSet<ulong> hashes = new HashSet<ulong>();
            
            for (int i = 0; i < NetworkConfig.NetworkedPrefabs.Count; i++)
            {
                if (hashes.Contains(NetworkConfig.NetworkedPrefabs[i].Hash))
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogError("PrefabHash collision! You have two prefabs with the same hash. This is not supported");
                }

                hashes.Add(NetworkConfig.NetworkedPrefabs[i].Hash);
            }

            int playerPrefabCount = NetworkConfig.NetworkedPrefabs.Count(x => x.PlayerPrefab == true);
            if (playerPrefabCount == 0)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("There is no NetworkedPrefab marked as a PlayerPrefab");
            }
            else if (playerPrefabCount > 1)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only one networked prefab can be marked as a player prefab");
            }
            else NetworkConfig.PlayerPrefabHash = NetworkConfig.NetworkedPrefabs.Find(x => x.PlayerPrefab == true).Hash;
        }

        private void Init(bool server)
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

            ResponseMessageManager.Clear();
            SpawnManager.SpawnedObjects.Clear();
            SpawnManager.SpawnedObjectsList.Clear();
            SpawnManager.releasedNetworkObjectIds.Clear();
            SpawnManager.pendingSoftSyncObjects.Clear();
            SpawnManager.customSpawnHandlers.Clear();
            SpawnManager.customDestroyHandlers.Clear();
            NetworkSceneManager.registeredSceneNames.Clear();
            NetworkSceneManager.sceneIndexToString.Clear();
            NetworkSceneManager.sceneNameToIndex.Clear();
            NetworkSceneManager.sceneSwitchProgresses.Clear();

            if (NetworkConfig.NetworkTransport == null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("No transport has been selected!");
                return;
            }
            
            try
            {
                string pfx = NetworkConfig.ServerBase64PfxCertificate.Trim();
                
                if (server && NetworkConfig.EnableEncryption && NetworkConfig.SignKeyExchange && !string.IsNullOrEmpty(pfx))
                {
                    try
                    {
                        byte[] decodedPfx = Convert.FromBase64String(pfx);

                        NetworkConfig.ServerX509Certificate = new X509Certificate2(decodedPfx);

                        if (!NetworkConfig.ServerX509Certificate.HasPrivateKey)
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The imported PFX file did not have a private key");
                        }
                    }
                    catch (FormatException ex)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Parsing PFX failed: " + ex.ToString());
                    }
                }
            }
            catch (CryptographicException ex)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Importing of certificate failed: " + ex.ToString());
            }

            NetworkConfig.RegisteredScenes.Sort();
            
            for (int i = 0; i < NetworkConfig.RegisteredScenes.Count; i++)
            {
                NetworkSceneManager.registeredSceneNames.Add(NetworkConfig.RegisteredScenes[i]);
                NetworkSceneManager.sceneIndexToString.Add((uint) i, NetworkConfig.RegisteredScenes[i]);
                NetworkSceneManager.sceneNameToIndex.Add(NetworkConfig.RegisteredScenes[i], (uint) i);
            }

            NetworkSceneManager.SetCurrentSceneIndex();
            
            NetworkConfig.NetworkTransport.Init();
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

            Init(true);
            NetworkConfig.NetworkTransport.StartServer();

            IsServer = true;
            IsClient = false;
            IsListening = true;

            SpawnManager.ServerSpawnSceneObjectsOnStartSweep();

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

            Init(false);
            NetworkConfig.NetworkTransport.StartClient();
            
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
            HashSet<ulong> disconnectedIds = new HashSet<ulong>();
            //Don't know if I have to disconnect the clients. I'm assuming the NetworkTransport does all the cleaning on shtudown. But this way the clients get a disconnect message from server (so long it does't get lost)
            
            foreach (KeyValuePair<ulong, NetworkedClient> pair in ConnectedClients)
            {
                if (!disconnectedIds.Contains(pair.Key))
                {
                    disconnectedIds.Add(pair.Key);
                    
					if (pair.Key == NetworkConfig.NetworkTransport.ServerClientId)
                        continue;

                    NetworkConfig.NetworkTransport.DisconnectRemoteClient(pair.Key);
                }
            }
            
            foreach (KeyValuePair<ulong, PendingClient> pair in PendingClients)
            {
                if(!disconnectedIds.Contains(pair.Key))
                {
                    disconnectedIds.Add(pair.Key);
                    if (pair.Key == NetworkConfig.NetworkTransport.ServerClientId)
                        continue;

                    NetworkConfig.NetworkTransport.DisconnectRemoteClient(pair.Key);
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
            NetworkConfig.NetworkTransport.DisconnectLocalClient();
            IsConnectedClient = false;
            Shutdown();
        }

        /// <summary>
        /// Starts a Host
        /// </summary>
        public void StartHost(Vector3? position = null, Quaternion? rotation = null, ulong? prefabHash = null, Stream payloadStream = null)
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
            
            Init(true);
            NetworkConfig.NetworkTransport.StartServer();

            IsServer = true;
            IsClient = true;
            IsListening = true;

            ulong hostClientId = NetworkConfig.NetworkTransport.ServerClientId;
            
            ConnectedClients.Add(hostClientId, new NetworkedClient()
            {
                ClientId = hostClientId
            });
            
            ConnectedClientsList.Add(ConnectedClients[hostClientId]);

            NetworkedObject netObject = SpawnManager.CreateLocalNetworkedObject(false, 0, (prefabHash == null ? NetworkConfig.PlayerPrefabHash : prefabHash.Value), position, rotation);
            SpawnManager.SpawnNetworkedObjectLocally(netObject, SpawnManager.GetNetworkObjectId(), false, true, hostClientId, payloadStream, payloadStream != null, payloadStream == null ? 0 : (int)payloadStream.Length, false, false);
            
            if (netObject.CheckObjectVisibility == null || netObject.CheckObjectVisibility(hostClientId))
            {
                netObject.observers.Add(hostClientId);
            }
            
            SpawnManager.ServerSpawnSceneObjectsOnStartSweep();

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
                if ((NetworkTime - lastSendTickTime >= (1f / NetworkConfig.SendTickrate)) || NetworkConfig.SendTickrate <= 0)
                {
                    if (NetworkConfig.EnableNetworkedVar)
                    {
                        NetworkedObject.NetworkedBehaviourUpdate();
                    }
                    
                    foreach (KeyValuePair<ulong, NetworkedClient> pair in ConnectedClients)
                    {
                        NetworkConfig.NetworkTransport.FlushSendQueue(pair.Key);
                        
                        if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Send Pending Queue: " + pair.Key);
                    }
                    
                    lastSendTickTime = NetworkTime;
                }
                if ((NetworkTime - lastReceiveTickTime >= (1f / NetworkConfig.ReceiveTickrate)) || NetworkConfig.ReceiveTickrate <= 0)
                {
                    NetworkProfiler.StartTick(TickType.Receive);
                    NetEventType eventType;
                    int processedEvents = 0;
                    do
                    {
                        processedEvents++;
                        eventType = NetworkConfig.NetworkTransport.PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload);

                        switch (eventType)
                        {
                            case NetEventType.Connect:
                                NetworkProfiler.StartEvent(TickType.Receive, (uint)payload.Count, channelName, "TRANSPORT_CONNECT");
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
                                            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CERTIFICATE_HAIL, "MLAPI_INTERNAL", hailStream, SecuritySendFlags.None, null, true);
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
                                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo($"Incoming Data From {clientId} : {payload.Count} bytes");

                                HandleIncomingData(clientId, channelName, payload);
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

                InternalMessageHandler.Send(ServerClientId, MLAPIConstants.MLAPI_CONNECTION_REQUEST, "MLAPI_INTERNAL", stream, SecuritySendFlags.Authenticated | SecuritySendFlags.Encrypted, null, true);
            }
        }

        private IEnumerator ApprovalTimeout(ulong clientId)
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

        private void HandleIncomingData(ulong clientId, string channelName, ArraySegment<byte> data)
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Unwrapping Data Header");

            using (BitStream inputStream = new BitStream(data.Array))
            {
                inputStream.SetLength(data.Count + data.Offset);
                inputStream.Position = data.Offset;
                
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
                    NetworkProfiler.StartEvent(TickType.Receive, (uint)(data.Count - headerByteSize), channelName, messageType);

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
                                InternalMessageHandler.HandleConnectionRequest(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_CONNECTION_APPROVED:
                            if (IsClient)
                                InternalMessageHandler.HandleConnectionApproved(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_ADD_OBJECT:
                            if (IsClient) InternalMessageHandler.HandleAddObject(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_DESTROY_OBJECT:
                            if (IsClient) InternalMessageHandler.HandleDestroyObject(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_SWITCH_SCENE:
                            if (IsClient) InternalMessageHandler.HandleSwitchScene(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_CHANGE_OWNER:
                            if (IsClient) InternalMessageHandler.HandleChangeOwner(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_ADD_OBJECTS:
                            if (IsClient) InternalMessageHandler.HandleAddObjects(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_TIME_SYNC:
                            if (IsClient) InternalMessageHandler.HandleTimeSync(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_NETWORKED_VAR_DELTA:
                            InternalMessageHandler.HandleNetworkedVarDelta(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_NETWORKED_VAR_UPDATE:
                            InternalMessageHandler.HandleNetworkedVarUpdate(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_SERVER_RPC:
                            if (IsServer) InternalMessageHandler.HandleServerRPC(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_SERVER_RPC_REQUEST:
                            if (IsServer) InternalMessageHandler.HandleServerRPCRequest(clientId, messageStream, channelName, security);
                            break;
                        case MLAPIConstants.MLAPI_SERVER_RPC_RESPONSE:
                            if (IsClient) InternalMessageHandler.HandleServerRPCResponse(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_CLIENT_RPC:
                            if (IsClient) InternalMessageHandler.HandleClientRPC(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_CLIENT_RPC_REQUEST:
                            if (IsClient) InternalMessageHandler.HandleClientRPCRequest(clientId, messageStream, channelName, security);
                            break;
                        case MLAPIConstants.MLAPI_CLIENT_RPC_RESPONSE:
                            if (IsServer) InternalMessageHandler.HandleClientRPCResponse(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_CUSTOM_MESSAGE:
                            InternalMessageHandler.HandleCustomMessage(clientId, messageStream);
                            break;
#if !DISABLE_CRYPTOGRAPHY
                        case MLAPIConstants.MLAPI_CERTIFICATE_HAIL:
                            if (IsClient) InternalMessageHandler.HandleHailRequest(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_CERTIFICATE_HAIL_RESPONSE:
                            if (IsServer) InternalMessageHandler.HandleHailResponse(clientId, messageStream);
                            break;
                        case MLAPIConstants.MLAPI_GREETINGS:
                            if (IsClient) InternalMessageHandler.HandleGreetings(clientId, messageStream);
                            break;
    #endif
                        case MLAPIConstants.MLAPI_CLIENT_SWITCH_SCENE_COMPLETED:
                            if (IsServer) InternalMessageHandler.HandleClientSwitchSceneCompleted(clientId, messageStream);
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

        internal void DisconnectClient(ulong clientId)
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

            NetworkConfig.NetworkTransport.DisconnectRemoteClient(clientId);
        }

        internal void OnClientDisconnectFromServer(ulong clientId)
        {
            if (PendingClients.ContainsKey(clientId))
                PendingClients.Remove(clientId);
            
            if (ConnectedClients.ContainsKey(clientId))
            {
                if (IsServer)
                {
                    if (ConnectedClients[clientId].PlayerObject != null)
                    {
                        if (SpawnManager.customDestroyHandlers.ContainsKey(ConnectedClients[clientId].PlayerObject.PrefabHash))
                        {
                            SpawnManager.customDestroyHandlers[ConnectedClients[clientId].PlayerObject.PrefabHash](ConnectedClients[clientId].PlayerObject);
                            SpawnManager.OnDestroyObject(ConnectedClients[clientId].PlayerObject.NetworkId, false);
                        }
                        else
                        {
                            Destroy(ConnectedClients[clientId].PlayerObject.gameObject);
                        }
                    }
                    
                    for (int i = 0; i < ConnectedClients[clientId].OwnedObjects.Count; i++)
                    {
                        if (ConnectedClients[clientId].OwnedObjects[i] != null)
                        {
                            if (!ConnectedClients[clientId].OwnedObjects[i].DontDestroyWithOwner)
                            {
                                if (SpawnManager.customDestroyHandlers.ContainsKey(ConnectedClients[clientId].OwnedObjects[i].PrefabHash))
                                {
                                    SpawnManager.customDestroyHandlers[ConnectedClients[clientId].OwnedObjects[i].PrefabHash](ConnectedClients[clientId].OwnedObjects[i]);
                                    SpawnManager.OnDestroyObject(ConnectedClients[clientId].OwnedObjects[i].NetworkId, false);
                                }
                                else
                                {
                                    Destroy(ConnectedClients[clientId].OwnedObjects[i].gameObject);
                                }
                            }
                            else
                            {
                                ConnectedClients[clientId].OwnedObjects[i].RemoveOwnership();
                            }
                        }
                    }

                    // TODO: Could(should?) be replaced with more memory per client, by storing the visiblity
                    for (int i = 0; i < SpawnManager.SpawnedObjectsList.Count; i++)
                    {
                        SpawnManager.SpawnedObjectsList[i].observers.Remove(clientId);
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
        }

        private void SyncTime()
        {
            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Syncing Time To Clients");
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteSinglePacked(NetworkTime);
                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_TIME_SYNC, "MLAPI_TIME_SYNC", stream, SecuritySendFlags.None, null);
                }
            }
        }
        
        private readonly List<NetworkedObject> _observedObjects = new List<NetworkedObject>();

        internal void HandleApproval(ulong clientId, ulong? prefabHash, bool approved, Vector3? position, Quaternion? rotation)
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
                
                NetworkedObject netObject = SpawnManager.CreateLocalNetworkedObject(false, 0, (prefabHash == null ? NetworkConfig.PlayerPrefabHash : prefabHash.Value), position, rotation);
                SpawnManager.SpawnNetworkedObjectLocally(netObject, SpawnManager.GetNetworkObjectId(), false, true, clientId, null, false, 0, false, false);
                
                ConnectedClients[clientId].PlayerObject = netObject;

                _observedObjects.Clear();
                
                for (int i = 0; i < SpawnManager.SpawnedObjectsList.Count; i++)
                {
                    if (clientId == ServerClientId || SpawnManager.SpawnedObjectsList[i].CheckObjectVisibility == null || SpawnManager.SpawnedObjectsList[i].CheckObjectVisibility(clientId))
                    {
                        _observedObjects.Add(SpawnManager.SpawnedObjectsList[i]);

                        SpawnManager.SpawnedObjectsList[i].observers.Add(clientId);
                    }
                } 

                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt64Packed(clientId);
                        
                        writer.WriteUInt32Packed(NetworkSceneManager.currentSceneIndex);
                        writer.WriteByteArray(NetworkSceneManager.currentSceneSwitchProgressGuid.ToByteArray());

                        writer.WriteSinglePacked(NetworkTime);
                        
                        writer.WriteUInt32Packed((uint)_observedObjects.Count);

                        for (int i = 0; i < _observedObjects.Count; i++)
                        {
                            writer.WriteBool(_observedObjects[i].IsPlayerObject);
                            writer.WriteUInt64Packed(_observedObjects[i].NetworkId);
                            writer.WriteUInt64Packed(_observedObjects[i].OwnerClientId);

                            if (NetworkConfig.UsePrefabSync)
                            {
                                writer.WriteUInt64Packed(_observedObjects[i].PrefabHash);
                            }
                            else
                            {
                                // Is this a scene object that we will soft map
                                writer.WriteBool(_observedObjects[i].IsSceneObject == null ? true : _observedObjects[i].IsSceneObject.Value);

                                if (_observedObjects[i].IsSceneObject == null || _observedObjects[i].IsSceneObject.Value == true)
                                {
                                    writer.WriteUInt64Packed(_observedObjects[i].NetworkedInstanceId);
                                }
                                else
                                {
                                    writer.WriteUInt64Packed(_observedObjects[i].PrefabHash);
                                }
                            }

                            writer.WriteBool(_observedObjects[i].DestroyWithScene);

                            writer.WriteSinglePacked(_observedObjects[i].transform.position.x);
                            writer.WriteSinglePacked(_observedObjects[i].transform.position.y);
                            writer.WriteSinglePacked(_observedObjects[i].transform.position.z);

                            writer.WriteSinglePacked(_observedObjects[i].transform.rotation.eulerAngles.x);
                            writer.WriteSinglePacked(_observedObjects[i].transform.rotation.eulerAngles.y);
                            writer.WriteSinglePacked(_observedObjects[i].transform.rotation.eulerAngles.z);

                            if (NetworkConfig.EnableNetworkedVar)
                            {
                                _observedObjects[i].WriteNetworkedVarData(stream, clientId);
                            }
                        }

                        InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CONNECTION_APPROVED, "MLAPI_INTERNAL", stream, SecuritySendFlags.Encrypted | SecuritySendFlags.Authenticated, null, true);

                        if (OnClientConnectedCallback != null)
                            OnClientConnectedCallback.Invoke(clientId);
                    }
                }

                //Inform old clients of the new player

                foreach (var clientPair in ConnectedClients)
                {
                    if (clientPair.Key == clientId || !ConnectedClients[clientId].PlayerObject.observers.Contains(clientPair.Key))
                        continue; //The new client.

                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                        {    
                            writer.WriteBool(true);
                            writer.WriteUInt64Packed(ConnectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().NetworkId);
                            writer.WriteUInt64Packed(clientId);

                            if (NetworkConfig.UsePrefabSync)
                            {
                                writer.WriteUInt64Packed(prefabHash == null ? NetworkConfig.PlayerPrefabHash : prefabHash.Value);
                            }
                            else
                            {
                                // Not a softmap aka scene object
                                writer.WriteBool(false);
                                writer.WriteUInt64Packed(prefabHash == null ? NetworkConfig.PlayerPrefabHash : prefabHash.Value);
                            }

                            writer.WriteBool(false); //Object won't have DestroyWithScene enabled.

                            writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.x);
                            writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.y);
                            writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.position.z);

                            writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.x);
                            writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.y);
                            writer.WriteSinglePacked(ConnectedClients[clientId].PlayerObject.transform.rotation.eulerAngles.z);

                            writer.WriteBool(false); //No payload data

                            if (NetworkConfig.EnableNetworkedVar)
                            {
                                ConnectedClients[clientId].PlayerObject.WriteNetworkedVarData(stream, clientPair.Key);
                            }
                            
                            InternalMessageHandler.Send(clientPair.Key, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None, null);
                        }
                    }
                }
            }
            else
            {
                if (PendingClients.ContainsKey(clientId))
                    PendingClients.Remove(clientId);

                NetworkConfig.NetworkTransport.DisconnectRemoteClient(clientId);
            }
        }
    }
}
