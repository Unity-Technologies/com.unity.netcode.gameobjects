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
        float ChangeInterval { get; set; } = 5f;
            
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

        INetworkEventsApi m_NetworkEventsApi;
        float m_TimeElapsed;
        int m_CurrentIndex;

        [field: SerializeReference]
        NetworkTypeByTime[] NetworkTypeByTimeList { get; set; } =
        {
            new() { Name = nameof(NetworkTypePresets.None), Duration = 0 },
            new() { Name = nameof(NetworkTypePresets.HomeBroadband), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile2G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile2_5G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile2_75G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile3G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile3_5G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile3_75G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile4G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile4_5G), Duration = 5f },
            new() { Name = nameof(NetworkTypePresets.Mobile5G), Duration = 5f }
        };

        NetworkTypeByTime CurrentType => NetworkTypeByTimeList[CurrentIndex];
    
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
            if (m_TimeElapsed < CurrentType.Duration)
            {
                return;
            }
    
            m_TimeElapsed -= CurrentType.Duration;
            IterateConnectionType();
        }
        
        void IterateConnectionType()
        {
            while (CurrentType.Duration == 0)
            {
                CurrentIndex++;
            }
    
            var connectionType = NetworkTypePresets.Values[CurrentIndex++];
            m_NetworkEventsApi.ChangeNetworkType(connectionType);
            Debug.Log($"Changed to: {connectionType.Name}");
        }
    }
}
