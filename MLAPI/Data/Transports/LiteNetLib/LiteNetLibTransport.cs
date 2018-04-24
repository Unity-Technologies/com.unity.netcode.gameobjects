using LiteNetLib;
using LiteNetLib.Utils;
using MLAPI.MonoBehaviours.Core;
using System.Collections.Generic;

namespace MLAPI.Data.Transports.LiteNetLib
{
    public class LiteNetLibTransport : IUDPTransport, INetEventListener
    {
        public ChannelType InternalChannel => ChannelType.ReliableFragmentedSequenced;

        public uint ServerNetId => (uint)serverPeer.ConnectId;

        public uint HostDummyId => uint.MaxValue - 1;

        public uint InvalidDummyId => uint.MaxValue - 2;

        private NetManager man = null;
        private class LiteNetLibEvent
        {
            public NetEventType eventType;
            public NetPeer peer;
            public byte[] data;
        }
        private Queue<LiteNetLibEvent> pendingEvents = new Queue<LiteNetLibEvent>();
        private Dictionary<uint, NetPeer> connectedPeers = new Dictionary<uint, NetPeer>();
        private Dictionary<int, SendOptions> channels = new Dictionary<int, SendOptions>();
        private int channelCounter = 0;
        private NetPeer serverPeer;

        public int AddChannel(ChannelType type, object settings)
        {
            SendOptions options = SendOptions.ReliableUnordered;
            switch (type)
            {
                case ChannelType.Unreliable:
                    options = SendOptions.Unreliable;
                    break;
                case ChannelType.UnreliableFragmented:
                    options = SendOptions.Unreliable;
                    break;
                case ChannelType.UnreliableSequenced:
                    options = SendOptions.Sequenced;
                    break;
                case ChannelType.Reliable:
                    options = SendOptions.ReliableUnordered;
                    break;
                case ChannelType.ReliableFragmented:
                    options = SendOptions.ReliableUnordered;
                    break;
                case ChannelType.ReliableSequenced:
                    options = SendOptions.ReliableOrdered;
                    break;
                case ChannelType.StateUpdate:
                    options = SendOptions.Unreliable;
                    break;
                case ChannelType.ReliableStateUpdate:
                    options = SendOptions.ReliableUnordered;
                    break;
                case ChannelType.AllCostDelivery:
                    options = SendOptions.ReliableOrdered;
                    break;
                case ChannelType.UnreliableFragmentedSequenced:
                    options = SendOptions.Unreliable;
                    break;
                case ChannelType.ReliableFragmentedSequenced:
                    options = SendOptions.ReliableOrdered;
                    break;
                default:
                    options = SendOptions.ReliableUnordered;
                    break;
            }
            channels.Add(channelCounter, options);
            channelCounter++;
            return channelCounter - 1;
        }

        public void Connect(string address, int port, object settings, out byte error)
        {
            man = new NetManager(this, string.Empty);
            man.Start();
            serverPeer = man.Connect(NetworkingManager.singleton.NetworkConfig.ConnectAddress, NetworkingManager.singleton.NetworkConfig.ConnectPort);
            error = 0;
        }

        public void DisconnectClient(uint clientId)
        {
            man.DisconnectPeer(connectedPeers[clientId]);
        }

        public void DisconnectFromServer()
        {
            man.DisconnectPeer(serverPeer);
        }

        public int GetCurrentRTT(uint clientId, out byte error)
        {
            error = 0;
            return connectedPeers[clientId].Ping;
        }

        public int GetNetworkTimestamp()
        {
            return 0;
        }

        public int GetRemoteDelayTimeMS(uint clientId, int remoteTimestamp, out byte error)
        {
            error = 0;
            return connectedPeers[clientId].Ping;
        }

        public object GetSettings()
        {
            return null;
        }

        public NetEventType PollReceive(out uint clientId, out int channelId, ref byte[] data, int bufferSize, out int receivedSize, out byte error)
        {
            error = 0;
            man.PollEvents();
            if (pendingEvents.Count == 0)
            {
                receivedSize = 0;
                channelId = 0;
                clientId = 0;
                return NetEventType.Nothing;
            }
            LiteNetLibEvent evnt = pendingEvents.Dequeue();
            switch (evnt.eventType)
            {
                case NetEventType.Data:
                    clientId = (uint)evnt.peer.ConnectId;
                    receivedSize = evnt.data.Length;
                    data = evnt.data;
                    channelId = 0;
                    return NetEventType.Data;
                case NetEventType.Connect:
                    clientId = (uint)evnt.peer.ConnectId;
                    connectedPeers.Add(clientId, evnt.peer);
                    channelId = 0;
                    receivedSize = 0;
                    return NetEventType.Connect;
                case NetEventType.Disconnect:
                    clientId = (uint)evnt.peer.ConnectId;
                    connectedPeers.Remove(clientId);
                    channelId = 0;
                    receivedSize = 0;
                    return NetEventType.Disconnect;
                case NetEventType.Nothing:
                    receivedSize = 0;
                    channelId = 0;
                    clientId = 0;
                    return NetEventType.Nothing;
                default:
                    receivedSize = 0;
                    channelId = 0;
                    clientId = 0;
                    return NetEventType.Nothing;
            }
        }

        public void QueueMessageForSending(uint clientId, ref byte[] dataBuffer, int dataSize, int channelId, bool skipQueue, out byte error)
        {
            error = 0;
            connectedPeers[clientId].Send(dataBuffer, 0, dataSize, channels[channelId]);
        }

        public void RegisterServerListenSocket(object settings)
        {
            man = new NetManager(this, NetworkingManager.singleton.NetworkConfig.MaxConnections, string.Empty);
            man.Start(NetworkingManager.singleton.NetworkConfig.ConnectPort);
        }

        public void SendQueue(uint clientId, out byte error)
        {
            error = 0;
            return;
        }

        public void Shutdown()
        {
            man.Stop();
            man = null;
        }

        //*LITE NET LIB EVENTS BELOW*//

        public void OnPeerConnected(NetPeer peer)
        {
            pendingEvents.Enqueue(new LiteNetLibEvent()
            {
                eventType = NetEventType.Connect,
                peer = peer
            });
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            pendingEvents.Enqueue(new LiteNetLibEvent()
            {
                eventType = NetEventType.Disconnect,
                peer = peer
            });
        }

        public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode)
        {
            return;
        }

        public void OnNetworkReceive(NetPeer peer, NetDataReader reader)
        {
            pendingEvents.Enqueue(new LiteNetLibEvent()
            {
                eventType = NetEventType.Data,
                peer = peer,
                data = reader.Data
            });
        }

        public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
        {
            return;
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            return;
        }
    }
}
