using UnityEngine.Networking;

namespace MLAPI.Data
{
    public class UnetTransport : IUDPTransport
    {
        public int Connect(string address, int port, object settings, bool websocket, out byte error)
        {
            NetworkTransport.Init();
            int hostId = NetworkTransport.AddHost((HostTopology)settings);
            return NetworkTransport.Connect(hostId, address, port, 0, out error);
        }

        public void Disconnect(uint clientId)
        {
            NetId netId = new NetId(clientId);
            byte error;
            NetworkTransport.Disconnect(netId.HostId, netId.ConnectionId, out error);
        }

        public int GetCurrentRTT(uint clientId, out byte error)
        {
            NetId netId = new NetId(clientId);
            return NetworkTransport.GetCurrentRTT(netId.HostId, netId.ConnectionId, out error);
        }

        public int GetNetworkTimestamp()
        {
            return NetworkTransport.GetNetworkTimestamp();
        }

        public int GetRemoteDelayTimeMS(uint clientId, int remoteTimestamp, out byte error)
        {
            NetId netId = new NetId(clientId);
            return NetworkTransport.GetRemoteDelayTimeMS(netId.HostId, netId.ConnectionId, remoteTimestamp, out error);
        }

        public NetEventType PollReceive(out uint clientId, out int channelId, ref byte[] data, int bufferSize, out int receivedSize, out byte error)
        {
            int hostId;
            int connectionId;
            byte err;
            NetworkEventType eventType = NetworkTransport.Receive(out hostId, out connectionId, out channelId, data, bufferSize, out receivedSize, out err);
            clientId = new NetId((byte)hostId, (ushort)connectionId, false, false).GetClientId();
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

        public int RegisterServerListenSocket(object settings, bool websockets)
        {
            NetworkTransport.Init();
            return NetworkTransport.AddHost((HostTopology)settings);
        }

        public void QueueMessageForSending(uint clientId, ref byte[] dataBuffer, int dataSize, int channelId, bool skipqueue, out byte error)
        {
            NetId netId = new NetId(clientId);
            if (skipqueue)
                NetworkTransport.Send(netId.HostId, netId.ConnectionId, channelId, dataBuffer, dataSize, out error);
            else
                NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channelId, dataBuffer, dataSize, out error);
        }

        public void Shutdown()
        {
            NetworkTransport.Shutdown();
        }

        public void SendQueue(uint clientId, out byte error)
        {
            NetId netId = new NetId(clientId);
            NetworkTransport.SendQueuedMessages(netId.HostId, netId.ConnectionId, out error);
        }
    }
}
