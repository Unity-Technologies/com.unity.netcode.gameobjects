using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Netcode.SampleScenarios
{
    [UsedImplicitly]
    public class NoOpScenario : NetworkSimulatorScenarioTask
    {
        protected override Task Run(INetworkEventsApi networkEventsApi, CancellationToken _)
        {
            return Task.CompletedTask;
        }
    }
    
    [UsedImplicitly]
    public class RandomlySwitchConnectionType : NetworkSimulatorScenarioTask
    {
        [field: SerializeField]
        float ChangeInterval { get; set; }
            
        protected override async Task Run(INetworkEventsApi networkEventsApi, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await Task.Delay(TimeSpan.FromSeconds(ChangeInterval), cancellationToken);
                var index = Random.Range(0, NetworkTypePresets.Values.Length);
                var connectionType = NetworkTypePresets.Values[index];
                networkEventsApi.ChangeNetworkType(connectionType);
                Debug.Log($"Changed to: {connectionType.Name}");
            }
        }
    }
        
    [UsedImplicitly]
    public class CycleThroughConnectionPresets : INetworkSimulatorScenarioUpdateHandler
    {
        [Serializable]
        public class NetworkTypeByTime
        {
            [HideInInspector]
            public string Name;
            public float Duration;
        }
            
        [field: SerializeReference]
        NetworkTypeByTime[] NetworkTypeByTimeList { get; set; }
    
        INetworkEventsApi m_NetworkEventsApi;
        NetworkTypeByTime m_CurrentType;
        int m_CurrentIndex;
        float m_TimeElapsed;
    
        int CurrentIndex
        {
            get => m_CurrentIndex;
            set => m_CurrentIndex = value < NetworkTypeByTimeList.Length ? value : 0;
        }
    
        public CycleThroughConnectionPresets()
        {
            NetworkTypeByTimeList = new NetworkTypeByTime[]
            {
                new() { Name = nameof(NetworkTypePresets.None), Duration = 0 },
                new() { Name = nameof(NetworkTypePresets.HomeBroadband), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile2G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile2_5G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile2_75G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile3G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile3_5G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile3_75G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile4G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile4_5G), Duration = 1 },
                new() { Name = nameof(NetworkTypePresets.Mobile5G), Duration = 1 },
            };
        }
            
        public void Start(INetworkEventsApi networkEventsApi)
        {
            m_NetworkEventsApi = networkEventsApi;
            var connectionType = FindNextConnectionType();
            m_NetworkEventsApi.ChangeNetworkType(connectionType);
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
    
            var connectionType = FindNextConnectionType();
            m_NetworkEventsApi.ChangeNetworkType(connectionType);
            Debug.Log($"Changed to: {connectionType.Name}");
        }
        
        NetworkSimulatorConfiguration FindNextConnectionType()
        {
            do
            {
                m_CurrentType = NetworkTypeByTimeList[++CurrentIndex];
            }
            while (m_CurrentType.Duration == 0);
    
            return NetworkTypePresets.Values[CurrentIndex];
        }
    }
}
