using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that <see cref="NetworkTransform.GetTickLatencyInSeconds()"/> returns what it is documented to
    /// return: the tick latency as a duration in seconds.
    /// </summary>
    /// <remarks>
    /// It previously returned <c>TimeTicksAgo(...).Time</c>, which is an absolute network timestamp rather than a
    /// duration, so the value grew for as long as the session ran.
    /// </remarks>
    internal class NetworkTransformTickLatencyTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        // Ticks of additional buffering applied part way through the test to confirm the returned duration
        // tracks the tick latency it is derived from.
        private const int k_AddedBufferTicks = 3;

        // Seconds of tolerance when comparing against the expected duration.
        private const float k_Tolerance = 0.0005f;

        // The number of samples taken while the session runs, to confirm the value does not drift with time.
        private const int k_Samples = 30;

        private int m_OriginalBufferTickOffset;

        protected override IEnumerator OnSetup()
        {
            m_OriginalBufferTickOffset = NetworkTransform.InterpolationBufferTickOffset;
            return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            // This is static, so leaving it modified would leak into every test that runs afterwards.
            NetworkTransform.InterpolationBufferTickOffset = m_OriginalBufferTickOffset;
            return base.OnTearDown();
        }

        private static float GetExpectedLatencyInSeconds(NetworkManager networkManager)
        {
            var ticksBehind = networkManager.NetworkTimeSystem.TickLatency + NetworkTransform.InterpolationBufferTickOffset;
            return (float)(ticksBehind * networkManager.ServerTime.FixedDeltaTimeAsDouble);
        }

        [UnityTest]
        public IEnumerator GetTickLatencyInSecondsReturnsADuration()
        {
            var client = m_ClientNetworkManagers[0];

            // Sample repeatedly while the session clock advances. A duration tracks the tick latency, where an
            // absolute timestamp would climb by roughly one second per second.
            //
            // NetworkTimeSystem.TickLatency is adaptive and can legitimately change mid-run, so the value is
            // only held to being unchanged across samples where the tick latency itself did not change.
            var previousTicksBehind = -1;
            var previousValue = 0f;
            for (int i = 0; i < k_Samples; i++)
            {
                var ticksBehind = client.NetworkTimeSystem.TickLatency + NetworkTransform.InterpolationBufferTickOffset;
                var expected = GetExpectedLatencyInSeconds(client);
                var actual = NetworkTransform.GetTickLatencyInSeconds(client);

                Assert.AreEqual(expected, actual, k_Tolerance,
                    $"Expected the tick latency to be {expected}s but it was {actual}s.");

                if (ticksBehind == previousTicksBehind)
                {
                    Assert.AreEqual(previousValue, actual, k_Tolerance,
                        $"The reported latency moved from {previousValue}s to {actual}s while the tick latency " +
                        $"stayed at {ticksBehind} ticks, so it is tracking elapsed time rather than latency.");
                }

                previousTicksBehind = ticksBehind;
                previousValue = actual;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator GetTickLatencyInSecondsTracksTheBufferTickOffset()
        {
            var client = m_ClientNetworkManagers[0];
            var tickInterval = (float)client.ServerTime.FixedDeltaTimeAsDouble;

            var latencyBefore = client.NetworkTimeSystem.TickLatency;
            var before = NetworkTransform.GetTickLatencyInSeconds(client);

            // Buffering more ticks has to lengthen the reported duration by exactly those ticks.
            NetworkTransform.InterpolationBufferTickOffset = m_OriginalBufferTickOffset + k_AddedBufferTicks;
            yield return null;

            var latencyAfter = client.NetworkTimeSystem.TickLatency;
            var after = NetworkTransform.GetTickLatencyInSeconds(client);

            // The adaptive tick latency may also have moved in between, so only the buffering is held to an
            // exact figure.
            var expectedIncrease = (k_AddedBufferTicks + (latencyAfter - latencyBefore)) * tickInterval;
            Assert.AreEqual(expectedIncrease, after - before, k_Tolerance,
                $"Adding {k_AddedBufferTicks} ticks of buffering changed the reported latency by " +
                $"{after - before}s when a tick is {tickInterval}s, so it should have changed by " +
                $"{expectedIncrease}s (tick latency went from {latencyBefore} to {latencyAfter}).");
        }
    }
}
