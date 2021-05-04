using UnityEngine;

namespace MLAPI.Timing
{
    public interface INetworkTimeProvider
    {
        public bool AdvanceTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime);

        public void InitializeClient(ref NetworkTime predictedTime, ref NetworkTime serverTime);
    }

    public class FixedNetworkTimeProvider : INetworkTimeProvider
    {
        public bool AdvanceTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime)
        {
            predictedTime += deltaTime;
            serverTime += deltaTime;
            return true;
        }

        public void InitializeClient(ref NetworkTime predictedTime, ref NetworkTime serverTime)
        {
            predictedTime = serverTime;
        }
    }

    public class DynamicNetworkTimeProvider : INetworkTimeProvider
    {
        private const float k_HardResetThreshold = 100f / 1000f; // 100ms
        private const float k_CorrectionTolerance = 50f / 1000f; // 50ms
        private const float k_AdjustmentRation = 0.01f;

        private const float k_StartRtt = 150f / 1000f; // 150ms

        private readonly float m_TargetClientBufferTime;
        private readonly float m_TargetServerBufferTime;

        private NetworkTimeSystem m_NetworkTimeSystem;

        private float m_ServerTimeScale;
        private float m_PredictedTimeScale;

        public DynamicNetworkTimeProvider(NetworkTimeSystem networkTimeSystem)
        {
            m_NetworkTimeSystem = networkTimeSystem;
        }
        
        public bool AdvanceTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime)
        {
            float rtt = 100f;

            // advance predicted time
            predictedTime += deltaTime * m_PredictedTimeScale;

            // advance server time
            serverTime += deltaTime * m_ServerTimeScale;

            float timeSinceLastSnapshot = m_NetworkTimeSystem.LastReceivedServerSnapshotTick.FixedTime - serverTime.Time;

            // time scale adjustment based on whether we are behind / ahead of the server in terms of inputs
            // This implementation uses RTT to calculate that without server input which is not ideal. In the future we might want to add a field to the protocol which allows the server to send the exact input buffers size to the client.
            float targetServerTime = timeSinceLastSnapshot + m_TargetClientBufferTime;
            float targetPredictedTime = targetServerTime + rtt + m_TargetServerBufferTime;

            // Reset timescale. Will be recalculated based on new values.
            bool reset = false;
            m_PredictedTimeScale = 1f;
            m_ServerTimeScale = 1f;

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

            // Adjust predicted time
            if (Mathf.Abs(targetPredictedTime - predictedTime.FixedTime) > k_CorrectionTolerance)
            {
                m_PredictedTimeScale += targetPredictedTime > predictedTime.FixedTime ? k_AdjustmentRation : -k_AdjustmentRation;
            }

            // Adjust server time
            if (Mathf.Abs(targetServerTime - serverTime.FixedTime) > k_CorrectionTolerance)
            {
                m_ServerTimeScale += targetServerTime > serverTime.FixedTime ? k_AdjustmentRation : -k_AdjustmentRation;
            }

            return true;
        }

        public void InitializeClient(ref NetworkTime predictedTime, ref NetworkTime serverTime)
        {
            serverTime += m_TargetClientBufferTime;
            predictedTime = serverTime;
            predictedTime += k_StartRtt;
        }
    }
}
