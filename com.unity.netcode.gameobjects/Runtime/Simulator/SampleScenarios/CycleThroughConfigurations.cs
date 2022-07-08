using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Netcode.SampleScenarios
{
    [UsedImplicitly]
    public class CycleThroughConfigurations : INetworkSimulatorScenarioUpdateHandler
    {
        [Serializable]
        public class NetworkTypeByTime
        {
            public NetworkSimulatorConfigurationObject m_ConfigurationObject;
            public float Duration;
        }

        INetworkEventsApi m_NetworkEventsApi;
        float m_TimeElapsed;
        int m_CurrentIndex;
        NetworkTypeByTime m_CurrentType;

        [field: SerializeField]
        NetworkTypeByTime[] NetworkTypeByTimeList { get; set; }
        
        int CurrentIndex
        {
            get => m_CurrentIndex;
            set => m_CurrentIndex = value >= NetworkTypeByTimeList.Length
                ? value % NetworkTypeByTimeList.Length
                : value;
        }
    
        public void Start(INetworkEventsApi networkEventsApi)
        {
            m_NetworkEventsApi = networkEventsApi;
            IterateConnectionType();
        }
            
        public void Dispose()
        {
        }
            
        public void Update(float deltaTime)
        {
            m_TimeElapsed += deltaTime;
            if (m_TimeElapsed < m_CurrentType.Duration)
            {
                return;
            }
    
            m_TimeElapsed -= m_CurrentType.Duration;
            IterateConnectionType();
        }
        
        void IterateConnectionType()
        {
            m_CurrentType = NetworkTypeByTimeList[CurrentIndex++];
            var connectionType = m_CurrentType.m_ConfigurationObject;
            m_NetworkEventsApi.ChangeNetworkType(connectionType);
            Debug.Log($"Changed to: {connectionType.Name}");
        }
    }
}
