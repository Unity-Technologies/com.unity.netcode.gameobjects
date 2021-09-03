using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Solves for jittered incoming values
    /// Doesn't solve for message loss
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BufferedLinearInterpolator<T> : IInterpolator<T> where T : struct
    {
        // public const float InterpolationConfigTimeSec = 0.100f; // todo expose global config, todo use in actual code

        // interface for mock testing, abstracting away external systems
        public interface IInterpolatorTime
        {
            public double BufferedServerTime { get; }
            public double BufferedServerFixedTime { get; }
            public double LocalTime { get; }
            public int TickRate { get; }
        }

        private class InterpolatorTime : IInterpolatorTime
        {
            public double BufferedServerTime => NetworkManager.Singleton.ServerTime.Time;
            public double BufferedServerFixedTime => NetworkManager.Singleton.ServerTime.FixedTime;
            public double LocalTime => NetworkManager.Singleton.LocalTime.Time;
            public int TickRate => NetworkManager.Singleton.ServerTime.TickRate;
        }

        private struct BufferedItem
        {
            public T item;
            public NetworkTime timeSent;
        }

        internal IInterpolatorTime interpolatorTime = new InterpolatorTime();

        public bool UseFixedUpdate { get; set; }

        /// <summary>
        /// Override this if you want configurable buffering, right now using ServerTick's own global buffering
        /// </summary>
        protected virtual double ServerTimeBeingHandledForBuffering => UseFixedUpdate ? interpolatorTime.BufferedServerFixedTime : interpolatorTime.BufferedServerTime;
        protected virtual double RenderTime => interpolatorTime.BufferedServerTime - 1f / interpolatorTime.TickRate;

        private T m_InterpStartValue;
        private T m_CurrentInterpValue;
        private T m_InterpEndValue;

        private NetworkTime m_EndTimeConsumed;
        private NetworkTime m_StartTimeConsumed;

        private readonly List<BufferedItem> m_Buffer = new List<BufferedItem>();
        private const int k_BufferSizeLimit = 100;

        private int m_LifetimeConsumedCount;

        public BufferedLinearInterpolator()
        {
        }

        public void Start()
        {
        }

        public void OnEnable()
        {
        }

        public void Awake()
        {
        }

        public void OnNetworkSpawn()
        {
        }

        public void ResetTo(T targetValue)
        {
            m_LifetimeConsumedCount = 1;
            m_InterpStartValue = targetValue;
            m_InterpEndValue = targetValue;
            m_CurrentInterpValue = targetValue;
            m_Buffer.Clear();
            m_EndTimeConsumed = new NetworkTime(interpolatorTime.TickRate, 0);
            m_StartTimeConsumed = new NetworkTime(interpolatorTime.TickRate, 0);

            simpleInterpolator.ResetTo(targetValue); // for statically placed objects, so we don't interpolate from 0 to current position
            Update(0);
        }

        double FixedOrTime(NetworkTime t)
        {
            if (UseFixedUpdate) return t.FixedTime;

            return t.Time;
        }
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
                    if (FixedOrTime(bufferedValue.timeSent) <= ServerTimeBeingHandledForBuffering) // todo do tick + 1 instead of changing the way tick is calculated? discuss with Luke
                    {
                        if (m_LifetimeConsumedCount == 0)
                        {
                            m_StartTimeConsumed = bufferedValue.timeSent;
                            m_InterpStartValue = bufferedValue.item;
                        }
                        else if (consumedCount == 0)
                        {
                            m_StartTimeConsumed = m_EndTimeConsumed;
                            m_InterpStartValue = m_InterpEndValue;
                        }

                        m_EndTimeConsumed = bufferedValue.timeSent;
                        m_InterpEndValue = bufferedValue.item;
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

                // if (t > 5) // max extrapolation
                {
                    t = 1;
                }

                Debug.Assert(t >= 0, $"t must be bigger or equal than 0. range {range}, RenderTime {RenderTime}, Start time {FixedOrTime(m_StartTimeConsumed)}, end time {FixedOrTime(m_EndTimeConsumed)}"); // todo remove GC alloc this creates

                simpleInterpolator.AddMeasurement(Interpolate(m_InterpStartValue, m_InterpEndValue, t), default); // using simple interpolation so there's no jump
                m_CurrentInterpValue = simpleInterpolator.Update(deltaTime);
            }

            return m_CurrentInterpValue;
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
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
                m_Buffer.Add(new BufferedItem {item = newMeasurement, timeSent = sentTime});
                m_Buffer.Sort((item1, item2) => item2.timeSent.Time.CompareTo(item1.timeSent.Time));
            }
        }

        public T GetInterpolatedValue()
        {
            return m_CurrentInterpValue;
        }

        public void OnDestroy()
        {
        }

        protected abstract T Interpolate(T start, T end, float time);

        protected abstract SimpleInterpolator<T> simpleInterpolator { get; }
    }

    public class BufferedLinearInterpolatorFloat : BufferedLinearInterpolator<float>
    {
        protected override float Interpolate(float start, float end, float time)
        {
            return Mathf.LerpUnclamped(start, end, time);
        }

        protected override SimpleInterpolator<float> simpleInterpolator { get; } = new SimpleInterpolatorFloat();
    }

    public class BufferedLinearInterpolatorFloatForScale : BufferedLinearInterpolatorFloat
    {
        protected override float Interpolate(float start, float end, float time)
        {
            // scale can't be negative, stopping negative extrapolation
            return Mathf.Max(Mathf.LerpUnclamped(start, end, time), 0);
        }
    }

    public class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        protected override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            return Quaternion.SlerpUnclamped(start, end, time);
        }

        public BufferedLinearInterpolatorQuaternion()
        {
        }

        protected override SimpleInterpolator<Quaternion> simpleInterpolator { get; } = new SimpleInterpolatorQuaternion();
    }
}