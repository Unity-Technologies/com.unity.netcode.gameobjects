namespace MLAPI.Data
{
    public interface IUDPTransport
    {
        void QueueMessageForSending(uint clientId, ref byte[] dataBuffer, int dataSize, int channelId, bool skipqueue, out byte error);
        void SendQueue(uint clientId, out byte error);
        NetEventType PollReceive(out uint clientId, out int channelId, ref byte[] data, int bufferSize, out int receivedSize, out byte error);
        int Connect(string address, int port, object settings, bool websocket, out byte error);
        int RegisterServerListenSocket(object settings, bool websocket);
        void Disconnect(uint clientId);
        int GetCurrentRTT(uint clientId, out byte error);
        int GetRemoteDelayTimeMS(uint clientId, int remoteTimestamp, out byte error);
        int GetNetworkTimestamp();
        void Shutdown();
    }
}
