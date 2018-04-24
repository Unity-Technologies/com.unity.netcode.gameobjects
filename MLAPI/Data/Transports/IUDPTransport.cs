namespace MLAPI.Data
{
    public interface IUDPTransport
    {
        ChannelType InternalChannel { get; }
        uint ServerNetId { get; }
        uint HostDummyId { get; }
        uint InvalidDummyId { get; }
        void QueueMessageForSending(uint clientId, ref byte[] dataBuffer, int dataSize, int channelId, bool skipQueue, out byte error);
        void SendQueue(uint clientId, out byte error);
        NetEventType PollReceive(out uint clientId, out int channelId, ref byte[] data, int bufferSize, out int receivedSize, out byte error);
        int AddChannel(ChannelType type, object settings);
        void Connect(string address, int port, object settings, out byte error);
        void RegisterServerListenSocket(object settings);
        void DisconnectClient(uint clientId);
        void DisconnectFromServer();
        int GetCurrentRTT(uint clientId, out byte error);
        int GetRemoteDelayTimeMS(uint clientId, int remoteTimestamp, out byte error);
        int GetNetworkTimestamp();
        object GetSettings();
        void Shutdown();
    }
}
