#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using MLAPI.Logging;
using UnityEngine.Networking;

namespace MLAPI.Transports.UNET
{
    public class UnetTransport : Transport
    {
        // Inspector / settings
        public int MessageBufferSize = 1024 * 5;
        public int MaxConnections = 100;

        public string ConnectAddress = "127.0.0.1";
        public int ConnectPort = 7777;
        public int ServerListenPort = 7777;
        public int ServerWebsocketListenPort = 8887;
        public bool SupportWebsocket = false;
        
        // Relay
        public bool UseMLAPIRelay = false;
        public string MLAPIRelayAddress = "184.72.104.138";
        public int MLAPIRelayPort = 8888;
        
        // Runtime / state
        private byte[] messageBuffer;
        private WeakReference temporaryBufferReference;

        // Lookup / translation
        private readonly Dictionary<string, int> channelNameToId = new Dictionary<string, int>();
        private readonly Dictionary<int, string> channelIdToName = new Dictionary<int, string>();
        private int serverConnectionId;
        private int serverHostId;

        public override ulong ServerClientId => GetMLAPIClientId(0, 0, true);
        
        public override void Send(ulong clientId, ArraySegment<byte> data, string channelName, bool skipQueue)
        {
            UpdateRelay();
            
            GetUnetConnectionDetails(clientId, out byte hostId, out ushort connectionId);
            
            int channelId = channelNameToId[channelName];

            byte[] buffer;

            if (data.Offset > 0)
            {
                // UNET cant handle this, do a copy

                if (messageBuffer.Length >= data.Count)
                {
                    buffer = messageBuffer;
                }
                else
                {
                    if (temporaryBufferReference != null && temporaryBufferReference.IsAlive && ((byte[]) temporaryBufferReference.Target).Length >= data.Count)
                    {
                        buffer = (byte[])temporaryBufferReference.Target;
                    }
                    else
                    {
                        buffer = new byte[data.Count];
                        temporaryBufferReference = new WeakReference(buffer);
                    }
                }
                
                Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, data.Count);
            }
            else
            {
                buffer = data.Array;
            }

            if (skipQueue)
            {
                RelayTransport.Send(hostId, connectionId, channelId, buffer, data.Count, out byte error);
            }
            else
            {
                RelayTransport.QueueMessageForSending(hostId, connectionId, channelId, buffer, data.Count, out byte error);
            }
        }

        public override void FlushSendQueue(ulong clientId)
        {
            UpdateRelay();
            
            GetUnetConnectionDetails(clientId, out byte hostId, out ushort connectionId);
            
            RelayTransport.SendQueuedMessages(hostId, connectionId, out byte error);
        }

        public override NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload)
        {
            UpdateRelay();
            
            NetworkEventType eventType = RelayTransport.Receive(out int hostId, out int connectionId, out int channelId, messageBuffer, messageBuffer.Length, out int receivedSize, out byte error);

            clientId = GetMLAPIClientId((byte) hostId, (ushort) connectionId, false);

            NetworkError networkError = (NetworkError) error;
            
            if (networkError == NetworkError.MessageToLong)
            {
                byte[] tempBuffer;
                
                if (temporaryBufferReference != null && temporaryBufferReference.IsAlive && ((byte[]) temporaryBufferReference.Target).Length >= receivedSize)
                {
                    tempBuffer = (byte[])temporaryBufferReference.Target;
                }
                else
                {
                    tempBuffer = new byte[receivedSize];
                    temporaryBufferReference = new WeakReference(tempBuffer);
                }
                
                eventType = RelayTransport.Receive(out hostId, out connectionId, out channelId, tempBuffer, tempBuffer.Length, out receivedSize, out error);
                payload = new ArraySegment<byte>(tempBuffer, 0, receivedSize);
            }
            else
            {
                payload = new ArraySegment<byte>(messageBuffer, 0, receivedSize);
            }

            channelName = channelIdToName[channelId];


            if (networkError == NetworkError.Timeout)
            {
                // In UNET. Timeouts are not disconnects. We have to translate that here.
                eventType = NetworkEventType.DisconnectEvent;
            }

            // Translate NetworkEventType to NetEventType
            switch (eventType)
            {
                case NetworkEventType.DataEvent:
                    return NetEventType.Data;
                case NetworkEventType.ConnectEvent:
                    return NetEventType.Connect;
                case NetworkEventType.DisconnectEvent:
                    return NetEventType.Disconnect;
                case NetworkEventType.Nothing:
                    return NetEventType.Nothing;
                case NetworkEventType.BroadcastEvent:
                    return NetEventType.Nothing;
            }
            
            return NetEventType.Nothing;
        }

        public override void StartClient()
        {
            UpdateRelay();
            
            serverHostId = RelayTransport.AddHost(new HostTopology(GetConfig(), 1), false);
            
            serverConnectionId = RelayTransport.Connect(serverHostId, ConnectAddress, ConnectPort, 0, out byte error);
        }

        public override void StartServer()
        {
            UpdateRelay();
            
            HostTopology topology = new HostTopology(GetConfig(), MaxConnections);
            
            if (SupportWebsocket)
            {
                if (!UseMLAPIRelay)
                {
                    int websocketHostId = NetworkTransport.AddWebsocketHost(topology, ServerWebsocketListenPort);
                }
                else
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Cannot create websocket host when using MLAPI relay");
                }
                    
            }
            
            int normalHostId = RelayTransport.AddHost(topology, ServerListenPort, true);
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            UpdateRelay();
            
            GetUnetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            RelayTransport.Disconnect((int) hostId, (int) connectionId, out byte error);
        }

        public override void DisconnectLocalClient()
        {
            UpdateRelay();
            
            RelayTransport.Disconnect(serverHostId, serverConnectionId, out byte error);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            GetUnetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            if (UseMLAPIRelay)
            {
                return 0;
            }
            else
            {
                return (ulong)NetworkTransport.GetCurrentRTT((int) hostId, (int) connectionId, out byte error);
            }
        }

        public override void Shutdown()
        {
            NetworkTransport.Shutdown();
        }

        public override void Init()
        {
            messageBuffer = new byte[MessageBufferSize];
            
            NetworkTransport.Init();
        }
        
        public ulong GetMLAPIClientId(byte hostId, ushort connectionId, bool isServer)
        {
            if (isServer)
            {
                return 0;
            }
            else
            {
                return ((ulong)connectionId | (ulong)hostId << 16) + 1;
            }
        }

        public void GetUnetConnectionDetails(ulong clientId, out byte hostId, out ushort connectionId)
        {
            if (clientId == 0)
            {
                hostId = (byte)serverHostId;
                connectionId = (ushort)serverConnectionId;
            }
            else
            {
                hostId = (byte) ((clientId - 1) >> 16);
                connectionId = (ushort) ((clientId - 1));
            }
        }

        public ConnectionConfig GetConfig()
        {
            ConnectionConfig config = new ConnectionConfig();
            
            for (int i = 0; i < MLAPI_CHANNELS.Length; i++)
            {
                int channelId = AddChannel(MLAPI_CHANNELS[i].Type, config);
                
                channelIdToName.Add(channelId, MLAPI_CHANNELS[i].Name);
                channelNameToId.Add(MLAPI_CHANNELS[i].Name, channelId);
            }

            return config;
        }
        
        public int AddChannel(ChannelType type, ConnectionConfig config)
        {
            switch (type)
            {
                case ChannelType.Unreliable:
                    return config.AddChannel(QosType.Unreliable);
                case ChannelType.UnreliableFragmented:
                    return config.AddChannel(QosType.UnreliableFragmented);
                case ChannelType.UnreliableSequenced:
                    return config.AddChannel(QosType.UnreliableSequenced);
                case ChannelType.Reliable:
                    return config.AddChannel(QosType.Reliable);
                case ChannelType.ReliableFragmented:
                    return config.AddChannel(QosType.ReliableFragmented);
                case ChannelType.ReliableSequenced:
                    return config.AddChannel(QosType.ReliableSequenced);
                case ChannelType.StateUpdate:
                    return config.AddChannel(QosType.StateUpdate);
                case ChannelType.ReliableStateUpdate:
                    return config.AddChannel(QosType.ReliableStateUpdate);
                case ChannelType.AllCostDelivery:
                    return config.AddChannel(QosType.AllCostDelivery);
                case ChannelType.UnreliableFragmentedSequenced:
                    return config.AddChannel(QosType.UnreliableFragmentedSequenced);
                case ChannelType.ReliableFragmentedSequenced:
                    return config.AddChannel(QosType.ReliableFragmentedSequenced);
            }
            return 0;
        }

        private void UpdateRelay()
        {
            RelayTransport.Enabled = UseMLAPIRelay;
            RelayTransport.RelayAddress = MLAPIRelayAddress;
            RelayTransport.RelayPort = (ushort)MLAPIRelayPort;
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member