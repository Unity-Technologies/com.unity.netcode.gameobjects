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
        public static NetworkingManager singleton;
        //Client only, what my connectionId is on the server
        public int MyClientId;
        //Server only
        private Dictionary<int, NetworkedClient> connectedClients;
        private HashSet<int> pendingClients;
        internal bool isServer;
        private bool isListening;
        private byte[] messageBuffer;
        private Dictionary<string, int> channels;
        private Dictionary<string, ushort> messageTypes;
        private Dictionary<ushort, Dictionary<int, Action<int, byte[]>>> messageCallbacks;
        private Dictionary<ushort, int> messageHandlerCounter;
        private Dictionary<ushort, Stack<int>> releasedMessageHandlerCounters;

        public NetworkingConfiguration NetworkConfig;

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

            if (NetworkConfig.ConnectionApproval)
            {
                if(NetworkConfig.ConnectionApprovalCallback == null)
                {
                    Debug.LogWarning("MLAPI: No ConnectionAproval callback defined. Connection aproval will timeout");
                }
            }

            NetworkTransport.Init();
            ConnectionConfig cConfig = new ConnectionConfig();

            //MLAPI channels and messageTypes
            NetworkConfig.Channels.Add("MLAPI_RELIABLE_FRAGMENTED", QosType.ReliableFragmented);
            messageTypes.Add("CONNECTION_REQUEST", 0);


            foreach (KeyValuePair<string, QosType> pair in NetworkConfig.Channels)
            {
                channels.Add(pair.Key, cConfig.AddChannel(pair.Value));
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
            ConnectionConfig cConfig = Init(netConfig);
            HostTopology hostTopology = new HostTopology(cConfig, NetworkConfig.MaxConnections);
            NetworkTransport.AddHost(hostTopology, NetworkConfig.Port);
            isServer = true;
            isListening = true;
        }

        private void OnEnable()
        {
            if (singleton != null)
            {
                Debug.LogWarning("MLAPI: Multiple NetworkingManagers");
                return;
            }
            singleton = this;
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
                    eventType = NetworkTransport.Receive(out hostId, out connectionId, out channelId, messageBuffer, NetworkConfig.MessageBufferSize, out receivedSize, out error);
                    NetworkError networkError = (NetworkError)error;
                    if (networkError != NetworkError.Ok)
                    {
                        Debug.LogWarning("MLAPI: NetworkTransport receive error: " + networkError.ToString());
                        return;
                    }
                    switch (eventType)
                    {
                        case NetworkEventType.ConnectEvent:
                            if(isServer)
                            {
                                if (NetworkConfig.ConnectionApproval)
                                {
                                    pendingClients.Add(connectionId);
                                    StartCoroutine(ApprovalTimeout(connectionId));
                                }
                                else
                                {
                                    //Connect
                                    HandleApproval(connectionId, false);
                                }
                            }
                            else
                            {
                                if (NetworkConfig.ConnectionApproval)
                                    Send(connectionId, "CONNECTION_REQUEST", "MLAPI_RELIABLE_FRAGMENTED", NetworkConfig.ConnectionData);
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
            else
            {
                //If the key nolonger exist in pending and not in connected, they disconnected
                if(!connectedClients.ContainsKey(connectionId))
                {
                    pendingClients.Remove(connectionId);
                }
            }
        }

        private void HandleIncomingData(int connectonId, ref byte[] data)
        {
            using(MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                ushort protocolVersion = reader.ReadUInt16();
                ushort messageType = reader.ReadUInt16();
                if(messageType >= 32)
                {
                    //Custom message
                    if(protocolVersion != NetworkConfig.ProtocolVersion)
                    {
                        Debug.LogWarning("MLAPI: Protocol version not matching");
                        DisconnectClient(connectionId);
                    }
                } 
                else
                {
                    //MLAPI message
                    switch(messageType)
                    {
                        case 0: //Client to server > sends connection buffer
                            byte[] connectionBuffer = reader.ReadBytes(int.MaxValue);
                            NetworkConfig.ConnectionApprovalCallback(connectionBuffer, connectionId, HandleApproval);
                            break;
                        case 1: //Server gives client it's connectionId
                            break;
                        case 2: //Server informs client of spawned objects
                            break;
                        case 3: //Server informs client of spawned players
                            break;
                    }
                }
            }
        }

        protected void Send(int connectionId, string messageType, string channelName, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(NetworkConfig.ProtocolVersion);
                writer.Write(messageTypes[messageType]);
                writer.Write(data);
                NetworkTransport.Send(hostId, connectionId, channels[channelName], data, data.Length, out error);
            }
        }

        protected void Send(int[] connectonIds, string messageType, string channelName, byte[] data)
        {
            for (int i = 0; i < connectonIds.Length; i++)
            {
                Send(connectonIds[i], messageType, channelName, data);
            }
        }

        protected void Send(List<int> connectonIds, string messageType, string channelName, byte[] data)
        {
            for (int i = 0; i < connectonIds.Count; i++)
            {
                Send(connectonIds[i], messageType, channelName, data);
            }
        }

        protected void DisconnectClient(int connectionId)
        {
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