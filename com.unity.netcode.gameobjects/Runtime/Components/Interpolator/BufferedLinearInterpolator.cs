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
        // Constant absolute value for max buffer count instead of dynamic time based value. This is in case we have very low tick rates, so
        // that we don't have a very small buffer because of this.
        private const int k_BufferCountLimit = 100;
        private const float k_AproximatePrecision = 0.0001f;
        private const double k_SmallValue = 9.999999439624929E-11; // copied from Vector3's equal operator

        #region Legacy notes
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
        #endregion

        #region Properties being deprecated
        /// <summary>
        /// The legacy list of <see cref="BufferedItem"/> items.
        /// </summary>
        /// <remarks>
        /// This is replaced by the <see cref="m_BufferQueue"/> of type <see cref="Queue{T}"/>.
        /// </remarks>
        [Obsolete("This list is no longer used and will be deprecated.", false)]
        protected internal readonly List<BufferedItem> m_Buffer = new List<BufferedItem>();

        /// <summary>
        /// ** Deprecating **
        /// The starting value of type <see cref="T"/> to interpolate from.
        /// </summary>
        [Obsolete("This property will be deprecated.", false)]
        protected internal T m_InterpStartValue;

        /// <summary>
        /// ** Deprecating **
        /// The current value of type <see cref="T"/>.
        /// </summary>
        [Obsolete("This property will be deprecated.", false)]
        protected internal T m_CurrentInterpValue;

        /// <summary>
        /// ** Deprecating **
        /// The end (or target) value of type <see cref="T"/> to interpolate towards.
        /// </summary>
        [Obsolete("This property will be deprecated.", false)]
        protected internal T m_InterpEndValue;
        #endregion

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
            /// The constructor.
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

        /// <summary>
        /// The current internal state of the <see cref="BufferedLinearInterpolator{T}"/>.
        /// </summary>
        /// <remarks>
        /// Not public API ready yet.
        /// </remarks>
        internal struct CurrentState
        {
            public BufferedItem? Target;
            public double StartTime;
            public double EndTime;
            public double TimeToTargetValue;
            public double DeltaTime;
            public double DeltaTimePredict;
            public double MaxDeltaTime;
            public float LerpT;
            public float LerpTPredict;
            public bool TargetReached;
            public bool PredictingNext;
            public T CurrentValue;
            public T PreviousValue;
            public T PredictValue;
            public T Phase1Value;
            public T Phase2Value;

            private float m_AverageDeltaTime;

            public float AverageDeltaTime => m_AverageDeltaTime;
            public double FinalTimeToTarget => Math.Max(0.0, TimeToTargetValue - DeltaTime);

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
                DeltaTime = Math.Min(DeltaTime + deltaTime, TimeToTargetValue);
                DeltaTimePredict = Math.Min(DeltaTime + deltaTime, TimeToTargetValue + MaxDeltaTime);
                LerpT = (float)(TimeToTargetValue == 0.0 ? 1.0 : DeltaTime / TimeToTargetValue);
                if (PredictingNext)
                {
                    LerpTPredict = (float)(TimeToTargetValue == 0.0 ? 1.0 : DeltaTimePredict / TimeToTargetValue);
                }
                else
                {
                    LerpTPredict = LerpT;
                }
            }

            public void SetTimeToTarget(double timeToTarget)
            {
                m_AverageDeltaTime = 0.0f;
                DeltaTimePredict = 0.0f;
                LerpTPredict = 0.0f;
                LerpT = 0.0f;
                DeltaTime = 0.0f;
                TimeToTargetValue = timeToTarget;
            }

            public bool TargetTimeAproximatelyReached(float adjustForNext = 1.0f)
            {
                if (!Target.HasValue)
                {
                    return false;
                }
                return (m_AverageDeltaTime * adjustForNext) >= FinalTimeToTarget;
            }

            public void Reset(T currentValue)
            {
                Target = null;
                CurrentValue = currentValue;
                PreviousValue = currentValue;
                PredictValue = currentValue;
                Phase1Value = currentValue;
                Phase2Value = currentValue;
                TargetReached = false;
                PredictingNext = false;
                LerpT = 0.0f;
                LerpTPredict = 0.0f;
                EndTime = 0.0;
                StartTime = 0.0;
                TimeToTargetValue = 0.0f;
                DeltaTime = 0.0f;
                DeltaTimePredict = 0.0f;
                m_AverageDeltaTime = 0.0f;
            }
        }

        internal bool LerpSmoothEnabled;

        /// <summary>
        /// Determines how much smoothing will be applied to the 2nd lerp when using the <see cref="Update(float, double, double)"/> (i.e. lerping and not smooth dampening).
        /// </summary>
        /// <remarks>
        /// There's two factors affecting interpolation: <br />
        /// - Buffering: Which can be adjusted in set in the <see cref="NetworkManager.NetworkTimeSystem"/>.<br />
        /// - Interpolation time: The divisor applied to delta time where the quotient is used as the lerp time.
        /// </remarks>
        [Range(0.016f, 1.0f)]
        public float MaximumInterpolationTime = 0.1f;

        /// <summary>
        /// The current buffered items received by the authority.
        /// </summary>
        protected internal readonly Queue<BufferedItem> m_BufferQueue = new Queue<BufferedItem>(k_BufferCountLimit);

        /// <summary>
        /// The current interpolation state
        /// </summary>
        internal CurrentState InterpolateState;

        /// <summary>
        /// The maximum Lerp "t" boundary when using standard lerping for interpolation
        /// </summary>
        internal float MaxInterpolationBound = 3.0f;
        internal bool EndOfBuffer => m_BufferQueue.Count == 0;
        internal bool InLocalSpace;

        private double m_LastMeasurementAddedTime = 0.0f;
        private int m_BufferCount;
        private int m_NbItemsReceivedThisFrame;
        private BufferedItem m_LastBufferedItemReceived;
        /// <summary>
        /// Represents the rate of change for the value being interpolated when smooth dampening is enabled.
        /// </summary>
        private T m_RateOfChange;

        /// <summary>
        /// Represents the predicted rate of change for the value being interpolated when smooth dampening is enabled.
        /// </summary>
        private T m_PredictedRateOfChange;

        /// <summary>
        /// When true, the value <see cref="T"/> is an angular numeric representation.
        /// </summary>
        private protected bool m_IsAngularValue;

        private bool m_WasPredictedLerp;

        /// <summary>
        /// Resets interpolator to the defaults.
        /// </summary>
        public void Clear()
        {
            m_BufferQueue.Clear();
            m_BufferCount = 0;
            m_LastMeasurementAddedTime = 0.0;
            InterpolateState.Reset(default);
            m_RateOfChange = default;
            m_PredictedRateOfChange = default;
        }

        /// <summary>
        /// Resets the current interpolator to the target value.
        /// </summary>
        /// <remarks>
        /// This is used when first synchronizing/initializing and when telporting an object.
        /// </remarks>
        /// <param name="targetValue">The target value to reset the interpolator to</param>
        /// <param name="serverTime">The current server time</param>
        /// <param name="isAngularValue">When rotation is expressed as Euler values (i.e. Vector3 and/or float) this helps determine what kind of smooth dampening to use.</param>
        public void ResetTo(T targetValue, double serverTime, bool isAngularValue = false)
        {
            // Clear the interpolator
            Clear();
            InternalReset(targetValue, serverTime, isAngularValue);
        }

        private void InternalReset(T targetValue, double serverTime, bool isAngularValue = false, bool addMeasurement = true)
        {
            m_RateOfChange = default;
            m_PredictedRateOfChange = default;
            // Set our initial value
            InterpolateState.Reset(targetValue);
            m_IsAngularValue = isAngularValue;

            if (addMeasurement)
            {
                // Add the first measurement for our baseline
                AddMeasurement(targetValue, serverTime);
            }
        }

        #region Smooth Dampening and Lerp Extrapolate Blend Interpolation Handling
        /// <summary>
        /// TryConsumeFromBuffer: Smooth Dampening Version
        /// </summary>
        /// <param name="renderTime">render time: the time in "ticks ago" relative to the current tick latency</param>
        /// <param name="minDeltaTime">minimum time delta (defaults to tick frequency)</param>
        /// <param name="maxDeltaTime">maximum time delta which defines the maximum time duration when consuming more than one item from the buffer</param>
        private void TryConsumeFromBuffer(double renderTime, double minDeltaTime, double maxDeltaTime, bool isPredictedLerp)
        {
            BufferedItem? previousItem = null;
            var startTime = 0.0;
            var alreadyHasBufferItem = false;
            var noStateSet = !InterpolateState.Target.HasValue;
            var potentialItemNeedsProcessing = false;
            var currentTargetTimeReached = false;
            var dequeuedCount = 0;

            // In the event there is nothing left in the queue (i.e. motion/change stopped), we still need to determine if the target has been reached.
            if (!noStateSet && m_BufferQueue.Count == 0)
            {
                if (!InterpolateState.TargetReached)
                {
                    InterpolateState.TargetReached = IsAproximately(InterpolateState.CurrentValue, InterpolateState.Target.Value.Item);
                }
                return;
            }

            // Continue to process any remaining state updates in the queue (if any)
            while (m_BufferQueue.TryPeek(out BufferedItem potentialItem))
            {
                // If we are still on the same buffered item (FIFO Queue), then exit early as there is nothing
                // to consume.
                if (previousItem.HasValue && previousItem.Value.TimeSent == potentialItem.TimeSent)
                {
                    break;
                }

                if (!noStateSet)
                {
                    potentialItemNeedsProcessing = (potentialItem.TimeSent <= renderTime) && potentialItem.TimeSent >= InterpolateState.Target.Value.TimeSent;
                    currentTargetTimeReached = InterpolateState.TargetTimeAproximatelyReached(potentialItemNeedsProcessing ? 1.0f : 0.85f) || InterpolateState.TargetReached;
                    if (!InterpolateState.TargetReached)
                    {
                        InterpolateState.TargetReached = IsAproximately(InterpolateState.CurrentValue, InterpolateState.Target.Value.Item);
                    }
                }

                // If we haven't set a target or the potential item's time sent is less that the current target's time sent
                // then pull the BufferedItem from the queue. The second portion of this accounts for scenarios where there
                // was bad latency and the buffer has more than one item in the queue that is less than the renderTime. Under
                // this scenario, we just want to continue pulling items from the queue until the last item pulled from the
                // queue is greater than the redner time or greater than the currently targeted item.
                if (noStateSet || ((currentTargetTimeReached || InterpolateState.TargetReached) && potentialItemNeedsProcessing))
                {
                    if (m_BufferQueue.TryDequeue(out BufferedItem target))
                    {
                        dequeuedCount++;
                        if (!InterpolateState.Target.HasValue)
                        {
                            InterpolateState.Target = target;
                            alreadyHasBufferItem = true;
                            InterpolateState.PredictValue = InterpolateState.CurrentValue;
                            InterpolateState.PreviousValue = InterpolateState.CurrentValue;
                            InterpolateState.Phase1Value = InterpolateState.CurrentValue;
                            InterpolateState.Phase2Value = InterpolateState.CurrentValue;
                            InterpolateState.SetTimeToTarget(minDeltaTime);
                            InterpolateState.TimeToTargetValue = minDeltaTime;
                            startTime = InterpolateState.Target.Value.TimeSent;
                            InterpolateState.TargetReached = false;
                            InterpolateState.PredictingNext = false;
                            InterpolateState.MaxDeltaTime = maxDeltaTime;
                        }
                        else
                        {
                            if (!alreadyHasBufferItem)
                            {
                                alreadyHasBufferItem = true;
                                InterpolateState.TargetReached = false;
                                startTime = InterpolateState.Target.Value.TimeSent;
                                if (isPredictedLerp)
                                {
                                    InterpolateState.Phase1Value = InterpolateState.PreviousValue;
                                    InterpolateState.Phase2Value = Interpolate(InterpolateState.PreviousValue, target.Item, InterpolateState.AverageDeltaTime);
                                }
                                else
                                {
                                    InterpolateState.PredictValue = InterpolateState.PreviousValue;
                                    InterpolateState.PreviousValue = InterpolateState.CurrentValue;
                                }
                                InterpolateState.MaxDeltaTime = maxDeltaTime;
                                InterpolateState.PredictingNext = m_BufferQueue.Count > 0;
                            }
                            // We continue to stretch the time out if we are far behind in processing the buffer.
                            // TODO: We need to compress time when there is a large amount of items to be processed.
                            InterpolateState.SetTimeToTarget(Math.Max((float)(target.TimeSent - startTime), minDeltaTime));
                            InterpolateState.Target = target;
                        }
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

        internal void ResetCurrentState()
        {
            if (InterpolateState.Target.HasValue)
            {
                InterpolateState.Reset(InterpolateState.CurrentValue);
                m_RateOfChange = default;
                m_PredictedRateOfChange = default;
            }
        }

        /// <summary>
        /// Interpolation Update to use when smooth dampening is enabled on a <see cref="Components.NetworkTransform"/>.
        /// </summary>
        /// <remarks>
        /// Alternate recommended interpolation when <see cref="Components.NetworkRigidbodyBase.UseRigidBodyForMotion"/> is enabled.<br />
        /// This can provide a precise interpolation result between the current and target values at the expense of not being as smooth as then doulbe Lerp approach.
        /// </remarks>
        /// <param name="deltaTime">The last frame time that is either <see cref="Time.deltaTime"/> for non-rigidbody motion and <see cref="Time.fixedDeltaTime"/> when using ridigbody motion.</param>
        /// <param name="tickLatencyAsTime">The tick latency in relative local time.</param>
        /// <param name="minDeltaTime">The minimum time delta between the current and target value.</param>
        /// <param name="maxDeltaTime">The maximum time delta between the current and target value.</param>
        /// <param name="isLerpAndExtrapolate">Determines whether to use smooth dampening or extrapolation.</param>
        /// <returns>The newly interpolated value of type 'T'</returns>
        internal T Update(float deltaTime, double tickLatencyAsTime, double minDeltaTime, double maxDeltaTime, bool isLerpAndExtrapolate)
        {
            TryConsumeFromBuffer(tickLatencyAsTime, minDeltaTime, maxDeltaTime, isLerpAndExtrapolate);
            // Only begin interpolation when there is a start and end point
            if (InterpolateState.Target.HasValue)
            {
                // As long as the target hasn't been reached, interpolate or smooth dampen.
                if (!InterpolateState.TargetReached)
                {
                    // Increases the time delta relative to the time to target.
                    // Also calculates the LerpT and LerpTPredicted values.
                    InterpolateState.AddDeltaTime(deltaTime);
                    var targetValue = InterpolateState.CurrentValue;
                    // SmoothDampen or LerpExtrapolateBlend
                    if (!isLerpAndExtrapolate)
                    {
                        InterpolateState.PreviousValue = SmoothDamp(InterpolateState.PreviousValue, InterpolateState.Target.Value.Item, ref m_RateOfChange, (float)InterpolateState.TimeToTargetValue, (float)InterpolateState.DeltaTime);
                        InterpolateState.PredictValue = SmoothDamp(InterpolateState.PredictValue, InterpolateState.Target.Value.Item, ref m_PredictedRateOfChange, (float)InterpolateState.TimeToTargetValue, (float)(InterpolateState.DeltaTime + deltaTime));
                    }
                    else
                    {
                        InterpolateState.PreviousValue = Interpolate(InterpolateState.Phase1Value, InterpolateState.Target.Value.Item, InterpolateState.LerpT);
                        // Note: InterpolateState.LerpTPredict is clamped to LerpT if we have no next target
                        InterpolateState.PredictValue = InterpolateUnclamped(InterpolateState.Phase2Value, InterpolateState.Target.Value.Item, InterpolateState.LerpTPredict);
                    }

                    // Lerp between the PreviousValue and PredictedValue using the calculated time delta
                    targetValue = Interpolate(InterpolateState.PreviousValue, InterpolateState.PredictValue, deltaTime);

                    // If lerp smoothing is enabled, then smooth current value towards the target value
                    if (LerpSmoothEnabled)
                    {
                        // Apply the smooth lerp from the oneThirpoint to the target to help smooth the final value
                        InterpolateState.CurrentValue = Interpolate(InterpolateState.CurrentValue, targetValue, deltaTime / MaximumInterpolationTime);
                    }
                    else
                    {
                        // Otherwise, just assign the target value.
                        InterpolateState.CurrentValue = targetValue;
                    }
                }
                else // If the target is reached and we have no more state updates, we want to check to see if we need to reset.
                if (m_BufferQueue.Count == 0)
                {
                    // When the delta between the time sent and the current tick latency time-window is greater than the max delta time
                    // plus the minimum delta time (a rough estimate of time to wait before we consider rate of change equal to zero),
                    // we will want to reset the interpolator with the current known value. This prevents the next received state update's
                    // time to be calculated against the last calculated time which if there is an extended period of time between the two
                    // it would cause a large delta time period between the two states (i.e. it stops moving for a second or two and then
                    // starts moving again).
                    if ((tickLatencyAsTime - InterpolateState.Target.Value.TimeSent) > InterpolateState.MaxDeltaTime + minDeltaTime)
                    {
                        InterpolateState.Reset(InterpolateState.CurrentValue);
                    }
                }
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
                while (m_BufferQueue.TryPeek(out BufferedItem potentialItem))
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
                        if (m_BufferQueue.TryDequeue(out BufferedItem target))
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

                if (LerpSmoothEnabled)
                {
                    // Assure our MaximumInterpolationTime is valid and that the second lerp time ranges between deltaTime and 1.0f.
                    InterpolateState.CurrentValue = Interpolate(InterpolateState.CurrentValue, target, deltaTime / MaximumInterpolationTime);
                }
                else
                {
                    InterpolateState.CurrentValue = target;
                }
            }
            m_NbItemsReceivedThisFrame = 0;
            return InterpolateState.CurrentValue;
        }
        #endregion

        /// <summary>
        /// Convenience version of 'Update' mainly for testing
        ///  the reason we don't want to always call this version is so that on the calling side we can compute
        ///  the renderTime once for the many things being interpolated (and the many interpolators per object)
        /// </summary>
        /// <param name="deltaTime">time since call</param>
        /// <param name="serverTime">current server time</param>
        /// <returns>The newly interpolated value of type 'T'</returns>
        [Obsolete("This method is being deprecated due to it being only used for internal testing purposes.", false)]
        public T Update(float deltaTime, NetworkTime serverTime)
        {
            return UpdateInternal(deltaTime, serverTime);
        }

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
                    // Clear the interpolator
                    Clear();
                    // Reset to the new value but don't automatically add the measurement (prevents recursion)
                    InternalReset(newMeasurement, sentTime, m_IsAngularValue, false);
                    m_LastMeasurementAddedTime = sentTime;
                    m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime, m_BufferCount);
                    // Next line keeps renderTime above m_StartTimeConsumed. Fixes pause/unpause issues
                    m_BufferQueue.Enqueue(m_LastBufferedItemReceived);
                }
                return;
            }

            // Drop measurements that are received out of order/late
            if (sentTime > m_LastMeasurementAddedTime || m_BufferCount == 0)
            {
                m_BufferCount++;
                m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime, m_BufferCount);
                m_BufferQueue.Enqueue(m_LastBufferedItemReceived);
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
        /// <remarks>
        /// Not public API ready yet.
        /// </remarks>
        /// <param name="current">Current item <see cref="T"/> value.</param>
        /// <param name="target">Target item <see cref="T"/> value.</param>
        /// <param name="rateOfChange">The velocity of change.</param>
        /// <param name="duration">Total time to smooth between the <paramref name="current"/> and <paramref name="target"/>.</param>
        /// <param name="deltaTime">The increasing delta time from when start to finish.</param>
        /// <param name="maxSpeed">Maximum rate of change per pass.</param>
        /// <returns>The smoothed <see cref="T"/> value.</returns>
        private protected virtual T SmoothDamp(T current, T target, ref T rateOfChange, float duration, float deltaTime, float maxSpeed = Mathf.Infinity)
        {
            return target;
        }

        /// <summary>
        /// Determines if two values of type <see cref="T"/> are close to the same value.
        /// </summary>
        /// <remarks>
        /// Not public API ready yet.
        /// </remarks>
        /// <param name="first">First value of type <see cref="T"/>.</param>
        /// <param name="second">Second value of type <see cref="T"/>.</param>
        /// <param name="precision">The precision of the aproximation.</param>
        /// <returns>true if the two values are aproximately the same and false if they are not</returns>
        private protected virtual bool IsAproximately(T first, T second, float precision = k_AproximatePrecision)
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

        /// <summary>
        /// Invoked by <see cref="Components.NetworkTransform"/> when the transform has transitioned between local to world or vice versa.
        /// </summary>
        /// <param name="transform">The transform that the <see cref="Components.NetworkTransform"/> is associated with.</param>
        /// <param name="inLocalSpace">Whether the <see cref="Components.NetworkTransform"/> is now being tracked in local or world spaced.</param>
        internal void ConvertTransformSpace(Transform transform, bool inLocalSpace)
        {
            var count = m_BufferQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = m_BufferQueue.Dequeue();
                entry.Item = OnConvertTransformSpace(transform, entry.Item, inLocalSpace);
                m_BufferQueue.Enqueue(entry);
            }
            InterpolateState.CurrentValue = OnConvertTransformSpace(transform, InterpolateState.CurrentValue, inLocalSpace);
            if (InterpolateState.Target.HasValue)
            {
                var end = InterpolateState.Target.Value;
                end.Item = OnConvertTransformSpace(transform, end.Item, inLocalSpace);
                InterpolateState.Target = end;
            }
            InLocalSpace = inLocalSpace;
        }
    }
}
