#if MULTIPLAYER_TOOLS
using System;
using MLAPI.NetworkVariable;

namespace MLAPI.RuntimeTests.Metrics.Utility
{
    public class NetworkVariableComponent : NetworkBehaviour
    {
        public NetworkVariableString MyNetworkVariable { get; } = new NetworkVariableString();

        void Update()
        {
            if (IsServer)
            {
                MyNetworkVariable.Value = Guid.NewGuid().ToString();
            }
        }
    }
}
#endif
