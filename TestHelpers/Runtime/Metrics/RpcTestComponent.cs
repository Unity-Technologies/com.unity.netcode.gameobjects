using System;

namespace Unity.Netcode.TestHelpers.Runtime.Metrics
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
        public void MyClientRpc(ClientRpcParams rpcParams = default)
        {
            OnClientRpcAction?.Invoke();
        }
    }
}
