#if MULTIPLAYER_TOOLS
using UnityEngine;

namespace Unity.Netcode.RuntimeTests.Metrics.Utlity
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
