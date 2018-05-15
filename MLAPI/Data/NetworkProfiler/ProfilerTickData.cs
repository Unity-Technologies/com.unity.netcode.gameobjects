using System;
using System.Collections.Generic;
using UnityEngine;

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
                if (Events[i].EndTime == -1)
                {
                    Events[i].EndTime = Time.unscaledTime;
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
                EndTime = -1,
                StartTime = Time.unscaledTime,
                EventType = type
            };
            Events.Add(tickEvent);
        }

        public TickType Type;
        public float StartTime;
        public float EndTime;
    }

    public class TickEvent
    {
        public TickType EventType;
        public uint Bytes;
        public string ChannelName;
        public string MessageType;
        public float StartTime = -1;
        public float EndTime = -1;
    }
}
