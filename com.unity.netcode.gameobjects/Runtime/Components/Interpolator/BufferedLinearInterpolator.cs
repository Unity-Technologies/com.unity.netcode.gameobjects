using System;
using System.Collections.Generic;
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
        private const float k_AproximatePrecision = 0.0001f;

        /// <summary>
        /// Represents a buffered item measurement.
        /// </summary>
        protected internal struct BufferedItem
        {
            /// <summary>
            /// THe item identifier
            /// </summary>
            public int ItemId;
            /// <summary>
            /// The item value
            /// </summary>
            public T Item;
            /// <summary>
            /// The time the item was sent.
            /// </summary>
            public double TimeSent;

            /// <summary>
            /// The constructor
            /// </summary>
            /// <param name="item">The item value.</param>
            /// <param name="timeSent">The time the item was sent.</param>
            /// <param name="itemId">The item identifier</param>
            public BufferedItem(T item, double timeSent, int itemId)
            {
                Item = item;
                TimeSent = timeSent;
                ItemId = itemId;
            }
        }
        internal struct CurrentState
        {
            public BufferedItem? Target;

            public double StartTime;
            public double EndTime;
            public float TimeToTargetValue;
            public float DeltaTime;
            public float LerpT;

            public T CurrentValue;
            public T PreviousValue;

            private float m_AverageDeltaTime;

            public float AverageDeltaTime => m_AverageDeltaTime;
            public float FinalTimeToTarget => TimeToTargetValue - DeltaTime;

            public void AddDeltaTime(float deltaTime)
            {
                if (m_AverageDeltaTime == 0.0f)
                {
                    m_AverageDeltaTime = deltaTime;
                }
                else
                {
                    m_AverageDeltaTime += deltaTime;
                    m_AverageDeltaTime *= 0.5f;
                }
                DeltaTime = Math.Min(DeltaTime + m_AverageDeltaTime, TimeToTargetValue);
                LerpT = TimeToTargetValue == 0.0f ? 1.0f : DeltaTime / TimeToTargetValue;
            }

            public void ResetDelta()
            {
                m_AverageDeltaTime = 0.0f;
                DeltaTime = 0.0f;
            }

            public bool TargetTimeAproximatelyReached()
            {
                if (!Target.HasValue)
                {
                    return false;
                }
                return m_AverageDeltaTime >= FinalTimeToTarget;
            }

            public void Reset(T currentValue)
            {
                Target = null;
                CurrentValue = currentValue;
                PreviousValue = currentValue;
                // When reset, we consider ourselves to have already arrived at the target (even if no target is set)
                LerpT = 0.0f;
                EndTime = 0.0;
                StartTime = 0.0;
                ResetDelta();
            }
        }

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

        private const double k_SmallValue = 9.999999439624929E-11; // copied from Vector3's equal operator


        /// <summary>
        /// There's two factors affecting interpolation: buffering (set in NetworkManager's NetworkTimeSystem) and interpolation time, which is the amount of time it'll take to reach the target. This is to affect the second one.
        /// </summary>
        public float MaximumInterpolationTime = 0.1f;

        /// <summary>
        /// The maximum Lerp "t" boundary when using standard lerping for interpolation
        /// </summary>
        internal float MaxInterpolationBound = 3.0f;

        private int m_BufferCount;

        private BufferedItem m_LastBufferedItemReceived;
        private int m_NbItemsReceivedThisFrame;

        private double m_LastMeasurementAddedTime = 0.0f;
        internal bool EndOfBuffer => m_Buffer.Count == 0;

        internal bool InLocalSpace;

        /// <summary>
        /// The current interpolation state
        /// </summary>
        internal CurrentState InterpolateState;

        /// <summary>
        /// The current buffered items received by the authority.
        /// </summary>
        protected internal readonly Queue<BufferedItem> m_Buffer = new Queue<BufferedItem>(k_BufferCountLimit);

        /// <summary>
        /// Represents the rate of change for the value being interpolated when smooth dampening is enabled.
        /// </summary>
        private T m_RateOfChange;

        /// <summary>
        /// Represents the predicted rate of change for the value being interpolated when smooth dampening is enabled.
        /// </summary>
        private T m_PredictedRateOfChange;

        private bool m_IsAngularValue;

        /// <summary>
        /// When true, the value <see cref="T"/> is an angular numeric representation.
        /// </summary>
        protected bool IsAngularValue => m_IsAngularValue;

        /// <summary>
        /// Resets interpolator to the defaults.
        /// </summary>
        public void Clear()
        {
            m_Buffer.Clear();
            m_BufferCount = 0;
            m_LastMeasurementAddedTime = 0.0;
            InterpolateState.Reset(default);
            m_RateOfChange = default;
        }

        /// <summary>
        /// Resets the current interpolator to the target valueTeleports current interpolation value to targetValue.
        /// </summary>
        /// <remarks>
        /// This is used when first synchronizing/initializing and when telporting an object.
        /// </remarks>
        /// <param name="targetValue">The target value to reset the interpolator to</param>
        /// <param name="serverTime">The current server time</param>        
        /// <param name="isAngularValue">When rotation is expressed as Euler values (i.e. Vector3 and/or float) this helps determine what kind of smooth dampening to use.</param>
        public void ResetTo(T targetValue, double serverTime, bool isAngularValue = false)
        {
            InternalReset(targetValue, serverTime, isAngularValue);
        }

        private void InternalReset(T targetValue, double serverTime, bool isAngularValue = false, bool addMeasurement = true)
        {
            m_RateOfChange = default;
            // Set our initial value
            InterpolateState.Reset(targetValue);
            m_IsAngularValue = isAngularValue;

            if (addMeasurement)
            {
                // Add the first measurement for our baseline
                AddMeasurement(targetValue, serverTime);
            }
        }

        #region Smooth Dampening Interpolation
        /// <summary>
        /// TryConsumeFromBuffer: Smooth Dampening Version
        /// </summary>
        /// <param name="renderTime">render time: the time in "ticks ago" relative to the current tick latency</param>
        /// <param name="minDeltaTime">minimum time delta (defaults to tick frequency)</param>
        /// <param name="maxDeltaTime">maximum time delta which defines the maximum time duration when consuming more than one item from the buffer</param>
        private void TryConsumeFromBuffer(double renderTime, float minDeltaTime, float maxDeltaTime)
        {
            if (!InterpolateState.Target.HasValue || (InterpolateState.Target.Value.TimeSent <= renderTime
                 && (InterpolateState.TargetTimeAproximatelyReached() || IsAproximately(InterpolateState.CurrentValue, InterpolateState.Target.Value.Item))))
            {
                BufferedItem? previousItem = null;
                var startTime = 0.0;
                var alreadyHasBufferItem = false;
                while (m_Buffer.TryPeek(out BufferedItem potentialItem))
                {
                    // If we are still on the same buffered item (FIFO Queue), then exit early as there is nothing
                    // to consume.
                    if (previousItem.HasValue && previousItem.Value.TimeSent == potentialItem.TimeSent)
                    {
                        break;
                    }

                    // If we haven't set a target or the potential item's time sent is less that the current target's time sent
                    // then pull the BufferedItem from the queue. The second portion of this accounts for scenarios where there
                    // was bad latency and the buffer has more than one item in the queue that is less than the renderTime. Under
                    // this scenario, we just want to continue pulling items from the queue until the last item pulled from the
                    // queue is greater than the redner time or greater than the currently targeted item.
                    if (!InterpolateState.Target.HasValue ||
                        ((potentialItem.TimeSent <= renderTime) && InterpolateState.Target.Value.TimeSent <= potentialItem.TimeSent))
                    {
                        if (m_Buffer.TryDequeue(out BufferedItem target))
                        {
                            if (!InterpolateState.Target.HasValue)
                            {
                                InterpolateState.Target = target;

                                alreadyHasBufferItem = true;
                                InterpolateState.PreviousValue = InterpolateState.CurrentValue;
                                InterpolateState.TimeToTargetValue = minDeltaTime;
                                startTime = InterpolateState.Target.Value.TimeSent;
                            }
                            else
                            {
                                if (!alreadyHasBufferItem)
                                {
                                    alreadyHasBufferItem = true;
                                    startTime = InterpolateState.Target.Value.TimeSent;
                                    InterpolateState.PreviousValue = InterpolateState.CurrentValue;
                                    InterpolateState.LerpT = 0.0f;
                                }
                                // TODO: We might consider creating yet another queue to add these items to and assure that the time is accelerated
                                // for each item as opposed to losing the resolution of the values.
                                InterpolateState.TimeToTargetValue = Mathf.Clamp((float)(target.TimeSent - startTime), minDeltaTime, maxDeltaTime);
                                InterpolateState.Target = target;
                            }
                            InterpolateState.ResetDelta();
                        }
                    }
                    else
                    {
                        break;
                    }

                    if (!InterpolateState.Target.HasValue)
                    {
                        break;
                    }
                    previousItem = potentialItem;
                }
            }
        }

        /// <summary>
        /// Interpolation Update to use when smooth dampening is enabled on a <see cref="Components.NetworkTransform"/>.
        /// </summary>
        /// <remarks>
        /// Alternate recommended interpolation when when <see cref="Components.NetworkRigidbodyBase.UseRigidBodyForMotion"/> is enabled.<br />
        /// This can provide a precise interpolation result between the current and target values at the expense of not being as smooth as then doulbe Lerp approach.
        /// </remarks>
        /// <param name="deltaTime">The last frame time that is either <see cref="Time.deltaTime"/> for non-rigidbody motion and <see cref="Time.fixedDeltaTime"/> when using ridigbody motion.</param>
        /// <param name="tickLatencyAsTime">The tick latency in relative local time.</param>
        /// <param name="minDeltaTime">The minimum time delta between the current and target value.</param>
        /// <param name="maxDeltaTime">The maximum time delta between the current and target value.</param>
        /// <returns>The newly interpolated value of type 'T'</returns>
        public T Update(float deltaTime, double tickLatencyAsTime, float minDeltaTime, float maxDeltaTime)
        {
            TryConsumeFromBuffer(tickLatencyAsTime, minDeltaTime, maxDeltaTime);
            // Only interpolate when there is a start and end point and we have not already reached the end value
            if (InterpolateState.Target.HasValue)
            {
                InterpolateState.AddDeltaTime(deltaTime);

                // Smooth dampen our current time
                var current = SmoothDamp(InterpolateState.CurrentValue, InterpolateState.Target.Value.Item, ref m_RateOfChange, InterpolateState.TimeToTargetValue, InterpolateState.DeltaTime);
                // Smooth dampen a predicted time based on our average delta time 
                var predict = SmoothDamp(InterpolateState.CurrentValue, InterpolateState.Target.Value.Item, ref m_PredictedRateOfChange, InterpolateState.TimeToTargetValue, InterpolateState.DeltaTime + InterpolateState.AverageDeltaTime);
                // Split the difference between the two.
                // Note: Since smooth dampening cannot over shoot, both current and predict will eventually become the same or will be very close to the same.
                // Upon stopping motion, the final resing value should be a very close aproximation of the authority side.
                InterpolateState.CurrentValue = Interpolate(current, predict, 0.5f);
            }
            m_NbItemsReceivedThisFrame = 0;
            return InterpolateState.CurrentValue;
        }
        #endregion

        #region Lerp Interpolation
        /// <summary>
        /// TryConsumeFromBuffer: Lerping Version
        /// </summary>
        /// <remarks>
        /// This version of TryConsumeFromBuffer adheres to the original BufferedLinearInterpolator buffer consumption pattern.
        /// </remarks>
        /// <param name="renderTime"></param>
        /// <param name="serverTime"></param>
        private void TryConsumeFromBuffer(double renderTime, double serverTime)
        {
            if (!InterpolateState.Target.HasValue || (InterpolateState.Target.Value.TimeSent <= renderTime))
            {
                BufferedItem? previousItem = null;
                var alreadyHasBufferItem = false;
                while (m_Buffer.TryPeek(out BufferedItem potentialItem))
                {
                    // If we are still on the same buffered item (FIFO Queue), then exit early as there is nothing
                    // to consume.
                    if (previousItem.HasValue && previousItem.Value.TimeSent == potentialItem.TimeSent)
                    {
                        break;
                    }

                    if ((potentialItem.TimeSent <= serverTime) &&
                        (!InterpolateState.Target.HasValue || InterpolateState.Target.Value.TimeSent < potentialItem.TimeSent))
                    {
                        if (m_Buffer.TryDequeue(out BufferedItem target))
                        {
                            if (!InterpolateState.Target.HasValue)
                            {
                                InterpolateState.Target = target;

                                alreadyHasBufferItem = true;
                                InterpolateState.PreviousValue = InterpolateState.CurrentValue;
                                InterpolateState.StartTime = target.TimeSent;
                                InterpolateState.EndTime = target.TimeSent;
                            }
                            else
                            {
                                if (!alreadyHasBufferItem)
                                {
                                    alreadyHasBufferItem = true;
                                    InterpolateState.StartTime = InterpolateState.Target.Value.TimeSent;
                                    InterpolateState.PreviousValue = InterpolateState.CurrentValue;
                                }
                                InterpolateState.EndTime = target.TimeSent;
                                InterpolateState.Target = target;
                            }
                            InterpolateState.ResetDelta();
                        }
                    }
                    else
                    {
                        break;
                    }

                    if (!InterpolateState.Target.HasValue)
                    {
                        break;
                    }
                    previousItem = potentialItem;
                }
            }
        }

        /// <summary>
        /// Call to update the state of the interpolators using Lerp.
        /// </summary>
        /// <remarks>
        /// This approah uses double lerping which can result in an over-smoothed result.
        /// </remarks>
        /// <param name="deltaTime">time since last call</param>
        /// <param name="renderTime">our current time</param>
        /// <param name="serverTime">current server time</param>
        /// <returns>The newly interpolated value of type 'T'</returns>
        public T Update(float deltaTime, double renderTime, double serverTime)
        {
            TryConsumeFromBuffer(renderTime, serverTime);
            // Only interpolate when there is a start and end point and we have not already reached the end value
            if (InterpolateState.Target.HasValue)
            {
                // The original BufferedLinearInterpolator lerping script to assure the Smooth Dampening updates do not impact
                // this specific behavior.
                float t = 1.0f;
                double range = InterpolateState.EndTime - InterpolateState.StartTime;
                if (range > k_SmallValue)
                {
                    t = (float)((renderTime - InterpolateState.StartTime) / range);

                    if (t < 0.0f)
                    {
                        t = 0.0f;
                    }

                    if (t > MaxInterpolationBound) // max extrapolation
                    {
                        // TODO this causes issues with teleport, investigate
                        t = 1.0f;
                    }
                }
                var target = Interpolate(InterpolateState.PreviousValue, InterpolateState.Target.Value.Item, t);
                InterpolateState.CurrentValue = Interpolate(InterpolateState.CurrentValue, target, deltaTime / MaximumInterpolationTime);
            }
            m_NbItemsReceivedThisFrame = 0;
            return InterpolateState.CurrentValue;
        }
        #endregion

        /// <summary>
        /// Used for internal testing
        /// </summary>
        internal T UpdateInternal(float deltaTime, NetworkTime serverTime)
        {
            return Update(deltaTime, serverTime.TimeTicksAgo(1).Time, serverTime.Time);
        }

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
                    InternalReset(newMeasurement, sentTime, IsAngularValue, false);
                    m_LastMeasurementAddedTime = sentTime;
                    m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime, m_BufferCount);
                    // Next line keeps renderTime above m_StartTimeConsumed. Fixes pause/unpause issues
                    m_Buffer.Enqueue(m_LastBufferedItemReceived);
                }
                return;
            }

            // Drop measurements that are received out of order/late
            if (sentTime > m_LastMeasurementAddedTime || m_BufferCount == 0)
            {
                m_BufferCount++;
                m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime, m_BufferCount);
                m_Buffer.Enqueue(m_LastBufferedItemReceived);
                m_LastMeasurementAddedTime = sentTime;
            }
        }

        /// <summary>
        /// Gets latest value from the interpolator. This is updated every update as time goes by.
        /// </summary>
        /// <returns>The current interpolated value of type 'T'</returns>
        public T GetInterpolatedValue()
        {
            return InterpolateState.CurrentValue;
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


        /// <summary>
        /// An alternate smoothing method to Lerp.
        /// </summary>
        /// <param name="current">Current item <see cref="T"/> value.</param>
        /// <param name="target">Target item <see cref="T"/> value.</param>
        /// <param name="rateOfChange">The velocity of change.</param>
        /// <param name="duration">Total time to smooth between the <paramref name="current"/> and <paramref name="target"/>.</param>
        /// <param name="deltaTime">The increasing delta time from when start to finish.</param>
        /// <param name="maxSpeed">Maximum rate of change per pass.</param>
        /// <returns>The smoothed <see cref="T"/> value.</returns>
        protected internal virtual T SmoothDamp(T current, T target, ref T rateOfChange, float duration, float deltaTime, float maxSpeed = Mathf.Infinity)
        {
            return target;
        }

        /// <summary>
        /// Determines if two values of type <see cref="T"/> are close to the same value.
        /// </summary>
        /// <param name="first">First value of type <see cref="T"/>.</param>
        /// <param name="second">Second value of type <see cref="T"/>.</param>
        /// <param name="precision">The precision of the aproximation.</param>
        /// <returns>true if the two values are aproximately the same and false if they are not</returns>
        protected internal virtual bool IsAproximately(T first, T second, float precision = k_AproximatePrecision)
        {
            return false;
        }

        /// <summary>
        /// Converts a value of type <see cref="T"/> from world to local space or vice versa.
        /// </summary>
        /// <param name="transform">Reference transform.</param>
        /// <param name="item">The item to convert.</param>
        /// <param name="inLocalSpace">local or world space (true or false).</param>
        /// <returns>The converted value.</returns>
        protected internal virtual T OnConvertTransformSpace(Transform transform, T item, bool inLocalSpace)
        {
            return default;
        }

        internal void ConvertTransformSpace(Transform transform, bool inLocalSpace)
        {
            var count = m_Buffer.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = m_Buffer.Dequeue();
                entry.Item = OnConvertTransformSpace(transform, entry.Item, inLocalSpace);
                m_Buffer.Enqueue(entry);
            }
            InterpolateState.CurrentValue = OnConvertTransformSpace(transform, InterpolateState.CurrentValue, inLocalSpace);
            var end = InterpolateState.Target.Value;
            end.Item = OnConvertTransformSpace(transform, end.Item, inLocalSpace);
            InterpolateState.Target = end;
            InLocalSpace = inLocalSpace;
        }
    }
}
