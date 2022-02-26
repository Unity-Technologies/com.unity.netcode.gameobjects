#if MULTIPLAYER_TOOLS
using UnityEngine;

namespace Unity.Netcode.TestHelpers.Runtime.Metrics
{
    public class NetworkVariableComponent : NetworkBehaviour
    {
        public NetworkVariable<int> MyNetworkVariable { get; } = new NetworkVariable<int>();

        private void Update()
        {
            if (IsServer)
            {
                MyNetworkVariable.Value = Random.Range(100, 999);
            }
        }
    }
}
#endif
