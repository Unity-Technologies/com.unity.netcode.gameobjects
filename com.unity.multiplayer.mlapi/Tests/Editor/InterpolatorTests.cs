using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class BufferedInterpolatorTests
    {
        private const float k_Precision = 0.00000001f;

        private class MockInterpolatorTime : BufferedLinearInterpolator<float>.IInterpolatorTime
        {
            public double BufferedServerTime { get; set; }
            public double LocalTime { get; }
            public int TickRate { get; set; }

            public MockInterpolatorTime(double serverTime, int tickRate)
            {
                BufferedServerTime = serverTime;
                TickRate = tickRate;
                LocalTime = serverTime; // todo
            }
        }

        /*
         * TODO
         * test normal interpolation
         * test with some jitter
         * test with high jitter
         * test with too high jitter
         * test with packet loss
         * test with out of order because of network measurements
         * test with negative time should fail
         * check https://github.com/vis2k/Mirror/blob/02cc3de7b8889f477118e20379b584eaf8bd43b6/Assets/Mirror/Tests/Editor/SnapshotInterpolationTests.cs
         * for examples of tests
         * Test every single public API
         */

        [Test]
        public void NormalUsage()
        {
            // Testing float instead of Vector3. The only difference with Vector3 is the lerp method used.
            const int mockTickRate = 1;
            float deltaTick = 1f / mockTickRate;
            NetworkTime T(float time)
            {
                return new NetworkTime(mockTickRate, timeSec: time);
            }

            var interpolator = new BufferedLinearInterpolatorFloat();
            var mockBufferedTime = new MockInterpolatorTime(0, mockTickRate);
            interpolator.interpolatorTime = mockBufferedTime;

            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0f));

            interpolator.AddMeasurement(0f, new NetworkTime(mockTickRate, 1.0));
            interpolator.AddMeasurement(1f, new NetworkTime(mockTickRate, 2.0));

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
            const int mockTickRate = 1;
            float deltaTick = 1f / mockTickRate;
            NetworkTime T(float time)
            {
                return new NetworkTime(mockTickRate, timeSec: time);
            }

            var interpolator = new BufferedLinearInterpolatorFloat();
            var mockBufferedTime = new MockInterpolatorTime(0, mockTickRate);
            interpolator.interpolatorTime = mockBufferedTime;

            interpolator.AddMeasurement(0, T(0f));
            interpolator.AddMeasurement(2, T(2f));

            mockBufferedTime.BufferedServerTime = 1.5;
            interpolator.Update(1.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0f).Within(k_Precision));

            mockBufferedTime.BufferedServerTime = 2f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0f).Within(k_Precision));

            mockBufferedTime.BufferedServerTime = 2.5;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(0.5f).Within(k_Precision));

            mockBufferedTime.BufferedServerTime = 3.5;
            interpolator.Update(1f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1.5f).Within(k_Precision));

            // makes sure that interpolation still continues in right direction
            interpolator.AddMeasurement(1, T(1f));
            mockBufferedTime.BufferedServerTime = 4;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(2f).Within(k_Precision));
        }

        [Test]
        public void MessageLoss()
        {
            const int mockTickRate = 1;
            float deltaTick = 1f / mockTickRate;

            var interpolator = new BufferedLinearInterpolatorFloat();
            var mockBufferedTime = new MockInterpolatorTime(0, mockTickRate);
            interpolator.interpolatorTime = mockBufferedTime;

            NetworkTime T(float time)
            {
                return new NetworkTime(mockTickRate, timeSec: time);
            }

            interpolator.AddMeasurement(1f, T(1f));
            interpolator.AddMeasurement(2f, T(2f));
            // message time=3 was lost
            interpolator.AddMeasurement(4f, T(4f));
            interpolator.AddMeasurement(5f, T(5f));

            mockBufferedTime.BufferedServerTime = 2f;
            interpolator.Update(2f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1f));

            mockBufferedTime.BufferedServerTime = 2.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(1.5f));

            mockBufferedTime.BufferedServerTime = 3f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(2f));

            // pausing until buffer reaches next value in buffer, should have been 2.5f, pausing to last value 2f
            mockBufferedTime.BufferedServerTime = 3.5f;
            interpolator.Update(0.5f);
            Assert.That(interpolator.GetInterpolatedValue(), Is.EqualTo(2f));

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
        }
    }
}