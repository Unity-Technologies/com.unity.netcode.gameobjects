using System.Collections.Generic;
using MLAPI.Attributes;

namespace MLAPI.Data.NetworkProfiler
{
    /// <summary>
    /// The type of Tick
    /// </summary>
    public enum TickType
    {
        /// <summary>
        /// Event tick. During EventTick SyncedVars are flushed etc
        /// </summary>
        Event,
        /// <summary>
        /// Receive tick. During ReceiveTick data is received from the transport
        /// </summary>
        Receive,
        /// <summary>
        /// Send tick. During Send data is sent from Transport queue
        /// </summary>
        Send
    }
    /// <summary>
    /// A tick in used for the Profiler
    /// </summary>
    public class ProfilerTick
    {
        /// <summary>
        /// The events that occured during this tick
        /// </summary>
        [BinaryIgnore]
        public readonly List<TickEvent> Events = new List<TickEvent>();
        private TickEvent[] events;

        internal void EndEvent()
        {
            for (int i = Events.Count - 1; i >= 0; i--)
            {
                if (!Events[i].Closed)
                {
                    Events[i].Closed = true;
                    return;
                }
            }
        }

        internal void PreSerialize()
        {
            events = new TickEvent[Events.Count];
            for (int i = 0; i < events.Length; i++)
                events[i] = Events[i];
        }

        internal void PostDeserialize()
        {
            Events.Clear();
            for (int i = 0; i < events.Length; i++)
                Events.Add(events[i]);
        }

        internal void StartEvent(TickType type, uint bytes, string channelName, string messageType)
        {
            TickEvent tickEvent = new TickEvent()
            {
                Bytes = bytes,
                ChannelName = string.IsNullOrEmpty(channelName) ? "NONE" : channelName,
                MessageType = string.IsNullOrEmpty(messageType) ? "NONE" : messageType,
                EventType = type,
                Closed = false
            };
            Events.Add(tickEvent);
        }

        /// <summary>
        /// The type of tick
        /// </summary>
        public TickType Type;
        /// <summary>
        /// The frame the tick executed on
        /// </summary>
        public int Frame;
        /// <summary>
        /// The id of the tick
        /// </summary>
        public int EventId;
        /// <summary>
        /// The amount of bytes that were sent and / or recieved during this tick
        /// </summary>
        public uint Bytes
        {
            get
            {
                uint bytes = 0;
                for (int i = 0; i < Events.Count; i++) bytes += Events[i].Bytes;
                return bytes;
            }
        }
    }

    /// <summary>
    /// A event that can occur during a Event
    /// </summary>
    public class TickEvent
    {
        /// <summary>
        /// The type of evenmt
        /// </summary>
        public TickType EventType;
        /// <summary>
        /// The amount of bytes sent or received
        /// </summary>
        public uint Bytes;
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string ChannelName;
        /// <summary>
        /// The message type
        /// </summary>
        public string MessageType;
        /// <summary>
        /// Wheter or not the event is closed
        /// </summary>
        public bool Closed;
    }
}
