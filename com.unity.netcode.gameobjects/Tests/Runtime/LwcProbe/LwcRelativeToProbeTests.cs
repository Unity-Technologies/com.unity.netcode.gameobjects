using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using BigInteger = System.Numerics.BigInteger;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode.RuntimeTests.LwcProbe
{
    // Temporary probe for unity/unity#138277 (LargeWorldCoordinate.RelativeTo), not for merging. It runs the candidate
    // RelativeTo forms as Burst jobs under FloatMode.Strict and FloatMode.Fast on each CI platform, and reports how many
    // results differ from the exactly rounded difference, a hash of the result bits to compare across platforms, and
    // the cost per element. The forms copy the PR's arithmetic with int3 cells in place of the internal type.
    internal static class RelativeToForms
    {
        public const float CellSize = 1024;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Double(int3 cellA, float3 offsetA, int3 cellB, float3 offsetB)
        {
            return (float3)((double3)(cellA - cellB) * CellSize + ((double3)offsetA - offsetB));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TwoSum(int3 cellA, float3 offsetA, int3 cellB, float3 offsetB)
        {
            var difference = offsetA - offsetB;
            var back = difference - offsetA;
            var error = (offsetA - (difference - back)) + (-offsetB - back);
            return ((float3)(cellA - cellB) * CellSize + difference) + error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 HalfCell(int3 cellA, float3 offsetA, int3 cellB, float3 offsetB)
        {
            var halfCells = (float3)(cellA - cellB) * (CellSize * 0.5f);
            return (halfCells + offsetA) - (offsetB - halfCells);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Naive(int3 cellA, float3 offsetA, int3 cellB, float3 offsetB)
        {
            return (float3)(cellA - cellB) * CellSize + (offsetA - offsetB);
        }
    }

    internal struct Pair
    {
        public int3 CellA;
        public float3 OffsetA;
        public int3 CellB;
        public float3 OffsetB;
    }

    internal static class ProbeJobUtility
    {
        // Burst drops this call, so the flag stays 0 only when the job ran Burst-compiled.
        [BurstDiscard]
        public static void MarkManaged(NativeArray<int> managed)
        {
            managed[0] = 1;
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
    internal struct DoubleStrictJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.Double(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, CompileSynchronously = true)]
    internal struct DoubleFastJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.Double(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
    internal struct TwoSumStrictJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.TwoSum(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, CompileSynchronously = true)]
    internal struct TwoSumFastJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.TwoSum(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
    internal struct HalfCellStrictJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.HalfCell(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, CompileSynchronously = true)]
    internal struct HalfCellFastJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.HalfCell(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
    internal struct NaiveStrictJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.Naive(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, CompileSynchronously = true)]
    internal struct NaiveFastJob : IJob
    {
        [ReadOnly] public NativeArray<Pair> Pairs;
        public NativeArray<float3> Results;
        public NativeArray<int> Managed;
        public int Repeat;

        public void Execute()
        {
            ProbeJobUtility.MarkManaged(Managed);
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    var p = Pairs[i];
                    Results[i] = RelativeToForms.Naive(p.CellA, p.OffsetA, p.CellB, p.OffsetB);
                }
            }
        }
    }

    internal class LwcRelativeToProbeTests
    {
        private const int k_TimingPairs = 4096;
        private const int k_TimingRepeat = 2000;
        private const int k_TimingSamples = 5;

        private struct Expected
        {
            public float Value;
            public bool NonZero;
            public bool UnderHalfCell;
        }

        private struct Summary
        {
            public int Inexact;
            public int Collapsed;
            public int UnderHalfCellInexact;
            public long MaxUlp;
            public ulong Hash;
        }

        private delegate void RunJob(NativeArray<Pair> pairs, NativeArray<float3> results, NativeArray<int> managed, int repeat);

        private NativeArray<Pair> m_Pairs;
        private Expected[] m_Expected;
        private int m_UnderHalfCell;

        [Test]
        public void RelativeToFormsPerFloatMode()
        {
            var scalars = BuildScalarCases();
            var pairCount = scalars.Count / 3;
            m_Pairs = new NativeArray<Pair>(pairCount, Allocator.Persistent);
            m_Expected = new Expected[pairCount * 3];
            m_UnderHalfCell = 0;
            for (int i = 0; i < pairCount; i++)
            {
                var x = scalars[3 * i];
                var y = scalars[3 * i + 1];
                var z = scalars[3 * i + 2];
                m_Pairs[i] = new Pair
                {
                    CellA = new int3(x.CellA, y.CellA, z.CellA),
                    OffsetA = new float3(x.OffsetA, y.OffsetA, z.OffsetA),
                    CellB = new int3(x.CellB, y.CellB, z.CellB),
                    OffsetB = new float3(x.OffsetB, y.OffsetB, z.OffsetB),
                };
                for (int axis = 0; axis < 3; axis++)
                {
                    m_Expected[3 * i + axis] = ExactDifference(scalars[3 * i + axis]);
                    if (m_Expected[3 * i + axis].UnderHalfCell)
                    {
                        m_UnderHalfCell++;
                    }
                }
            }

            var report = new StringBuilder();
            report.AppendLine($"LWCPROBE platform={Application.platform} processArch={RuntimeInformation.ProcessArchitecture} pointer={IntPtr.Size * 8}bit cpu=\"{SystemInfo.processorType}\" scalars={pairCount * 3} underHalfCell={m_UnderHalfCell}");
            var failures = new StringBuilder();
            try
            {
                Measure(report, failures, "Double", "Strict", true, (p, r, m, n) => new DoubleStrictJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
                Measure(report, failures, "Double", "Fast", true, (p, r, m, n) => new DoubleFastJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
                Measure(report, failures, "TwoSum", "Strict", false, (p, r, m, n) => new TwoSumStrictJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
                Measure(report, failures, "TwoSum", "Fast", false, (p, r, m, n) => new TwoSumFastJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
                Measure(report, failures, "HalfCell", "Strict", false, (p, r, m, n) => new HalfCellStrictJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
                Measure(report, failures, "HalfCell", "Fast", false, (p, r, m, n) => new HalfCellFastJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
                Measure(report, failures, "Naive", "Strict", false, (p, r, m, n) => new NaiveStrictJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
                Measure(report, failures, "Naive", "Fast", false, (p, r, m, n) => new NaiveFastJob { Pairs = p, Results = r, Managed = m, Repeat = n, }.Run());
            }
            finally
            {
                m_Pairs.Dispose();
            }

            Debug.Log(report.ToString());
            if (failures.Length > 0)
            {
                Assert.Fail(failures.ToString() + report);
            }
            Assert.Pass(report.ToString());
        }

        private void Measure(StringBuilder report, StringBuilder failures, string form, string mode, bool mustBeExact, RunJob run)
        {
            var results = new NativeArray<float3>(m_Pairs.Length, Allocator.Persistent);
            var managed = new NativeArray<int>(1, Allocator.Persistent);
            var timingPairs = m_Pairs.GetSubArray(m_Pairs.Length - k_TimingPairs, k_TimingPairs);
            var timingResults = new NativeArray<float3>(k_TimingPairs, Allocator.Persistent);
            try
            {
                run(m_Pairs, results, managed, 1);
                var summary = Compare(results);
                var burst = managed[0] == 0;

                run(timingPairs, timingResults, managed, 1);
                var best = double.MaxValue;
                for (int s = 0; s < k_TimingSamples; s++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    run(timingPairs, timingResults, managed, k_TimingRepeat);
                    stopwatch.Stop();
                    best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds * 1e6 / ((double)k_TimingPairs * k_TimingRepeat));
                }

                report.AppendLine($"LWCPROBE form={form} mode={mode} burst={burst} inexact={summary.Inexact} collapsed={summary.Collapsed} underHalfCellInexact={summary.UnderHalfCellInexact}/{m_UnderHalfCell} maxUlp={summary.MaxUlp} hash=0x{summary.Hash:x16} nsPerElement={best:F3}");
                if (!burst)
                {
                    failures.AppendLine($"{form}/{mode} did not run Burst-compiled, so its results say nothing about Burst.");
                }
                if (mustBeExact && (summary.Collapsed != 0 || summary.UnderHalfCellInexact != 0))
                {
                    failures.AppendLine($"{form}/{mode}: {summary.Collapsed} collapsed, {summary.UnderHalfCellInexact} inexact under half a cell apart.");
                }
            }
            finally
            {
                results.Dispose();
                managed.Dispose();
                timingResults.Dispose();
            }
        }

        private Summary Compare(NativeArray<float3> results)
        {
            var summary = new Summary { Hash = 14695981039346656037UL, };
            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                for (int axis = 0; axis < 3; axis++)
                {
                    var got = result[axis];
                    var expected = m_Expected[3 * i + axis];
                    var bits = (uint)BitConverter.SingleToInt32Bits(got);
                    for (int b = 0; b < 4; b++)
                    {
                        summary.Hash = (summary.Hash ^ ((bits >> (8 * b)) & 0xFF)) * 1099511628211UL;
                    }
                    var ulp = Math.Abs(Ordered(expected.Value) - Ordered(got));
                    if (ulp != 0)
                    {
                        summary.Inexact++;
                        if (expected.UnderHalfCell)
                        {
                            summary.UnderHalfCellInexact++;
                        }
                    }
                    if (expected.NonZero && got == 0)
                    {
                        summary.Collapsed++;
                    }
                    summary.MaxUlp = Math.Max(summary.MaxUlp, ulp);
                }
            }
            return summary;
        }

        private struct ScalarCase
        {
            public int CellA;
            public float OffsetA;
            public int CellB;
            public float OffsetB;
        }

        // Inputs come from integer operations and exact float steps, so every platform runs the same cases.
        private static List<ScalarCase> BuildScalarCases()
        {
            var cases = new List<ScalarCase>();
            var edges = new List<float>();
            foreach (var edge in new[] { 512f, 256f, 0f, -256f })
            {
                for (int k = 0; k <= 6; k++)
                {
                    AddOffset(edges, Step(edge, -k));
                    AddOffset(edges, Step(edge, k));
                }
            }
            for (int k = 0; k <= 6; k++)
            {
                AddOffset(edges, Step(-512f, k));
            }
            AddOffset(edges, 1e-30f);
            AddOffset(edges, -1e-30f);
            AddOffset(edges, float.Epsilon);
            AddOffset(edges, -float.Epsilon);

            foreach (var cellA in new[] { -30, 0, 1000, short.MaxValue - 1 })
            {
                for (int d = -1; d <= 1; d++)
                {
                    foreach (var offsetA in edges)
                    {
                        foreach (var offsetB in edges)
                        {
                            cases.Add(new ScalarCase { CellA = cellA, OffsetA = offsetA, CellB = cellA - d, OffsetB = offsetB, });
                        }
                    }
                }
            }

            uint state = 20261117;
            var random = cases.Count;
            for (int i = 0; i < random; i++)
            {
                var cellA = (int)(Next(ref state) % 65536) + short.MinValue;
                var cellB = Next(ref state) % 4 == 0
                    ? (int)(Next(ref state) % 65536) + short.MinValue
                    : math.clamp(cellA + (int)(Next(ref state) % 5) - 2, short.MinValue, short.MaxValue);
                cases.Add(new ScalarCase { CellA = cellA, OffsetA = RandomOffset(ref state), CellB = cellB, OffsetB = RandomOffset(ref state), });
            }

            // Pad to a whole number of float3 lanes.
            while (cases.Count % 3 != 0)
            {
                cases.Add(new ScalarCase());
            }
            return cases;
        }

        private static void AddOffset(List<float> offsets, float offset)
        {
            if (offset < 512f && offset >= -512f && !offsets.Contains(offset))
            {
                offsets.Add(offset);
            }
        }

        private static uint Next(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        // Half the offsets are spread evenly on a 2^-15 m grid, half have random bits, which reaches tiny values.
        private static float RandomOffset(ref uint state)
        {
            if ((Next(ref state) & 1) == 0)
            {
                // k * 2^-15 is exact in float for |k| <= 2^24.
                var k = (int)(Next(ref state) % (1u << 25)) - (1 << 24);
                return k * (1f / 32768f);
            }
            var magnitude = BitConverter.Int32BitsToSingle((int)(Next(ref state) % (uint)BitConverter.SingleToInt32Bits(512f)));
            return (Next(ref state) & 1) == 0 ? magnitude : -magnitude;
        }

        private static float Step(float value, int ulps)
        {
            return FromOrdered(Ordered(value) + ulps);
        }

        private static long Ordered(float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            return bits < 0 ? (long)int.MinValue - bits : bits;
        }

        private static float FromOrdered(long ordered)
        {
            return BitConverter.Int32BitsToSingle(ordered < 0 ? (int)(int.MinValue - ordered) : (int)ordered);
        }

        // The value times 2^149, so every float is an integer.
        private static BigInteger Scaled(float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            int exponent = (bits >> 23) & 0xFF;
            long mantissa = bits & 0x7FFFFF;
            if (exponent != 0)
            {
                mantissa |= 0x800000;
            }
            else
            {
                exponent = 1;
            }
            var scaled = (BigInteger)mantissa << (exponent - 1);
            return bits < 0 ? -scaled : scaled;
        }

        private static Expected ExactDifference(ScalarCase c)
        {
            var exact = ((BigInteger)(c.CellA - c.CellB) * 1024 << 149) + Scaled(c.OffsetA) - Scaled(c.OffsetB);
            var guess = (float)((double)exact * Math.Pow(2, -149));
            var best = guess;
            var bestDistance = BigInteger.Abs(exact - Scaled(guess));
            for (int k = -2; k <= 2; k++)
            {
                var candidate = Step(guess, k);
                var distance = BigInteger.Abs(exact - Scaled(candidate));
                if (distance < bestDistance || (distance == bestDistance && (BitConverter.SingleToInt32Bits(candidate) & 1) == 0))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return new Expected
            {
                Value = best,
                NonZero = !exact.IsZero,
                UnderHalfCell = BigInteger.Abs(exact) < ((BigInteger)512 << 149),
            };
        }
    }
}
