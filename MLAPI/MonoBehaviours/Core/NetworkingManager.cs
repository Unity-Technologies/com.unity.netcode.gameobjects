using MLAPI.NetworkingManagerComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

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
        public bool isHost
        {
            get
            {
                return isServer && isClient;
            }
        }
        private bool isListening;
        private byte[] messageBuffer;
        internal int serverClientId;


        public NetworkingConfiguration NetworkConfig;

        private void OnValidate()
        {
            for (int i = 0; i < SpawnablePrefabs.Count; i++)
            {
                NetworkedObject netObject = SpawnablePrefabs[i].GetComponentInChildren<NetworkedObject>();
                if (netObject == null)
                {
                    Debug.LogWarning("MLAPI: All SpawnablePrefabs need a NetworkedObject component. Please add one to the prefab " + SpawnablePrefabs[i].gameObject.name);
                    continue;
                }
                netObject.SpawnablePrefabIndex = i;
            }
        }

        private ConnectionConfig Init(NetworkingConfiguration netConfig)
        {
            NetworkConfig = netConfig;

            pendingClients = new HashSet<int>();
            connectedClients = new Dictionary<int, NetworkedClient>();
            messageBuffer = new byte[NetworkConfig.MessageBufferSize];
            MessageManager.channels = new Dictionary<string, int>();
            MessageManager.messageTypes = new Dictionary<string, ushort>();
            MessageManager.messageCallbacks = new Dictionary<ushort, Dictionary<int, Action<int, byte[]>>>();
            MessageManager.messageHandlerCounter = new Dictionary<ushort, int>();
            MessageManager.releasedMessageHandlerCounters = new Dictionary<ushort, Stack<int>>();
            MessageManager.targetedMessages = new Dictionary<ushort, Dictionary<uint, List<int>>>();
            SpawnManager.spawnedObjects = new Dictionary<uint, NetworkedObject>();
            SpawnManager.releasedNetworkObjectIds = new Stack<uint>();
            if (NetworkConfig.HandleObjectSpawning)
            {
                NetworkedObject[] sceneObjects = FindObjectsOfType<NetworkedObject>();
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    uint networkId = SpawnManager.GetNetworkObjectId();
                    SpawnManager.spawnedObjects.Add(networkId, sceneObjects[i]);
                }
            }

            NetworkTransport.Init();
            ConnectionConfig cConfig = new ConnectionConfig();

            //MLAPI channels and messageTypes
            NetworkConfig.Channels.Add("MLAPI_RELIABLE_FRAGMENTED", QosType.ReliableFragmented);
            NetworkConfig.Channels.Add("MLAPI_POSITION_UPDATE", QosType.StateUpdate);
            MessageManager.messageTypes.Add("MLAPI_CONNECTION_REQUEST", 0);
            MessageManager.messageTypes.Add("MLAPI_CONNECTION_APPROVED", 1);
            MessageManager.messageTypes.Add("MLAPI_ADD_OBJECT", 2);
            MessageManager.messageTypes.Add("MLAPI_CLIENT_DISCONNECT", 3);
            MessageManager.messageTypes.Add("MLAPI_DESTROY_OBJECT", 4);
            NetworkConfig.MessageTypes.Add("MLAPI_OnRecieveTransformFromClient");
            NetworkConfig.MessageTypes.Add("MLAPI_OnRecieveTransformFromServer");

            HashSet<string> channelNames = new HashSet<string>();
            foreach (KeyValuePair<string, QosType> pair in NetworkConfig.Channels)
            {
                if(channelNames.Contains(pair.Key))
                {
                    Debug.LogWarning("MLAPI: Duplicate channel name: " + pair.Key);
                    continue;
                }
                int channelId = cConfig.AddChannel(pair.Value);
                MessageManager.channels.Add(pair.Key, channelId);
                channelNames.Add(pair.Key);
            }
            //0-32 are reserved for MLAPI messages
            ushort messageId = 32;
            for (ushort i = 0; i < NetworkConfig.MessageTypes.Count; i++)
            {
                MessageManager.messageTypes.Add(NetworkConfig.MessageTypes[i], messageId);
                messageId++;
            }
            return cConfig;
        }


        public void StartServer(NetworkingConfiguration netConfig)
        {
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
            Shutdown();
        }

        public void StopHost()
        {
            StopServer();
            //We don't stop client since we dont actually have a transport connection to our own host. We just handle host messages directly in the MLAPI
        }

        public void StopClient()
        {
            NetworkTransport.Disconnect(hostId, serverClientId, out error);
            Shutdown();
        }

        public void StartHost(NetworkingConfiguration netConfig)
        {
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
                SpawnManager.SpawnPlayerObject(-1, 0);
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
            Shutdown();
        }

        private void Shutdown()
        {
            isListening = false;
            isClient = false;
            isServer = false;
            NetworkTransport.Shutdown();
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

        private void HandleIncomingData(int connectonId, byte[] data)
        {
            using(MemoryStream readStream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(readStream))
                {
                    ushort messageType = reader.ReadUInt16();
                    bool targeted = reader.ReadBoolean();
                    uint targetNetworkId = 0;
                    if(targeted)
                        targetNetworkId = reader.ReadUInt32();

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
                        if(targeted)
                        {
                            List<int> handlerIds = MessageManager.targetedMessages[messageType][targetNetworkId];
                            Debug.Log(handlerIds.Count);
                            for (int i = 0; i < handlerIds.Count; i++)
                            {
                                MessageManager.messageCallbacks[messageType][handlerIds[i]](clientId, incommingData);
                            }
                        }
                        else
                        {
                            foreach (KeyValuePair<int, Action<int, byte[]>> pair in MessageManager.messageCallbacks[messageType])
                            {
                                pair.Value(clientId, incommingData);
                            }
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
                                                        SpawnManager.SpawnPlayerObject(ownerId, networkId);
                                                    }
                                                    else
                                                    {
                                                        SpawnManager.SpawnObject(prefabId, networkId, ownerId);
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
                                                    SpawnManager.SpawnPlayerObject(ownerId, networkId);
                                                }
                                                else
                                                {
                                                    SpawnManager.SpawnObject(prefabId, networkId, ownerId);
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
                        }
                    }
                }
            }
        }

        internal void Send(int clientId, string messageType, string channelName, byte[] data, uint? networkId = null)
        {
            if(isHost && clientId == -1)
            {
                //Host trying to send data to it's own client
                if (networkId == null)
                    MessageManager.InvokeMessageHandlers(messageType, data, clientId);
                else
                    MessageManager.InvokeTargetedMessageHandler(messageType, data, clientId, networkId.Value);
                return;
            }
            else if(clientId == -1)
            {
                //Client trying to send data to host
                clientId = serverClientId;
            }
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                byte[] dataToSend = stream.ToArray();
                NetworkTransport.Send(hostId, clientId, MessageManager.channels[channelName], dataToSend, dataToSend.Length, out error);
            }
        }

        internal void Send(int[] clientIds, string messageType, string channelName, byte[] data, uint? networkId = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                byte[] dataToSend = stream.ToArray();
                int channel = MessageManager.channels[channelName];
                for (int i = 0; i < clientIds.Length; i++)
                {
                    int clientId = clientIds[i];
                    if (isHost && clientId == -1)
                    {
                        if (networkId == null)
                            MessageManager.InvokeMessageHandlers(messageType, data, clientId);
                        else
                            MessageManager.InvokeTargetedMessageHandler(messageType, data, clientId, networkId.Value);
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.Send(hostId, clientId, channel, dataToSend, dataToSend.Length, out error);
                }
            }
        }

        internal void Send(List<int> clientIds, string messageType, string channelName, byte[] data, uint? networkId = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                byte[] dataToSend = stream.ToArray();
                int channel = MessageManager.channels[channelName];
                for (int i = 0; i < clientIds.Count; i++)
                {
                    int clientId = clientIds[i];
                    if (isHost && clientId == -1)
                    {
                        if (networkId == null)
                            MessageManager.InvokeMessageHandlers(messageType, data, clientId);
                        else
                            MessageManager.InvokeTargetedMessageHandler(messageType, data, clientId, networkId.Value);
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.Send(hostId, clientId, channel, dataToSend, dataToSend.Length, out error);
                }
            }
        }

        internal void Send(string messageType, string channelName, byte[] data, uint? networkId = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                byte[] dataToSend = stream.ToArray();
                int channel = MessageManager.channels[channelName];
                foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
                {
                    int clientId = pair.Key;
                    if(isHost && pair.Key == -1)
                    {
                        if (networkId == null)
                            MessageManager.InvokeMessageHandlers(messageType, data, clientId);
                        else
                            MessageManager.InvokeTargetedMessageHandler(messageType, data, clientId, networkId.Value);
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.Send(hostId, clientId, channel, dataToSend, dataToSend.Length, out error);
                    
                }
            }
        }

        internal void Send(string messageType, string channelName, byte[] data, int clientIdToIgnore, uint? networkId = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
                byte[] dataToSend = stream.ToArray();
                int channel = MessageManager.channels[channelName];
                foreach (KeyValuePair<int, NetworkedClient> pair in connectedClients)
                {
                    if (pair.Key == clientIdToIgnore)
                        continue;
                    int clientId = pair.Key;
                    if (isHost && pair.Key == -1)
                    {
                        if (networkId == null)
                            MessageManager.InvokeMessageHandlers(messageType, data, clientId);
                        else
                            MessageManager.InvokeTargetedMessageHandler(messageType, data, clientId, networkId.Value);
                        continue;
                    }
                    else if (clientId == -1)
                    {
                        //Client trying to send data to host
                        clientId = serverClientId;
                    }
                    NetworkTransport.Send(hostId, clientId, channel, dataToSend, dataToSend.Length, out error);
                }
            }
        }

        private void DisconnectClient(int clientId)
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
                    for (int i = 0; i < connectedClients[clientId].OwnedObjects.Count; i++)
                    {
                        Destroy(connectedClients[clientId].OwnedObjects[i].gameObject);
                    }
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
                    uint networkId = SpawnManager.GetNetworkObjectId();
                    GameObject go = SpawnManager.SpawnPlayerObject(clientId, networkId);
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
                            foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
                            {
                                if (pair.Value.ServerOnly)
                                    continue;
                                else
                                    amountOfObjectsToSend++;
                            }
                            writer.Write(amountOfObjectsToSend);

                            foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
                            {
                                if (pair.Value.ServerOnly)
                                    continue;
                                else
                                    amountOfObjectsToSend++;

                                if (pair.Value.ServerOnly)
                                    continue;
                                writer.Write(pair.Value.isPlayerObject);
                                writer.Write(pair.Value.NetworkId);
                                writer.Write(pair.Value.OwnerClientId);
                                writer.Write(pair.Value.SpawnablePrefabIndex);
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