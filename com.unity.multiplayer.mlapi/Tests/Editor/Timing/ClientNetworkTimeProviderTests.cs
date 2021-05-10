using System;
using MLAPI.Timing;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.EditorTests.Timing
{
    public class ClientNetworkTimeProviderTests
    {
        private const float k_AcceptableRttOffset = 0.03f; // 30ms offset is fine

        [Test]
        public void InitializeClientTest()
        {
            NetworkTime serverTime = new NetworkTime(60);
            NetworkTime predictedTime = new NetworkTime(60);

            var clientNetworkTimeProvider = new ClientNetworkTimeProvider(new DummyNetworkStats(), 60);
            clientNetworkTimeProvider.InitializeClient(ref predictedTime, ref serverTime);

            Assert.IsTrue(serverTime.Time > 0f);
            Assert.IsTrue(predictedTime.Time > serverTime.Time);
        }

        /// <summary>
        /// Tests whether time is stable if RTT is stable
        /// </summary>
        [Test]
        public void StableRttTest()
        {
            NetworkTime serverTime = new NetworkTime(60);
            NetworkTime predictedTime = new NetworkTime(60);

            var networkStats = new DummyNetworkStats() { Rtt = 0.1f, LastReceivedSnapshotTick = serverTime };

            var clientNetworkTimeProvider = new ClientNetworkTimeProvider(networkStats, 60);
            clientNetworkTimeProvider.InitializeClient(ref predictedTime, ref serverTime);

            var steps = TimingTestHelper.GetRandomTimeSteps(100f, 0.01f, 0.1f, 42);
            var rttSteps = TimingTestHelper.GetRandomTimeSteps(1000f, 0.095f, 0.105f, 42); // 10ms jitter

            // run for a while so that we reach regular RTT offset
            TimingTestHelper.ApplySteps(clientNetworkTimeProvider, steps, ref predictedTime, ref serverTime, delegate(int step)
            {
                // increase last received server tick
                networkStats.LastReceivedSnapshotTick = networkStats.LastReceivedSnapshotTick + steps[step];

                // Update RTT
                networkStats.Rtt = rttSteps[step];

                // if (step % 50 == 0)
                // {
                //     Debug.Log( (predictedTime - serverTime ).Time - 0.1f - clientNetworkTimeProvider.TargetServerBufferTime);
                // }
            });

            // check how we close we are to target time.
            var offsetToTarget = (predictedTime - serverTime ).Time - 0.1f - clientNetworkTimeProvider.TargetServerBufferTime;
            Assert.IsTrue(offsetToTarget < k_AcceptableRttOffset);
            Debug.Log($"offset to target time after running for a while: {offsetToTarget}");

            // run again, test that we never need to speed up or slow down under stable RTT
            TimingTestHelper.ApplySteps(clientNetworkTimeProvider, steps, ref predictedTime, ref serverTime, delegate(int step)
            {
                networkStats.LastReceivedSnapshotTick = networkStats.LastReceivedSnapshotTick + steps[step];
                networkStats.Rtt = rttSteps[step];
            });

            // check again to ensure we are still close to the target
            var newOffsetToTarget = (predictedTime - serverTime ).Time - 0.1f - clientNetworkTimeProvider.TargetServerBufferTime;
            Assert.IsTrue(newOffsetToTarget < k_AcceptableRttOffset);
            Debug.Log($"offset to target time after running longer: {newOffsetToTarget}");

            // difference between first and second offset should be minimal
            var dif = offsetToTarget - newOffsetToTarget;
            Assert.IsTrue(Mathf.Abs(dif) < 0.01f); // less than 10ms

        }

        [Test]
        public void RttCatchupSlowdownTest()
        {
            NetworkTime serverTime = new NetworkTime(60);
            NetworkTime predictedTime = new NetworkTime(60);

            var networkStats = new DummyNetworkStats() { Rtt = 0.1f, LastReceivedSnapshotTick = serverTime };

            var clientNetworkTimeProvider = new ClientNetworkTimeProvider(networkStats, 60);
            clientNetworkTimeProvider.InitializeClient(ref predictedTime, ref serverTime);

            var steps = TimingTestHelper.GetRandomTimeSteps(100f, 0.01f, 0.1f, 42);
            var rttSteps = TimingTestHelper.GetRandomTimeSteps(1000f, 0.095f, 0.105f, 42); // 10ms jitter

            // run for a while so that we reach regular RTT offset
            TimingTestHelper.ApplySteps(clientNetworkTimeProvider, steps, ref predictedTime, ref serverTime, delegate(int step)
            {
                networkStats.LastReceivedSnapshotTick = networkStats.LastReceivedSnapshotTick + steps[step];
                networkStats.Rtt = rttSteps[step];
            });

            // increase RTT to ~200ms from ~100ms
            var rttSteps2 = TimingTestHelper.GetRandomTimeSteps(1000f, 0.195f, 0.205f, 42);

            // we run again and check how much speed up is done. In theory this should be around 0.1f at the end because the predicted time is trying to catch up.
            float totalPredictedSpeedUpTime = 0f;

            TimingTestHelper.ApplySteps(clientNetworkTimeProvider, steps, ref predictedTime, ref serverTime, delegate(int step)
            {
                networkStats.LastReceivedSnapshotTick = networkStats.LastReceivedSnapshotTick + steps[step];
                networkStats.Rtt = rttSteps2[step]; // note; uses new rtt steps

                if (step < steps.Count - 2)
                {
                    totalPredictedSpeedUpTime += (clientNetworkTimeProvider.PredictedTimeScale - 1f) * steps[step + 1]; // +1 because the scale will be applied to the next time
                }
            });

            // speed up of 0.1f expected
            Assert.True(Mathf.Abs(totalPredictedSpeedUpTime - 0.1f) < k_AcceptableRttOffset);
            Debug.Log($"Total predicted speed up time catch up: {totalPredictedSpeedUpTime}");

            // run again with RTT ~100ms and see whether we slow down by -0.1f
            totalPredictedSpeedUpTime = 0f;

            TimingTestHelper.ApplySteps(clientNetworkTimeProvider, steps, ref predictedTime, ref serverTime, delegate(int step)
            {
                networkStats.LastReceivedSnapshotTick = networkStats.LastReceivedSnapshotTick + steps[step];
                networkStats.Rtt = rttSteps[step];

                if (step < steps.Count - 2)
                {
                    totalPredictedSpeedUpTime += (clientNetworkTimeProvider.PredictedTimeScale - 1f) * steps[step + 1]; // +1 because the scale will be applied to the next time
                }
            });

            // slow down of 0.1f expected
            Assert.True(Mathf.Abs(totalPredictedSpeedUpTime + 0.1f) < k_AcceptableRttOffset);
            Debug.Log($"Total predicted speed up time slow down: {totalPredictedSpeedUpTime}");

        }

    }
}
