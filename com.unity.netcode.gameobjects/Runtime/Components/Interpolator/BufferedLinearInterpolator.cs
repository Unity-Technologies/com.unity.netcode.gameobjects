using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Solves for incoming values that are jittered.
    /// Partially solves for message loss. Unclamped lerping helps hide this, but not completely
    /// </summary>
    /// <typeparam name="T">The type of interpolated value</typeparam>
    public abstract class BufferedLinearInterpolator<T> where T : struct
    {
        internal float MaxInterpolationBound = 1.0f;
        protected internal struct BufferedItem
        {
            public T Item;
            public double TimeSent;

            public BufferedItem(T item, double timeSent)
            {
                Item = item;
                TimeSent = timeSent;
            }
        }

        /// <summary>
        /// There's two factors affecting interpolation: buffering (set in NetworkManager's NetworkTimeSystem) and interpolation time, which is the amount of time it'll take to reach the target. This is to affect the second one.
        /// </summary>
        public float MaximumInterpolationTime = 0.1f;

        private const double k_SmallValue = 9.999999439624929E-11; // copied from Vector3's equal operator

        protected internal readonly Queue<BufferedItem> m_Buffer = new Queue<BufferedItem>(k_BufferCountLimit);



        // Buffer consumption scenarios
        // Perfect case consumption
        // | 1 | 2 | 3 |
        // | 2 | 3 | 4 | consume 1
        // | 3 | 4 | 5 | consume 2
        // | 4 | 5 | 6 | consume 3
        // | 5 | 6 | 7 | consume 4
        // jittered case
        // | 1 | 2 | 3 |
        // | 2 | 3 |   | consume 1
        // | 3 |   |   | consume 2
        // | 4 | 5 | 6 | consume 3
        // | 5 | 6 | 7 | consume 4
        // bursted case (assuming max count is 5)
        // | 1 | 2 | 3 |
        // | 2 | 3 |   | consume 1
        // | 3 |   |   | consume 2
        // |   |   |   | consume 3
        // |   |   |   |
        // | 4 | 5 | 6 | 7 | 8 | --> consume all and teleport to last value <8> --> this is the nuclear option, ideally this example would consume 4 and 5
        // instead of jumping to 8, but since in OnValueChange we don't yet have an updated server time (updated in pre-update) to know which value
        // we should keep and which we should drop, we don't have enough information to do this. Another thing would be to not have the burst in the first place.

        // Constant absolute value for max buffer count instead of dynamic time based value. This is in case we have very low tick rates, so
        // that we don't have a very small buffer because of this.
        private const int k_BufferCountLimit = 100;
        private BufferedItem m_LastBufferedItemReceived;
        private int m_NbItemsReceivedThisFrame;

        protected internal T m_CurrentInterpValue;
        private int m_LifetimeConsumedCount;

        private bool InvalidState => m_Buffer.Count == 0 && m_LifetimeConsumedCount == 0;

        internal bool EndOfBuffer => m_Buffer.Count == 0;

        internal bool InLocalSpace;

        protected internal virtual void OnConvertTransformSpace(Transform transform, bool inLocalSpace)
        {

        }

        internal void ConvertTransformSpace(Transform transform, bool inLocalSpace)
        {
            OnConvertTransformSpace(transform, inLocalSpace);
            InLocalSpace = inLocalSpace;
        }

        /// <summary>
        /// Resets interpolator to initial state
        /// </summary>
        public void Clear()
        {
            m_Buffer.Clear();
            m_CurrentInterpValue = default;
            InterpolateState = new CurrentState()
            {
                CurrentValue = default,
                LerpT = 0.0000001f,
            };
        }

        /// <summary>
        /// Teleports current interpolation value to targetValue.
        /// </summary>
        /// <param name="targetValue">The target value to teleport instantly</param>
        /// <param name="serverTime">The current server time</param>
        public void ResetTo(T targetValue, double serverTime)
        {
            m_LifetimeConsumedCount = 1;
            m_Buffer.Clear();
            m_Name = GetType().Name;
            m_CurrentInterpValue = targetValue;
            InterpolateState = new CurrentState()
            {
                CurrentValue = targetValue,
                LerpT = 0.0000001f,
            };
            Update(0, serverTime, serverTime);
        }

        // todo if I have value 1, 2, 3 and I'm treating 1 to 3, I shouldn't interpolate between 1 and 3, I should interpolate from 1 to 2, then from 2 to 3 to get the best path
#if OLDSTUFF
        private void TryConsumeFromBufferLDK(double renderTime, double serverTime)
        {
            int consumedCount = 0;
            // only consume if we're ready

            //  this operation was measured as one of our most expensive, and we should put some thought into this.
            //   NetworkTransform has (currently) 7 buffered linear interpolators (3 position, 3 scale, 1 rot), and
            //   each has its own independent buffer and 'm_endTimeConsume'.  That means every frame I have to do 7x
            //   these checks vs. if we tracked these values in a unified way
            if (renderTime >= m_EndTimeConsumed)
            {
                BufferedItem? itemToInterpolateTo = null;
                // assumes we're using sequenced messages for netvar syncing
                // buffer contains oldest values first, iterating from end to start to remove elements from list while iterating

                // calling m_Buffer.Count shows up hot in the profiler.
                for (int i = m_Buffer.Count - 1; i >= 0; i--) // todo stretch: consume ahead if we see we're missing values due to packet loss
                {
                    var bufferedValue = m_Buffer[i];
                    // Consume when ready and interpolate to last value we can consume. This can consume multiple values from the buffer
                    if (bufferedValue.TimeSent <= serverTime)
                    {
                        if (!itemToInterpolateTo.HasValue || bufferedValue.TimeSent > itemToInterpolateTo.Value.TimeSent)
                        {
                            if (m_LifetimeConsumedCount == 0)
                            {
                                // if interpolator not initialized, teleport to first value when available
                                m_StartTimeConsumed = bufferedValue.TimeSent;
                                m_InterpStartValue = bufferedValue.Item;
                            }
                            else if (consumedCount == 0)
                            {
                                // Interpolating to new value, end becomes start. We then look in our buffer for a new end.

                                // !!!! This does not account for gaps between values !!!
                                // if last entry is > several ticks then the range is going to be very large!
                                m_StartTimeConsumed = renderTime;
                                m_InterpStartValue = m_CurrentInterpValue;
                            }

                            if ((bufferedValue.TimeSent - m_StartTimeConsumed) >= k_TickFrequency)
                            {
                                itemToInterpolateTo = bufferedValue;
                                m_EndTimeConsumed = bufferedValue.TimeSent;
                                m_InterpEndValue = bufferedValue.Item;
                                m_Buffer.RemoveAt(i);
                                m_LifetimeConsumedCount++;
                                break;
                            }
                        }

                        m_Buffer.RemoveAt(i);
                        consumedCount++;
                        m_LifetimeConsumedCount++;
                    }
                }
            }
        }
#endif
        private string m_Name;

        internal struct CurrentState
        {
            public BufferedItem? Start;

            public BufferedItem? End;

            public double RelativeTime;

            public T CurrentValue;

            public float LerpT;
        }

        internal CurrentState InterpolateState;

        private void TryConsumeFromBuffer(double renderTime, double serverTime)
        {
            // If we don't have our initial buffered item/starting point or our end point or the end point's time sent is less than the
            // render time
            if (!InterpolateState.Start.HasValue || !InterpolateState.End.HasValue || InterpolateState.End.Value.TimeSent < renderTime)
            {
                BufferedItem? previousItem = null;
                while (m_Buffer.TryPeek(out BufferedItem potentialItem))
                {
                    if (previousItem.HasValue && previousItem.Value.TimeSent == potentialItem.TimeSent)
                    {
                        break;
                    }

                    if (potentialItem.TimeSent <= serverTime)
                    {
                        // We want to initialize and then always set the end
                        if (!InterpolateState.Start.HasValue)
                        {
                            if (m_Buffer.TryDequeue(out BufferedItem start))
                            {
                                InterpolateState.Start = start;
                                InterpolateState.RelativeTime = InterpolateState.Start.Value.TimeSent;
                                InterpolateState.CurrentValue = start.Item;
                                InterpolateState.LerpT = 0.0f;
                                InterpolateState.Start = start;
                            }
                        }
                        else if (!InterpolateState.End.HasValue || InterpolateState.End.Value.TimeSent < potentialItem.TimeSent)
                        {
                            if (m_Buffer.TryDequeue(out BufferedItem end))
                            {
                                if (InterpolateState.End.HasValue)
                                {
                                    InterpolateState.Start = InterpolateState.End;
                                    //m_CurrentState.RelativeTime = m_CurrentState.End.Value.TimeSent;
                                }
                                InterpolateState.End = end;
                                InterpolateState.LerpT = 0.0f;
                                m_LifetimeConsumedCount++;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (!InterpolateState.Start.HasValue)
                    {
                        break;
                    }
                    previousItem = potentialItem;
                }
            }
        }

        /// <summary>
        /// Convenience version of 'Update' mainly for testing
        ///  the reason we don't want to always call this version is so that on the calling side we can compute
        ///  the renderTime once for the many things being interpolated (and the many interpolators per object)
        /// </summary>
        /// <param name="deltaTime">time since call</param>
        /// <param name="serverTime">current server time</param>
        /// <returns>The newly interpolated value of type 'T'</returns>
        public T Update(float deltaTime, NetworkTime serverTime)
        {
            return Update(deltaTime, serverTime.TimeTicksAgo(1).Time, serverTime.Time);
        }

        /// <summary>
        /// Call to update the state of the interpolators before reading out
        /// </summary>
        /// <param name="deltaTime">time since last call</param>
        /// <param name="renderTime">our current time</param>
        /// <param name="serverTime">current server time</param>
        /// <returns>The newly interpolated value of type 'T'</returns>
        public T Update(float deltaTime, double renderTime, double serverTime)
        {
            TryConsumeFromBuffer(renderTime, serverTime);

            if (InvalidState)
            {
                throw new InvalidOperationException("trying to update interpolator when no data has been added to it yet");
            }

            // Only interpolate when there is a start and end point and we have not already reached the end value
            if (InterpolateState.Start.HasValue && InterpolateState.End.HasValue)
            {
                if (InterpolateState.LerpT < 1.0f)
                {
                    InterpolateState.RelativeTime = Math.Clamp(InterpolateState.RelativeTime + deltaTime, 0.000001f, InterpolateState.End.Value.TimeSent);
                    //var t = 1.0f - Mathf.Clamp((float)((m_EndTimeConsumed - renderTime) * rangeFactor), 0.0f, 1.0f);
                    //var alt_t = 1.0f - Mathf.Clamp((float)((renderTime - m_StartTimeConsumed) * rangeFactor), 0.0f, 1.0f);
                    InterpolateState.LerpT = (float)(InterpolateState.RelativeTime / InterpolateState.End.Value.TimeSent);
                    InterpolateState.CurrentValue = Interpolate(InterpolateState.CurrentValue, InterpolateState.End.Value.Item, InterpolateState.LerpT);
                    if (InterpolateState.LerpT < 1.0f)
                    {
                        m_CurrentInterpValue = Interpolate(m_CurrentInterpValue, InterpolateState.CurrentValue, 0.5f);
                    }
                    else
                    {
                        m_CurrentInterpValue = InterpolateState.CurrentValue;
                    }
                }
            }

            m_NbItemsReceivedThisFrame = 0;
            return m_CurrentInterpValue;
        }


        private double m_LastSentTime = 0.0f;
        /// <summary>
        /// Add measurements to be used during interpolation. These will be buffered before being made available to be displayed as "latest value".
        /// </summary>
        /// <param name="newMeasurement">The new measurement value to use</param>
        /// <param name="sentTime">The time to record for measurement</param>
        public void AddMeasurement(T newMeasurement, double sentTime)
        {
            m_NbItemsReceivedThisFrame++;

            // This situation can happen after a game is paused. When starting to receive again, the server will have sent a bunch of messages in the meantime
            // instead of going through thousands of value updates just to get a big teleport, we're giving up on interpolation and teleporting to the latest value
            if (m_NbItemsReceivedThisFrame > k_BufferCountLimit)
            {
                if (m_LastBufferedItemReceived.TimeSent < sentTime)
                {
                    m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime);
                    ResetTo(newMeasurement, sentTime);
                    // Next line keeps renderTime above m_StartTimeConsumed. Fixes pause/unpause issues
                    m_Buffer.Enqueue(m_LastBufferedItemReceived);
                }

                return;
            }

            // Part the of reason for disabling extrapolation is how we add and use measurements over time.
            // TODO: Add detailed description of this area in Jira ticket
            if (sentTime > m_LastSentTime || m_LifetimeConsumedCount == 0) // treat only if value is newer than the one being interpolated to right now
            {
                m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime);
                m_Buffer.Enqueue(m_LastBufferedItemReceived);
                m_LastSentTime = sentTime;
            }
            else
            {
                Debug.Log($"[{m_Name}] Dropping measurement -- Time: {sentTime} Value: {newMeasurement}");
            }
        }

        /// <summary>
        /// Gets latest value from the interpolator. This is updated every update as time goes by.
        /// </summary>
        /// <returns>The current interpolated value of type 'T'</returns>
        public T GetInterpolatedValue()
        {
            return m_CurrentInterpValue;
        }

        /// <summary>
        /// Method to override and adapted to the generic type. This assumes interpolation for that value will be clamped.
        /// </summary>
        /// <param name="start">The start value (min)</param>
        /// <param name="end">The end value (max)</param>
        /// <param name="time">The time value used to interpolate between start and end values (pos)</param>
        /// <returns>The interpolated value</returns>
        protected abstract T Interpolate(T start, T end, float time);

        /// <summary>
        /// Method to override and adapted to the generic type. This assumes interpolation for that value will not be clamped.
        /// </summary>
        /// <param name="start">The start value (min)</param>
        /// <param name="end">The end value (max)</param>
        /// <param name="time">The time value used to interpolate between start and end values (pos)</param>
        /// <returns>The interpolated value</returns>
        protected abstract T InterpolateUnclamped(T start, T end, float time);
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a buffered linear interpolator for a <see cref="float"/> type value
    /// </remarks>
    public class BufferedLinearInterpolatorFloat : BufferedLinearInterpolator<float>
    {
        /// <inheritdoc />
        protected override float InterpolateUnclamped(float start, float end, float time)
        {
            // Disabling Extrapolation:
            // TODO: Add Jira Ticket
            return Mathf.LerpUnclamped(start, end, time);
        }

        /// <inheritdoc />
        protected override float Interpolate(float start, float end, float time)
        {
            return Mathf.Lerp(start, end, time);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a buffered linear interpolator for a <see cref="Quaternion"/> type value
    /// </remarks>
    public class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        /// <summary>
        /// Use <see cref="Quaternion.Slerp"/> when <see cref="true"/>.
        /// Use <see cref="Quaternion.Lerp"/> when <see cref="false"/>
        /// </summary>
        /// <remarks>
        /// When using half precision (due to the imprecision) using <see cref="Quaternion.Lerp"/> is
        /// less processor intensive (i.e. precision is already "imprecise").
        /// When using full precision (to maintain precision) using <see cref="Quaternion.Slerp"/> is
        /// more processor intensive yet yields more precise results.
        /// </remarks>
        public bool IsSlerp;

        /// <inheritdoc />
        protected override Quaternion InterpolateUnclamped(Quaternion start, Quaternion end, float time)
        {
            if (IsSlerp)
            {
                return Quaternion.SlerpUnclamped(start, end, time);
            }
            else
            {
                return Quaternion.LerpUnclamped(start, end, time);
            }
        }

        /// <inheritdoc />
        protected override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            if (IsSlerp)
            {
                return Quaternion.Slerp(start, end, time);
            }
            else
            {
                return Quaternion.Lerp(start, end, time);
            }
        }

        private Quaternion ConvertToNewTransformSpace(Transform transform, Quaternion rotation, bool inLocalSpace)
        {
            if (inLocalSpace)
            {
                return Quaternion.Inverse(transform.rotation) * rotation;

            }
            else
            {
                return transform.rotation * rotation;
            }
        }

        protected internal override void OnConvertTransformSpace(Transform transform, bool inLocalSpace)
        {
            var buffer = m_Buffer.ToList();
            m_Buffer.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                var entry = buffer[i];
                entry.Item = ConvertToNewTransformSpace(transform, entry.Item, inLocalSpace);
                m_Buffer.Enqueue(entry);
            }
            InterpolateState.CurrentValue = ConvertToNewTransformSpace(transform, InterpolateState.CurrentValue, inLocalSpace);
            m_CurrentInterpValue = ConvertToNewTransformSpace(transform, m_CurrentInterpValue, inLocalSpace);
            var end = InterpolateState.End.Value;
            end.Item = ConvertToNewTransformSpace(transform, end.Item, inLocalSpace);
            InterpolateState.End = end;

            base.OnConvertTransformSpace(transform, inLocalSpace);
        }
    }

    /// <summary>
    /// A <see cref="BufferedLinearInterpolator{T}"/> <see cref="Vector3"/> implementation.
    /// </summary>
    public class BufferedLinearInterpolatorVector3 : BufferedLinearInterpolator<Vector3>
    {
        /// <summary>
        /// Use <see cref="Vector3.Slerp"/> when <see cref="true"/>.
        /// Use <see cref="Vector3.Lerp"/> when <see cref="false"/>
        /// </summary>
        public bool IsSlerp;
        /// <inheritdoc />
        protected override Vector3 InterpolateUnclamped(Vector3 start, Vector3 end, float time)
        {
            if (IsSlerp)
            {
                return Vector3.SlerpUnclamped(start, end, time);
            }
            else
            {
                return Vector3.LerpUnclamped(start, end, time);
            }
        }

        /// <inheritdoc />
        protected override Vector3 Interpolate(Vector3 start, Vector3 end, float time)
        {
            if (IsSlerp)
            {
                return Vector3.Slerp(start, end, time);
            }
            else
            {
                return Vector3.Lerp(start, end, time);
            }
        }

        private Vector3 ConvertToNewTransformSpace(Transform transform, Vector3 position, bool inLocalSpace)
        {
            if (inLocalSpace)
            {
                return transform.InverseTransformPoint(position);

            }
            else
            {
                return transform.TransformPoint(position);
            }
        }

        protected internal override void OnConvertTransformSpace(Transform transform, bool inLocalSpace)
        {
            var buffer = m_Buffer.ToList();
            m_Buffer.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                var entry = buffer[i];
                entry.Item = ConvertToNewTransformSpace(transform, entry.Item, inLocalSpace);
                m_Buffer.Enqueue(entry);
            }

            InterpolateState.CurrentValue = ConvertToNewTransformSpace(transform, InterpolateState.CurrentValue, inLocalSpace);
            m_CurrentInterpValue = ConvertToNewTransformSpace(transform, m_CurrentInterpValue, inLocalSpace);
            var end = InterpolateState.End.Value;
            end.Item = ConvertToNewTransformSpace(transform, end.Item, inLocalSpace);
            InterpolateState.End = end;

            base.OnConvertTransformSpace(transform, inLocalSpace);
        }
    }
}
