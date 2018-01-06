using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace MLAPI
{
    public class NetworkingManager : MonoBehaviour
    {
        public List<GameObject> SpawnablePrefabs;
        public GameObject DefaultPlayerPrefab;
        public static NetworkingManager singleton;
        //Client only, what my connectionId is on the server
        public int MyClientId;
        private Dictionary<int, NetworkedClient> connectedClients;
        private HashSet<int> pendingClients;
        internal bool isServer;
        internal bool isClient;
        internal bool isHost
        {
            get
            {
                return isServer && isClient;
            }
        }
        private bool isListening;
        private byte[] messageBuffer;
        private Dictionary<string, int> channels;
        private Dictionary<string, ushort> messageTypes;
        private Dictionary<ushort, Dictionary<int, Action<int, byte[]>>> messageCallbacks;
        private Dictionary<ushort, int> messageHandlerCounter;
        private Dictionary<ushort, Stack<int>> releasedMessageHandlerCounters;
        private int localConnectionId;
        public int PlaySceneIndex;
        public int MenuSceneIndex;
        private Dictionary<uint, NetworkedObject> spawnedObjects;
        private List<uint> spawnedObjectIds;
        private Stack<uint> releasedNetworkObjectIds;
        private uint networkObjectIdCounter;
        private uint GetNetworkObjectId()
        {
            if (releasedNetworkObjectIds.Count > 0)
            {
                return releasedNetworkObjectIds.Pop();
            }
            else
            {
                networkObjectIdCounter++;
                return networkObjectIdCounter;
            }
        }


        public NetworkingConfiguration NetworkConfig;

        private void OnValidate()
        {
            for (int i = 0; i < SpawnablePrefabs.Count; i++)
            {
                if(SpawnablePrefabs[i].GetComponentInChildren<NetworkedObject>() == null)
                {
                    Debug.LogWarning("MLAPI: All SpawnablePrefabs need a NetworkedObject component. Please add one to the prefab " + SpawnablePrefabs[i].gameObject.name);
                }
            }
        }

        internal GameObject SpawnObject(int spawnablePrefabIndex, uint networkId, int ownerId)
        {
            GameObject go = Instantiate(singleton.SpawnablePrefabs[spawnablePrefabIndex]);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if(netObject == null)
            {
                Debug.LogWarning("MLAPI: Please add a NetworkedObject component to the root of all spawnable objects");
                netObject = go.AddComponent<NetworkedObject>();
            }
            netObject.SpawnablePrefabId = spawnablePrefabIndex;
            if(singleton.isServer)
            {
                netObject.NetworkId = singleton.GetNetworkObjectId();
            }
            else
            {
                netObject.NetworkId = networkId;
            }
            netObject.OwnerClientId = ownerId;

            singleton.spawnedObjectIds.Add(netObject.NetworkId);
            singleton.spawnedObjects.Add(netObject.NetworkId, netObject);
            return go;
        }

        internal GameObject SpawnPlayerObject(int connectionId, uint networkId)
        {
            GameObject go = Instantiate(DefaultPlayerPrefab);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                Debug.LogWarning("MLAPI: Please add a NetworkedObject component to the root of the player prefab");
                netObject = go.AddComponent<NetworkedObject>();
            }
            netObject.OwnerClientId = connectionId;
            if(isServer)
            {
                netObject.NetworkId = singleton.GetNetworkObjectId();
            }
            else
            {
                netObject.NetworkId = networkId;
            }
            netObject.IsPlayerObject = true;
            connectedClients[connectionId].PlayerObject = go;
            singleton.spawnedObjectIds.Add(netObject.NetworkId);
            singleton.spawnedObjects.Add(netObject.NetworkId, netObject);
            return go;
        }

        internal static void OnDestroyObject(uint networkId)
        {
            if (!singleton.isServer)
                return;

            NetworkedObject netObject = singleton.spawnedObjects[networkId];
            singleton.releasedNetworkObjectIds.Push(networkId);
        }

        internal int AddIncomingMessageHandler(string name, Action<int, byte[]> action)
        {
            if(messageTypes.ContainsKey(name))
            {
                if(messageCallbacks.ContainsKey(messageTypes[name]))
                {
                    int handlerId = 0;
                    if (messageHandlerCounter.ContainsKey(messageTypes[name]))
                    {
                        if (!releasedMessageHandlerCounters.ContainsKey(messageTypes[name]))
                            releasedMessageHandlerCounters.Add(messageTypes[name], new Stack<int>());

                        if(releasedMessageHandlerCounters[messageTypes[name]].Count == 0)
                        {
                            handlerId = messageHandlerCounter[messageTypes[name]];
                            messageHandlerCounter[messageTypes[name]]++;
                        }
                        else
                        {
                            handlerId = releasedMessageHandlerCounters[messageTypes[name]].Pop();
                        }
                    }
                    else
                    {
                        messageHandlerCounter.Add(messageTypes[name], handlerId + 1);
                    }
                    messageCallbacks[messageTypes[name]].Add(handlerId, action);
                    return handlerId;
                }
                else
                {
                    messageCallbacks.Add(messageTypes[name], new Dictionary<int, Action<int, byte[]>>());
                    messageHandlerCounter.Add(messageTypes[name], 1);
                    messageCallbacks[messageTypes[name]].Add(0, action);
                    return 0;
                }
            }
            else
            {
                Debug.LogWarning("MLAPI: The message type " + name + " has not been registered. Please define it in the netConfig");
                return -1;
            }
        }

        internal void RemoveIncomingMessageHandler(string name, int counter)
        {
            if (counter == -1)
                return;

            if (messageTypes.ContainsKey(name) && messageCallbacks.ContainsKey(messageTypes[name]) && messageCallbacks[messageTypes[name]].ContainsKey(counter))
            {
                messageCallbacks[messageTypes[name]].Remove(counter);
                releasedMessageHandlerCounters[messageTypes[name]].Push(counter);
            }
        }

        private ConnectionConfig Init(NetworkingConfiguration netConfig)
        {
            NetworkConfig = netConfig;

            pendingClients = new HashSet<int>();
            connectedClients = new Dictionary<int, NetworkedClient>();
            messageBuffer = new byte[NetworkConfig.MessageBufferSize];
            channels = new Dictionary<string, int>();
            messageTypes = new Dictionary<string, ushort>();
            messageCallbacks = new Dictionary<ushort, Dictionary<int, Action<int, byte[]>>>();
            messageHandlerCounter = new Dictionary<ushort, int>();
            releasedMessageHandlerCounters = new Dictionary<ushort, Stack<int>>();
            spawnedObjects = new Dictionary<uint, NetworkedObject>();
            spawnedObjectIds = new List<uint>();
            releasedNetworkObjectIds = new Stack<uint>();
            if(NetworkConfig.HandleObjectSpawning)
            {
                NetworkedObject[] sceneObjects = FindObjectsOfType<NetworkedObject>();
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    uint networkId = GetNetworkObjectId();
                    spawnedObjects.Add(networkId, sceneObjects[i]);
                    spawnedObjectIds.Add(networkId);
                }
            }

            if (NetworkConfig.ConnectionApproval)
            {
                if(NetworkConfig.ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }

            NetworkTransport.Init();
            ConnectionConfig cConfig = new ConnectionConfig();

            //MLAPI channels and messageTypes
            NetworkConfig.Channels.Add("MLAPI_RELIABLE_FRAGMENTED", QosType.ReliableFragmented);
            messageTypes.Add("MLAPI_CONNECTION_REQUEST", 0);
            messageTypes.Add("MLAPI_CLIENT_LIST", 1);
            //messageTypes.Add("")


            HashSet<string> channelNames = new HashSet<string>();
            foreach (KeyValuePair<string, QosType> pair in NetworkConfig.Channels)
            {
                if(channelNames.Contains(pair.Key))
                {
                    Debug.LogWarning("MLAPI: Duplicate channel name: " + pair.Key);
                    continue;
                }
                int channelId = cConfig.AddChannel(pair.Value);
                channels.Add(pair.Key, channelId);
                channelNames.Add(pair.Key);
            }
            //0-32 are reserved for MLAPI messages
            for (ushort i = 32; i < NetworkConfig.MessageTypes.Count; i++)
            {
                messageTypes.Add(NetworkConfig.MessageTypes[i], i);
            }
            return cConfig;
        }


        public void StartServer(NetworkingConfiguration netConfig)
        {
            SceneManager.LoadScene(PlaySceneIndex);
            ConnectionConfig cConfig = Init(netConfig);
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            hostId = NetworkTransport.AddHost(hostTopology, NetworkConfig.Port);
            isServer = true;
            isClient = false;
            isListening = true;
        }

        public void StartClient(NetworkingConfiguration netConfig)
        {
            ConnectionConfig cConfig = Init(netConfig);
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            hostId =  NetworkTransport.AddHost(hostTopology, 0, null);
            //NetworkTransport.AddSceneId(PlaySceneIndex);

            isServer = false;
            isClient = true;
            isListening = true;
            localConnectionId = NetworkTransport.Connect(hostId, NetworkConfig.Address, NetworkConfig.Port, 0, out error);
        }

        public void StartHost(NetworkingConfiguration netConfig)
        {
            SceneManager.LoadScene(PlaySceneIndex);
            ConnectionConfig cConfig = Init(netConfig);
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            hostId = NetworkTransport.AddHost(hostTopology, NetworkConfig.Port, null);
            isServer = true;
            isClient = true;
            isListening = true;
            connectedClients.Add(-1, new NetworkedClient() { ClientId = -1 });
        }

        private void OnEnable()
        {
            if (singleton != null)
            {
                Debug.LogWarning("MLAPI: Multiple NetworkingManagers");
                return;
            }
            singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnDisable()
        {
            singleton = null;
        }

        //Receive stuff
        int hostId;
        int connectionId;
        int channelId;
        int receivedSize;
        byte error;
        private void Update()
        {
            if(isListening)
            {
                NetworkEventType eventType;
                int messagesProcessed = 0;
                do
                {
                    messagesProcessed++;
                    eventType = NetworkTransport.Receive(out hostId, out connectionId, out channelId, messageBuffer, messageBuffer.Length, out receivedSize, out error);
                    NetworkError networkError = (NetworkError)error;
                    if (networkError != NetworkError.Ok)
                    {
                        Debug.LogWarning("MLAPI: NetworkTransport receive error: " + networkError.ToString());
                        continue;
                    }
                    switch (eventType)
                    {
                        case NetworkEventType.ConnectEvent:
                            if (isServer)
                            {
                                pendingClients.Add(connectionId);
                                StartCoroutine(ApprovalTimeout(connectionId));
                            }
                            else
                            {
                                using (MemoryStream writeStream = new MemoryStream())
                                {
                                    using (BinaryWriter writer = new BinaryWriter(writeStream))
                                    {
                                        writer.Write(NetworkConfig.GetConfig());
                                        if(NetworkConfig.ConnectionApproval)
                                        {
                                            writer.Write(NetworkConfig.ConnectionData);
                                        }
                                    }
                                    Send(connectionId, "MLAPI_CONNECTION_REQUEST", "MLAPI_RELIABLE_FRAGMENTED", writeStream.ToArray());
                                }
                            }
                            break;
                        case NetworkEventType.DataEvent:
                            HandleIncomingData(connectionId, ref messageBuffer);
                            break;
                    }
                } while (eventType != NetworkEventType.Nothing && 
                    (messagesProcessed < NetworkConfig.MaxMessagesPerFrame || NetworkConfig.MaxMessagesPerFrame < 0));

            }
        }

        IEnumerator ApprovalTimeout(int connectionId)
        {
            float timeStarted = Time.time;
            //We yield every frame incase a pending client disconnects and someone else gets its connection id
            while (Time.time - timeStarted < NetworkConfig.ClientConnectionBufferTimeout && pendingClients.Contains(connectionId))
            {
                yield return null;
            }
            if(pendingClients.Contains(connectionId))
            {
                //Timeout
                DisconnectClient(connectionId);
            }
        }

        private void HandleIncomingData(int connectonId, ref byte[] data)
        {
            using(MemoryStream readStream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(readStream))
                {
                    ushort messageType = reader.ReadUInt16();

                    //Client tried to send a network message that was not the connection request before he was accepted.
                    if (pendingClients.Contains(connectionId) && messageType != 0)
                        return;

                    if (messageType >= 32)
                    {
                        //Custom message, invoke all message handlers
                        foreach (KeyValuePair<int, Action<int, byte[]>> pair in messageCallbacks[messageType])
                        {
                            pair.Value(connectionId, reader.ReadBytes(int.MaxValue));
                        }  
                    }
                    else
                    {
                        //MLAPI message
                        switch (messageType)
                        {
                            case 0: //Client to server > sends connection buffer
                                if (isServer)
                                {
                                    using (MemoryStream messageReadStream = new MemoryStream(reader.ReadBytes(int.MaxValue)))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            byte[] configHash = messageReader.ReadBytes(32);
                                            if (!NetworkConfig.CompareConfig(configHash))
                                            {
                                                Debug.LogWarning("MLAPI: NetworkConfiguration missmatch. The configuration between the server and client does not match.");
                                                DisconnectClient(connectionId);
                                                return;
                                            }
                                            if (NetworkConfig.ConnectionApproval)
                                            {
                                                byte[] connectionBuffer = messageReader.ReadBytes(int.MaxValue);
                                                NetworkConfig.ConnectionApprovalCallback(connectionBuffer, connectionId, HandleApproval);
                                            }
                                            else
                                            {
                                                HandleApproval(connectionId, true);
                                            }
                                        }
                                    }
                                }
                                break;
                            case 1: //Server informs client it has been approved:
                                if (isClient)
                                {
                                    SceneManager.LoadScene(PlaySceneIndex);
                                    using (MemoryStream messageReadStream = new MemoryStream(reader.ReadBytes(int.MaxValue)))
                                    {
                                        using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                                        {
                                            MyClientId = messageReader.ReadInt32();
                                            connectedClients.Add(MyClientId, new NetworkedClient() { ClientId = MyClientId });
                                            int clientCount = messageReader.ReadInt32();
                                            for (int i = 0; i < clientCount; i++)
                                            {
                                                int conId = messageReader.ReadInt32();
                                                connectedClients.Add(conId, new NetworkedClient() { ClientId = conId });
                                            }
                                            if(NetworkConfig.HandleObjectSpawning)
                                            {
                                                int objectCount = messageReader.ReadInt32();
                                                for (int i = 0; i < objectCount; i++)
                                                {
                                                    bool isPlayerObject = messageReader.ReadBoolean();
                                                    uint networkId = messageReader.ReadUInt32();
                                                    int ownerId = messageReader.ReadInt32();
                                                    int prefabId = messageReader.ReadInt32();
                                                    if(isPlayerObject)
                                                    {
                                                        SpawnPlayerObject(ownerId, networkId);
                                                    }
                                                    else
                                                    {
                                                        SpawnObject(prefabId, networkId, ownerId);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        internal void Send(int connectionId, string messageType, string channelName, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageTypes[messageType]);
                    writer.Write(data);
                    //2 bytes for message type
                    int size = data.Length + 2;
                    NetworkTransport.Send(hostId, connectionId, channels[channelName], stream.ToArray(), size, out error);
                }
            }
        }

        internal void Send(int[] connectonIds, string messageType, string channelName, byte[] data)
        {
            for (int i = 0; i < connectonIds.Length; i++)
            {
                Send(connectonIds[i], messageType, channelName, data);
            }
        }

        internal void Send(List<int> connectonIds, string messageType, string channelName, byte[] data)
        {
            for (int i = 0; i < connectonIds.Count; i++)
            {
                Send(connectonIds[i], messageType, channelName, data);
            }
        }

        internal void DisconnectClient(int connectionId)
        {
            if (!isServer)
                return;
            if (pendingClients.Contains(connectionId))
                pendingClients.Remove(connectionId);
            if (connectedClients.ContainsKey(connectionId))
                connectedClients.Remove(connectionId);
            NetworkTransport.Disconnect(hostId, connectionId, out error);
                
        }

        private void HandleApproval(int connectionId, bool approved)
        {
            if(approved)
            {
                NetworkedClient client = new NetworkedClient()
                {
                    ClientId = connectionId
                };
                connectedClients.Add(connectionId, client);
                if(NetworkConfig.HandleObjectSpawning)
                {
                    uint networkId = GetNetworkObjectId();
                    GameObject go = SpawnPlayerObject(connectionId, networkId);
                    connectedClients[connectionId].PlayerObject = go;
                }
                using (MemoryStream writeStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(writeStream))
                    {
                        writer.Write(connectionId);
                        writer.Write(connectedClients.Count - 1);
                        foreach (KeyValuePair<int, NetworkedClient> item in connectedClients)
                        {
                            //Our own ID. Already added as the first one above
                            if (item.Key == connectionId)
                                continue;
                            writer.Write(item.Key); //Connection id
                        }
                        if (NetworkConfig.HandleObjectSpawning)
                        {
                            writer.Write(spawnedObjectIds.Count);
                            for (int i = 0; i < spawnedObjectIds.Count; i++)
                            {
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].IsPlayerObject);
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].NetworkId);
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].OwnerClientId);
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].SpawnablePrefabId);
                            }
                        }
                    }
                    Send(connectionId, "MLAPI_CLIENT_LIST", "MLAPI_RELIABLE_FRAGMENTED", writeStream.ToArray());
                }
            }
            else
            {
                if (pendingClients.Contains(connectionId))
                    pendingClients.Remove(connectionId);
                NetworkTransport.Disconnect(hostId, connectionId, out error);
            }
        }
    }
}