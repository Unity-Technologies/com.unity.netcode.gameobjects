using System;
using UnityEngine;

namespace MLAPI.Data.NetworkProfiler
{
    public static class NetworkProfiler
    {
        const int tickCount = 1024;
        public static readonly FixedQueue<ProfilerTick> Ticks = new FixedQueue<ProfilerTick>(tickCount);
        private static ProfilerTick CurrentTick;

        internal static void StartTick(TickType type)
        {
            if (Ticks.Count == tickCount)
                Ticks.Dequeue();
            
            ProfilerTick tick = new ProfilerTick()
            {
                StartTime = Time.unscaledTime,
                EndTime = -1,
                Type = type
            };
            Ticks.Enqueue(tick);
            CurrentTick = tick;
        }

        internal static void EndTick()
        {
            if (CurrentTick == null)
                return;
            CurrentTick.EndTime = Time.unscaledTime;
            CurrentTick = null;
        }

        internal static void StartEvent(TickType eventType, uint bytes, string channelName, string messageType)
        {
            if (CurrentTick == null)
                return;
            CurrentTick.StartEvent(eventType, bytes, channelName, messageType);
        }

        internal static void EndEvent()
        {
            if (CurrentTick == null)
                return;
            CurrentTick.EndEvent();
        }
    }
}
