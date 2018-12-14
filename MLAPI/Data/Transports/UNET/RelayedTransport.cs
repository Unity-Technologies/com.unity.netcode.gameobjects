#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.Collections.Generic;
using UnityEngine.Networking;

namespace MLAPI.Transports.UNET
{
    public class RelayedTransport : IUDPTransport
    {
        public ChannelType InternalChannel => ChannelType.ReliableFragmentedSequenced;
		public uint ServerClientId => new NetId(0, 0, true).GetClientId();
        public int serverConnectionId;
        public int serverHostId;

        public static readonly List<TransportHost> ServerTransports = new List<TransportHost>()
        {
            new TransportHost()
            {
                Name = "UDP Socket",
                Port = 7777,
                Websockets = false
            }
        };

        public int Connect(string address, int port, object settings, bool websocket, out byte error)
        {
            NetworkTransport.Init();
            int hostId = RelayTransport.AddHost((HostTopology)settings, false);
            return RelayTransport.Connect(hostId, address, port, 0, out error);
        }

        public void DisconnectClient(uint clientId)
        {
            NetId netId = new NetId(clientId);
			if (netId.IsServer())
                return;
            RelayTransport.Disconnect(netId.HostId, netId.ConnectionId, out byte error);
        }

        public void DisconnectFromServer() => RelayTransport.Disconnect(serverHostId, serverConnectionId, out byte error);

        public int GetCurrentRTT(uint clientId, out byte error)
        {
            NetId netId = new NetId(clientId);
			if (netId.IsServer())
            {
                netId.ConnectionId = (ushort)serverConnectionId;
                netId.HostId = (byte)serverHostId;
            }
            return NetworkTransport.GetCurrentRTT(netId.HostId, netId.ConnectionId, out error);
        }

        public int GetNetworkTimestamp() => NetworkTransport.GetNetworkTimestamp();

        public int GetRemoteDelayTimeMS(uint clientId, int remoteTimestamp, out byte error)
        {
            NetId netId = new NetId(clientId);
			if (netId.IsServer())
            {
                netId.ConnectionId = (ushort)serverConnectionId;
                netId.HostId = (byte)serverHostId;
            }
            return NetworkTransport.GetRemoteDelayTimeMS(netId.HostId, netId.ConnectionId, remoteTimestamp, out error);
        }

        public NetEventType PollReceive(out uint clientId, out int channelId, ref byte[] data, int bufferSize, out int receivedSize, out byte error)
        {
            NetworkEventType eventType = RelayTransport.Receive(out int hostId, out int connectionId, out channelId, data, bufferSize, out receivedSize, out byte err);
            clientId = new NetId((byte)hostId, (ushort)connectionId, false).GetClientId();
            NetworkError errorType = (NetworkError)err;
            if (errorType == NetworkError.Timeout)
                eventType = NetworkEventType.DisconnectEvent; //In UNET. Timeouts are not disconnects. We have to translate that here.
            error = 0;

            //Translate NetworkEventType to NetEventType
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

        public void QueueMessageForSending(uint clientId, byte[] dataBuffer, int dataSize, int channelId, bool skipqueue, out byte error)
        {
            NetId netId = new NetId(clientId);
			if (netId.IsServer())
            {
                netId.ConnectionId = (ushort)serverConnectionId;
                netId.HostId = (byte)serverHostId;
            }
            if (skipqueue)
                RelayTransport.Send(netId.HostId, netId.ConnectionId, channelId, dataBuffer, dataSize, out error);
            else
                RelayTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channelId, dataBuffer, dataSize, out error);
        }

        public void Shutdown() => NetworkTransport.Shutdown();

        public void SendQueue(uint clientId, out byte error)
        {
            NetId netId = new NetId(clientId);
			if (netId.IsServer())
            {
                netId.ConnectionId = (ushort)serverConnectionId;
                netId.HostId = (byte)serverHostId;
            }
            RelayTransport.SendQueuedMessages(netId.HostId, netId.ConnectionId, out error);
        }

        public void RegisterServerListenSocket(object settings)
        {
            HostTopology topology = new HostTopology((ConnectionConfig)settings, NetworkingManager.Singleton.NetworkConfig.MaxConnections);
            for (int i = 0; i < ServerTransports.Count; i++)
            {
                if (ServerTransports[i].Websockets)
                    RelayTransport.AddWebsocketHost(topology, ServerTransports[i].Port, true);
                else
                    RelayTransport.AddHost(topology, ServerTransports[i].Port, true);
            }
        }

        public int AddChannel(ChannelType type, object settings)
        {
            ConnectionConfig config = (ConnectionConfig)settings;
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

        public object GetSettings()
        {
            NetworkTransport.Init();
            return new ConnectionConfig()
            {
                SendDelay = 0
            };
        }

        public void Connect(string address, int port, object settings, out byte error)
        {
            serverHostId = RelayTransport.AddHost(new HostTopology((ConnectionConfig)settings, 1), false);
            serverConnectionId = RelayTransport.Connect(serverHostId, address, port, 0, out error);
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
