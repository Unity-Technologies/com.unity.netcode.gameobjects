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
            public int ItemId;
            public T Item;
            public double TimeSent;

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
            public double RelativeTime;
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
                }
                DeltaTime = Math.Max(DeltaTime + m_AverageDeltaTime, TimeToTargetValue);
                LerpT = DeltaTime / TimeToTargetValue;
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
                LerpT = 1.0f;
                RelativeTime = 0.0;
                DeltaTime = 0.0f;
                m_AverageDeltaTime = 0.0f;
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

        /// <summary>
        /// There's two factors affecting interpolation: buffering (set in NetworkManager's NetworkTimeSystem) and interpolation time, which is the amount of time it'll take to reach the target. This is to affect the second one.
        /// </summary>
        public float MaximumInterpolationTime = 0.1f;

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
#if UNITY_EDITOR
            m_Name = GetType().Name;
#endif
            // Clear everything first
            Clear();
            // Set our initial value
            InterpolateState.Reset(targetValue);
            m_IsAngularValue = isAngularValue;

            // Add the first measurement for our baseline
            AddMeasurement(targetValue, serverTime);
        }

        /// <summary>
        /// TryConsumeFromBuffer: Smooth Dampening Version
        /// </summary>
        /// <param name="renderTime">render time: the time in "ticks ago" relative to the current tick latency</param>
        /// <param name="minDeltaTime">minimum time delta (defaults to tick frequency)</param>
        /// <param name="maxDeltaTime">maximum time delta which defines the maximum time duration when consuming more than one item from the buffer</param>
        private void TryConsumeFromBuffer(double renderTime, float minDeltaTime, float maxDeltaTime, bool isSmoothed = false)
        {
            var canGetNextItem = true;

            if (isSmoothed && InterpolateState.Target.HasValue)
            {
                canGetNextItem = InterpolateState.TargetTimeAproximatelyReached() || IsAproximately(InterpolateState.CurrentValue, InterpolateState.Target.Value.Item);
            }

            if (!InterpolateState.Target.HasValue || (InterpolateState.Target.Value.TimeSent <= renderTime && canGetNextItem))
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
                        (potentialItem.TimeSent <= renderTime && InterpolateState.Target.Value.TimeSent < potentialItem.TimeSent))
                    {
                        if (m_Buffer.TryDequeue(out BufferedItem target))
                        {
                            if (!InterpolateState.Target.HasValue)
                            {
                                InterpolateState.Target = target;

                                alreadyHasBufferItem = true;
                                InterpolateState.PreviousValue = InterpolateState.CurrentValue;
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
                                if (isSmoothed)
                                {
                                    InterpolateState.TimeToTargetValue = Mathf.Clamp((float)(target.TimeSent - startTime), minDeltaTime, maxDeltaTime);
                                }
                                else
                                {
                                    InterpolateState.TimeToTargetValue = (float)(target.TimeSent - startTime);
                                }
                                InterpolateState.Target = target;
                            }
                            InterpolateState.DeltaTime = 0.0f;
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
        /// Used for internal testing
        /// </summary>
        internal T UpdateInternal(float deltaTime, NetworkTime serverTime)
        {
            return Update(deltaTime, serverTime.TimeTicksAgo(1).Time, serverTime.Time);
        }

        /// <summary>
        /// ** Recommended to use when <see cref="Components.NetworkRigidbodyBase.UseRigidBodyForMotion"/> is enabled. ** <br />
        /// Provides a closer/more precise update between the current and target values that uses a smooth dampening approach.
        /// </summary>
        /// <param name="deltaTime">The last frame time that is either <see cref="Time.deltaTime"/> for non-rigidbody motion and <see cref="Time.fixedDeltaTime"/> when using ridigbody motion.</param>
        /// <param name="tickLatencyAsTime">The tick latency in relative local time.</param>
        /// <param name="minDeltaTime">The minimum time delta between the current and target value.</param>
        /// <param name="maxDeltaTime">The maximum time delta between the current and target value.</param>
        /// <returns>The newly interpolated value of type 'T'</returns>
        public T Update(float deltaTime, double tickLatencyAsTime, float minDeltaTime, float maxDeltaTime)
        {
            TryConsumeFromBuffer(tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);
            // Only interpolate when there is a start and end point and we have not already reached the end value
            if (InterpolateState.Target.HasValue)
            {
                InterpolateState.AddDeltaTime(deltaTime);
                InterpolateState.CurrentValue = SmoothDamp(InterpolateState.CurrentValue, InterpolateState.Target.Value.Item, ref m_RateOfChange, InterpolateState.TimeToTargetValue, deltaTime);
            }
            m_NbItemsReceivedThisFrame = 0;
            return InterpolateState.CurrentValue;
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
            TryConsumeFromBuffer(renderTime, deltaTime, (float)(serverTime - renderTime));
            // Only interpolate when there is a start and end point and we have not already reached the end value
            if (InterpolateState.Target.HasValue)
            {
                InterpolateState.AddDeltaTime(deltaTime);
                var target = Interpolate(InterpolateState.PreviousValue, InterpolateState.Target.Value.Item, InterpolateState.LerpT);
                InterpolateState.CurrentValue = Interpolate(InterpolateState.CurrentValue, target, deltaTime / MaximumInterpolationTime);
            }
            m_NbItemsReceivedThisFrame = 0;
            return InterpolateState.CurrentValue;
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
                    m_BufferCount++;
                    m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime, m_BufferCount);
                    ResetTo(newMeasurement, sentTime);
                    // Next line keeps renderTime above m_StartTimeConsumed. Fixes pause/unpause issues
                    m_Buffer.Enqueue(m_LastBufferedItemReceived);
                }
                return;
            }

            // Drop measurements that are received out of order/late
            if (sentTime > m_LastMeasurementAddedTime)
            {
                m_BufferCount++;
                m_LastBufferedItemReceived = new BufferedItem(newMeasurement, sentTime, m_BufferCount);
                m_Buffer.Enqueue(m_LastBufferedItemReceived);
                m_LastMeasurementAddedTime = sentTime;
            }
#if UNITY_EDITOR
            else if (EnableLogging)
            {
                Debug.Log($"[{m_Name}] Dropping measurement -- Time: {sentTime} Value: {newMeasurement} | Last measurement -- Time: {m_LastMeasurementAddedTime} Value: {m_LastBufferedItemReceived.Item}");
            }
#endif
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

        // TODO: Collect data points so a single buffered linear interpolator can provide additional data points
        // to be visualized in RNSM.
        #region DEBUG_LOGGING
#if UNITY_EDITOR

        private string m_Name;
        internal bool EnableLogging = false;
        internal ulong NetworkObjectId;
        internal ushort NetworkBehaviourId;

        private double m_LastDebugUpdate = 0.0;

        private double m_AvgTimeDelta = 0.0;

        private float m_LowestLerpT = float.MaxValue;
        private float m_LerpTAverage = 0.0f;
        private int m_MaxBufferedItems = 0;
        private float m_AverageBufferCount = 0.0f;

        private void LogInfo(double serverTime)
        {
            if (!EnableLogging)
            {
                return;
            }
            if (m_LowestLerpT > InterpolateState.LerpT)
            {
                m_LowestLerpT = InterpolateState.LerpT;
            }

            if (m_LerpTAverage == 0.0f)
            {
                m_LerpTAverage = InterpolateState.LerpT;
            }
            else
            {
                m_LerpTAverage = (m_LerpTAverage + InterpolateState.LerpT) * 0.5f;
            }

            if (m_MaxBufferedItems < m_Buffer.Count)
            {
                m_MaxBufferedItems = m_Buffer.Count;
            }

            if (m_AverageBufferCount == 0.0f)
            {
                m_AverageBufferCount = (m_AverageBufferCount + m_Buffer.Count) * 0.5f;
            }

            if (m_AvgTimeDelta == 0.0)
            {
                m_AvgTimeDelta = InterpolateState.DeltaTime;
            }
            else
            {
                m_AvgTimeDelta += InterpolateState.DeltaTime;
                m_AvgTimeDelta *= 0.5;
            }

            if (m_LastDebugUpdate < serverTime)
            {
                //Debug.Log($"[{m_Name}][{NetworkObjectId}-{NetworkBehaviourId}][{InterpolateState.Target.Value.ItemId}] Min LerpT: {m_LowestLerpT} | Avg LerpT: {m_LerpTAverage} | Max Count: {m_MaxBufferedItems} | Avg Count: {m_AverageBufferCount} | Avg TD: {m_AvgTimeDelta}");
                Debug.Log($"[{m_Name}][{NetworkObjectId}-{NetworkBehaviourId}] Min LerpT: {m_LowestLerpT} | Avg LerpT: {m_LerpTAverage} | Max Count: {m_MaxBufferedItems} | Avg Count: {m_AverageBufferCount} | Avg TD: {m_AvgTimeDelta}");
                m_LastDebugUpdate = serverTime + 1.0;
                m_AverageBufferCount = 0.0f;
                m_MaxBufferedItems = 0;
                m_LerpTAverage = 0.0f;
                m_LowestLerpT = float.MaxValue;
            }
        }
        //private bool m_LogSegment;

        private float m_AvgDepth;

        private float m_AvgRTT;

        private float m_MaxLerpT = 0.0f;

        private void LogSecondInfo(double serverTime, float lerpT, int depth, float serverHalfRtt)
        {
            if (!EnableLogging)
            {
                return;
            }
            if (m_LowestLerpT > lerpT)
            {
                m_LowestLerpT = lerpT;
            }

            if (m_MaxLerpT < lerpT)
            {
                m_MaxLerpT = lerpT;
            }

            if (m_LerpTAverage == 0.0f)
            {
                m_LerpTAverage = lerpT;
            }
            else
            {
                m_LerpTAverage = (m_LerpTAverage + lerpT) * 0.5f;
            }

            if (m_AvgRTT == 0.0f)
            {
                m_AvgRTT = serverHalfRtt;
            }
            else
            {
                m_AvgRTT = (m_AvgRTT + serverHalfRtt) * 0.5f;
            }

            if (m_AvgDepth == 0.0f)
            {
                m_AvgDepth = depth;
            }
            else
            {
                m_AvgDepth = (m_AvgDepth + depth) * 0.5f;
            }



            if (m_MaxBufferedItems < m_Buffer.Count)
            {
                m_MaxBufferedItems = m_Buffer.Count;
            }

            if (m_AverageBufferCount == 0.0f)
            {
                m_AverageBufferCount = (m_AverageBufferCount + m_Buffer.Count) * 0.5f;
            }

            if (m_LastDebugUpdate < serverTime)
            {
                //Debug.Log($"[{m_Name}][{NetworkObjectId}-{NetworkBehaviourId}][{InterpolateState.Target.Value.ItemId}] Min LerpT: {m_LowestLerpT} | Avg LerpT: {m_LerpTAverage} | Max Count: {m_MaxBufferedItems} | Avg Count: {m_AverageBufferCount} | Avg TD: {m_AvgTimeDelta}");
                Debug.Log($"[{m_Name}][{NetworkObjectId}-{NetworkBehaviourId}] Min/Max LT: {m_LowestLerpT}/{m_MaxLerpT} | Avg LT: {m_LerpTAverage} | Max Count: {m_MaxBufferedItems} | Avg Count: {m_AverageBufferCount} | Avg Depth: {m_AvgDepth} Avg HRtt: {m_AvgRTT}");
                m_LastDebugUpdate = serverTime + 1.0;
                m_AverageBufferCount = 0.0f;
                m_MaxBufferedItems = 0;
                m_LerpTAverage = 0.0f;
                m_LowestLerpT = float.MaxValue;
                m_AvgDepth = 0.0f;
                m_AvgRTT = 0.0f;
                m_MaxLerpT = 0.0f;
                //m_LogSegment = true;
            }
        }
#endif
        #endregion
    }
}
