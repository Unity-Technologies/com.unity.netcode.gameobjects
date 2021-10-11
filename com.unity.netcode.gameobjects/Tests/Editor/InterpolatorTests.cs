using System;
using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class InterpolatorTests
    {
        private const float k_Precision = 0.00000001f;

        private class MockInterpolatorTime : BufferedLinearInterpolator<float>.IInterpolatorTime
        {
            public double BufferedServerTime { get; set; }
            public double BufferedServerFixedTime { get; }
            public uint TickRate { get; set; }

            public MockInterpolatorTime(double serverTime, uint tickRate)
            {
                BufferedServerTime = serverTime;
                TickRate = tickRate;
            }
        }

        private const int k_MockTickRate = 1;

        private NetworkTime T(float time, uint tickRate = k_MockTickRate)
        {
            return new NetworkTime(tickRate, timeSec: time);
        }

        [Test]
        public void TestReset()
        {
            var timeMock = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(timeMock);

            timeMock.BufferedServerTime = 100f;

            interpolator.AddMeasurement(5, T(1.0f));
            var initVal = interpolator.Update(10); // big value
            Assert.That(initVal, Is.EqualTo(5f));
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(5f));

            interpolator.ResetTo(100f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(100f));
            var val = interpolator.Update(1f);
            Assert.That(val, Is.EqualTo(100f));
        }

        [Test]
        public void NormalUsage()
        {
            // Testing float instead of Vector3. The only difference with Vector3 is the lerp method used.

            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0f));

            interpolator.AddMeasurement(0f, T(1.0f));
            interpolator.AddMeasurement(1f, T(2.0f));

            // too small update, nothing happens, doesn't consume from buffer yet
            float deltaTime = 0.01f;
            mockBufferedTime.BufferedServerTime = 0.01f;
            interpolator.Update(deltaTime);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0f));

            // consume first measurement, still can't interpolate with just one tick consumed
            mockBufferedTime.BufferedServerTime = 1.01f;
            interpolator.Update(1.0f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0f));

            // consume second measurement, start to interpolate
            mockBufferedTime.BufferedServerTime = 2.01f;
            var valueFromUpdate = interpolator.Update(1.0f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0.01f).Within(k_Precision));
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0.01f).Within(k_Precision)); // test a second time, to make sure the get doesn't update the value
            Assert.That(valueFromUpdate, Is.EqualTo(interpolator.GetInterpolatedValue()).Within(k_Precision));

            // continue interpolation
            mockBufferedTime.BufferedServerTime = 2.5f;
            interpolator.Update(2.5f - 2.01f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0.5f).Within(k_Precision));
            // check when reaching end
            mockBufferedTime.BufferedServerTime = 3f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1f).Within(k_Precision));
        }

        /// <summary>
        /// Out of order or 'ACB' problem
        /// Given two measurements have already arrived A and C, if a new measurement B arrives, the interpolation shouldn't go to B, but continue
        /// to C.
        /// Adding B should be ignored if interpolation is already interpolating between A and C
        /// </summary>
        [Test]
        public void OutOfOrderShouldStillWork()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            interpolator.AddMeasurement(0, T(0f));
            interpolator.AddMeasurement(2, T(2f));

            mockBufferedTime.BufferedServerTime = 1.5;
            interpolator.Update(1.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0f).Within(k_Precision));

            mockBufferedTime.BufferedServerTime = 2f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1f).Within(k_Precision));

            mockBufferedTime.BufferedServerTime = 2.5;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1.5f).Within(k_Precision));

            // makes sure that interpolation still continues in right direction
            interpolator.AddMeasurement(1, T(1f));

            mockBufferedTime.BufferedServerTime = 3f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(2f).Within(k_Precision));
        }

        [Test]
        public void MessageLoss()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            interpolator.AddMeasurement(1f, T(1f));
            interpolator.AddMeasurement(2f, T(2f));
            // message time=3 was lost
            interpolator.AddMeasurement(4f, T(4f));
            interpolator.AddMeasurement(5f, T(5f));
            // message time=6 was lost
            interpolator.AddMeasurement(100f, T(7f)); // high value to produce a misprediction

            // first value teleports interpolator
            mockBufferedTime.BufferedServerTime = 1f;
            interpolator.Update(1f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1f));

            // nothing happens, not ready to consume second value yet
            mockBufferedTime.BufferedServerTime = 1.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1f));

            // beginning of interpolation, second value consumed, currently at start
            mockBufferedTime.BufferedServerTime = 2f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1f));

            // interpolation starts
            mockBufferedTime.BufferedServerTime = 2.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1.5f));

            mockBufferedTime.BufferedServerTime = 3f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(2f));

            // extrapolating to 2.5
            mockBufferedTime.BufferedServerTime = 3.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(2.5f));

            // next value skips to where it was supposed to be once buffer time is showing the next value
            mockBufferedTime.BufferedServerTime = 4f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(3f));

            // interpolation continues as expected
            mockBufferedTime.BufferedServerTime = 4.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(3.5f));

            mockBufferedTime.BufferedServerTime = 5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(4f));

            // lost time=6, extrapolating
            mockBufferedTime.BufferedServerTime = 5.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(4.5f));

            mockBufferedTime.BufferedServerTime = 6.0f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(5f));

            // misprediction
            mockBufferedTime.BufferedServerTime = 6.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(5.5f));

            // lerp to right value
            mockBufferedTime.BufferedServerTime = 7.0f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.GreaterThan(6.0f));
            Assert.That(interpolator.GetInterpolatedValue(), Is.LessThanOrEqualTo(100f));
        }

        [Test]
        public void AddFirstMeasurement()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            interpolator.AddMeasurement(2f, T(1f));
            interpolator.AddMeasurement(3f, T(2f));
            mockBufferedTime.BufferedServerTime = 1f;
            var interpolatedValue = interpolator.Update(1f);
            // when consuming only one measurement and it's the first one consumed, teleport to it
            Assert.That(interpolatedValue, Is.EqualTo(2f));

            // then interpolation should work as usual
            mockBufferedTime.BufferedServerTime = 2f;
            interpolatedValue = interpolator.Update(1f);
            Assert.That(interpolatedValue, Is.EqualTo(2f));

            mockBufferedTime.BufferedServerTime = 2.5f;
            interpolatedValue = interpolator.Update(0.5f);
            Assert.That(interpolatedValue, Is.EqualTo(2.5f));

            mockBufferedTime.BufferedServerTime = 3f;
            interpolatedValue = interpolator.Update(0.5f);
            Assert.That(interpolatedValue, Is.EqualTo(3f));
        }

        [Test]
        public void JumpToEachValueIfDeltaTimeTooBig()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            interpolator.AddMeasurement(2f, T(1f));
            interpolator.AddMeasurement(3f, T(2f));
            mockBufferedTime.BufferedServerTime = 1f;
            var interpolatedValue = interpolator.Update(1f);
            Assert.That(interpolatedValue, Is.EqualTo(2f));

            // big deltaTime, jumping to latest value
            mockBufferedTime.BufferedServerTime = 10f;
            interpolatedValue = interpolator.Update(8f);
            Assert.That(interpolatedValue, Is.EqualTo(3));
        }

        [Test]
        public void JumpToLastValueFromStart()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            interpolator.AddMeasurement(1f, T(1f));
            interpolator.AddMeasurement(2f, T(2f));
            interpolator.AddMeasurement(3f, T(3f));

            // big time jump
            mockBufferedTime.BufferedServerTime = 10f;
            var interpolatedValue = interpolator.Update(10f);
            Assert.That(interpolatedValue, Is.EqualTo(3f));

            // interpolation continues as normal
            interpolator.AddMeasurement(11f, T(11f));
            mockBufferedTime.BufferedServerTime = 10.5f;
            interpolatedValue = interpolator.Update(0.5f);
            Assert.That(interpolatedValue, Is.EqualTo(3f));
            mockBufferedTime.BufferedServerTime = 11f;
            interpolatedValue = interpolator.Update(0.5f);
            Assert.That(interpolatedValue, Is.EqualTo(10f));
            mockBufferedTime.BufferedServerTime = 11.5f;
            interpolatedValue = interpolator.Update(0.5f);
            Assert.That(interpolatedValue, Is.EqualTo(10.5f));
            mockBufferedTime.BufferedServerTime = 12f;
            interpolatedValue = interpolator.Update(0.5f);
            Assert.That(interpolatedValue, Is.EqualTo(11f));
        }

        [Test]
        public void TestBufferSizeLimit()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            // set first value
            interpolator.AddMeasurement(-1f, T(1f));
            mockBufferedTime.BufferedServerTime = 1f;
            interpolator.Update(1f);

            // max + 1
            interpolator.AddMeasurement(2, T(2)); // +1, this should trigger a burst and teleport to last value
            for (int i = 0; i < 100; i++)
            {
                interpolator.AddMeasurement(i + 3, T(i + 3));
            }

            // client was paused for a while, some time has past, we just got a burst of values from the server that teleported us to the last value received
            mockBufferedTime.BufferedServerTime = 102;
            var interpolatedValue = interpolator.Update(101f);
            Assert.That(interpolatedValue, Is.EqualTo(102));
        }

        [Test]
        public void TestUpdatingInterpolatorWithNoData()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            // invalid case, this is undefined behaviour
            Assert.Throws<InvalidOperationException>(() => interpolator.Update(1f));
        }

        [Test]
        public void TestDuplicatedValues()
        {
            var mockBufferedTime = new MockInterpolatorTime(0, k_MockTickRate);
            var interpolator = new BufferedLinearInterpolatorFloat(mockBufferedTime);

            interpolator.AddMeasurement(1f, T(1f));
            interpolator.AddMeasurement(2f, T(2f));
            interpolator.AddMeasurement(2f, T(2f));

            // empty interpolator teleports to initial value
            mockBufferedTime.BufferedServerTime = 1f;
            var interp = interpolator.Update(1f);
            Assert.That(interp, Is.EqualTo(1f));

            // consume value, start interp, currently at start value
            mockBufferedTime.BufferedServerTime = 2f;
            interp = interpolator.Update(1f);
            Assert.That(interp, Is.EqualTo(1f));
            // interp
            mockBufferedTime.BufferedServerTime = 2.5f;
            interp = interpolator.Update(0.5f);
            Assert.That(interp, Is.EqualTo(1.5f));
            // reach end
            mockBufferedTime.BufferedServerTime = 3f;
            interp = interpolator.Update(0.5f);
            Assert.That(interp, Is.EqualTo(2f));

            // with unclamped interpolation, we continue mispredicting since the two last values are actually treated as the same. Therefore we're not stopping at "2"
            mockBufferedTime.BufferedServerTime = 3.5f;
            interp = interpolator.Update(0.5f);
            Assert.That(interp, Is.EqualTo(2.5f));
            mockBufferedTime.BufferedServerTime = 4f;
            interp = interpolator.Update(0.5f);
            Assert.That(interp, Is.EqualTo(3f));

            // we add a measurement with an updated time
            interpolator.AddMeasurement(2f, T(3f));
            interp = interpolator.Update(0.5f);
            Assert.That(interp, Is.EqualTo(2f));
        }
    }
}
