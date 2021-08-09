using System;

namespace Unity.Netcode.RuntimeTests.Metrics.Utlity
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
