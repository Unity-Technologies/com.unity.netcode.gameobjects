using System.Collections.Generic;

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
        private readonly List<TickEvent> Events = new List<TickEvent>();

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
