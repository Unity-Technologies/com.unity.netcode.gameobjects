using System;
using System.Collections.Generic;
using MLAPI.Timing;
using UnityEngine;
using Random = System.Random;

namespace MLAPI.EditorTests.Timing
{
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

        public static void ApplySteps(INetworkTimeProvider timeProvider, List<float> steps, ref NetworkTime predictedTime, ref NetworkTime serverTime, StepCheckDelegate stepCheck = null)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var noReset = timeProvider.AdvanceTime(ref predictedTime, ref serverTime, step);
                if (noReset == false)
                {
                    Debug.Log($"step: {i} hard reset");
                }
                if (stepCheck != null)
                {
                    stepCheck(i);
                }
            }
        }
    }

    public class DummyNetworkStats: INetworkStats
    {
        public float Rtt { get; set; }

        public NetworkTime LastReceivedSnapshotTick { get; set; }

        public float GetRtt()
        {
            return Rtt;
        }

        public NetworkTime GetLastReceivedSnapshotTick()
        {
            return LastReceivedSnapshotTick.ToFixedTime();
        }
    }

}

