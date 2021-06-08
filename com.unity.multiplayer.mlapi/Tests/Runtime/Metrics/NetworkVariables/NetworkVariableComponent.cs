using System;
using MLAPI.NetworkVariable;

namespace MLAPI.RuntimeTests.Metrics.NetworkVariables
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