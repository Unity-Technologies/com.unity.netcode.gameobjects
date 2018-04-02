using MLAPI.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Security.Cryptography;
using MLAPI.NetworkingManagerComponents.Cryptography;
using MLAPI.NetworkingManagerComponents.Core;

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
        /// A list of spawnable prefabs
        /// </summary>
        public List<GameObject> SpawnablePrefabs;
        /// <summary>
        /// The default prefab to give to players
        /// </summary>
        public GameObject DefaultPlayerPrefab;
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
        [HideInInspector]
        public int MyClientId
        {
            get
            {
                return myClientId;
            }
        }
        internal int myClientId;
        internal Dictionary<int, NetworkedClient> connectedClients;
        /// <summary>
        /// Gets a dictionary of connected clients
        /// </summary>
        public Dictionary<int, NetworkedClient> ConnectedClients
        {
            get
            {
                return connectedClients;
            }
        }
        internal HashSet<int> pendingClients;
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
        internal int serverClientId;
        /// <summary>
        /// Gets if we are connected as a client
        /// </summary>
        [HideInInspector]
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
        public Action<int> OnClientConnectedCallback = null;
        /// <summary>
        /// The callback to invoke when a client disconnects
        /// </summary>
        public Action<int> OnClientDisconnectCallback = null;
        /// <summary>
        /// The callback to invoke once the server is ready
        /// </summary>
        public Action OnServerStarted = null;
        /// <summary>
        /// The callback to invoke during connection approval
        /// </summary>
        public Action<byte[], int, Action<int, bool>> ConnectionApprovalCallback = null;
        /// <summary>
        /// The current NetworkingConfiguration
        /// </summary>
        public NetworkConfig NetworkConfig;

        private EllipticDiffieHellman clientDiffieHellman;
        private Dictionary<int, byte[]> diffieHellmanPublicKeys;
        private byte[] clientAesKey;

        public bool RegenerateRSAKeys = false;

        private void OnValidate()
        {
            if (SpawnablePrefabs != null)
            {
                for (int i = 0; i < SpawnablePrefabs.Count; i++)
                {
                    if (SpawnablePrefabs[i] == null)
                        continue;
                    NetworkedObject netObject = SpawnablePrefabs[i].GetComponentInChildren<NetworkedObject>();
                    if (netObject == null)
                    {
                        Debug.LogWarning("MLAPI: All SpawnablePrefabs need a NetworkedObject component. Please add one to the prefab " + SpawnablePrefabs[i].gameObject.name);
                        continue;
                    }
                    netObject.spawnablePrefabIndex = i;
                }
            }
            if (DefaultPlayerPrefab != null)
            {
                NetworkedObject netObject = DefaultPlayerPrefab.GetComponentInChildren<NetworkedObject>();
                if (netObject == null)
                {
                    Debug.LogWarning("MLAPI: The player object needs a NetworkedObject component.");
                }
            }

            if (!NetworkConfig.EnableEncryption)
                RegenerateRSAKeys = false;
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

        private ConnectionConfig Init()
        {
            networkTime = 0f;
            lastSendTickTime = 0;
            lastEventTickTime = 0;
            lastReceiveTickTime = 0;
            pendingClients = new HashSet<int>();
            connectedClients = new Dictionary<int, NetworkedClient>();
            messageBuffer = new byte[NetworkConfig.MessageBufferSize];
            diffieHellmanPublicKeys = new Dictionary<int, byte[]>();
            MessageManager.channels = new Dictionary<string, int>();
            MessageManager.messageTypes = new Dictionary<string, ushort>();
            MessageManager.messageCallbacks = new Dictionary<ushort, Dictionary<int, Action<int, byte[]>>>();
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
            ClientIdManager.clientIdCounter = 0;
            ClientIdManager.clientIdToKey = new Dictionary<int, ClientIdKey>();
            ClientIdManager.keyToClientId = new Dictionary<ClientIdKey, int>();
            ClientIdManager.releasedClientIds = new Queue<int>();

            if (NetworkConfig.HandleObjectSpawning)
            {
                NetworkedObject[] sceneObjects = FindObjectsOfType<NetworkedObject>();
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    uint networkId = SpawnManager.GetNetworkObjectId();
                    SpawnManager.spawnedObjects.Add(networkId, sceneObjects[i]);
                    sceneObjects[i]._isSpawned = true;
                    sceneObjects[i].sceneObject = true;
                }
            }

            NetworkTransport.Init();
            ConnectionConfig cConfig = new ConnectionConfig()
            {
                SendDelay = 0
            };

            //MLAPI channels and messageTypes
            List<Channel> internalChannels = new List<Channel>();
            internalChannels.Add(new Channel()
            {
                Name = "MLAPI_INTERNAL",
                Type = QosType.ReliableFragmentedSequenced
            });
            internalChannels.Add(new Channel()
            {
                Name = "MLAPI_POSITION_UPDATE",
                Type = QosType.StateUpdate
            });
            internalChannels.Add(new Channel()
            {
                Name = "MLAPI_ANIMATION_UPDATE",
                Type = QosType.ReliableSequenced
            });
            internalChannels.Add(new Channel()
            {
                Name = "MLAPI_NAV_AGENT_STATE",
                Type = QosType.ReliableSequenced
            });
            internalChannels.Add(new Channel()
            {
                Name = "MLAPI_NAV_AGENT_CORRECTION",
                Type = QosType.StateUpdate
            });

            HashSet<string> channelNames = new HashSet<string>();
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

            List<string> messageTypes = new List<string>(NetworkConfig.MessageTypes);
            messageTypes.Add("MLAPI_OnRecieveTransformFromClient");
            messageTypes.Add("MLAPI_OnRecieveTransformFromServer");
            messageTypes.Add("MLAPI_HandleAnimationMessage");
            messageTypes.Add("MLAPI_HandleAnimationParameterMessage");
            messageTypes.Add("MLAPI_HandleAnimationTriggerMessage");
            messageTypes.Add("MLAPI_OnNavMeshStateUpdate");
            messageTypes.Add("MLAPI_OnNavMeshCorrectionUpdate");

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

            if(NetworkConfig.EnableEncryption)
            {
                List<string> channelNameList = NetworkConfig.Channels.Select(x => x.Name).ToList();
                for (int i = 0; i < NetworkConfig.EncryptedChannels.Count; i++)
                {
                    if (NetworkConfig.EncryptedChannelsHashSet.Contains(NetworkConfig.EncryptedChannels[i]))
                    {
                        Debug.LogWarning("MLAPI: Duplicate encrypted channel: " + NetworkConfig.EncryptedChannels[i]);
                        continue;
                    }
                    else if (!channelNameList.Contains(NetworkConfig.EncryptedChannels[i]))
                    {
                        Debug.LogWarning("MLAPI: Channel  " + NetworkConfig.EncryptedChannels[i] + " doesn't exist");
                    }
                    NetworkConfig.EncryptedChannelsHashSet.Add(NetworkConfig.EncryptedChannels[i]);
                }
            }

            if (NetworkConfig.AllowPassthroughMessages)
            {
                for (int i = 0; i < NetworkConfig.PassthroughMessageTypes.Count; i++)
                {
                    if(!NetworkConfig.MessageTypes.Contains(NetworkConfig.PassthroughMessageTypes[i]))
                    {
                        Debug.LogWarning("MLAPI: The messageType " + NetworkConfig.PassthroughMessageTypes[i] + " doesn't exist");
                        continue;
                    }
                    NetworkConfig.PassthroughMessageHashSet.Add(MessageManager.messageTypes[NetworkConfig.PassthroughMessageTypes[i]]);
                }
            }

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
                MessageManager.messageTypes.Add(messageTypes[i], messageId);
                MessageManager.reverseMessageTypes.Add(messageId, messageTypes[i]);
                messageId++;
            }
            return cConfig;
        }

        /// <summary>
        /// Starts a server with a given NetworkingConfiguration
        /// </summary>
        /// <param name="netConfig">The NetworkingConfiguration to use</param>
        public void StartServer()
        {
            if (isServer || isClient)
            {
                Debug.LogWarning("MLAPI: Cannot start server while an instance is already running");
                return;
            }

            ConnectionConfig cConfig = Init();
            if (NetworkConfig.ConnectionApproval)
            {
                if (ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            hostId = NetworkTransport.AddHost(hostTopology, NetworkConfig.Port);
            _isServer = true;
            _isClient = false;
            isListening = true;

            if (OnServerStarted != null)
                OnServerStarted.Invoke();
        }

        /// <summary>
        /// Starts a client with a given NetworkingConfiguration
        /// </summary>
        /// <param name="netConfig">The NetworkingConfiguration to use</param>
        public void StartClient()
        {
            if (isServer || isClient)
            {
                Debug.LogWarning("MLAPI: Cannot start client while an instance is already running");
                return;
            }

            ConnectionConfig cConfig = Init();
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            hostId =  NetworkTransport.AddHost(hostTopology, 0, null);

            _isServer = false;
            _isClient = true;
            isListening = true;
            serverClientId = NetworkTransport.Connect(hostId, NetworkConfig.Address, NetworkConfig.Port, 0, out error);
        }

        /// <summary>
        /// Stops the running server
        /// </summary>
        public void StopServer()
        {
            HashSet<int> sentIds = new HashSet<int>();
            //Don't know if I have to disconnect the clients. I'm assuming the NetworkTransport does all the cleaning on shtudown. But this way the clients get a disconnect message from server (so long it does't get lost)
            foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
            {
                if(!sentIds.Contains(pair.Key))
                {
                    sentIds.Add(pair.Key);
                    NetworkTransport.Disconnect(hostId, pair.Key, out error);
                }
            }
            foreach (int clientId in pendingClients)
            {
                if (!sentIds.Contains(clientId))
                {
                    sentIds.Add(clientId);
                    NetworkTransport.Disconnect(hostId, clientId, out error);
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
            NetworkTransport.Disconnect(hostId, serverClientId, out error);
            Shutdown();
        }

        /// <summary>
        /// Starts a Host with a given NetworkingConfiguration
        /// </summary>
        /// <param name="netConfig">The NetworkingConfiguration to use</param>
        public void StartHost()
        {
            if (isServer || isClient)
            {
                Debug.LogWarning("MLAPI: Cannot start host while an instance is already running");
                return;
            }
            ConnectionConfig cConfig = Init();
            if (NetworkConfig.ConnectionApproval)
            {
                if (ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            hostId = NetworkTransport.AddHost(hostTopology, NetworkConfig.Port, null);
            _isServer = true;
            _isClient = true;
            isListening = true;
            connectedClients.Add(-1, new NetworkedClient() { ClientId = -1 });
            if(NetworkConfig.HandleObjectSpawning)
            {
                SpawnManager.SpawnPlayerObject(-1, 0);
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

        //Receive stuff
        internal int hostId;
        private int clientId;
        private int channelId;
        private int receivedSize;
        private byte error;
        private float lastReceiveTickTime;
        private float lastSendTickTime;
        private float lastEventTickTime;
        private void Update()
        {
            if(isListening)
            {
                if((Time.time - lastSendTickTime >= (1f / NetworkConfig.SendTickrate)) || NetworkConfig.SendTickrate <= 0)
                {
                    foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
                    {
                        NetworkTransport.SendQueuedMessages(hostId, pair.Key, out error);
                    }
                    lastSendTickTime = Time.time;
                }
                if((Time.time - lastReceiveTickTime >= (1f / NetworkConfig.ReceiveTickrate)) || NetworkConfig.ReceiveTickrate <= 0)
                {
                    NetworkEventType eventType;
                    int processedEvents = 0;
                    do
                    {
                        processedEvents++;
                        eventType = NetworkTransport.Receive(out hostId, out clientId, out channelId, messageBuffer, messageBuffer.Length, out receivedSize, out error);
                        NetworkError networkError = (NetworkError)error;
                        if (networkError == NetworkError.Timeout)
                        {
                            //Client timed out. 
                            if (isServer)
                            {
                                OnClientDisconnect(clientId);
                                return;
                            }
                            else
                                _isClientConnected = false;

                            if (OnClientDisconnectCallback != null)
                                OnClientDisconnectCallback.Invoke(clientId);
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
                                    pendingClients.Add(clientId);
                                    StartCoroutine(ApprovalTimeout(clientId));
                                }
                                else
                                {
                                    byte[] diffiePublic = new byte[0];
                                    if(NetworkConfig.EnableEncryption)
                                    {
                                        clientDiffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                                        diffiePublic = clientDiffieHellman.GetPublicKey();
                                    }

                                    int sizeOfStream = 32;
                                    if (NetworkConfig.ConnectionApproval)
                                        sizeOfStream += 2 + NetworkConfig.ConnectionData.Length;
                                    if (NetworkConfig.EnableEncryption)
                                        sizeOfStream += 2 + diffiePublic.Length;

                                    using (MemoryStream writeStream = new MemoryStream(sizeOfStream))
                                    {
                                        using (BinaryWriter writer = new BinaryWriter(writeStream))
                                        {
                                            writer.Write(NetworkConfig.GetConfig());
                                            if (NetworkConfig.EnableEncryption)
                                            {
                                                writer.Write((ushort)diffiePublic.Length);
                                                writer.Write(diffiePublic);
                                            }
                                            if (NetworkConfig.ConnectionApproval)
                                            {
                                                writer.Write((ushort)NetworkConfig.ConnectionData.Length);
                                                writer.Write(NetworkConfig.ConnectionData);
                                            }
                                        }
                                        Send(clientId, "MLAPI_CONNECTION_REQUEST", "MLAPI_INTERNAL", writeStream.GetBuffer(), null, null, true);
                                    }
                                }
                                break;
                            case NetworkEventType.DataEvent:
                                HandleIncomingData(clientId, messageBuffer, channelId);
                                break;
                            case NetworkEventType.DisconnectEvent:
                                if (isServer)
                                    OnClientDisconnect(clientId);
                                else
                                    _isClientConnected = false;

                                if (OnClientDisconnectCallback != null)
                                    OnClientDisconnectCallback.Invoke(clientId);
                                break;
                        }
                        // Only do another iteration if: there are no more messages AND (there is no limit to max events or we have processed less than the maximum)
                    } while (eventType != NetworkEventType.Nothing && (NetworkConfig.MaxReceiveEventsPerTickRate <= 0 || processedEvents < NetworkConfig.MaxReceiveEventsPerTickRate));
                    lastReceiveTickTime = Time.time;
                }
                if (isServer && ((Time.time - lastEventTickTime >= (1f / NetworkConfig.EventTickrate)) || NetworkConfig.EventTickrate <= 0))
                {
                    LagCompensationManager.AddFrames();
                    NetworkedObject.InvokeSyncvarUpdate();
                    lastEventTickTime = Time.time;
                }
                networkTime += Time.deltaTime;
            }
        }

        private IEnumerator ApprovalTimeout(int clientId)
        {
            float timeStarted = Time.time;
            //We yield every frame incase a pending client disconnects and someone else gets its connection id
            while (Time.time - timeStarted < NetworkConfig.ClientConnectionBufferTimeout && pendingClients.Contains(clientId))
            {
                yield return null;
            }
            if(pendingClients.Contains(clientId) && !connectedClients.ContainsKey(clientId))
            {
                //Timeout
                DisconnectClient(clientId);
            }
        }

        private void HandleIncomingData(int clientId, byte[] data, int channelId)
        {
            using(MemoryStream readStream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(readStream))
                {
                    ushort messageType = reader.ReadUInt16();
                    bool targeted = reader.ReadBoolean();
                    uint targetNetworkId = 0;
                    ushort networkOrderId = 0;
                    if(targeted)
                    {
                        targetNetworkId = reader.ReadUInt32();
                        networkOrderId = reader.ReadUInt16();
                    }
                    bool isPassthrough = reader.ReadBoolean();

                    int passthroughOrigin = 0;
                    int passthroughTarget = 0;

                    if (isPassthrough && isServer)
                        passthroughTarget = reader.ReadInt32();
                    else if (isPassthrough && !isServer)
                        passthroughOrigin = reader.ReadInt32();


                    //Client tried to send a network message that was not the connection request before he was accepted.
                    if (isServer && pendingClients.Contains(clientId) && messageType != 0)
                    {
                        Debug.LogWarning("MLAPI: Message recieved from clientId " + clientId + " before it has been accepted");
                        return;
                    }


                    ushort bytesToRead = reader.ReadUInt16();
                    byte[] incommingData = reader.ReadBytes(bytesToRead);
                    if(NetworkConfig.EncryptedChannelsHashSet.Contains(MessageManager.reverseChannels[channelId]))
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
                    else if(isClient && isPassthrough && !NetworkConfig.PassthroughMessageHashSet.Contains(messageType))
                    {
                        Debug.LogWarning("MLAPI: Server tried to send a passthrough message for a messageType not registered as passthrough");
                        return;
                    }
                    else if(isServer && isPassthrough)
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
                        PassthroughSend(passthroughTarget, clientId, messageType, channelId, incommingData, netIdTarget, netOrderId);
                        return;
                    }
                       
                    if (messageType >= 32)
                    {
                        #region CUSTOM MESSAGE
                        //Custom message, invoke all message handlers
                        if(targeted)
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
                            foreach (KeyValuePair<int, Action<int, byte[]>> pair in MessageManager.messageCallbacks[messageType])
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
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            byte[] configHash = messageReader.ReadBytes(32);
                                            if (!NetworkConfig.CompareConfig(configHash))
                                            {
                                                Debug.LogWarning("MLAPI: NetworkConfiguration missmatch. The configuration between the server and client does not match.");
                                                DisconnectClient(clientId);
                                                return;
                                            }
                                            byte[] aesKey = new byte[0];
                                            if(NetworkConfig.EnableEncryption)
                                            {
                                                ushort diffiePublicSize = messageReader.ReadUInt16();
                                                byte[] diffiePublic = messageReader.ReadBytes(diffiePublicSize);
                                                diffieHellmanPublicKeys.Add(clientId, diffiePublic);

                                            }
                                            if (NetworkConfig.ConnectionApproval)
                                            {
                                                ushort bufferSize = messageReader.ReadUInt16();
                                                byte[] connectionBuffer = messageReader.ReadBytes(bufferSize);
                                                ConnectionApprovalCallback(connectionBuffer, clientId, HandleApproval);
                                            }
                                            else
                                            {
                                                HandleApproval(clientId, true);
                                            }
                                        }
                                    }
                                }
                                break;
                            case 1: //Server informs client it has been approved:
                                if (isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            myClientId = messageReader.ReadInt32();
                                            uint sceneIndex = 0;
                                            if(NetworkConfig.EnableSceneSwitching)
                                            {
                                                sceneIndex = messageReader.ReadUInt32();
                                            }

                                            if (NetworkConfig.EnableEncryption)
                                            {
                                                ushort keyLength = messageReader.ReadUInt16();
                                                byte[] serverPublicKey = messageReader.ReadBytes(keyLength);
                                                clientAesKey = clientDiffieHellman.GetSharedSecret(serverPublicKey);
                                                if (NetworkConfig.SignKeyExchange)
                                                {
                                                    ushort signatureLength = messageReader.ReadUInt16();
                                                    byte[] publicKeySignature = messageReader.ReadBytes(signatureLength);
                                                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                                                    {
                                                        rsa.PersistKeyInCsp = false;
                                                        rsa.FromXmlString(NetworkConfig.RSAPublicKey);
                                                        if(!rsa.VerifyData(serverPublicKey, new SHA512CryptoServiceProvider(), publicKeySignature))
                                                        {
                                                            //Man in the middle.
                                                            Debug.LogWarning("MLAPI: Signature doesnt match for the key exchange public part. Disconnecting");
                                                            StopClient();
                                                            return;
                                                        }
                                                    }
                                                }
                                            }

                                            float netTime = messageReader.ReadSingle();
                                            int remoteStamp = messageReader.ReadInt32();
                                            int msDelay = NetworkTransport.GetRemoteDelayTimeMS(hostId, clientId, remoteStamp, out error);
                                            if ((NetworkError)error != NetworkError.Ok)
                                                msDelay = 0;
                                            networkTime = netTime + (msDelay / 1000f);

                                            connectedClients.Add(MyClientId, new NetworkedClient() { ClientId = MyClientId });
                                            int clientCount = messageReader.ReadInt32();
                                            for (int i = 0; i < clientCount; i++)
                                            {
                                                int conId = messageReader.ReadInt32();
                                                connectedClients.Add(conId, new NetworkedClient() { ClientId = conId });
                                            }
                                            if(NetworkConfig.HandleObjectSpawning)
                                            {
                                                SpawnManager.DestroySceneObjects();
                                                int objectCount = messageReader.ReadInt32();
                                                for (int i = 0; i < objectCount; i++)
                                                {
                                                    bool isPlayerObject = messageReader.ReadBoolean();
                                                    uint networkId = messageReader.ReadUInt32();
                                                    int ownerId = messageReader.ReadInt32();
                                                    int prefabId = messageReader.ReadInt32();
                                                    bool isActive = messageReader.ReadBoolean();

                                                    float xPos = messageReader.ReadSingle();
                                                    float yPos = messageReader.ReadSingle();
                                                    float zPos = messageReader.ReadSingle();

                                                    float xRot = messageReader.ReadSingle();
                                                    float yRot = messageReader.ReadSingle();
                                                    float zRot = messageReader.ReadSingle();

                                                    if (isPlayerObject)
                                                    {
                                                        SpawnManager.SpawnPlayerObject(ownerId, networkId);
                                                    }
                                                    else
                                                    {
                                                        GameObject go = SpawnManager.SpawnObject(prefabId, networkId, ownerId, 
                                                            new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));

                                                        go.SetActive(isActive);
                                                    }
                                                }
                                            }
                                            if(NetworkConfig.EnableSceneSwitching)
                                            {
                                                NetworkSceneManager.OnSceneSwitch(sceneIndex);
                                            }
                                        }
                                    }
                                    _isClientConnected = true;
                                    if (OnClientConnectedCallback != null)
                                        OnClientConnectedCallback.Invoke(clientId);
                                }
                                break;
                            case 2:
                                //Server informs client another client connected
                                //MLAPI_ADD_OBJECT
                                if (isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            if(NetworkConfig.HandleObjectSpawning)
                                            {
                                                bool isPlayerObject = messageReader.ReadBoolean();
                                                uint networkId = messageReader.ReadUInt32();
                                                int ownerId = messageReader.ReadInt32();
                                                int prefabId = messageReader.ReadInt32();

                                                float xPos = messageReader.ReadSingle();
                                                float yPos = messageReader.ReadSingle();
                                                float zPos = messageReader.ReadSingle();

                                                float xRot = messageReader.ReadSingle();
                                                float yRot = messageReader.ReadSingle();
                                                float zRot = messageReader.ReadSingle();

                                                if (isPlayerObject)
                                                {
                                                    connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                                                    SpawnManager.SpawnPlayerObject(ownerId, networkId);
                                                }
                                                else
                                                {
                                                    SpawnManager.SpawnObject(prefabId, networkId, ownerId,
                                                        new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));
                                                }
                                            }
                                            else
                                            {
                                                int ownerId = messageReader.ReadInt32();
                                                connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                                            }
                                        }
                                    }
                                }
                                break;
                            case 3:
                                //Server informs client another client disconnected
                                //MLAPI_CLIENT_DISCONNECT
                                if(isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            int disconnectedClientId = messageReader.ReadInt32();
                                            OnClientDisconnect(disconnectedClientId);
                                        }
                                    }
                                }
                                break;
                            case 4:
                                //Server infroms clients to destroy an object
                                if(isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            uint netId = messageReader.ReadUInt32();
                                            SpawnManager.OnDestroyObject(netId, true);
                                        }
                                    }
                                }
                                break;
                            case 5:
                                //Scene switch
                                if (isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            NetworkSceneManager.OnSceneSwitch(messageReader.ReadUInt32());
                                        }
                                    }
                                }  
                                break;
                            case 6: //Spawn pool object
                                if(isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            uint netId = messageReader.ReadUInt32();
                                            float xPos = messageReader.ReadSingle();
                                            float yPos = messageReader.ReadSingle();
                                            float zPos = messageReader.ReadSingle();
                                            float xRot = messageReader.ReadSingle();
                                            float yRot = messageReader.ReadSingle();
                                            float zRot = messageReader.ReadSingle();
                                            SpawnManager.spawnedObjects[netId].transform.position = new Vector3(xPos, yPos, zPos);
                                            SpawnManager.spawnedObjects[netId].transform.rotation = Quaternion.Euler(new Vector3(xRot, yRot, zRot));
                                            SpawnManager.spawnedObjects[netId].gameObject.SetActive(true);
                                        }
                                    }
                                }
                                break;
                            case 7: //Destroy pool object
                                if(isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            uint netId = messageReader.ReadUInt32();
                                            SpawnManager.spawnedObjects[netId].gameObject.SetActive(false);
                                        }
                                    }
                                }
                                break;
                            case 8: //Change owner
                                if(isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            uint netId = messageReader.ReadUInt32();
                                            int ownerClientId = messageReader.ReadInt32();
                                            if (SpawnManager.spawnedObjects[netId].OwnerClientId == MyClientId)
                                            {
                                                //We are current owner.
                                                SpawnManager.spawnedObjects[netId].InvokeBehaviourOnLostOwnership();
                                            }
                                            if(ownerClientId == MyClientId)
                                            {
                                                //We are new owner.
                                                SpawnManager.spawnedObjects[netId].InvokeBehaviourOnGainedOwnership();
                                            }
                                            SpawnManager.spawnedObjects[netId].ownerClientId = ownerClientId;
                                        }
                                    }
                                }
                                break;
                            case 9: //Syncvar
                                if (isClient)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            byte dirtyCount = messageReader.ReadByte();
                                            uint netId = messageReader.ReadUInt32();
                                            ushort orderIndex = messageReader.ReadUInt16();
                                            if (dirtyCount > 0)
                                            {
                                                for (int i = 0; i < dirtyCount; i++)
                                                {
                                                    byte fieldIndex = messageReader.ReadByte();
                                                    if(!SpawnManager.spawnedObjects.ContainsKey(netId))
                                                    {
                                                        Debug.LogWarning("MLAPI: Sync message recieved for a non existant object with id: " + netId);
                                                        return;
                                                    }
                                                    else if(SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex) == null)
                                                    {
                                                        Debug.LogWarning("MLAPI: Sync message recieved for a non existant behaviour");
                                                        return;
                                                    }
                                                    else if(fieldIndex > (SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedFieldTypes.Count - 1))
                                                    {
                                                        Debug.LogWarning("MLAPI: Sync message recieved for field out of bounds");
                                                        return;
                                                    }
                                                    FieldType type = SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedFieldTypes[fieldIndex];
                                                    switch (type)
                                                    {
                                                        case FieldType.Bool:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadBoolean(), fieldIndex);
                                                            break;
                                                        case FieldType.Byte:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadByte(), fieldIndex);
                                                            break;
                                                        case FieldType.Char:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadChar(), fieldIndex);
                                                            break;
                                                        case FieldType.Double:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadDouble(), fieldIndex);
                                                            break;
                                                        case FieldType.Single:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadSingle(), fieldIndex);
                                                            break;
                                                        case FieldType.Int:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadInt32(), fieldIndex);
                                                            break;
                                                        case FieldType.Long:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadInt64(), fieldIndex);
                                                            break;
                                                        case FieldType.SByte:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadSByte(), fieldIndex);
                                                            break;
                                                        case FieldType.Short:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadInt16(), fieldIndex);
                                                            break;
                                                        case FieldType.UInt:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadUInt32(), fieldIndex);
                                                            break;
                                                        case FieldType.ULong:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadUInt64(), fieldIndex);
                                                            break;
                                                        case FieldType.UShort:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadUInt16(), fieldIndex);
                                                            break;
                                                        case FieldType.String:
                                                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadString(), fieldIndex);
                                                            break;
                                                        case FieldType.Vector3:
                                                            {   //Cases aren't their own scope. Therefor we create a scope for them as they share the X,Y,Z local variables otherwise.
                                                                float x = messageReader.ReadSingle();
                                                                float y = messageReader.ReadSingle();
                                                                float z = messageReader.ReadSingle();
                                                                SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(new Vector3(x, y, z), fieldIndex);
                                                            }
                                                            break;
                                                        case FieldType.Vector2:
                                                            {
                                                                float x = messageReader.ReadSingle();
                                                                float y = messageReader.ReadSingle();
                                                                SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(new Vector2(x, y), fieldIndex);
                                                            }
                                                            break;
                                                        case FieldType.Quaternion:
                                                            {
                                                                float x = messageReader.ReadSingle();
                                                                float y = messageReader.ReadSingle();
                                                                float z = messageReader.ReadSingle();
                                                                SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(Quaternion.Euler(x, y, z), fieldIndex);
                                                            }
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                        #endregion
                    }
                }
            }
        }

        #region SEND METHODS
        internal void PassthroughSend(int targetId, int sourceId, ushort messageType, int channelId, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (isHost && targetId == -1)
            {
                //Host trying to send data to it's own client
                Debug.LogWarning("MLAPI: Send method got message aimed at server from the server?");
                return;
            }

            int sizeOfStream = 10;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageType);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(true);
                    writer.Write(sourceId);
                    if(NetworkConfig.EncryptedChannelsHashSet.Contains(MessageManager.reverseChannels[channelId]))
                    {
                        //Encrypted message
                        byte[] encrypted = CryptographyHelper.Encrypt(data, connectedClients[targetId].AesKey);
                        writer.Write((ushort)encrypted.Length);
                        writer.Write(encrypted);
                    }
                    else
                    {
                        writer.Write((ushort)data.Length);
                        writer.Write(data);
                    }
                }
                NetworkTransport.QueueMessageForSending(hostId, targetId, channelId, stream.GetBuffer(), sizeOfStream, out error);
            }
        }

        internal void Send(int clientId, string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null, bool skipQueue = false)
        {
            if(clientId == -1 && isHost)
            {
                //Don't invoke the message on our own machine. Instant stack overflow.
                Debug.LogWarning("MLAPI: Cannot send message to own client");
                return;
            }
            else if(clientId == -1)
            {
                //Client trying to send data to host
                clientId = serverClientId;
            }

            bool isPassthrough = (!isServer && clientId != serverClientId && NetworkConfig.AllowPassthroughMessages);
            if (isPassthrough && !NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType]))
            {
                Debug.LogWarning("MLAPI: The The MessageType " + messageType + " is not registered as an allowed passthrough message type.");
                return;
            }

            int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            if (isPassthrough)
                sizeOfStream += 4;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(isPassthrough);
                    if (isPassthrough)
                        writer.Write(clientId);

                    if (NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
                    {
                        //This is an encrypted message.
                        byte[] encrypted;
                        if (isServer)
                            encrypted = CryptographyHelper.Encrypt(data, connectedClients[clientId].AesKey);
                        else
                            encrypted = CryptographyHelper.Encrypt(data, clientAesKey);

                        writer.Write((ushort)encrypted.Length);
                        writer.Write(encrypted);
                    }
                    else
                    {
                        //Send in plaintext.
                        writer.Write((ushort)data.Length);
                        writer.Write(data);
                    }
                }
                if (isPassthrough)
                    clientId = serverClientId;
                if (skipQueue)
                    NetworkTransport.Send(hostId, clientId, MessageManager.channels[channelName], stream.GetBuffer(), sizeOfStream, out error);
                else
                    NetworkTransport.QueueMessageForSending(hostId, clientId, MessageManager.channels[channelName], stream.GetBuffer(), sizeOfStream, out error);
            }
        }

        internal void Send(int[] clientIds, string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }
                int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                int channel = MessageManager.channels[channelName];
                for (int i = 0; i < clientIds.Length; i++)
                {
                    int clientId = clientIds[i];
                    if (isHost && clientId == -1)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.QueueMessageForSending(hostId, clientId, channel, stream.GetBuffer(), sizeOfStream, out error);
                }
            }
        }

        internal void Send(List<int> clientIds, string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }

            //2 bytes for messageType, 2 bytes for buffer length and one byte for target bool
            int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                int channel = MessageManager.channels[channelName];
                for (int i = 0; i < clientIds.Count; i++)
                {
                    int clientId = clientIds[i];
                    if (clientId == -1 && isHost)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.QueueMessageForSending(hostId, clientId, channel, stream.GetBuffer(), sizeOfStream, out error);
                }
            }
        }

        internal void Send(string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (NetworkConfig.EncryptedChannels.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }

            //2 bytes for messageType, 2 bytes for buffer length and one byte for target bool
            int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                int channel = MessageManager.channels[channelName];
                foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
                {
                    int clientId = pair.Key;
                    if(isHost && pair.Key == -1)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.QueueMessageForSending(hostId, clientId, channel, stream.GetBuffer(), sizeOfStream, out error);
                    
                }
            }
        }

        internal void Send(string messageType, string channelName, byte[] data, int clientIdToIgnore, uint? networkId = null, ushort? orderId = null)
        {
            if (NetworkConfig.EncryptedChannels.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }

            //2 bytes for messageType, 2 bytes for buffer length and one byte for target bool
            int sizeOfStream = 5;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                int channel = MessageManager.channels[channelName];
                foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
                {
                    if (pair.Key == clientIdToIgnore)
                        continue;
                    int clientId = pair.Key;
                    if (isHost && pair.Key == -1)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.QueueMessageForSending(hostId, clientId, channel, stream.GetBuffer(), sizeOfStream, out error);
                }
            }
        }
        #endregion

        private void DisconnectClient(int clientId)
        {
            if (!isServer)
                return;

            if (pendingClients.Contains(clientId))
                pendingClients.Remove(clientId);

            if (connectedClients.ContainsKey(clientId))
                connectedClients.Remove(clientId);

            if (diffieHellmanPublicKeys.ContainsKey(clientId))
                diffieHellmanPublicKeys.Remove(clientId);

            NetworkTransport.Disconnect(hostId, clientId, out error);          
        }

        private void OnClientDisconnect(int clientId)
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
                using (MemoryStream stream = new MemoryStream(4))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(clientId);
                    }
                    Send("MLAPI_CLIENT_DISCONNECT", "MLAPI_INTERNAL", stream.GetBuffer(), clientId);
                }   
            }
        }

        private void HandleApproval(int clientId, bool approved)
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
                    GameObject go = SpawnManager.SpawnPlayerObject(clientId, networkId);
                    connectedClients[clientId].PlayerObject = go;
                }
                int sizeOfStream = 16 + ((connectedClients.Count - 1) * 4);

                int amountOfObjectsToSend = SpawnManager.spawnedObjects.Values.Count(x => x.ServerOnly == false);

                if (NetworkConfig.HandleObjectSpawning)
                {
                    sizeOfStream += 4;
                    sizeOfStream += 38 * amountOfObjectsToSend;
                }

                if (NetworkConfig.EnableEncryption)
                {
                    sizeOfStream += 2 + publicKey.Length;
                    if (NetworkConfig.SignKeyExchange)
                    {
                        sizeOfStream += 2 + publicKeySignature.Length;
                    }
                }

                if (NetworkConfig.EnableSceneSwitching)
                {
                    sizeOfStream += 4;
                }

                using (MemoryStream writeStream = new MemoryStream(sizeOfStream))
                {
                    using (BinaryWriter writer = new BinaryWriter(writeStream))
                    {
                        writer.Write(clientId);
                        if(NetworkConfig.EnableSceneSwitching)
                        {
                            writer.Write(NetworkSceneManager.CurrentSceneIndex);
                        }

                        if (NetworkConfig.EnableEncryption)
                        {
                            writer.Write((ushort)publicKey.Length);
                            writer.Write(publicKey);
                            if (NetworkConfig.SignKeyExchange)
                            {
                                writer.Write((ushort)publicKeySignature.Length);
                                writer.Write(publicKeySignature);
                            }
                        }

                        writer.Write(NetworkTime);
                        writer.Write(NetworkTransport.GetNetworkTimestamp());

                        writer.Write(connectedClients.Count - 1);
                        foreach (KeyValuePair<int, NetworkedClient> item in connectedClients)
                        {
                            //Our own ID. Already added as the first one above
                            if (item.Key == clientId)
                                continue;
                            writer.Write(item.Key); //Connection id
                        }
                        if (NetworkConfig.HandleObjectSpawning)
                        {
                            writer.Write(amountOfObjectsToSend);

                            foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
                            {
                                if (pair.Value.ServerOnly)
                                    continue;
                                writer.Write(pair.Value.isPlayerObject);
                                writer.Write(pair.Value.NetworkId);
                                writer.Write(pair.Value.OwnerClientId);
                                writer.Write(pair.Value.SpawnablePrefabIndex);
                                writer.Write(pair.Value.gameObject.activeInHierarchy);

                                writer.Write(pair.Value.transform.position.x);
                                writer.Write(pair.Value.transform.position.y);
                                writer.Write(pair.Value.transform.position.z);

                                writer.Write(pair.Value.transform.rotation.x);
                                writer.Write(pair.Value.transform.rotation.y);
                                writer.Write(pair.Value.transform.rotation.z);
                            }
                        }
                    }
                    Send(clientId, "MLAPI_CONNECTION_APPROVED", "MLAPI_INTERNAL", writeStream.GetBuffer(), null, null, true);

                    if (OnClientConnectedCallback != null)
                        OnClientConnectedCallback.Invoke(clientId);
                }

                //Inform old clients of the new player

                if(NetworkConfig.HandleObjectSpawning)
                    sizeOfStream = 13;
                else
                    sizeOfStream = 4;

                using (MemoryStream stream = new MemoryStream(sizeOfStream))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        if (NetworkConfig.HandleObjectSpawning)
                        {
                            writer.Write(true);
                            writer.Write(connectedClients[clientId].PlayerObject.GetComponent<NetworkedObject>().NetworkId);
                            writer.Write(clientId);
                            writer.Write(-1);
                        }
                        else
                        {
                            writer.Write(clientId);
                        }
                    }
                    Send("MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer(), clientId);
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

                NetworkTransport.Disconnect(hostId, clientId, out error);
            }
        }
    }
}
