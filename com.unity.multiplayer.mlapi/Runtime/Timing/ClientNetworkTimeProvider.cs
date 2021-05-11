using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Timing
{
    public class ClientNetworkTimeProvider : INetworkTimeProvider
    {
        private const float k_HardResetThreshold = 200f / 1000f; // 200ms
        private const float k_CorrectionTolerance = 20f / 1000f; // 20ms
        private const float k_AdjustmentRatio = 0.01f;

        private const float k_StartRtt = 150f / 1000f; // 150ms

        public float ServerTimeScale { get; private set; } = 1f;
        public float PredictedTimeScale { get; private set; } = 1f;

        private INetworkStats m_NetworkStats;

        public float TargetClientBufferTime { get; set; }
        public float TargetServerBufferTime { get; set; }

        public ClientNetworkTimeProvider(INetworkStats networkStats, int tickRate)
        {
            m_NetworkStats = networkStats;

            // default buffer values. Can be manually overriden.
            TargetClientBufferTime = 1f / tickRate;
            TargetServerBufferTime = 2f / tickRate;
        }

        public bool AdvanceTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime)
        {
            // advance time
            predictedTime += deltaTime * PredictedTimeScale;
            serverTime += deltaTime * ServerTimeScale;

            // time scale adjustment based on whether we are behind / ahead of the server in terms of inputs
            // This implementation uses RTT to calculate that without server input which is not ideal. In the future we might want to add a field to the protocol which allows the server to send the exact input buffers size to the client.
            float lastReceivedSnapshotTime = m_NetworkStats.GetLastReceivedSnapshotTick().Time;
            float targetServerTime = lastReceivedSnapshotTime - TargetClientBufferTime;
            float targetPredictedTime = lastReceivedSnapshotTime + m_NetworkStats.GetRtt() + TargetServerBufferTime;

            // Reset timescale. Will be recalculated based on new values.
            bool reset = false;
            PredictedTimeScale = 1f;
            ServerTimeScale = 1f;

            // reset because too large predicted offset?
            if (predictedTime.FixedTime < targetPredictedTime - k_HardResetThreshold || predictedTime.FixedTime > targetPredictedTime + k_HardResetThreshold)
            {
                reset = true;
            }

            // reset because too large server offset?
            if (serverTime.FixedTime < targetServerTime - k_HardResetThreshold || serverTime.FixedTime > targetServerTime + k_HardResetThreshold)
            {
                reset = true;
            }

            // Always reset both times to not break simulation integrity.
            if (reset)
            {
                predictedTime = new NetworkTime(predictedTime.TickRate, targetPredictedTime);
                serverTime = new NetworkTime(serverTime.TickRate, targetServerTime);
                return false;
            }

            // Adjust predicted time scale
            if (Mathf.Abs(targetPredictedTime - predictedTime.FixedTime) > k_CorrectionTolerance)
            {
                PredictedTimeScale += targetPredictedTime > predictedTime.FixedTime ? k_AdjustmentRatio : -k_AdjustmentRatio;
            }

            // Adjust server time scale
            if (Mathf.Abs(targetServerTime - serverTime.FixedTime) > k_CorrectionTolerance)
            {
                ServerTimeScale += targetServerTime > serverTime.FixedTime ? k_AdjustmentRatio : -k_AdjustmentRatio;
            }

            return true;
        }

        public void InitializeClient(ref NetworkTime predictedTime, ref NetworkTime serverTime)
        {
            serverTime += TargetClientBufferTime;
            predictedTime = serverTime;
            predictedTime += k_StartRtt;
        }
    }
}
