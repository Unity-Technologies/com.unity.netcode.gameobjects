using UnityEngine;

namespace Unity.Netcode.Transports.UTP
{
    [RequireComponent(typeof(UnityTransport))]
    public class NetworkSimulator : MonoBehaviour
    {
        [field: SerializeField]
        public NetworkTypeConfiguration NetworkTypeConfiguration { get; set; }

        public void UpdateLiveParameters()
        {
            var transport = GetComponent<UnityTransport>();
            transport.RefreshSimulationPipelineParameters(NetworkTypeConfiguration);
        }
    }
}