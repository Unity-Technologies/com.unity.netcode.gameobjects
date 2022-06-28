using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Netcode.SampleScenarios
{
    public class NetworkSimulatorConfigurationProvider : MonoBehaviour, INetworkSimulatorConfiguration
    {
        [field: SerializeField] NetworkSimulator NetworkSimulator { get; set; }
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public string Description { get; set; }
        [field: SerializeField] public int PacketDelayMs { get; set; }
        [field: SerializeField] public int PacketJitterMs { get; set; }
        [field: SerializeField] public int PacketLossInterval { get; set; }
        [field: SerializeField] public int PacketLossPercent { get; set; }
        [field: SerializeField] public int PacketDuplicationPercent { get; set; }

        void Start()
        {
            if (NetworkSimulator == null)
            {
                NetworkSimulator = GetComponent<NetworkSimulator>();
            }

            AssertNetworkSimulatorIsNotNull();
        }

        [UsedImplicitly]
        public void ChangeConnectionPresetByName(string presetName)
        {
            AssertNetworkSimulatorIsNotNull();

            var networkTypeConfiguration = NetworkTypePresets.Values.FirstOrDefault(configuration => configuration.name == presetName);
            if (networkTypeConfiguration == null)
            {
                Debug.LogWarning($"No preset found for the provided name: \"{presetName}\"");
                return;
            }

            NetworkSimulator.NetworkEventsApi.ChangeNetworkType(networkTypeConfiguration);
        }

        [UsedImplicitly]
        public void ChangeConnectionPresetByIndex(int index)
        {
            AssertNetworkSimulatorIsNotNull();

            var length = NetworkTypePresets.Values.Length;
            if (index < 0 || index >= length)
            {
                Debug.LogWarning($"No preset found for the provided index: \"{index}\"");
            }

            var networkTypeConfiguration = NetworkTypePresets.Values[index];
            NetworkSimulator.NetworkEventsApi.ChangeNetworkType(networkTypeConfiguration);
        }

        void Update()
        {
            if (NetworkSimulator == null)
            {
                return;
            }

            NetworkSimulator.NetworkEventsApi.ChangeNetworkType(this);
        }

        void AssertNetworkSimulatorIsNotNull()
        {
            Debug.Assert(NetworkSimulator != null, $"You must reference a valid NetworkSimulator to the \"{nameof(NetworkSimulatorConfigurationProvider)}\" script.", this);
        }
    }
}
