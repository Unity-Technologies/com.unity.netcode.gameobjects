using MLAPI;
using UnityEngine;

namespace MLAPI_Examples
{
    // Note that the script invoking doesn't have to be a NetworkedBehaviour
    public class RunRpcOnOtherGameObject : MonoBehaviour
    {
        public ConvenienceMessagingPing Target;
        
        public void OnEnable()
        {
            // Note this is NOT good practice. Don't rely on time to make sure the object is ready to be sent
            Invoke(nameof(ExecuteRemoteRpc), 5f);
        }

        private void ExecuteRemoteRpc()
        {
            // Long version of IsServer
            if (Target != null && NetworkingManager.Singleton.IsClient)
            {
                int rnd = Random.Range(0, int.MaxValue);
                
                Debug.LogFormat("Pining server with number {0}", rnd);
                
                // Note that the InvokeServerRPC has to be called on the same behaviour as the actual RPC method
                Target.InvokeServerRpc(Target.PingServer, rnd);
            }
        }
    }
}