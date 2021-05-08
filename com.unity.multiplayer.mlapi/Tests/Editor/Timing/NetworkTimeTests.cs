using System;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Timing;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace MLAPI.EditorTests.Timing
{
    public class NetworkTimeTests
    {

        [Test]
        public void NetworkTimeCreate()
        {
            float a = 34f;
            float b = 17.32f;
            float c = -42.4f;
            float d = -6f;
            float e = int.MaxValue / 61f;

            NetworkTime timeA = new NetworkTime(60, a);
            NetworkTime timeB =  new NetworkTime(60, b);
            NetworkTime timeC =  new NetworkTime(60, c);
            NetworkTime timeD = new NetworkTime(60, d);
            NetworkTime timeE =  new NetworkTime(60, e);

            Assert.IsTrue(Mathf.Approximately(a, timeA.Time));
            Assert.IsTrue(Mathf.Approximately(b, timeB.Time));
            Assert.IsTrue(Mathf.Approximately(c, timeC.Time));
            Assert.IsTrue(Mathf.Approximately(d, timeD.Time));
            Assert.IsTrue(Mathf.Approximately(e, timeE.Time));

            Assert.IsTrue(timeA.TickDuration >= 0);
            Assert.IsTrue(timeB.TickDuration >= 0);
            Assert.IsTrue(timeC.TickDuration >= 0);
            Assert.IsTrue(timeD.TickDuration >= 0);
            Assert.IsTrue(timeE.TickDuration >= 0);
        }

        [Test]
        public void NetworkTimeDefault()
        {
            NetworkTime defaultTime = default;

            Assert.IsTrue(defaultTime.Time == 0f);
        }

        [Test]
        public void NetworkTimeAddFloatTest()
        {
            float a = 34f;
            float b = 17.32f;
            float c = -42.4f;
            float d = -6f;
            float e = int.MaxValue / 61f;

            float floatResultB = a + b;
            float floatResultC = a + c;
            float floatResultD = a + d;
            float floatResultE = a + e;

            NetworkTime timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA + b;
            NetworkTime timeC = timeA + c;
            NetworkTime timeD = timeA + d;
            NetworkTime timeE = timeA + e;

            Assert.IsTrue(Mathf.Approximately(floatResultB, timeB.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultC, timeC.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultD, timeD.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultE, timeE.Time));
        }

        [Test]
        public void NetworkTimeSubFloatTest()
        {
            float a = 34f;
            float b = 17.32f;
            float c = -42.4f;
            float d = -6f;
            float e = int.MaxValue / 61f;

            float floatResultB = a - b;
            float floatResultC = a - c;
            float floatResultD = a - d;
            float floatResultE = a - e;

            NetworkTime timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA - b;
            NetworkTime timeC = timeA - c;
            NetworkTime timeD = timeA - d;
            NetworkTime timeE = timeA - e;

            Assert.IsTrue(Mathf.Approximately(floatResultB, timeB.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultC, timeC.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultD, timeD.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultE, timeE.Time));
        }


        [Test]
        public void NetworkTimeAddNetworkTimeTest()
        {
            float a = 34f;
            float b = 17.32f;
            float c = -42.4f;
            float d = -6f;
            float e = int.MaxValue / 61f;

            float floatResultB = a + b;
            float floatResultC = a + c;
            float floatResultD = a + d;
            float floatResultE = a + e;

            NetworkTime timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA + new NetworkTime(60, b);
            NetworkTime timeC = timeA + new NetworkTime(60, c);
            NetworkTime timeD = timeA + new NetworkTime(60, d);
            NetworkTime timeE = timeA + new NetworkTime(60, e);

            Assert.IsTrue(Mathf.Approximately(floatResultB, timeB.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultC, timeC.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultD, timeD.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultE, timeE.Time));
        }

        [Test]
        public void NetworkTimeSubNetworkTimeTest()
        {
            float a = 34f;
            float b = 17.32f;
            float c = -42.4f;
            float d = -6f;
            float e = int.MaxValue / 61f;

            float floatResultB = a - b;
            float floatResultC = a - c;
            float floatResultD = a - d;
            float floatResultE = a - e;

            NetworkTime timeA = new NetworkTime(60, a);
            NetworkTime timeB = timeA - new NetworkTime(60, b);
            NetworkTime timeC = timeA - new NetworkTime(60, c);
            NetworkTime timeD = timeA - new NetworkTime(60, d);
            NetworkTime timeE = timeA - new NetworkTime(60, e);

            Assert.IsTrue(Mathf.Approximately(floatResultB, timeB.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultC, timeC.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultD, timeD.Time));
            Assert.IsTrue(Mathf.Approximately(floatResultE, timeE.Time));
        }

        [Test]
        public void NetworkTimeAdvanceTest()
        {
            var random = new Random(42);
            var randomSteps = Enumerable.Repeat(0f, 1000).Select(t => Mathf.InverseLerp(1/25f, 1.80f, (float)random.NextDouble())).ToList();

            NetworkTimeAdvanceTestInternal(randomSteps, 60, 0f);
            NetworkTimeAdvanceTestInternal(randomSteps, 1, 0f);
            NetworkTimeAdvanceTestInternal(randomSteps, 10, 0f);
            NetworkTimeAdvanceTestInternal(randomSteps, 20, 0f);
            NetworkTimeAdvanceTestInternal(randomSteps, 30, 0f);
            NetworkTimeAdvanceTestInternal(randomSteps, 144, 0f);


            NetworkTimeAdvanceTestInternal(randomSteps, 60, 23132.231f);
            NetworkTimeAdvanceTestInternal(randomSteps, 1, 23132.231f);
            NetworkTimeAdvanceTestInternal(randomSteps, 10, 23132.231f);
            NetworkTimeAdvanceTestInternal(randomSteps, 20, 23132.231f);
            NetworkTimeAdvanceTestInternal(randomSteps, 30, 23132.231f);
            NetworkTimeAdvanceTestInternal(randomSteps, 30, 23132.231f);
            NetworkTimeAdvanceTestInternal(randomSteps, 144, 23132.231f);

            var shortSteps = Enumerable.Repeat(1 / 30f, 1000);

            NetworkTimeAdvanceTestInternal(shortSteps, 60, 0f);
            NetworkTimeAdvanceTestInternal(shortSteps, 1, 0f);
            NetworkTimeAdvanceTestInternal(shortSteps, 10, 0f);
            NetworkTimeAdvanceTestInternal(shortSteps, 20, 0f);
            NetworkTimeAdvanceTestInternal(shortSteps, 30, 0f);
            NetworkTimeAdvanceTestInternal(shortSteps, 144, 0f);

            NetworkTimeAdvanceTestInternal(shortSteps, 60, 1000000f);
            NetworkTimeAdvanceTestInternal(shortSteps, 60, 1000000f);
            NetworkTimeAdvanceTestInternal(shortSteps, 1, 1000000f);
            NetworkTimeAdvanceTestInternal(shortSteps, 10, 1000000f);
            NetworkTimeAdvanceTestInternal(shortSteps, 20, 1000000f);
            NetworkTimeAdvanceTestInternal(shortSteps, 30, 1000000f);
            NetworkTimeAdvanceTestInternal(shortSteps, 144, 1000000f);
        }

        private void NetworkTimeAdvanceTestInternal(IEnumerable<float> steps, int tickRate, float start, float start2 = 0f)
        {
            float maxAcceptableTotalOffset = 0.005f;

            NetworkTime startTime = new NetworkTime(tickRate, start);
            NetworkTime startTime2 = new NetworkTime(tickRate, start2);
            NetworkTime dif = startTime2 - startTime;

            int i = 1;
            foreach (var step in steps)
            {
                startTime += step;
                startTime2 += step;
                Assert.IsTrue(Mathf.Approximately( startTime.Time, (startTime2 - dif).Time));
                i++;
            }

            Assert.IsTrue(Approximately( startTime.Time, (startTime2 - dif).Time, maxAcceptableTotalOffset));

            Debug.Log((startTime.Time - (startTime2 - dif).Time).ToString("n10"));
        }

        private static bool Approximately(float a, float b, float epsilon)
        {
            var dif = Math.Abs(a - b);
            return dif <= epsilon;
        }

    }
}
