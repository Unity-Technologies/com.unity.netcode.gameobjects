using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// The value type being interpolated for a given <see cref="NativeInterpolatorState"/>.
    /// </summary>
    internal enum InterpolatorValueKind
    {
        /// <summary>
        /// Used to define the position or scale states.
        /// </summary>
        Vector3,

        /// <summary>
        /// Always used for rotation.
        /// </summary>
        Quaternion,
    }

    /// <summary>
    /// The blittable (managed and native compatible) equivalent of <see cref="BufferedLinearInterpolator{T}.BufferedItem"/>.
    /// </summary>
    /// <remarks>
    /// A single <see cref="float4"/> covers every transform value type being synchronized.<br />
    /// For position and scale, the w (4th) element is not used.
    /// </remarks>
    internal struct BufferedItemNative
    {
        internal float4 Item;
        internal double TimeSent;
        internal int ItemId;
    }

    /// <summary>
    /// The blittable (managed and native compatible) equivalent of a <see cref="BufferedLinearInterpolator{T}"/> and its
    /// <see cref="BufferedLinearInterpolator{T}.CurrentState"/>.
    /// </summary>
    /// <remarks>
    /// The managed interpolator holds its measurements in a <see cref="System.Collections.Generic.Queue{T}"/>
    /// and tracks the parent <see cref="UnityEngine.Transform"/> each measurement was taken under, neither of
    /// which can exist inside a job. Here the measurements live in a fixed size ring buffer carved out of one
    /// shared native array, addressed by <see cref="BufferOffset"/>.<br />
    /// <br />
    /// The smooth parenting transition flag, <see cref="NetworkTransform.SwitchTransformSpaceWhenParented"/>,
    /// is excluded from this state as it is handled differently.<br />
    /// - <see cref="NativeInterpolator.ConvertSpace"/> provides additional details on this. <br />
    /// - <see cref="NetworkTransformStateManager.ConvertInterpolationSpace"/> is where this happens (for now).
    /// </remarks>
    internal struct NativeInterpolatorState
    {
        /// <summary>
        /// Where this interpolator's slice of the shared item array begins.
        /// </summary>
        internal int BufferOffset;
        internal int BufferCapacity;

        /// <summary>
        /// Index of the oldest buffered item, relative to <see cref="BufferOffset"/>.
        /// </summary>
        internal int BufferHead;
        internal int BufferCount;

        internal InterpolatorValueKind ValueKind;

        /// <summary>
        /// Whether to slerp rather than lerp. Position uses this for
        /// <see cref="NetworkTransform.SlerpPosition"/> and rotation uses it when not running at half
        /// precision.
        /// </summary>
        internal bool IsSlerp;

        internal bool LerpSmoothEnabled;
        internal float MaximumInterpolationTime;

        /// <summary>
        /// The <see cref="BufferedLinearInterpolator{T}.CurrentState"/> blittable equivalbent.
        /// </summary>
        internal float4 CurrentValue;
        internal float4 PreviousValue;
        internal float4 NextValue;
        internal float4 RateOfChange;
        internal BufferedItemNative Target;
        internal bool HasTarget;
        internal double StartTime;
        internal double EndTime;
        internal double TimeToTargetValue;
        internal double DeltaTime;
        internal double MaxDeltaTime;
        internal double LastRemainingTime;
        internal float LerpT;
        internal bool TargetReached;
        internal float CurrentDeltaTime;

        // State measurement tracking related properties
        internal double LastMeasurementAddedTime;
        internal int BufferCounter;
        internal int ItemsReceivedThisFrame;
        internal BufferedItemNative LastBufferedItemReceived;
    }

    /// <summary>
    /// A job friendly version of the <see cref="BufferedLinearInterpolator{T}"/>.
    /// </summary>
    /// <remarks>
    /// The managed implementation stays in place as batched <see cref="NetworkTransform"/>s is
    /// a user opt-in feature and the original managed version must continue to work as expected
    /// until it becomes deprecated.
    /// </remarks>
    internal static class NativeInterpolator
    {
        /// <summary>
        /// Matches <see cref="BufferedLinearInterpolator{T}"/>'s buffer count limit, which is the point at
        /// which it gives up on interpolating and teleports to the newest value.
        /// </summary>
        internal const int BufferCountLimit = 100;

        private const float k_ApproximateLowPrecision = 0.000001f;
        private const float k_ApproximateHighPrecision = 1E-10f;
        private const double k_SmallValue = 9.999999439624929E-11;

        /// <summary>
        /// The frame rate that <see cref="NativeInterpolatorState.MaximumInterpolationTime"/> is relative to when lerp smoothing.
        /// </summary>
        private const float k_LerpSmoothReferenceFrameRate = 60.0f;

        /// <summary>
        /// Keeps a <see cref="NativeInterpolatorState.MaximumInterpolationTime"/> of 1.0f from retaining the entire delta
        /// each frame, which would stop the value from ever advancing towards the target.
        /// </summary>
        private const float k_MaximumLerpSmoothRetention = 0.99f;

        /// <summary>
        /// Calculates the frame rate independent lerp smoothing "t" for the current frame.
        /// </summary>
        /// <remarks>
        /// Raising the retained portion to the number of reference frames elapsed makes the smoothing rate
        /// a function of elapsed time rather than of how often this is called.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetLerpSmoothTime(in NativeInterpolatorState state, float deltaTime)
        {
            var retained = math.saturate(state.MaximumInterpolationTime);
            if (retained >= 1.0f)
            {
                retained = k_MaximumLerpSmoothRetention;
            }
            return 1.0f - math.pow(retained, deltaTime * k_LerpSmoothReferenceFrameRate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetPrecision(in NativeInterpolatorState state)
        {
            return state.BufferCount == 0 ? k_ApproximateHighPrecision : k_ApproximateLowPrecision;
        }

        #region Job friendly ring buffer (i.e. Queue) methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BufferedItemNative Peek(in NativeInterpolatorState state, in NativeArray<BufferedItemNative> items)
        {
            return items[state.BufferOffset + state.BufferHead];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BufferedItemNative Dequeue(ref NativeInterpolatorState state, in NativeArray<BufferedItemNative> items)
        {
            var item = items[state.BufferOffset + state.BufferHead];
            state.BufferHead = (state.BufferHead + 1) % state.BufferCapacity;
            state.BufferCount--;
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Enqueue(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items, in BufferedItemNative item)
        {
            if (state.BufferCount == state.BufferCapacity)
            {
                // Full: drop the oldest so the newest always makes it in, which is the behavior the managed
                // interpolator gets from its unbounded queue combined with the buffer count limit below.
                state.BufferHead = (state.BufferHead + 1) % state.BufferCapacity;
                state.BufferCount--;
            }
            var tail = (state.BufferHead + state.BufferCount) % state.BufferCapacity;
            items[state.BufferOffset + tail] = item;
            state.BufferCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearBuffer(ref NativeInterpolatorState state)
        {
            state.BufferHead = 0;
            state.BufferCount = 0;
        }

        #endregion

        #region Interpolation, Smooth dampening, and approximation methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 Interpolate(in NativeInterpolatorState state, float4 start, float4 end, float time)
        {
            if (state.ValueKind == InterpolatorValueKind.Quaternion)
            {
                return state.IsSlerp
                    ? NetworkTransformMath.Slerp(new quaternion(start), new quaternion(end), time).value
                    : NetworkTransformMath.Nlerp(new quaternion(start), new quaternion(end), time).value;
            }

            var result = state.IsSlerp
                ? NetworkTransformMath.Slerp(start.xyz, end.xyz, time)
                : NetworkTransformMath.Lerp(start.xyz, end.xyz, time);
            return new float4(result, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 SmoothDamp(ref NativeInterpolatorState state, float4 current, float4 target, float duration, float deltaTime)
        {
            if (state.ValueKind == InterpolatorValueKind.Quaternion)
            {
                // Matches BufferedLinearInterpolatorQuaternion, which smooth dampens each euler angle.
                var currentEuler = NetworkTransformMath.EulerAngles(new quaternion(current));
                var targetEuler = NetworkTransformMath.EulerAngles(new quaternion(target));
                var rate = state.RateOfChange;
                var result = float3.zero;
                for (int i = 0; i < 3; i++)
                {
                    var velocity = rate[i];
                    result[i] = NetworkTransformMath.SmoothDampAngle(currentEuler[i], targetEuler[i], ref velocity, duration, float.PositiveInfinity, deltaTime);
                    rate[i] = velocity;
                }
                state.RateOfChange = rate;
                return NetworkTransformMath.Euler(result).value;
            }

            var rateOfChange = state.RateOfChange.xyz;
            var damped = NetworkTransformMath.SmoothDamp(current.xyz, target.xyz, ref rateOfChange, duration, float.PositiveInfinity, deltaTime);
            state.RateOfChange = new float4(rateOfChange, 0.0f);
            return new float4(damped, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsApproximately(in NativeInterpolatorState state, float4 first, float4 second, float precision)
        {
            if (state.ValueKind == InterpolatorValueKind.Quaternion)
            {
                return math.abs(first.x - second.x) <= precision
                    && math.abs(first.y - second.y) <= precision
                    && math.abs(first.z - second.z) <= precision
                    && math.abs(first.w - second.w) <= precision;
            }

            // Matches BufferedLinearInterpolatorVector3, which rounds to two decimal places first.
            return math.round(math.abs(first.x - second.x) * 100.0f) * 0.01f <= precision
                && math.round(math.abs(first.y - second.y) * 100.0f) * 0.01f <= precision
                && math.round(math.abs(first.z - second.z) * 100.0f) * 0.01f <= precision;
        }

        #endregion

        #region State measurement, resetting, clearing, and related state methods

        internal static void Clear(ref NativeInterpolatorState state)
        {
            ClearBuffer(ref state);
            state.BufferCounter = 0;
            state.LastMeasurementAddedTime = 0.0;
            Reset(ref state, float4.zero);
            state.RateOfChange = float4.zero;
        }

        /// <summary>
        /// <see cref="BufferedLinearInterpolator{T}.CurrentState.Reset"/>.
        /// </summary>
        internal static void Reset(ref NativeInterpolatorState state, float4 currentValue)
        {
            state.HasTarget = false;
            state.Target = default;
            state.CurrentValue = currentValue;
            state.NextValue = currentValue;
            state.PreviousValue = currentValue;
            state.TargetReached = false;
            state.LerpT = 0.0f;
            state.EndTime = 0.0;
            state.StartTime = 0.0;
            state.TimeToTargetValue = 0.0;
            state.DeltaTime = 0.0;
            state.CurrentDeltaTime = 0.0f;
            state.MaxDeltaTime = 0.0;
            state.LastRemainingTime = 0.0;
        }

        /// <summary>
        /// <see cref="BufferedLinearInterpolator{T}.ResetTo"/>.
        /// </summary>
        /// <remarks>
        /// Clears the buffer and holds <paramref name="targetValue"/> as the current value.<br />
        /// No baseline measurement is recorded, which is what the managed implementation does as well. See
        /// <see cref="BufferedLinearInterpolator{T}.ResetTo(T, double)"/> for why.<br />
        /// This leaves the interpolator in the state a freshly spawned one is in, so the next measurement to
        /// arrive is taken unconditionally.
        /// </remarks>
        internal static void ResetTo(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items, float4 targetValue)
        {
            Clear(ref state);
            state.RateOfChange = float4.zero;
            Reset(ref state, targetValue);
        }

        /// <summary>
        /// <see cref="BufferedLinearInterpolator{T}.AddMeasurement"/>.
        /// </summary>
        internal static void AddMeasurement(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items, float4 newMeasurement, double sentTime)
        {
            state.ItemsReceivedThisFrame++;

            // This situation can happen after a game is paused. When starting to receive again, the server will
            // have sent a bunch of messages in the meantime; instead of going through thousands of value updates
            // just to get a big teleport, give up on interpolating and teleport to the latest value.
            if (state.ItemsReceivedThisFrame > BufferCountLimit)
            {
                if (state.LastBufferedItemReceived.TimeSent < sentTime)
                {
                    ClearBuffer(ref state);
                    state.BufferCounter = 0;
                    state.LastMeasurementAddedTime = 0.0;
                    state.RateOfChange = float4.zero;
                    Reset(ref state, newMeasurement);

                    state.LastMeasurementAddedTime = sentTime;
                    state.LastBufferedItemReceived = new BufferedItemNative()
                    {
                        Item = newMeasurement,
                        TimeSent = sentTime,
                        ItemId = state.BufferCounter,
                    };
                    // Keeps render time above the consumed start time, which fixes pause and unpause.
                    Enqueue(ref state, ref items, state.LastBufferedItemReceived);
                }
                return;
            }

            // Drop measurements received out of order or late (unreliable deltas can do both).
            if (sentTime > state.LastMeasurementAddedTime || state.BufferCounter == 0)
            {
                state.BufferCounter++;
                state.LastBufferedItemReceived = new BufferedItemNative()
                {
                    Item = newMeasurement,
                    TimeSent = sentTime,
                    ItemId = state.BufferCounter,
                };
                Enqueue(ref state, ref items, state.LastBufferedItemReceived);
                state.LastMeasurementAddedTime = sentTime;
            }
        }

        /// <summary>
        /// <see cref="BufferedLinearInterpolator{T}.ResetCurrentState"/>.
        /// </summary>
        internal static void ResetCurrentState(ref NativeInterpolatorState state)
        {
            if (state.HasTarget)
            {
                Reset(ref state, state.CurrentValue);
                state.RateOfChange = float4.zero;
            }
        }

        /// <summary>
        /// Re-expresses the buffered measurements and the in flight values in a different transform space.
        /// </summary>
        /// <remarks>
        /// Invoked when the instance is reparented. Reparenting is the only thing that changes the
        /// transform space for its measurements.<br />
        /// Converting everything at that point is what keeps the interpolation job free of any parent
        /// knowledge.<br />
        /// The managed interpolator converts lazily as its queue drains instead. Both reach the same result.
        /// </remarks>
        /// <param name="pointTransform">Converts a position from the old transform space to the new one.</param>
        /// <param name="rotationTransform">Converts a rotation from the old transform space to the new one.</param>
        internal static void ConvertSpace(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items, in float4x4 pointTransform, in quaternion rotationTransform)
        {
            for (int i = 0; i < state.BufferCount; i++)
            {
                var index = state.BufferOffset + (state.BufferHead + i) % state.BufferCapacity;
                var item = items[index];
                item.Item = ConvertValue(state.ValueKind, item.Item, pointTransform, rotationTransform);
                items[index] = item;
            }

            state.CurrentValue = ConvertValue(state.ValueKind, state.CurrentValue, pointTransform, rotationTransform);
            state.PreviousValue = ConvertValue(state.ValueKind, state.PreviousValue, pointTransform, rotationTransform);
            state.NextValue = ConvertValue(state.ValueKind, state.NextValue, pointTransform, rotationTransform);

            if (state.HasTarget)
            {
                var target = state.Target;
                target.Item = ConvertValue(state.ValueKind, target.Item, pointTransform, rotationTransform);
                state.Target = target;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 ConvertValue(InterpolatorValueKind valueKind, float4 value, in float4x4 pointTransform, in quaternion rotationTransform)
        {
            if (valueKind == InterpolatorValueKind.Quaternion)
            {
                return math.mul(rotationTransform, new quaternion(value)).value;
            }
            return new float4(math.transform(pointTransform, value.xyz), 0.0f);
        }

        #endregion

        #region Buffer consumption and timing related methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddDeltaTime(ref NativeInterpolatorState state, float deltaTime)
        {
            state.CurrentDeltaTime = deltaTime;
            state.DeltaTime = math.min(state.DeltaTime + deltaTime, state.TimeToTargetValue);
            state.LerpT = (float)(state.TimeToTargetValue == 0.0 ? 1.0 : state.DeltaTime / state.TimeToTargetValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetTimeToTarget(ref NativeInterpolatorState state, double timeToTarget)
        {
            state.LerpT = 0.0f;
            state.DeltaTime = 0.0;
            state.TimeToTargetValue = timeToTarget;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FinalTimeToTarget(in NativeInterpolatorState state)
        {
            return math.max(0.0, state.TimeToTargetValue - state.DeltaTime);
        }

        /// <summary>
        /// The smooth dampening and lerp ahead version of
        /// <see cref="BufferedLinearInterpolator{T}"/>'s buffer consumption.
        /// </summary>
        private static void TryConsumeFromBuffer(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items, double renderTime, double minDeltaTime, double maxDeltaTime)
        {
            var hasPreviousItem = false;
            var previousTimeSent = 0.0;
            var startTime = 0.0;
            var alreadyHasBufferItem = false;
            var noStateSet = !state.HasTarget;

            // With nothing left in the queue (motion stopped) the target still has to be checked for arrival.
            if (!noStateSet && !state.TargetReached)
            {
                state.TargetReached = IsApproximately(state, state.CurrentValue, state.Target.Item, GetPrecision(state));
            }

            while (state.BufferCount > 0)
            {
                var potentialItem = Peek(state, items);

                // Still on the same buffered item, so there is nothing to consume.
                if (hasPreviousItem && previousTimeSent == potentialItem.TimeSent)
                {
                    break;
                }

                var potentialItemNeedsProcessing = false;
                if (!noStateSet)
                {
                    potentialItemNeedsProcessing = potentialItem.TimeSent <= renderTime && potentialItem.TimeSent > state.Target.TimeSent;
                }

                if ((noStateSet && potentialItem.TimeSent <= renderTime) || potentialItemNeedsProcessing)
                {
                    var target = Dequeue(ref state, items);

                    if (!state.HasTarget)
                    {
                        state.Target = target;
                        state.HasTarget = true;
                        alreadyHasBufferItem = true;
                        state.NextValue = state.CurrentValue;
                        state.PreviousValue = state.CurrentValue;
                        SetTimeToTarget(ref state, minDeltaTime);
                        startTime = state.Target.TimeSent;
                        state.TargetReached = false;
                        state.MaxDeltaTime = maxDeltaTime;
                    }
                    else
                    {
                        if (!alreadyHasBufferItem)
                        {
                            alreadyHasBufferItem = true;
                            state.LastRemainingTime = FinalTimeToTarget(state);
                            state.TargetReached = false;
                            state.MaxDeltaTime = maxDeltaTime;
                            state.PreviousValue = state.NextValue;
                            startTime = state.Target.TimeSent;
                        }
                        SetTimeToTarget(ref state, math.max(target.TimeSent - startTime, minDeltaTime));
                        state.Target = target;
                    }
                    // noStateSet is deliberately not cleared here. The managed implementation evaluates it
                    // once before the loop, so when it starts out true every pass keeps taking the branch that
                    // only compares against render time.
                }
                else
                {
                    break;
                }

                hasPreviousItem = true;
                previousTimeSent = potentialItem.TimeSent;
            }
        }

        /// <summary>
        /// The lerping version of <see cref="BufferedLinearInterpolator{T}"/>'s buffer consumption, which
        /// preserves the original consumption pattern used by <see cref="NetworkTransform.InterpolationTypes.LegacyLerp"/>.
        /// </summary>
        private static void TryConsumeFromBufferLegacy(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items, double renderTime, double serverTime)
        {
            if (state.HasTarget && state.Target.TimeSent > renderTime)
            {
                return;
            }

            var hasPreviousItem = false;
            var previousTimeSent = 0.0;
            var alreadyHasBufferItem = false;

            while (state.BufferCount > 0)
            {
                var potentialItem = Peek(state, items);
                if (hasPreviousItem && previousTimeSent == potentialItem.TimeSent)
                {
                    break;
                }

                // Continue processing until reaching the most current state.
                if (potentialItem.TimeSent <= serverTime && (!state.HasTarget || potentialItem.TimeSent > state.Target.TimeSent))
                {
                    var target = Dequeue(ref state, items);
                    if (!state.HasTarget)
                    {
                        state.Target = target;
                        state.HasTarget = true;
                        alreadyHasBufferItem = true;
                        state.NextValue = state.CurrentValue;
                        state.PreviousValue = state.CurrentValue;
                        state.StartTime = target.TimeSent;
                        state.EndTime = target.TimeSent;
                    }
                    else
                    {
                        if (!alreadyHasBufferItem)
                        {
                            alreadyHasBufferItem = true;
                            state.StartTime = state.Target.TimeSent;
                            state.PreviousValue = state.NextValue;
                            state.TargetReached = false;
                        }
                        state.EndTime = target.TimeSent;
                        state.TimeToTargetValue = state.EndTime - state.StartTime;
                        state.Target = target;
                    }
                }
                else
                {
                    break;
                }

                hasPreviousItem = true;
                previousTimeSent = potentialItem.TimeSent;
            }
        }

        #endregion

        #region Update methods

        /// <summary>
        /// The smooth dampening and lerp version of <see cref="BufferedLinearInterpolator{T}.Update(float, double, double, double, bool)"/>.
        /// </summary>
        internal static float4 Update(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items,
            float deltaTime, double tickLatencyAsTime, double minDeltaTime, double maxDeltaTime, bool lerp)
        {
            TryConsumeFromBuffer(ref state, ref items, tickLatencyAsTime, minDeltaTime, maxDeltaTime);

            // Only begin interpolation when there is a start and end point.
            if (state.HasTarget)
            {
                if (!state.TargetReached)
                {
                    AddDeltaTime(ref state, deltaTime);

                    if (!lerp)
                    {
                        state.NextValue = SmoothDamp(ref state, state.NextValue, state.Target.Item,
                            (float)state.TimeToTargetValue * state.LerpT, deltaTime);
                    }
                    else
                    {
                        state.NextValue = Interpolate(state, state.PreviousValue, state.Target.Item, state.LerpT);
                    }

                    if (state.LerpSmoothEnabled)
                    {
                        state.CurrentValue = Interpolate(state, state.CurrentValue, state.NextValue, GetLerpSmoothTime(state, deltaTime));
                    }
                    else
                    {
                        state.CurrentValue = state.NextValue;
                    }
                }
                else if (state.BufferCount == 0)
                {
                    // Once the target is reached and nothing is left, reset if enough time has passed that the
                    // rate of change should be considered zero. Without this the next state update's time is
                    // measured against a stale one, producing a large delta after a pause in motion.
                    if (tickLatencyAsTime - state.Target.TimeSent > state.MaxDeltaTime + minDeltaTime)
                    {
                        Reset(ref state, state.CurrentValue);
                    }
                }
            }
            state.ItemsReceivedThisFrame = 0;
            return state.CurrentValue;
        }

        /// <summary>
        /// The legacy lerp version of <see cref="BufferedLinearInterpolator{T}.Update(float, double, double)"/>.
        /// </summary>
        internal static float4 UpdateLegacy(ref NativeInterpolatorState state, ref NativeArray<BufferedItemNative> items,
            float deltaTime, double renderTime, double serverTime)
        {
            TryConsumeFromBufferLegacy(ref state, ref items, renderTime, serverTime);

            if (!state.TargetReached && state.HasTarget)
            {
                state.LerpT = 1.0f;
                if (state.TimeToTargetValue > k_SmallValue)
                {
                    state.LerpT = math.clamp((float)((renderTime - state.StartTime) / state.TimeToTargetValue), 0.0f, 1.0f);
                }

                state.NextValue = Interpolate(state, state.PreviousValue, state.Target.Item, state.LerpT);

                if (state.LerpSmoothEnabled)
                {
                    state.CurrentValue = Interpolate(state, state.CurrentValue, state.NextValue, deltaTime / state.MaximumInterpolationTime);
                }
                else
                {
                    state.CurrentValue = state.NextValue;
                }

                state.TargetReached = IsApproximately(state, state.CurrentValue, state.Target.Item, GetPrecision(state));
            }
            else if (state.TargetReached && state.BufferCount == 0)
            {
                // If nothing has been received within 300ms, assume motion stopped.
                if (renderTime - state.Target.TimeSent > 0.3)
                {
                    Reset(ref state, state.CurrentValue);
                }
            }
            state.ItemsReceivedThisFrame = 0;
            return state.CurrentValue;
        }

        #endregion
    }
}
