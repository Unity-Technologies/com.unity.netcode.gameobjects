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
        // protected virtual double ServerTimeBeingHandledForBuffering => interpolatorTime.BufferedServerTime; // override this if you want configurable buffering, right now using ServerTick's own global buffering
        protected virtual double ServerTimeBeingHandledForBuffering => UseFixedUpdate ? interpolatorTime.BufferedServerFixedTime : interpolatorTime.BufferedServerTime; // override this if you want configurable buffering, right now using ServerTick's own global buffering
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
            Update(0);
        }

        double TickOrTime(NetworkTime t)
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
                for (int i = m_Buffer.Count - 1; i >= 0; i--)
                {
                    var bufferedValue = m_Buffer[i];
                    // Consume when ready. This can consume multiple times
                    if (TickOrTime(bufferedValue.timeSent) <= ServerTimeBeingHandledForBuffering) // todo do tick + 1 instead of changing the way tick is calculated? discuss with Luke
                    {
                        // if (RenderTime > TickOrTime(m_EndTimeConsumed))
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
            if (m_LifetimeConsumedCount > 2) // todo remove debug
            Debug.Log($"Sam got value m_Buffer.Count {m_Buffer.Count}");
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
                double range = TickOrTime(m_EndTimeConsumed) - TickOrTime(m_StartTimeConsumed);
                float t;
                if (range == 0)
                {
                    t = 1;
                }
                else
                {
                    t = (float) ((RenderTime - TickOrTime(m_StartTimeConsumed)) / range);
                }

                if (t > 2) // max extrapolation
                {
                    t = 1;
                }

                if (m_LifetimeConsumedCount > 2) // todo remove debug
                Debug.Log($"sam got value t {t}, TT(m_EndTimeConsumed) {TickOrTime(m_EndTimeConsumed)}, TT(m_StartTimeConsumed) {TickOrTime(m_StartTimeConsumed)}, range {range}, " +
                          $" RenderTime {RenderTime}, ServerTimeBeingHandledForBuffering {ServerTimeBeingHandledForBuffering}, interpolatorTime.TickRate {interpolatorTime.TickRate}, " +
                          $" interpolatorTime.BufferedServerFixedTime {interpolatorTime.BufferedServerFixedTime}, interpolatorTime.BufferedServerTime {interpolatorTime.BufferedServerTime}");
                Debug.Assert(t >= 0, $"t must be bigger or equal than 0. range {range}, RenderTime {RenderTime}, Start time {TickOrTime(m_StartTimeConsumed)}, end time {TickOrTime(m_EndTimeConsumed)}");
                m_CurrentInterpValue = Interpolate(m_InterpStartValue, m_InterpEndValue, t);
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

        public abstract T Interpolate(T start, T end, float time);
    }

    public class BufferedLinearInterpolatorFloat : BufferedLinearInterpolator<float>
    {
        public override float Interpolate(float start, float end, float time)
        {
            return Mathf.Lerp(start, end, time);
            // return Mathf.LerpUnclamped(start, end, time);
        }

        public BufferedLinearInterpolatorFloat()
        {
        }
    }

    public class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        public override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            // return Quaternion.SlerpUnclamped(start, end, time);
            return Quaternion.Slerp(start, end, time);
        }

        public BufferedLinearInterpolatorQuaternion()
        {
        }
    }
}