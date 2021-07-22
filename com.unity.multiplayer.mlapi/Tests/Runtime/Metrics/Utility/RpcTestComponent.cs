using System;
using MLAPI.Messaging;

namespace MLAPI.RuntimeTests.Metrics.Utility
{
    public class RpcTestComponent : NetworkBehaviour
    {
        public event Action OnServerRpcAction;
        public event Action OnClientRpcAction;

        [ServerRpc]
        public void MyServerRpc()
        {
            OnServerRpcAction?.Invoke();
        }

        [ClientRpc]
        public void MyClientRpc()
        {
            OnClientRpcAction?.Invoke();
        }
    }
}