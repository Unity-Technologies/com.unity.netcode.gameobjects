using System;
using System.Text;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Measures how closely <see cref="NetworkTransformMath"/> agrees with the engine math it replaces.
    /// </summary>
    /// <remarks>
    /// The replacements exist because the engine equivalents are native bindings that Burst cannot compile.
    /// Some of them are ports of implementations that are managed C# in the engine and are expected to match
    /// exactly; the rest are mathematically equivalent but cannot be verified as bit identical because the
    /// engine's operation order is not observable.<br /><br />
    /// Each test reports the worst disagreement it found, so a failure states the actual measured error rather
    /// than just that a threshold was crossed.
    /// </remarks>
    // These tests do not need to run against the Rust server.
    [IgnoreIfServiceEnvironmentVariableSet]
    internal class NetworkTransformMathTests
    {
        private const int k_Iterations = 20000;

        /// <summary>
        /// Ports of engine implementations that are managed C#, so they are expected to match exactly.
        /// </summary>
        private const float k_ExactTolerance = 0.0f;

        /// <summary>
        /// Replacements for native implementations. Tight enough that a real divergence fails while ordinary
        /// floating point reassociation does not.
        /// </summary>
        private const float k_EquivalentTolerance = 0.001f;

        /// <summary>
        /// <see cref="NetworkTransformMath.SmoothDamp"/> agrees with the engine to within a single float ulp
        /// over the value ranges used here, but not bit for bit.
        /// </summary>
        /// <remarks>
        /// The remaining difference is one rounding step, not an algorithmic one: every other ported function
        /// (including the scalar <see cref="NetworkTransformMath.SmoothDampAngle"/>, which shares this
        /// arithmetic) matches exactly. Chasing it would mean guessing at how the engine's build contracts
        /// multiply and add, and a difference this size is far below anything interpolation can express.
        /// </remarks>
        private const float k_SingleUlpTolerance = 1E-5f;

        private static System.Random s_Random;

        [SetUp]
        public void SetUp()
        {
            // Fixed seed so a failure is reproducible.
            s_Random = new System.Random(20260814);
        }

        private static float RandomFloat(float min, float max)
        {
            return (float)(s_Random.NextDouble() * (max - min) + min);
        }

        private static Vector3 RandomVector(float range)
        {
            return new Vector3(RandomFloat(-range, range), RandomFloat(-range, range), RandomFloat(-range, range));
        }

        private static Quaternion RandomRotation()
        {
            // Uniformly distributed rotations, which reaches the pole cases the euler conversion special cases.
            var u1 = (float)s_Random.NextDouble();
            var u2 = (float)s_Random.NextDouble();
            var u3 = (float)s_Random.NextDouble();
            var sqrt1MinusU1 = Mathf.Sqrt(1.0f - u1);
            var sqrtU1 = Mathf.Sqrt(u1);
            return new Quaternion(
                sqrt1MinusU1 * Mathf.Sin(2.0f * Mathf.PI * u2),
                sqrt1MinusU1 * Mathf.Cos(2.0f * Mathf.PI * u2),
                sqrtU1 * Mathf.Sin(2.0f * Mathf.PI * u3),
                sqrtU1 * Mathf.Cos(2.0f * Mathf.PI * u3));
        }

        /// <summary>
        /// Tracks the largest disagreement seen so a failure can report it.
        /// </summary>
        private struct Worst
        {
            public float Error;
            public string Detail;

            public void Record(float error, Func<string> detail)
            {
                if (error > Error)
                {
                    Error = error;
                    Detail = detail();
                }
            }

            public void Assert(string name, float tolerance)
            {
                NUnit.Framework.Assert.LessOrEqual(Error, tolerance,
                    $"{name} deviates from the engine implementation by {Error} (tolerance {tolerance}).\n{Detail}");
            }
        }

        [Test]
        public void DeltaAngleMatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var current = RandomFloat(-1080.0f, 1080.0f);
                var target = RandomFloat(-1080.0f, 1080.0f);
                var expected = Mathf.DeltaAngle(current, target);
                var actual = NetworkTransformMath.DeltaAngle(current, target);
                worst.Record(Mathf.Abs(expected - actual), () => $"current={current} target={target} expected={expected} actual={actual}");
            }
            worst.Assert(nameof(NetworkTransformMath.DeltaAngle), k_ExactTolerance);
        }

        [Test]
        public void RepeatMatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var t = RandomFloat(-1080.0f, 1080.0f);
                var expected = Mathf.Repeat(t, 360.0f);
                var actual = NetworkTransformMath.Repeat(t, 360.0f);
                worst.Record(Mathf.Abs(expected - actual), () => $"t={t} expected={expected} actual={actual}");
            }
            worst.Assert(nameof(NetworkTransformMath.Repeat), k_ExactTolerance);
        }

        [Test]
        public void LerpVector3MatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var start = RandomVector(100.0f);
                var end = RandomVector(100.0f);
                var t = RandomFloat(-0.5f, 1.5f);
                var expected = Vector3.Lerp(start, end, t);
                var actual = (Vector3)NetworkTransformMath.Lerp(start, end, t);
                worst.Record(Vector3.Distance(expected, actual), () => $"start={start} end={end} t={t} expected={expected} actual={actual}");
            }
            worst.Assert("Lerp(Vector3)", k_ExactTolerance);
        }

        [Test]
        public void SmoothDampVector3MatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var current = RandomVector(50.0f);
                var target = RandomVector(50.0f);
                var velocity = RandomVector(10.0f);
                var smoothTime = RandomFloat(0.001f, 1.0f);
                var maxSpeed = RandomFloat(0.1f, 100.0f);
                var deltaTime = RandomFloat(0.001f, 0.1f);

                var engineVelocity = velocity;
                var expected = Vector3.SmoothDamp(current, target, ref engineVelocity, smoothTime, maxSpeed, deltaTime);

                float3 portedVelocity = velocity;
                var actual = (Vector3)NetworkTransformMath.SmoothDamp(current, target, ref portedVelocity, smoothTime, maxSpeed, deltaTime);

                var error = Mathf.Max(Vector3.Distance(expected, actual), Vector3.Distance(engineVelocity, (Vector3)portedVelocity));
                worst.Record(error, () => $"current={current} target={target} smoothTime={smoothTime} maxSpeed={maxSpeed} dt={deltaTime}\n" +
                    $"  expected={expected} vel={engineVelocity}\n  actual  ={actual} vel={(Vector3)portedVelocity}");
            }
            worst.Assert("SmoothDamp(Vector3)", k_SingleUlpTolerance);
        }

        [Test]
        public void SmoothDampAngleMatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var current = RandomFloat(-720.0f, 720.0f);
                var target = RandomFloat(-720.0f, 720.0f);
                var velocity = RandomFloat(-50.0f, 50.0f);
                var smoothTime = RandomFloat(0.001f, 1.0f);
                var maxSpeed = RandomFloat(0.1f, 500.0f);
                var deltaTime = RandomFloat(0.001f, 0.1f);

                var engineVelocity = velocity;
                var expected = Mathf.SmoothDampAngle(current, target, ref engineVelocity, smoothTime, maxSpeed, deltaTime);

                var portedVelocity = velocity;
                var actual = NetworkTransformMath.SmoothDampAngle(current, target, ref portedVelocity, smoothTime, maxSpeed, deltaTime);

                var error = Mathf.Max(Mathf.Abs(expected - actual), Mathf.Abs(engineVelocity - portedVelocity));
                worst.Record(error, () => $"current={current} target={target} smoothTime={smoothTime} dt={deltaTime} expected={expected} actual={actual}");
            }
            worst.Assert("SmoothDampAngle", k_ExactTolerance);
        }

        [Test]
        public void EulerAnglesMatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var rotation = RandomRotation();
                var expected = rotation.eulerAngles;
                var actual = (Vector3)NetworkTransformMath.EulerAngles(rotation);

                // Compared as angles so that 359.999 and 0.001 are not treated as a large disagreement.
                var error = Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(expected.x, actual.x)),
                    Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(expected.y, actual.y)), Mathf.Abs(Mathf.DeltaAngle(expected.z, actual.z))));
                worst.Record(error, () => $"rotation={rotation} expected={expected} actual={actual}");
            }
            worst.Assert(nameof(NetworkTransformMath.EulerAngles), k_EquivalentTolerance);
        }

        [Test]
        public void EulerMatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var euler = new Vector3(RandomFloat(-360.0f, 360.0f), RandomFloat(-360.0f, 360.0f), RandomFloat(-360.0f, 360.0f));
                var expected = Quaternion.Euler(euler);
                var actual = (Quaternion)NetworkTransformMath.Euler(euler);

                // q and -q are the same rotation, so compare the angle between them.
                var error = Quaternion.Angle(expected, actual);
                worst.Record(error, () => $"euler={euler} expected={expected} actual={actual}");
            }
            worst.Assert(nameof(NetworkTransformMath.Euler), k_EquivalentTolerance);
        }

        [Test]
        public void SlerpQuaternionMatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var start = RandomRotation();
                var end = RandomRotation();
                var t = RandomFloat(0.0f, 1.0f);
                var expected = Quaternion.Slerp(start, end, t);
                var actual = (Quaternion)NetworkTransformMath.Slerp(start, end, t);
                worst.Record(Quaternion.Angle(expected, actual), () => $"start={start} end={end} t={t} expected={expected} actual={actual}");
            }
            worst.Assert("Slerp(Quaternion)", k_EquivalentTolerance);
        }

        [Test]
        public void LerpQuaternionMatchesEngine()
        {
            var worst = new Worst();
            for (int i = 0; i < k_Iterations; i++)
            {
                var start = RandomRotation();
                var end = RandomRotation();
                var t = RandomFloat(0.0f, 1.0f);
                var expected = Quaternion.Lerp(start, end, t);
                var actual = (Quaternion)NetworkTransformMath.Nlerp(start, end, t);
                worst.Record(Quaternion.Angle(expected, actual), () => $"start={start} end={end} t={t} expected={expected} actual={actual}");
            }
            worst.Assert(nameof(NetworkTransformMath.Nlerp), k_EquivalentTolerance);
        }

        [Test]
        public void SlerpVector3MatchesEngine()
        {
            var worst = new Worst();
            var worstAntiparallel = new Worst();
            var antiparallelCount = 0;

            for (int i = 0; i < k_Iterations; i++)
            {
                var start = RandomVector(50.0f);
                var end = RandomVector(50.0f);
                var t = RandomFloat(0.0f, 1.0f);
                var expected = Vector3.Slerp(start, end, t);
                var actual = (Vector3)NetworkTransformMath.Slerp((float3)start, (float3)end, t);
                var error = Vector3.Distance(expected, actual);

                // Nearly antiparallel inputs have no defined rotation plane, so both implementations have to
                // pick one arbitrarily. Measured and reported, but not asserted on.
                var cosAngle = Vector3.Dot(start.normalized, end.normalized);
                if (cosAngle < -0.999f)
                {
                    antiparallelCount++;
                    worstAntiparallel.Record(error, () => $"start={start} end={end} t={t}");
                }
                else
                {
                    worst.Record(error, () => $"start={start} end={end} t={t} expected={expected} actual={actual}");
                }
            }

            Debug.Log($"Slerp(Vector3): nearly antiparallel max deviation {worstAntiparallel.Error:E3} over {antiparallelCount} samples (not asserted).");
            worst.Assert("Slerp(Vector3)", k_EquivalentTolerance);
        }

        /// <summary>
        /// Reports every measurement in one place so the numbers can be reviewed together rather than one
        /// assertion at a time.
        /// </summary>
        [Test]
        public void ReportAllDeviations()
        {
            var report = new StringBuilder();
            report.AppendLine($"{nameof(NetworkTransformMath)} agreement with the engine ({k_Iterations} samples each):");

            void Measure(string name, Func<float> sample)
            {
                var worst = 0.0f;
                for (int i = 0; i < k_Iterations; i++)
                {
                    worst = Mathf.Max(worst, sample());
                }
                report.AppendLine($"  {name,-24} max deviation {worst:E3}");
            }

            Measure("DeltaAngle", () =>
            {
                var a = RandomFloat(-1080.0f, 1080.0f);
                var b = RandomFloat(-1080.0f, 1080.0f);
                return Mathf.Abs(Mathf.DeltaAngle(a, b) - NetworkTransformMath.DeltaAngle(a, b));
            });
            Measure("EulerAngles", () =>
            {
                var r = RandomRotation();
                var e = r.eulerAngles;
                var a = (Vector3)NetworkTransformMath.EulerAngles(r);
                return Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(e.x, a.x)), Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(e.y, a.y)), Mathf.Abs(Mathf.DeltaAngle(e.z, a.z))));
            });
            Measure("Euler", () =>
            {
                var e = new Vector3(RandomFloat(-360.0f, 360.0f), RandomFloat(-360.0f, 360.0f), RandomFloat(-360.0f, 360.0f));
                return Quaternion.Angle(Quaternion.Euler(e), (Quaternion)NetworkTransformMath.Euler(e));
            });
            Measure("Slerp(Quaternion)", () =>
            {
                var s = RandomRotation();
                var e = RandomRotation();
                var t = RandomFloat(0.0f, 1.0f);
                return Quaternion.Angle(Quaternion.Slerp(s, e, t), (Quaternion)NetworkTransformMath.Slerp(s, e, t));
            });
            Measure("Lerp(Quaternion)", () =>
            {
                var s = RandomRotation();
                var e = RandomRotation();
                var t = RandomFloat(0.0f, 1.0f);
                return Quaternion.Angle(Quaternion.Lerp(s, e, t), (Quaternion)NetworkTransformMath.Nlerp(s, e, t));
            });
            Measure("Slerp(Vector3)", () =>
            {
                var s = RandomVector(50.0f);
                var e = RandomVector(50.0f);
                var t = RandomFloat(0.0f, 1.0f);
                return Vector3.Distance(Vector3.Slerp(s, e, t), (Vector3)NetworkTransformMath.Slerp((float3)s, (float3)e, t));
            });

            Debug.Log(report.ToString());
        }
    }
}
