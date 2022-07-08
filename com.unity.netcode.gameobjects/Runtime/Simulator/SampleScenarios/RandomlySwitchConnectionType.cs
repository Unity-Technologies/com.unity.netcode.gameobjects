using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Netcode.SampleScenarios
{
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
}
