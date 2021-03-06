using System.Collections.Generic;
using System.IO;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Profiling
{
    /// <summary>
    /// The type of Tick
    /// </summary>
    public enum TickType
    {
        /// <summary>
        /// Event tick. During EventTick NetworkVars are flushed etc
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
        public readonly List<TickEvent> Events = new List<TickEvent>();

        /// <summary>
        /// Writes the current ProfilerTick to the stream
        /// </summary>
        /// <param name="stream">The stream containing</param>
        public void SerializeToStream(Stream stream)
        {
            using (var writer = PooledNetworkWriter.Get(stream))
            {
                writer.WriteUInt16Packed((ushort)Events.Count);

                for (int i = 0; i < Events.Count; i++)
                {
                    Events[i].SerializeToStream(stream);
                }
            }
        }

        /// <summary>
        /// Creates a ProfilerTick from data in the provided stream
        /// </summary>
        /// <param name="stream">The stream containing the ProfilerTick data</param>
        /// <returns>The ProfilerTick with data read from the stream</returns>
        public static ProfilerTick FromStream(Stream stream)
        {
            var tick = new ProfilerTick();

            using (var reader = PooledNetworkReader.Get(stream))
            {
                ushort count = reader.ReadUInt16Packed();
                for (int i = 0; i < count; i++)
                {
                    tick.Events.Add(TickEvent.FromStream(stream));
                }

                return tick;
            }
        }

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

        internal void StartEvent(TickType type, uint bytes, string channelName, string messageType)
        {
            var tickEvent = new TickEvent()
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
        /// The amount of bytes that were sent and / or received during this tick
        /// </summary>
        public uint Bytes
        {
            get
            {
                uint bytes = 0;
                for (int i = 0; i < Events.Count; i++)
                {
                    bytes += Events[i].Bytes;
                }

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
        /// Whether or not the event is closed
        /// </summary>
        public bool Closed;

        /// <summary>
        /// Writes the TickEvent data to the stream
        /// </summary>
        /// <param name="stream">The stream to write the TickEvent data to</param>
        public void SerializeToStream(Stream stream)
        {
            using (var writer = PooledNetworkWriter.Get(stream))
            {
                writer.WriteByte((byte)EventType);
                writer.WriteUInt32Packed(Bytes);
                writer.WriteStringPacked(ChannelName);
                writer.WriteStringPacked(MessageType);
                writer.WriteBool(Closed);
            }
        }

        /// <summary>
        /// Creates a TickEvent from data in the provided stream
        /// </summary>
        /// <param name="stream">The stream containing the TickEvent data</param>
        /// <returns>The TickEvent with data read from the stream</returns>
        public static TickEvent FromStream(Stream stream)
        {
            using (var reader = PooledNetworkReader.Get(stream))
            {
                var tickEvent = new TickEvent
                {
                    EventType = (TickType)reader.ReadByte(),
                    Bytes = reader.ReadUInt32Packed(),
                    ChannelName = reader.ReadStringPacked(),
                    MessageType = reader.ReadStringPacked(),
                    Closed = reader.ReadBool()
                };

                return tickEvent;
            }
        }
    }
}