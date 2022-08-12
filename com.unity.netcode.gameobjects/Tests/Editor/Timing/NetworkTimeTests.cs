using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace Unity.Netcode.EditorTests
{
    public class NetworkTimeTests
    {
        [Test]
        [TestCase(0d, 0u)]
        [TestCase(5d, 0u)]
        [TestCase(-5d, 0u)]
        public void TestFailCreateInvalidTime(double time, uint tickrate)
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => new NetworkTime(tickrate, time));
        }

        [Test]
        [TestCase(0d, 0f, 20u)]
        [TestCase(0d, 0f, 30u)]
        [TestCase(0d, 0f, 60u)]
        [TestCase(201d, 201f, 20u)]
        [TestCase(201d, 201f, 30u)]
        [TestCase(201d, 201f, 60u)]
        [TestCase(-4301d, -4301f, 20u)]
        [TestCase(-4301d, -4301f, 30u)]
        [TestCase(-4301d, -4301f, 60u)]
        [TestCase(float.MaxValue, float.MaxValue, 20u)]
        [TestCase(float.MaxValue, float.MaxValue, 30u)]
        [TestCase(float.MaxValue, float.MaxValue, 60u)]
        public void TestTimeAsFloat(double d, float f, uint tickRate)
        {
            var networkTime = new NetworkTime(tickRate, d);
            Assert.True(Mathf.Approximately(networkTime.TimeAsFloat, f));
        }

        [Test]
        [TestCase(53.55d, 53.5d, 10u)]
        [TestCase(1013553.55d, 1013553.5d, 10u)]
        [TestCase(0d, 0d, 10u)]
        [TestCase(-27.41d, -27.5d, 10u)]
        [TestCase(53.55d, 53.54d, 50u)]
        [TestCase(1013553.55d, 1013553.54d, 50u)]
        [TestCase(0d, 0d, 50u)]
        [TestCase(-27.4133d, -27.42d, 50u)]
        public void TestToFixedTime(double time, double expectedFixedTime, uint tickRate)
        {
            Assert.AreEqual(expectedFixedTime, new NetworkTime(tickRate, time).ToFixedTime().Time);
        }

        [Test]
        [TestCase(34d, 0)]
        [TestCase(17.32d, 0.2d / 60d)]
        [TestCase(-42.44d, 1d / 60d - 0.4d / 60d)]
        [TestCase(-6d, 0)]
        [TestCase(int.MaxValue / 61d, 0.00082, 10d)] // Int.Max / 61 / (1/60) to get divisor then: Int.Max - divisor * 1 / 60
        public void NetworkTimeCreate(double time, double tickOffset, double epsilon = 0.0001d)
        {
            var networkTime = new NetworkTime(60, time);

            Assert.IsTrue(Approximately(time, networkTime.Time));
            Assert.IsTrue(Approximately(networkTime.Tick * networkTime.FixedDeltaTime + networkTime.TickOffset, networkTime.Time, epsilon));
            Assert.IsTrue(Approximately(networkTime.TickOffset, tickOffset));
        }

        [Test]
        public void NetworkTimeDefault()
        {
            NetworkTime defaultTime = default;

            Assert.IsTrue(defaultTime.Time == 0f);
        }

        [Test]
        [TestCase(17.32d)]
        [TestCase(34d)]
        [TestCase(-42.4d)]
        [TestCase(-6d)]
        [TestCase(int.MaxValue / 61d)]
        public void NetworkTimeAddFloatTest(double time)
        {
            double a = 34d;
            double floatResultB = a + time;

            var timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA + time;

            Assert.IsTrue(Approximately(floatResultB, timeB.Time));
        }

        [Test]
        [TestCase(17.32d)]
        [TestCase(34d)]
        [TestCase(-42.4d)]
        [TestCase(-6d)]
        [TestCase(int.MaxValue / 61d)]
        public void NetworkTimeSubFloatTest(double time)
        {
            double a = 34d;
            double floatResultB = a - time;

            var timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA - time;

            Assert.IsTrue(Approximately(floatResultB, timeB.Time));
        }

        [Test]
        [TestCase(17.32d)]
        [TestCase(34d)]
        [TestCase(-42.4d)]
        [TestCase(-6d)]
        [TestCase(int.MaxValue / 61d)]
        public void NetworkTimeAddNetworkTimeTest(double time)
        {
            double a = 34d;
            double floatResultB = a + time;

            var timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA + new NetworkTime(60, time);
            Assert.IsTrue(Approximately(floatResultB, timeB.Time));
        }

        [Test]
        [TestCase(17.32d)]
        [TestCase(34d)]
        [TestCase(-42.4d)]
        [TestCase(-6d)]
        [TestCase(int.MaxValue / 61d)]
        public void NetworkTimeSubNetworkTimeTest(double time)
        {
            double a = 34d;

            double floatResultB = a - time;

            var timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA - new NetworkTime(60, time);
            Assert.IsTrue(Approximately(floatResultB, timeB.Time));
        }

        [Test]
        public void NetworkTimeAdvanceTest()
        {
            var random = new Random(42);
            var randomSteps = Enumerable.Repeat(0f, 1000).Select(t => Mathf.Lerp(1 / 25f, 1.80f, (float)random.NextDouble())).ToList();

            void CheckResults((bool, string) value)
            {
                if (!value.Item1)
                {
                    Assert.Fail(value.Item2);
                }
            }

            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 60, 0f));

            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 1, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 10, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 20, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 30, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 144, 0f));

            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 60, 23132.231f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 1, 23132.231f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 10, 23132.231f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 20, 23132.231f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 30, 23132.231f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 30, 23132.231f));
            CheckResults(NetworkTimeAdvanceTestInternal(randomSteps, 144, 23132.231f));
            randomSteps.Clear();
            randomSteps = null;

            var shortSteps = Enumerable.Repeat(1 / 30f, 1000);

            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 60, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 1, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 10, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 20, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 30, 0f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 144, 0f));

            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 60, 1000000f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 60, 1000000f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 1, 1000000f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 10, 1000000f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 20, 1000000f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 30, 1000000f));
            CheckResults(NetworkTimeAdvanceTestInternal(shortSteps, 144, 1000000f));
            shortSteps = null;

        }

        private (bool, string) NetworkTimeAdvanceTestInternal(IEnumerable<float> steps, uint tickRate, float start, float start2 = 0f)
        {
            float maxAcceptableTotalOffset = 0.005f;

            var startTime = new NetworkTime(tickRate, start);
            var startTime2 = new NetworkTime(tickRate, start2);
            NetworkTime dif = startTime2 - startTime;

            foreach (var step in steps)
            {
                startTime += step;
                startTime2 += step;
                var isExpectedValue = Approximately(startTime.Time, (startTime2 - dif).Time);
                if (!isExpectedValue)
                {
                    return (false, $"[NetworkTimeAdvanceTest-Failure] startTime: {startTime.Time} | Step Time Diff: {(startTime2 - dif).Time}");
                }
            }
            if (!Approximately(startTime.Time, (startTime2 - dif).Time, maxAcceptableTotalOffset))
            {
                return (false, $"[NetworkTimeAdvanceTest-Failure-End] startTime: {startTime.Time} | Step Time Diff: {(startTime2 - dif).Time} | Max Accepted Total Offset: {maxAcceptableTotalOffset}");
            }
            return (true, string.Empty);
        }

        [Test]
        public void NetworkTickAdvanceTest()
        {
            var shortSteps = Enumerable.Repeat(1 / 30f, 1000);
            NetworkTickAdvanceTestInternal(shortSteps, 30, 0.0f, 0.0f);
        }

        private NetworkTickSystem m_TickSystem;
        private NetworkTimeSystem m_TimeSystem;
        private int m_PreviousTick;

        private void NetworkTickAdvanceTestInternal(IEnumerable<float> steps, uint tickRate, float start, float start2 = 0f)
        {
            m_PreviousTick = 0;
            m_TickSystem = new NetworkTickSystem(tickRate, start, start2);
            m_TimeSystem = NetworkTimeSystem.ServerTimeSystem();

            m_TickSystem.Tick += TickUpdate;
            foreach (var step in steps)
            {
                m_TimeSystem.Advance(step);
                m_TickSystem.UpdateTick(m_TimeSystem.LocalTime, m_TimeSystem.ServerTime);
            }
        }

        private void TickUpdate()
        {
            // Make sure our tick is precisely 1 + m_PreviousTick
            Assert.IsTrue(m_TickSystem.LocalTime.Tick == m_PreviousTick + 1);
            // Assign the m_PreviousTick value for next tick check
            m_PreviousTick = m_TickSystem.LocalTime.Tick;
        }

        private static bool Approximately(double a, double b, double epsilon = 0.000001d)
        {
            var dif = Math.Abs(a - b);
            return dif <= epsilon;
        }
    }
}
