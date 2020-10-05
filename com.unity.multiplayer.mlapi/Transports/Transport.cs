using System;
using System.Collections.Generic;
using MLAPI.Transports.Tasks;
using UnityEngine;

namespace MLAPI.Transports
{
    /// <summary>
    /// A network transport
    /// </summary>
    public abstract class Transport : MonoBehaviour
    {
        /// <summary>
        /// Delegate used to request channels on the underlying transport.
        /// </summary>
        public delegate void RequestChannelsDelegate(List<TransportChannel> channels);

        /// <summary>
        /// Delegate called when the transport wants to know what channels to register.
        /// </summary>
        public event RequestChannelsDelegate OnChannelRegistration;

        /// <summary>
        /// A constant clientId that represents the server.
        /// When this value is found in methods such as Send, it should be treated as a placeholder that means "the server"
        /// </summary>
        public abstract ulong ServerClientId { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:MLAPI.Transports.Transport"/> is supported in the current runtime context.
        /// This is used by multiplex adapters.
        /// </summary>
        /// <value><c>true</c> if is supported; otherwise, <c>false</c>.</value>
        public virtual bool IsSupported => true;

        private TransportChannel[] _channelsCache = null;

        internal void ResetChannelCache()
        {
            _channelsCache = null;
        }

        public TransportChannel[] MLAPI_CHANNELS
        {
            get
            {
                if (_channelsCache == null)
                {
                    List<TransportChannel> channels = new List<TransportChannel>();

                    if (OnChannelRegistration != null)
                    {
                        OnChannelRegistration(channels);
                    }

                    _channelsCache = new TransportChannel[MLAPI_INTERNAL_CHANNELS.Length + channels.Count];

                    for (int i = 0; i < MLAPI_INTERNAL_CHANNELS.Length; i++)
                    {
                        _channelsCache[i] = MLAPI_INTERNAL_CHANNELS[i];
                    }

                    for (int i = 0; i < channels.Count; i++)
                    {
                        _channelsCache[i + MLAPI_INTERNAL_CHANNELS.Length] = channels[i];
                    }
                }

                return _channelsCache;
            }
        }

        /// <summary>
        /// The channels the MLAPI will use when sending internal messages.
        /// </summary>
        private TransportChannel[] MLAPI_INTERNAL_CHANNELS = new TransportChannel[]
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
                Type = ChannelType.UnreliableSequenced
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
                Type = ChannelType.UnreliableSequenced
            },
            new TransportChannel()
            {
                Name = "MLAPI_TIME_SYNC",
                Type = ChannelType.Unreliable
            }
        };

        /// <summary>
        /// Delegate for transport events.
        /// </summary>
        public delegate void TransportEventDelegate(NetEventType type, ulong clientId, string channelName, ArraySegment<byte> payload, float receiveTime);

        /// <summary>
        /// Occurs when the transport has a new transport event. Can be used to make an event based transport instead of a poll based.
        /// Invokation has to occur on the Unity thread in the Update loop.
        /// </summary>
        public event TransportEventDelegate OnTransportEvent;

        /// <summary>
        /// Send a payload to the specified clientId, data and channelName.
        /// </summary>
        /// <param name="clientId">The clientId to send to</param>
        /// <param name="data">The data to send</param>
        /// <param name="channelName">The channel to send data to</param>
        public abstract void Send(ulong clientId, ArraySegment<byte> data, string channelName);

        /// <summary>
        /// Polls for incoming events, with an extra output parameter to report the precise time the event was received.
        /// </summary>
        /// <param name="clientId">The clientId this event is for</param>
        /// <param name="channelName">The channel the data arrived at. This is usually used when responding to things like RPCs</param>
        /// <param name="payload">The incoming data payload</param>
        /// <param name="receiveTime">The time the event was received, as reported by Time.realtimeSinceStartup.</param>
        /// <returns>Returns the event type</returns>
        public abstract NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload, out float receiveTime);

        /// <summary>
        /// Connects client to server
        /// </summary>
        public abstract SocketTasks StartClient();

        /// <summary>
        /// Starts to listen for incoming clients.
        /// </summary>
        public abstract SocketTasks StartServer();

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
