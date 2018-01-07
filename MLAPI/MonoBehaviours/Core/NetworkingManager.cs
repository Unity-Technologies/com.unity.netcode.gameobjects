using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        internal Dictionary<int, NetworkedClient> connectedClients;
        public Dictionary<int, NetworkedClient> ConnectedClients
        {
            get
            {
                return connectedClients;
            }
        }
        internal HashSet<int> pendingClients;
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
        internal int serverClientId;
        public int PlaySceneIndex;
        public int MenuSceneIndex;
        internal Dictionary<uint, NetworkedObject> spawnedObjects;
        public Dictionary<uint, NetworkedObject> SpawnedObjects
        {
            get
            {
                return spawnedObjects;
            }
        }
        public List<uint> spawnedObjectIds;
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

        internal GameObject SpawnPlayerObject(int clientId, uint networkId)
        {
            GameObject go = Instantiate(DefaultPlayerPrefab);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                Debug.LogWarning("MLAPI: Please add a NetworkedObject component to the root of the player prefab");
                netObject = go.AddComponent<NetworkedObject>();
            }
            netObject.OwnerClientId = clientId;
            if(isServer)
            {
                netObject.NetworkId = singleton.GetNetworkObjectId();
            }
            else
            {
                netObject.NetworkId = networkId;
            }
            netObject.IsPlayerObject = true;
            connectedClients[clientId].PlayerObject = go;
            singleton.spawnedObjectIds.Add(netObject.NetworkId);
            singleton.spawnedObjects.Add(netObject.NetworkId, netObject);
            return go;
        }

        internal static void OnDestroyObject(uint networkId)
        {
            if (!singleton.isServer)
                return;

            if (!singleton.spawnedObjects.ContainsKey(networkId))
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
                if (!releasedMessageHandlerCounters.ContainsKey(messageTypes[name]))
                    releasedMessageHandlerCounters.Add(messageTypes[name], new Stack<int>());
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
            if (NetworkConfig.HandleObjectSpawning)
            {
                NetworkedObject[] sceneObjects = FindObjectsOfType<NetworkedObject>();
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    uint networkId = GetNetworkObjectId();
                    spawnedObjects.Add(networkId, sceneObjects[i]);
                    spawnedObjectIds.Add(networkId);
                }
            }

            NetworkTransport.Init();
            ConnectionConfig cConfig = new ConnectionConfig();

            //MLAPI channels and messageTypes
            NetworkConfig.Channels.Add("MLAPI_RELIABLE_FRAGMENTED", QosType.ReliableFragmented);
            messageTypes.Add("MLAPI_CONNECTION_REQUEST", 0);
            messageTypes.Add("MLAPI_CONNECTION_APPROVED", 1);
            messageTypes.Add("MLAPI_ADD_OBJECT", 2);
            messageTypes.Add("MLAPI_CLIENT_DISCONNECT", 3);


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
            ushort messageId = 32;
            for (ushort i = 0; i < NetworkConfig.MessageTypes.Count; i++)
            {
                messageTypes.Add(NetworkConfig.MessageTypes[i], messageId);
                messageId++;
            }
            return cConfig;
        }


        public void StartServer(NetworkingConfiguration netConfig)
        {
            SceneManager.LoadScene(PlaySceneIndex);
            ConnectionConfig cConfig = Init(netConfig);
            if (NetworkConfig.ConnectionApproval)
            {
                if (NetworkConfig.ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }
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

            isServer = false;
            isClient = true;
            isListening = true;
            serverClientId = NetworkTransport.Connect(hostId, NetworkConfig.Address, NetworkConfig.Port, 0, out error);
        }

        public void StartHost(NetworkingConfiguration netConfig)
        {
            SceneManager.LoadScene(PlaySceneIndex);
            ConnectionConfig cConfig = Init(netConfig);
            if (NetworkConfig.ConnectionApproval)
            {
                if (NetworkConfig.ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionApproval callback defined. Connection approval will timeout");
                }
            }
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            hostId = NetworkTransport.AddHost(hostTopology, NetworkConfig.Port, null);
            isServer = true;
            isClient = true;
            isListening = true;
            connectedClients.Add(-1, new NetworkedClient() { ClientId = -1 });
            if(NetworkConfig.HandleObjectSpawning)
            {
                SpawnPlayerObject(-1, 0);
            }
        }

        private void OnEnable()
        {
            if (singleton != null)
            {
                Debug.LogWarning("MLAPI: Multiple NetworkingManagers");
                Destroy(this);
                return;
            }
            singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnDestroy()
        {
            singleton = null;
        }

        //Receive stuff
        int hostId;
        int clientId;
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
                    eventType = NetworkTransport.Receive(out hostId, out clientId, out channelId, messageBuffer, messageBuffer.Length, out receivedSize, out error);
                    NetworkError networkError = (NetworkError)error;
                    if(networkError == NetworkError.Timeout)
                    {
                        //Client timed out. 
                        if (isServer)
                        {
                            OnClientDisconnect(clientId);
                            continue;
                        }
                    }
                    else if (networkError != NetworkError.Ok)
                    {
                        Debug.LogWarning("MLAPI: NetworkTransport receive error: " + networkError.ToString());
                        continue;
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
                                using (MemoryStream writeStream = new MemoryStream())
                                {
                                    using (BinaryWriter writer = new BinaryWriter(writeStream))
                                    {
                                        writer.Write(NetworkConfig.GetConfig());
                                        if(NetworkConfig.ConnectionApproval)
                                        {
                                            writer.Write((ushort)NetworkConfig.ConnectionData.Length);
                                            writer.Write(NetworkConfig.ConnectionData);
                                        }
                                    }
                                    Send(clientId, "MLAPI_CONNECTION_REQUEST", "MLAPI_RELIABLE_FRAGMENTED", writeStream.ToArray());
                                }
                            }
                            break;
                        case NetworkEventType.DataEvent:
                            HandleIncomingData(clientId, messageBuffer);
                            break;
                        case NetworkEventType.DisconnectEvent:
                            if(isServer)
                            {
                                OnClientDisconnect(clientId);
                            }
                            break;
                    }
                } while (eventType != NetworkEventType.Nothing && 
                    (messagesProcessed < NetworkConfig.MaxMessagesPerFrame || NetworkConfig.MaxMessagesPerFrame < 0));

            }
        }

        IEnumerator ApprovalTimeout(int clientId)
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

        private void HandleIncomingData(int connectonId, byte[] data)
        {
            using(MemoryStream readStream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(readStream))
                {
                    ushort messageType = reader.ReadUInt16();

                    //Client tried to send a network message that was not the connection request before he was accepted.
                    if (isServer && pendingClients.Contains(clientId) && messageType != 0)
                    {
                        Debug.LogWarning("MLAPI: Message recieved from clientId " + clientId + " before it has been accepted");
                        return;
                    }

                    ushort bytesToRead = reader.ReadUInt16();
                    byte[] incommingData = reader.ReadBytes(bytesToRead);
                    if (messageType >= 32)
                    {
                        //Custom message, invoke all message handlers
                        foreach (KeyValuePair<int, Action<int, byte[]>> pair in messageCallbacks[messageType])
                        {
                            pair.Value(clientId, incommingData);
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
                                            if (NetworkConfig.ConnectionApproval)
                                            {
                                                ushort bufferSize = messageReader.ReadUInt16();
                                                byte[] connectionBuffer = messageReader.ReadBytes(bufferSize);
                                                NetworkConfig.ConnectionApprovalCallback(connectionBuffer, clientId, HandleApproval);
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
                                    //SceneManager.LoadScene(PlaySceneIndex);
                                    using (MemoryStream messageReadStream = new MemoryStream(incommingData))
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
                                                if (isPlayerObject)
                                                {
                                                    connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                                                    SpawnPlayerObject(ownerId, networkId);
                                                }
                                                else
                                                {
                                                    SpawnObject(prefabId, networkId, ownerId);
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
                        }
                    }
                }
            }
        }

        internal void Send(int clientId, string messageType, string channelName, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageTypes[messageType]);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                //2 bytes for message type and 2 bytes for byte length
                int size = data.Length + 4;
                byte[] dataToSend = stream.ToArray();
                NetworkTransport.Send(hostId, clientId, channels[channelName], dataToSend, dataToSend.Length, out error);
            }
        }

        internal void Send(int[] clientIds, string messageType, string channelName, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageTypes[messageType]);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                //2 bytes for message type and 2 bytes for byte length
                int size = data.Length + 4;
                byte[] dataToSend = stream.ToArray();
                int channel = channels[channelName];
                for (int i = 0; i < clientIds.Length; i++)
                {
                    NetworkTransport.Send(hostId, clientIds[i], channel, dataToSend, size, out error);
                }
            }
        }

        internal void Send(List<int> clientIds, string messageType, string channelName, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageTypes[messageType]);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                //2 bytes for message type and 2 bytes for byte length
                int size = data.Length + 4;
                byte[] dataToSend = stream.ToArray();
                int channel = channels[channelName];
                for (int i = 0; i < clientIds.Count; i++)
                {
                    NetworkTransport.Send(hostId, clientIds[i], channel, dataToSend, size, out error);
                }
            }
        }

        internal void Send(string messageType, string channelName, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageTypes[messageType]);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                //2 bytes for message type and 2 bytes for byte length
                int size = data.Length + 4;
                byte[] dataToSend = stream.ToArray();
                int channel = channels[channelName];
                foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
                {
                    NetworkTransport.Send(hostId, pair.Key, channel, dataToSend, size, out error);
                }
            }
        }

        internal void Send(string messageType, string channelName, byte[] data, int clientIdToIgnore)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageTypes[messageType]);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                //2 bytes for message type and 2 bytes for byte length
                int size = data.Length + 4;
                byte[] dataToSend = stream.ToArray();
                int channel = channels[channelName];
                foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
                {
                    if (pair.Key == clientIdToIgnore)
                        continue;
                    NetworkTransport.Send(hostId, pair.Key, channel, dataToSend, size, out error);
                }
            }
        }

        internal void DisconnectClient(int clientId)
        {
            if (!isServer)
                return;
            if (pendingClients.Contains(clientId))
                pendingClients.Remove(clientId);
            if (connectedClients.ContainsKey(clientId))
                connectedClients.Remove(clientId);
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
                    Destroy(connectedClients[clientId].PlayerObject);
                }
                connectedClients.Remove(clientId);
            }

            if (isServer)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(clientId);
                    }
                    Send("MLAPI_CLIENT_DISCONNECT", "MLAPI_RELIABLE_FRAGMENTED", stream.ToArray(), clientId);
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
                NetworkedClient client = new NetworkedClient()
                {
                    ClientId = clientId
                };
                connectedClients.Add(clientId, client);
                
                if(NetworkConfig.HandleObjectSpawning)
                {
                    uint networkId = GetNetworkObjectId();
                    GameObject go = SpawnPlayerObject(clientId, networkId);
                    connectedClients[clientId].PlayerObject = go;
                }
                using (MemoryStream writeStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(writeStream))
                    {
                        writer.Write(clientId);
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
                            int amountOfObjectsToSend = 0;
                            for (int i = 0; i < spawnedObjectIds.Count; i++)
                            {
                                if (spawnedObjects[spawnedObjectIds[i]].ServerOnly)
                                    continue;
                                else
                                    amountOfObjectsToSend++;
                            }
                            writer.Write(amountOfObjectsToSend);
                            for (int i = 0; i < spawnedObjectIds.Count; i++)
                            {
                                if (spawnedObjects[spawnedObjectIds[i]].ServerOnly)
                                    continue;
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].IsPlayerObject);
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].NetworkId);
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].OwnerClientId);
                                writer.Write(spawnedObjects[spawnedObjectIds[i]].SpawnablePrefabId);
                            }
                        }
                    }
                    Send(clientId, "MLAPI_CONNECTION_APPROVED", "MLAPI_RELIABLE_FRAGMENTED", writeStream.ToArray());
                }

                //Inform old clients of the new player
                using (MemoryStream stream = new MemoryStream())
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
                    Send("MLAPI_ADD_OBJECT", "MLAPI_RELIABLE_FRAGMENTED", stream.ToArray(), clientId);
                }
            }
            else
            {
                if (pendingClients.Contains(clientId))
                    pendingClients.Remove(clientId);
                NetworkTransport.Disconnect(hostId, clientId, out error);
            }
        }
    }
}