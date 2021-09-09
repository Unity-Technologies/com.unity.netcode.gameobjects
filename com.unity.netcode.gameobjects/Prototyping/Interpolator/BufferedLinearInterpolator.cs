using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Solves for incoming values that are jittered
    /// Partially solves for message loss. Unclamped lerping helps hide this, but not completely
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BufferedLinearInterpolator<T> where T : struct
    {
        // interface for mock testing, abstracting away external systems
        public interface IInterpolatorTime
        {
            double BufferedServerTime { get; }
            double BufferedServerFixedTime { get; }
            int TickRate { get; }
        }

        private class InterpolatorTime : IInterpolatorTime
        {
            public double BufferedServerTime => NetworkManager.Singleton.ServerTime.Time;
            public double BufferedServerFixedTime => NetworkManager.Singleton.ServerTime.FixedTime;
            public int TickRate => NetworkManager.Singleton.ServerTime.TickRate;
        }

        internal IInterpolatorTime InterpolatorTimeProxy = new InterpolatorTime();

        private struct BufferedItem
        {
            public T Item;
            public NetworkTime TimeSent;
        }

        public bool UseFixedUpdate { get; set; }

        /// <summary>
        /// Override this if you want configurable buffering, right now using ServerTick's own global buffering
        /// </summary>
        private double ServerTimeBeingHandledForBuffering => UseFixedUpdate ? InterpolatorTimeProxy.BufferedServerFixedTime : InterpolatorTimeProxy.BufferedServerTime;

        private double RenderTime => InterpolatorTimeProxy.BufferedServerTime - 1f / InterpolatorTimeProxy.TickRate;

        private T m_InterpStartValue;
        private T m_CurrentInterpValue;
        private T m_InterpEndValue;

        private NetworkTime m_EndTimeConsumed;
        private NetworkTime m_StartTimeConsumed;

        private readonly List<BufferedItem> m_Buffer = new List<BufferedItem>();
        private const int k_BufferSizeLimit = 100;

        private int m_LifetimeConsumedCount;

        public void ResetTo(T targetValue)
        {
            m_LifetimeConsumedCount = 1;
            m_InterpStartValue = targetValue;
            m_InterpEndValue = targetValue;
            m_CurrentInterpValue = targetValue;
            m_Buffer.Clear();
            m_EndTimeConsumed = new NetworkTime(InterpolatorTimeProxy.TickRate, 0);
            m_StartTimeConsumed = new NetworkTime(InterpolatorTimeProxy.TickRate, 0);

            // SimpleInterpolator.ResetTo(targetValue); // for statically placed objects, so we don't interpolate from 0 to current position
            Update(0);
        }

        private double FixedOrTime(NetworkTime t)
        {
            return UseFixedUpdate ? t.FixedTime : t.Time;
        }

        // todo if I have value 1, 2, 3 and I'm treating 1 to 3, I shouldn't interpolate between 1 and 3, I should interpolate from 1 to 2, then from 2 to 3 to get the best path
        private void TryConsumeFromBuffer()
        {
            int consumedCount = 0;
            // only consume if we're ready
            if (RenderTime >= m_EndTimeConsumed.Time)
            {
                // buffer is sorted so older (smaller) time values are at the end.
                for (int i = m_Buffer.Count - 1; i >= 0; i--) // todo stretch: consume ahead if we see we're missing values
                {
                    var bufferedValue = m_Buffer[i];
                    // Consume when ready. This can consume multiple times
                    if (FixedOrTime(bufferedValue.TimeSent) <= ServerTimeBeingHandledForBuffering)
                    {
                        if (m_LifetimeConsumedCount == 0)
                        {
                            m_StartTimeConsumed = bufferedValue.TimeSent;
                            m_InterpStartValue = bufferedValue.Item;
                        }
                        else if (consumedCount == 0)
                        {
                            m_StartTimeConsumed = m_EndTimeConsumed;
                            m_InterpStartValue = m_InterpEndValue;
                        }

                        m_EndTimeConsumed = bufferedValue.TimeSent;
                        m_InterpEndValue = bufferedValue.Item;
                        m_Buffer.RemoveAt(i);
                        consumedCount++;
                        m_LifetimeConsumedCount++;
                    }
                }
            }
        }

        public T Update(float deltaTime)
        {
            TryConsumeFromBuffer();

            if (m_LifetimeConsumedCount == 0 && m_Buffer.Count == 0)
            {
                throw new InvalidOperationException("trying to update interpolator when no data has been added to it yet");
            }

            // Interpolation example to understand the math below
            // 4   4.5      6   6.5
            // |   |        |   |
            // A   render   B   Server

            if (m_LifetimeConsumedCount >= 1) // shouldn't interpolate between default values, let's wait to receive data first, should only interpolate between real measurements
            {
                double range = FixedOrTime(m_EndTimeConsumed) - FixedOrTime(m_StartTimeConsumed);
                float t;
                if (range == 0)
                {
                    t = 1;
                }
                else
                {
                    t = (float) ((RenderTime - FixedOrTime(m_StartTimeConsumed)) / range);
                }

                if (t > 3) // max extrapolation
                {
                    t = 1;
                }

                if (Debug.isDebugBuild)
                {
                    Debug.Assert(t >= 0, $"t must be bigger than or equal to 0. range {range}, RenderTime {RenderTime}, Start time {FixedOrTime(m_StartTimeConsumed)}, end time {FixedOrTime(m_EndTimeConsumed)}");
                }

                var target = InterpolateUnclamped(m_InterpStartValue, m_InterpEndValue, t);
                float maxInterpTime = 0.1f;
                m_CurrentInterpValue = Interpolate(m_CurrentInterpValue, target, deltaTime / maxInterpTime); // second interpolate to smooth out extrapolation jumps
            }

            return m_CurrentInterpValue;
        }

        public void AddMeasurement(T newMeasurement, NetworkTime sentTime)
        {
            if (m_Buffer.Count >= k_BufferSizeLimit)
            {
                Debug.LogWarning("Going over buffer size limit while adding new interpolation values, interpolation buffering isn't consuming fast enough, removing oldest value now.");
                m_Buffer.RemoveAt(m_Buffer.Count - 1);
            }

            if (sentTime.Time > m_EndTimeConsumed.Time || m_LifetimeConsumedCount == 0) // treat only if value is newer than the one being interpolated to right now
            {
                m_Buffer.Add(new BufferedItem {Item = newMeasurement, TimeSent = sentTime});
                m_Buffer.Sort((item1, item2) => item2.TimeSent.Time.CompareTo(item1.TimeSent.Time));
            }
        }

        public T GetInterpolatedValue()
        {
            return m_CurrentInterpValue;
        }

        protected abstract T Interpolate(T start, T end, float time);
        protected abstract T InterpolateUnclamped(T start, T end, float time);

        // protected abstract SimpleInterpolator<T> SimpleInterpolator { get; }
    }

    public class BufferedLinearInterpolatorFloat : BufferedLinearInterpolator<float>
    {
        protected override float InterpolateUnclamped(float start, float end, float time)
        {
            return Mathf.LerpUnclamped(start, end, time);
        }

        protected override float Interpolate(float start, float end, float time)
        {
            return Mathf.Lerp(start, end, time);
        }

        // protected override SimpleInterpolator<float> SimpleInterpolator { get; } = new SimpleInterpolatorFloat();
    }

    public class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        protected override Quaternion InterpolateUnclamped(Quaternion start, Quaternion end, float time)
        {
            return Quaternion.SlerpUnclamped(start, end, time);
        }

        protected override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            return Quaternion.SlerpUnclamped(start, end, time);
        }

        // protected override SimpleInterpolator<Quaternion> SimpleInterpolator { get; } = new SimpleInterpolatorQuaternion();
    }
}