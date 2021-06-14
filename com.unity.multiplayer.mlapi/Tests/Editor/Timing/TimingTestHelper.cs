using System;
using System.Collections.Generic;
using MLAPI.Timing;
using UnityEngine;
using Random = System.Random;

namespace MLAPI.EditorTests.Timing
{
    /// <summary>
    /// Helper functions for timing related tests. Allows to get a set of time steps and simulate time advancing without the need of a full playmode test.
    /// </summary>
    public static class TimingTestHelper
    {
        public static List<float> GetRandomTimeSteps(float totalDuration, float min, float max, int seed)
        {
            var random = new Random(seed);
            List<float> steps = new List<float>();

            while (totalDuration > 0f)
            {
                var next = Mathf.Lerp(min, max, (float)random.NextDouble());
                steps.Add(next);
                totalDuration -= next;
            }

            // correct overshoot at the end
            steps[steps.Count - 1] -= totalDuration;

            return steps;
        }

        public delegate void StepCheckDelegate(int step);

        public delegate void StepCheckResetDelegate(int step, bool reset);

        public static void ApplySteps(INetworkTimeProvider timeProvider, List<float> steps, ref NetworkTime localTime, ref NetworkTime serverTime, StepCheckDelegate stepCheck = null)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                timeProvider.AdvanceTime(ref localTime, ref serverTime, step);
                if (stepCheck != null)
                {
                    stepCheck(i);
                }
            }
        }

        public static void ApplySteps(INetworkTimeProvider timeProvider, List<float> steps, ref NetworkTime localTime, ref NetworkTime serverTime, StepCheckResetDelegate stepCheck = null)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var reset = timeProvider.AdvanceTime(ref localTime, ref serverTime, step);
                if (stepCheck != null)
                {
                    stepCheck(i, reset);
                }
            }
        }
    }

    public class DummyNetworkStats: INetworkStats
    {
        public float Rtt { get; set; }

        public NetworkTime LastReceivedSnapshotTick { get; set; }
    }

}

