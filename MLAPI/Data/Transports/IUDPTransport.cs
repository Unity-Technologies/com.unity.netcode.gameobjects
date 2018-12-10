namespace MLAPI.Transports
{
    /// <summary>
    /// A UDP transport
    /// </summary>
    public interface IUDPTransport
    {
        /// <summary>
        /// The channelType the Internal library will use
        /// </summary>
        ChannelType InternalChannel { get; }
        /// <summary>
        /// The clientId the transport identifies as the server, should be constant
        /// </summary>
        uint ServerClientId { get; }
        /// <summary>
        /// Queues a message for sending.
        /// </summary>
        /// <param name="clientId">The clientId to send to</param>
        /// <param name="dataBuffer">The buffer to read from</param>
        /// <param name="dataSize">The amount of data to send from the buffer</param>
        /// <param name="channelId">The channelId to send on</param>
        /// <param name="skipQueue">Wheter or not Send will have to be called for this message to be sent</param>
        /// <param name="error">Error byte. Does nothhing</param>
        void QueueMessageForSending(uint clientId, byte[] dataBuffer, int dataSize, int channelId, bool skipQueue, out byte error);
        /// <summary>
        /// Sends queued messages for a specific clientId
        /// </summary>
        /// <param name="clientId">The clientId to send</param>
        /// <param name="error">Error byte. Does nothhing</param>
        void SendQueue(uint clientId, out byte error);
        /// <summary>
        /// Polls for incoming events
        /// </summary>
        /// <param name="clientId">The clientId this event is for</param>
        /// <param name="channelId">The channelId this message comes from</param>
        /// <param name="data">The data buffer, if any</param>
        /// <param name="bufferSize">The size of the buffer</param>
        /// <param name="receivedSize">The amount of bytes we received</param>
        /// <param name="error">Error byte. Does nothhing</param>
        /// <returns>Returns the event type</returns>
        NetEventType PollReceive(out uint clientId, out int channelId, ref byte[] data, int bufferSize, out int receivedSize, out byte error);
        /// <summary>
        /// Adds a channel with a specific type
        /// </summary>
        /// <param name="type">The channelType</param>
        /// <param name="settings">The settings object used by the transport</param>
        /// <returns>Returns a unique id for the channel</returns>
        int AddChannel(ChannelType type, object settings);
        /// <summary>
        /// Connects client to server
        /// </summary>
        /// <param name="address">The address to connect to</param>
        /// <param name="port">The port to connect to</param>
        /// <param name="settings">The settings object to use for the transport</param>
        /// <param name="error">Error byte. Does nothhing</param>
        void Connect(string address, int port, object settings, out byte error);
        /// <summary>
        /// Starts to listen for incoming clients.
        /// </summary>
        /// <param name="settings">The settings object for the transport</param>
        void RegisterServerListenSocket(object settings);
        /// <summary>
        /// Disconnects a client from the server
        /// </summary>
        /// <param name="clientId">The clientId to disconnect</param>
        void DisconnectClient(uint clientId);
        /// <summary>
        /// Disconnects client from the server
        /// </summary>
        void DisconnectFromServer();
        /// <summary>
        /// Gets the round trip time for a specific client
        /// </summary>
        /// <param name="clientId">The clientId to get the rtt from</param>
        /// <param name="error">Error byte. Does nothhing</param>
        /// <returns>Returns the event type</returns>
        int GetCurrentRTT(uint clientId, out byte error);
        /// <summary>
        /// Gets a delay in miliseconds based on a timestamp
        /// </summary>
        /// <param name="clientId">The clientId to get the delay for</param>
        /// <param name="remoteTimestamp">The timestamp</param>
        /// <param name="error">Error byte. Does nothhing</param>
        /// <returns>Returns the delay in miliseconds</returns>
        int GetRemoteDelayTimeMS(uint clientId, int remoteTimestamp, out byte error);
        /// <summary>
        /// Gets a timestamp to be used for calculating latency
        /// </summary>
        /// <returns>The timestamp</returns>
        int GetNetworkTimestamp();
        /// <summary>
        /// Inits the transport and returns a settings object to be used for listening, connecting and registering channels
        /// </summary>
        /// <returns>The settings object</returns>
        object GetSettings();
        /// <summary>
        /// Shuts down the transport
        /// </summary>
        void Shutdown();
    }
}
