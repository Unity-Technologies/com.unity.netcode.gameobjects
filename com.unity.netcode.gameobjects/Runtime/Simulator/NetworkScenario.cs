using UnityEngine;

namespace Unity.Netcode
{
    public class NetworkScenario : MonoBehaviour
    {
        // TODO: Define this with Ben
        [field: SerializeField]
        public INetworkSimulatorScenario NetworkSimulatorScenario { get; set; }
    }
}
