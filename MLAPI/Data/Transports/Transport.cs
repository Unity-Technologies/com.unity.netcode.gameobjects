using System;
using UnityEngine;

namespace MLAPI.Transports
{
    /// <summary>
    /// A network transport
    /// </summary>
    public abstract class Transport : MonoBehaviour
    {
        /// <summary>
        /// A constant clientId that represents the server.
        /// When this value is found in methods such as Send, it should be treated as a placeholder that means "the server"
        /// </summary>
        public abstract ulong ServerClientId { get; }

        /// <summary>
        /// The channels the MLAPI will use when sending internal messages.
        /// </summary>
        public static TransportChannel[] MLAPI_CHANNELS = new TransportChannel[]
        {
            new TransportChannel()
            {
                Name = "MLAPI_INTERNAL",
                Type = ChannelType.ReliableFragmentedSequenced
            },
            new TransportChannel()
            {
                Name = "MLAPI_DEFAULT_MESSAGE",
                Type = ChannelType.Reliable
            },
            new TransportChannel()
            {
                Name = "MLAPI_POSITION_UPDATE",
                Type = ChannelType.StateUpdate
            },
            new TransportChannel()
            {
                Name = "MLAPI_ANIMATION_UPDATE",
                Type = ChannelType.ReliableSequenced
            },
            new TransportChannel()
            {
                Name = "MLAPI_NAV_AGENT_STATE",
                Type = ChannelType.ReliableSequenced
            },
            new TransportChannel()
            {
                Name = "MLAPI_NAV_AGENT_CORRECTION",
                Type = ChannelType.StateUpdate
            },
            new TransportChannel()
            {
                Name = "MLAPI_TIME_SYNC",
                Type = ChannelType.Unreliable
            }
        };
        
        /// <summary>
        /// Queues a message for sending if the transports supports manual queueing and you want to use the MLAPIs tick system.
        /// If the transport does not support queueing, you can ignore the FlushSendQueue method and do all sending here.
        /// </summary>
        /// <param name="clientId">The clientId to send to</param>
        /// <param name="data">The data to send</param>
        /// <param name="channelName">The channel to send data to</param>
        /// <param name="skipQueue">Whether or not Send will have to be called for this message to be sent</param>
        public abstract void Send(ulong clientId, ArraySegment<byte> data, string channelName, bool skipQueue);
        
        /// <summary>
        /// Sends queued messages for a specific clientId if queueing is supported.
        /// THIS METHOD IS OPTIONAL. IF THE TRANSPORT DOESNT SUPPORT QUEUEING, YOU CAN DO ALL SENDING IN THE QUEUE METHOD.
        /// </summary>
        /// <param name="clientId">The clientId to send</param>
        public abstract void FlushSendQueue(ulong clientId);
        
        /// <summary>
        /// Polls for incoming events
        /// </summary>
        /// <param name="clientId">The clientId this event is for</param>
        /// <param name="channelName">The channel the data arrived at. This is usually used when responding to things like RPCs</param>
        /// <param name="payload">The incoming data payload</param>
        /// <returns>Returns the event type</returns>
        public abstract NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload);
        
        /// <summary>
        /// Connects client to server
        /// </summary>
        public abstract void StartClient();
        
        /// <summary>
        /// Starts to listen for incoming clients.
        /// </summary>
        public abstract void StartServer();
        
        /// <summary>
        /// Disconnects a client from the server
        /// </summary>
        /// <param name="clientId">The clientId to disconnect</param>
        public abstract void DisconnectRemoteClient(ulong clientId);
        
        /// <summary>
        /// Disconnects the local client from the server
        /// </summary>
        public abstract void DisconnectLocalClient();

        /// <summary>
        /// Gets the round trip time for a specific client. This method is optional
        /// </summary>
        /// <param name="clientId">The clientId to get the rtt from</param>
        /// <returns>Returns the round trip time in milliseconds </returns>
        public abstract ulong GetCurrentRtt(ulong clientId);
        
        /// <summary>
        /// Shuts down the transport
        /// </summary>
        public abstract void Shutdown();

        /// <summary>
        /// Initializes the transport
        /// </summary>
        public abstract void Init();
    }
}
