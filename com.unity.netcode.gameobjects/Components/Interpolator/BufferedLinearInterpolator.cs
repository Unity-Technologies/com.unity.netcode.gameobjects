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
    internal abstract class BufferedLinearInterpolator<T> where T : struct
    {
        // interface for mock testing, abstracting away external systems
        internal interface IInterpolatorTime
        {
            double BufferedServerTime { get; }
            double BufferedServerFixedTime { get; }
            uint TickRate { get; }
        }

        private class InterpolatorTime : IInterpolatorTime
        {
            private readonly NetworkManager m_Manager;
            public InterpolatorTime(NetworkManager manager)
            {
                m_Manager = manager;
            }
            public double BufferedServerTime => m_Manager.ServerTime.Time;
            public double BufferedServerFixedTime => m_Manager.ServerTime.FixedTime;
            public uint TickRate => m_Manager.ServerTime.TickRate;
        }

        internal IInterpolatorTime InterpolatorTimeProxy;

        private struct BufferedItem
        {
            public T Item;
            public NetworkTime TimeSent;

            public BufferedItem(T item, NetworkTime timeSent)
            {
                Item = item;
                TimeSent = timeSent;
            }
        }

        private const double k_SmallValue = 9.999999439624929E-11; // copied from Vector3's equal operator

        /// <summary>
        /// Override this if you want configurable buffering, right now using ServerTick's own global buffering
        /// </summary>
        private double ServerTimeBeingHandledForBuffering => InterpolatorTimeProxy.BufferedServerTime;

        private double RenderTime => InterpolatorTimeProxy.BufferedServerTime - 1f / InterpolatorTimeProxy.TickRate;

        private T m_InterpStartValue;
        private T m_CurrentInterpValue;
        private T m_InterpEndValue;

        private NetworkTime m_EndTimeConsumed;
        private NetworkTime m_StartTimeConsumed;

        private readonly List<BufferedItem> m_Buffer = new List<BufferedItem>();
        private const float k_SupportedBurstSizeSeconds = 1f; // will try to interpolate x seconds before teleporting
        private int BufferSizeLimit { get; }


        private int m_LifetimeConsumedCount;

        private bool InvalidState => m_Buffer.Count == 0 && m_LifetimeConsumedCount == 0;

        internal BufferedLinearInterpolator(NetworkManager manager)
        {
            InterpolatorTimeProxy = new InterpolatorTime(manager);
            BufferSizeLimit = Mathf.CeilToInt(k_SupportedBurstSizeSeconds * InterpolatorTimeProxy.TickRate);
        }

        public void ResetTo(T targetValue)
        {
            m_LifetimeConsumedCount = 1;
            m_InterpStartValue = targetValue;
            m_InterpEndValue = targetValue;
            m_CurrentInterpValue = targetValue;
            m_Buffer.Clear();
            m_EndTimeConsumed = new NetworkTime(InterpolatorTimeProxy.TickRate, 0);
            m_StartTimeConsumed = new NetworkTime(InterpolatorTimeProxy.TickRate, 0);

            Update(0);
        }


        // todo if I have value 1, 2, 3 and I'm treating 1 to 3, I shouldn't interpolate between 1 and 3, I should interpolate from 1 to 2, then from 2 to 3 to get the best path
        private void TryConsumeFromBuffer()
        {
            int consumedCount = 0;
            // only consume if we're ready
            if (RenderTime >= m_EndTimeConsumed.Time)
            {
                BufferedItem? itemToInterpolateTo = null;
                for (int i = m_Buffer.Count - 1; i >= 0; i--) // todo stretch: consume ahead if we see we're missing values due to packet loss
                {
                    var bufferedValue = m_Buffer[i];
                    // Consume when ready and interpolate to last value we can consume. This can consume multiple values from the buffer
                    if (bufferedValue.TimeSent.Time <= ServerTimeBeingHandledForBuffering)
                    {
                        if (!itemToInterpolateTo.HasValue || bufferedValue.TimeSent.Time > itemToInterpolateTo.Value.TimeSent.Time)
                        {
                            itemToInterpolateTo = bufferedValue;
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
                        }

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

            if (InvalidState)
            {
                throw new InvalidOperationException("trying to update interpolator when no data has been added to it yet");
            }

            // Interpolation example to understand the math below
            // 4   4.5      6   6.5
            // |   |        |   |
            // A   render   B   Server

            if (m_LifetimeConsumedCount >= 1) // shouldn't interpolate between default values, let's wait to receive data first, should only interpolate between real measurements
            {
                float t = 1.0f;
                double range = m_EndTimeConsumed.Time - m_StartTimeConsumed.Time;
                if (range > k_SmallValue)
                {
                    t = (float)((RenderTime - m_StartTimeConsumed.Time) / range);

                    if (t < 0.0f)
                    {
                        throw new OverflowException($"t = {t} but must be >= 0. range {range}, RenderTime {RenderTime}, Start time {m_StartTimeConsumed.Time}, end time {m_EndTimeConsumed.Time}");
                    }

                    if (t > 3.0f) // max extrapolation
                    {
                        // TODO this causes issues with teleport, investigate
                        // todo make this configurable
                        t = 1.0f;
                    }
                }

                var target = InterpolateUnclamped(m_InterpStartValue, m_InterpEndValue, t);
                float maxInterpTime = 0.1f;
                m_CurrentInterpValue = Interpolate(m_CurrentInterpValue, target, deltaTime / maxInterpTime); // second interpolate to smooth out extrapolation jumps
            }

            m_NbItemsReceivedThisFrame = 0;
            return m_CurrentInterpValue;
        }

        private BufferedItem m_LastBufferedItemReceived;
        private int m_NbItemsReceivedThisFrame;
        public void AddMeasurement(T newMeasurement, NetworkTime sentTime)
        {
            m_NbItemsReceivedThisFrame++;

            // This situation can happen after a game is paused. When starting to receive again, the server will have sent a bunch of messages in the meantime
            // instead of going through thousands of value updates just to get a big teleport, we're giving up on interpolation and teleporting to the correct value
            if (m_NbItemsReceivedThisFrame > BufferSizeLimit)
            {
                if (m_LastBufferedItemReceived.TimeSent.Time > sentTime.Time)
                {
                    m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime);
                    ResetTo(newMeasurement);
                }

                return;
            }

            if (sentTime.Time > m_EndTimeConsumed.Time || m_LifetimeConsumedCount == 0) // treat only if value is newer than the one being interpolated to right now
            {
                m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime);
                m_Buffer.Add(m_LastBufferedItemReceived);
            }
        }

        public T GetInterpolatedValue()
        {
            return m_CurrentInterpValue;
        }

        protected abstract T Interpolate(T start, T end, float time);
        protected abstract T InterpolateUnclamped(T start, T end, float time);
    }

    internal class BufferedLinearInterpolatorFloat : BufferedLinearInterpolator<float>
    {
        protected override float InterpolateUnclamped(float start, float end, float time)
        {
            return Mathf.LerpUnclamped(start, end, time);
        }

        protected override float Interpolate(float start, float end, float time)
        {
            return Mathf.Lerp(start, end, time);
        }

        public BufferedLinearInterpolatorFloat(NetworkManager manager) : base(manager)
        {
        }
    }

    internal class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        protected override Quaternion InterpolateUnclamped(Quaternion start, Quaternion end, float time)
        {
            return Quaternion.SlerpUnclamped(start, end, time);
        }

        protected override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            return Quaternion.SlerpUnclamped(start, end, time);
        }

        public BufferedLinearInterpolatorQuaternion(NetworkManager manager) : base(manager)
        {
        }
    }
}
