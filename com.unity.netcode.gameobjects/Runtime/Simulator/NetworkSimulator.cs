using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode
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

        public void OnValidate()
        {
            Debug.Log("Value has changed");
        }
    }
}
