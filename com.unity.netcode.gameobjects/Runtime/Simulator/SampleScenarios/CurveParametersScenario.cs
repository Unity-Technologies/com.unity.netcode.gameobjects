using System;
using UnityEngine;

namespace Unity.Netcode.SampleScenarios
{
    [Serializable]
    public class CurveParametersScenario : INetworkSimulatorScenarioUpdateHandler
    {
        [field: SerializeField] public float LoopDuration { get; set; }
        [field: SerializeField] public AnimationCurve PacketDelayMs { get; set; }
        [field: SerializeField] public AnimationCurve PacketJitterMs { get; set; }
        [field: SerializeField] public AnimationCurve PacketLossInterval { get; set; }
        [field: SerializeField] public AnimationCurve PacketLossPercent { get; set; }
        [field: SerializeField] public AnimationCurve PacketDuplicationPercent { get; set; }

        readonly INetworkSimulatorConfiguration m_Configuration = NetworkSimulatorConfiguration.Create("Curve Parameters");
        INetworkEventsApi m_NetworkEventsApi;
        float m_TimeElapsed;
    
        public void Start(INetworkEventsApi networkEventsApi)
        {
            m_NetworkEventsApi = networkEventsApi;
            UpdateParameters();
        }
            
        public void Dispose()
        {
        }
            
        public void Update(float deltaTime)
        {
            m_TimeElapsed += deltaTime;
            
            if (m_TimeElapsed >= LoopDuration)
            {
                m_TimeElapsed -= LoopDuration;
            }
    
            UpdateParameters();
        }
        
        void UpdateParameters()
        {
            var progress = m_TimeElapsed / LoopDuration;
            m_Configuration.PacketDelayMs = (int)PacketDelayMs.Evaluate(progress);
            m_Configuration.PacketJitterMs = (int)PacketJitterMs.Evaluate(progress);
            m_Configuration.PacketLossInterval = (int)PacketLossInterval.Evaluate(progress);
            m_Configuration.PacketLossPercent = (int)PacketLossPercent.Evaluate(progress);
            m_Configuration.PacketDuplicationPercent = (int)PacketDuplicationPercent.Evaluate(progress);
            m_NetworkEventsApi.ChangeNetworkType(m_Configuration);
        }
    }
}
