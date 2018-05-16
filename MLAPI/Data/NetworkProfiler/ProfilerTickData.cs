using System.Collections.Generic;
using MLAPI.Attributes;

namespace MLAPI.Data.NetworkProfiler
{
    public enum TickType
    {
        Event,
        Receive,
        Send
    }
    public class ProfilerTick
    {
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

        public TickType Type;
        public int Frame;
        public int EventId;
    }

    public class TickEvent
    {
        public TickType EventType;
        public uint Bytes;
        public string ChannelName;
        public string MessageType;
        public bool Closed;
    }
}
