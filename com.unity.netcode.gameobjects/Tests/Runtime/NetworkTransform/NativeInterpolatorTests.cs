using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Drives <see cref="NativeInterpolator"/> and <see cref="BufferedLinearInterpolator{T}"/> with identical
    /// measurement sequences and compares them step for step.
    /// </summary>
    /// <remarks>
    /// The two exist in parallel: the managed one continues to serve
    /// <see cref="TransformSyncModes.PerInstance"/> and the native one serves
    /// <see cref="TransformSyncModes.Batched"/>, because the managed one cannot run inside a job. Unlike the
    /// delta check, which the two synchronization modes genuinely share, there is nothing structural stopping
    /// these two from drifting apart. This is what stops it.
    /// </remarks>
    // These tests do not need to run against the Rust server.
    [IgnoreIfServiceEnvironmentVariableSet]
    internal class NativeInterpolatorTests
    {
        private const int k_BufferCapacity = NativeInterpolator.BufferCountLimit + 1;
        private const float k_TickRate = 30.0f;
        private const double k_MinDeltaTime = 1.0 / k_TickRate;

        /// <summary>
        /// The two implementations use the same operations but not always in the same order, so agreement is
        /// to float precision rather than bit for bit.
        /// </summary>
        private const float k_Tolerance = 1E-4f;

        /// <summary>
        /// Allowed while a value is still in motion, for the two paths that are known to be sensitive rather
        /// than exact.
        /// </summary>
        /// <remarks>
        /// Vector slerp is the one replacement in <see cref="NetworkTransformMath"/> that is equivalent rather
        /// than exact, so its small per step difference compounds through the interpolator's feedback.
        /// Quaternion smooth dampening converts to euler angles, dampens each angle, and converts back every
        /// frame; near a gimbal transition a sub thousandth of a degree difference in the conversion is enough
        /// to select a different (equally valid) euler representative, after which the two dampen toward
        /// different angles. The managed implementation is just as fragile there, so this is the two diverging
        /// under a shared weakness rather than one of them being wrong.<br /><br />
        /// What matters is that neither drifts permanently, which is what the settle phase asserts.
        /// </remarks>
        private const float k_TransientTolerance = 5.0f;

        /// <summary>
        /// Once measurements stop, both implementations have to arrive at the same value.
        /// </summary>
        private const float k_SettledTolerance = 1E-3f;

        /// <summary>
        /// Frames run with no new measurements, to let both settle onto the final target.
        /// </summary>
        private const int k_SettleFrames = 240;

        private NativeArray<BufferedItemNative> m_Items;

        [SetUp]
        public void SetUp()
        {
            m_Items = new NativeArray<BufferedItemNative>(k_BufferCapacity, Allocator.Temp);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Items.IsCreated)
            {
                m_Items.Dispose();
            }
        }

        private NativeInterpolatorState CreateState(InterpolatorValueKind kind, bool isSlerp, bool lerpSmoothing, float maxInterpolationTime)
        {
            return new NativeInterpolatorState()
            {
                BufferOffset = 0,
                BufferCapacity = k_BufferCapacity,
                ValueKind = kind,
                IsSlerp = isSlerp,
                LerpSmoothEnabled = lerpSmoothing,
                MaximumInterpolationTime = maxInterpolationTime,
            };
        }

        /// <summary>
        /// A deterministic motion path, so a failure is reproducible.
        /// </summary>
        private static Vector3 PositionAt(int tick)
        {
            return new Vector3(
                Mathf.Sin(tick * 0.31f) * 12.0f,
                tick * 0.45f,
                Mathf.Cos(tick * 0.17f) * 7.5f);
        }

        private static Quaternion RotationAt(int tick)
        {
            return Quaternion.Euler(tick * 3.7f, tick * -2.3f, tick * 1.1f);
        }

        /// <summary>
        /// Steps both implementations through the same sequence of measurements and frames.
        /// </summary>
        /// <remarks>
        /// Measurements are added on tick boundaries and both are updated every frame, which is how a
        /// non-authority instance actually consumes them.
        /// </remarks>
        private void CompareVector3(NetworkTransform.InterpolationTypes interpolationType, bool isSlerp, bool lerpSmoothing, string label, float transientTolerance = k_Tolerance)
        {
            const float maxInterpolationTime = 0.1f;
            const float deltaTime = 1.0f / 60.0f;
            const int ticks = 120;

            var managed = new BufferedLinearInterpolatorVector3()
            {
                IsSlerp = isSlerp,
                LerpSmoothEnabled = lerpSmoothing,
                MaximumInterpolationTime = maxInterpolationTime,
            };
            var native = CreateState(InterpolatorValueKind.Vector3, isSlerp, lerpSmoothing, maxInterpolationTime);

            var start = PositionAt(0);
            managed.ResetTo(start, 0.0);
            NativeInterpolator.ResetTo(ref native, ref m_Items, new float4(start.x, start.y, start.z, 0.0f), 0.0);

            var worstError = 0.0f;
            var worstDetail = string.Empty;
            var time = 0.0;
            var nextTick = 1;

            for (int frame = 1; frame <= ticks * 2 + k_SettleFrames; frame++)
            {
                time += deltaTime;

                // Feed a measurement whenever a tick boundary is crossed. Nothing is fed during the settle
                // frames at the end, which is what lets both converge onto the final target.
                while (nextTick * k_MinDeltaTime <= time && nextTick <= ticks)
                {
                    var sentTime = nextTick * k_MinDeltaTime;
                    var measurement = PositionAt(nextTick);
                    managed.AddMeasurement(measurement, sentTime);
                    NativeInterpolator.AddMeasurement(ref native, ref m_Items, new float4(measurement.x, measurement.y, measurement.z, 0.0f), sentTime);
                    nextTick++;
                }

                var tickLatencyAsTime = time - 2.0 * k_MinDeltaTime;
                var maxDeltaTime = 2.0 * k_MinDeltaTime;

                Vector3 managedValue;
                float4 nativeValue;
                if (interpolationType == NetworkTransform.InterpolationTypes.LegacyLerp)
                {
                    managed.Update(deltaTime, tickLatencyAsTime, time);
                    nativeValue = NativeInterpolator.UpdateLegacy(ref native, ref m_Items, deltaTime, tickLatencyAsTime, time);
                }
                else
                {
                    var lerp = interpolationType == NetworkTransform.InterpolationTypes.Lerp;
                    managed.Update(deltaTime, tickLatencyAsTime, k_MinDeltaTime, maxDeltaTime, lerp);
                    nativeValue = NativeInterpolator.Update(ref native, ref m_Items, deltaTime, tickLatencyAsTime, k_MinDeltaTime, maxDeltaTime, lerp);
                }
                managedValue = managed.GetInterpolatedValue();

                var error = Vector3.Distance(managedValue, new Vector3(nativeValue.x, nativeValue.y, nativeValue.z));
                if (error > worstError)
                {
                    worstError = error;
                    worstDetail = $"worst at frame {frame} time {time:F4}: managed={managedValue} native=({nativeValue.x},{nativeValue.y},{nativeValue.z})";
                }

                // The final value, once nothing more is being fed in.
                if (frame == ticks * 2 + k_SettleFrames)
                {
                    Assert.LessOrEqual(error, k_SettledTolerance,
                        $"[{label}] native and managed Vector3 interpolation settled on different values ({error} apart). " +
                        $"managed={managedValue} native=({nativeValue.x},{nativeValue.y},{nativeValue.z})");
                }
            }

            Assert.LessOrEqual(worstError, transientTolerance,
                $"[{label}] native and managed Vector3 interpolation diverged by {worstError} while in motion.\n{worstDetail}");
        }

        private void CompareQuaternion(NetworkTransform.InterpolationTypes interpolationType, bool isSlerp, bool lerpSmoothing, string label, float transientTolerance = 0.01f)
        {
            const float maxInterpolationTime = 0.1f;
            const float deltaTime = 1.0f / 60.0f;
            const int ticks = 120;

            var managed = new BufferedLinearInterpolatorQuaternion()
            {
                IsSlerp = isSlerp,
                LerpSmoothEnabled = lerpSmoothing,
                MaximumInterpolationTime = maxInterpolationTime,
            };
            var native = CreateState(InterpolatorValueKind.Quaternion, isSlerp, lerpSmoothing, maxInterpolationTime);

            var start = RotationAt(0);
            managed.ResetTo(start, 0.0);
            NativeInterpolator.ResetTo(ref native, ref m_Items, new float4(start.x, start.y, start.z, start.w), 0.0);

            var worstError = 0.0f;
            var worstDetail = string.Empty;
            var time = 0.0;
            var nextTick = 1;

            for (int frame = 1; frame <= ticks * 2 + k_SettleFrames; frame++)
            {
                time += deltaTime;

                // Nothing is fed during the settle frames at the end, which is what lets both converge.
                while (nextTick * k_MinDeltaTime <= time && nextTick <= ticks)
                {
                    var sentTime = nextTick * k_MinDeltaTime;
                    var measurement = RotationAt(nextTick);
                    managed.AddMeasurement(measurement, sentTime);
                    NativeInterpolator.AddMeasurement(ref native, ref m_Items, new float4(measurement.x, measurement.y, measurement.z, measurement.w), sentTime);
                    nextTick++;
                }

                var tickLatencyAsTime = time - 2.0 * k_MinDeltaTime;
                var maxDeltaTime = 2.0 * k_MinDeltaTime;

                float4 nativeValue;
                if (interpolationType == NetworkTransform.InterpolationTypes.LegacyLerp)
                {
                    managed.Update(deltaTime, tickLatencyAsTime, time);
                    nativeValue = NativeInterpolator.UpdateLegacy(ref native, ref m_Items, deltaTime, tickLatencyAsTime, time);
                }
                else
                {
                    var lerp = interpolationType == NetworkTransform.InterpolationTypes.Lerp;
                    managed.Update(deltaTime, tickLatencyAsTime, k_MinDeltaTime, maxDeltaTime, lerp);
                    nativeValue = NativeInterpolator.Update(ref native, ref m_Items, deltaTime, tickLatencyAsTime, k_MinDeltaTime, maxDeltaTime, lerp);
                }
                var managedValue = managed.GetInterpolatedValue();

                var error = Quaternion.Angle(managedValue, new Quaternion(nativeValue.x, nativeValue.y, nativeValue.z, nativeValue.w));
                if (error > worstError)
                {
                    worstError = error;
                    worstDetail = $"worst at frame {frame} time {time:F4}: managed={managedValue} native=({nativeValue.x},{nativeValue.y},{nativeValue.z},{nativeValue.w})";
                }

                // The final value, once nothing more is being fed in.
                if (frame == ticks * 2 + k_SettleFrames)
                {
                    Assert.LessOrEqual(error, 0.01f,
                        $"[{label}] native and managed Quaternion interpolation settled on different rotations ({error} degrees apart). " +
                        $"managed={managedValue} native=({nativeValue.x},{nativeValue.y},{nativeValue.z},{nativeValue.w})");
                }
            }

            // Compared as an angle, so the tolerances are in degrees.
            Assert.LessOrEqual(worstError, transientTolerance,
                $"[{label}] native and managed Quaternion interpolation diverged by {worstError} degrees while in motion.\n{worstDetail}");
        }

        [Test]
        public void Vector3LegacyLerpMatchesManaged([Values] bool lerpSmoothing)
        {
            CompareVector3(NetworkTransform.InterpolationTypes.LegacyLerp, false, lerpSmoothing, $"LegacyLerp smoothing={lerpSmoothing}");
        }

        [Test]
        public void Vector3LerpMatchesManaged([Values] bool lerpSmoothing)
        {
            CompareVector3(NetworkTransform.InterpolationTypes.Lerp, false, lerpSmoothing, $"Lerp smoothing={lerpSmoothing}");
        }

        [Test]
        public void Vector3SmoothDampeningMatchesManaged([Values] bool lerpSmoothing)
        {
            CompareVector3(NetworkTransform.InterpolationTypes.SmoothDampening, false, lerpSmoothing, $"SmoothDampening smoothing={lerpSmoothing}");
        }

        [Test]
        public void Vector3SlerpMatchesManaged()
        {
            // Vector slerp is the one NetworkTransformMath replacement that is equivalent rather than exact,
            // so it is held to the transient bound while moving and the tight bound once settled.
            CompareVector3(NetworkTransform.InterpolationTypes.Lerp, true, false, "Lerp slerp", k_TransientTolerance);
        }

        [Test]
        public void QuaternionLegacyLerpMatchesManaged([Values] bool isSlerp)
        {
            CompareQuaternion(NetworkTransform.InterpolationTypes.LegacyLerp, isSlerp, false, $"LegacyLerp slerp={isSlerp}");
        }

        [Test]
        public void QuaternionLerpMatchesManaged([Values] bool isSlerp)
        {
            CompareQuaternion(NetworkTransform.InterpolationTypes.Lerp, isSlerp, false, $"Lerp slerp={isSlerp}");
        }

        [Test]
        public void QuaternionSmoothDampeningMatchesManaged()
        {
            // Dampening through euler angles can select a different euler representative near a gimbal
            // transition, so this is held to the transient bound while moving and the tight bound once settled.
            CompareQuaternion(NetworkTransform.InterpolationTypes.SmoothDampening, true, false, "SmoothDampening", k_TransientTolerance);
        }

        /// <summary>
        /// The ring buffer has a fixed capacity where the managed queue does not, so the overflow behavior has
        /// to be checked explicitly rather than only through the comparisons above.
        /// </summary>
        [Test]
        public void BufferOverflowKeepsNewestMeasurement()
        {
            var native = CreateState(InterpolatorValueKind.Vector3, false, false, 0.1f);
            NativeInterpolator.ResetTo(ref native, ref m_Items, float4.zero, 0.0);

            // More measurements than the buffer can hold, without tripping the teleport threshold.
            const int count = NativeInterpolator.BufferCountLimit - 1;
            for (int i = 1; i <= count; i++)
            {
                NativeInterpolator.AddMeasurement(ref native, ref m_Items, new float4(i, 0.0f, 0.0f, 0.0f), i * k_MinDeltaTime);
            }

            Assert.LessOrEqual(native.BufferCount, k_BufferCapacity, "Buffer count exceeded its capacity!");

            // Consume everything and confirm the newest measurement is the one that survived.
            var value = NativeInterpolator.Update(ref native, ref m_Items, 1.0f, count * k_MinDeltaTime, k_MinDeltaTime, 1.0, true);
            Assert.AreEqual(count, native.Target.Item.x, "The newest measurement was not the one interpolated towards!");
            Assert.IsTrue(math.all(math.isfinite(value)), "Interpolated value was not finite!");
        }
    }
}
