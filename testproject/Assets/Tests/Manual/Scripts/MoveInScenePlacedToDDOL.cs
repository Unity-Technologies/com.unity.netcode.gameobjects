using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    public class MoveInScenePlacedToDDOL : NetworkBehaviour
    {
        public bool ProcessedRpc { get; private set; }

        private void Awake()
        {
            ProcessedRpc = false;
            var networkObject = GetComponent<NetworkObject>();
            Debug.Log($"[{name}][Moving to DDOL] InScenePlaced: {networkObject.InScenePlaced}");
            DontDestroyOnLoad(gameObject);
        }

        protected override void OnNetworkPostSpawn()
        {
            if (HasAuthority)
            {
                SendOnSpawnRpc();
            }

            base.OnNetworkPostSpawn();
        }

        [Rpc(SendTo.Everyone)]
        private void SendOnSpawnRpc(RpcParams rpcParams = default)
        {
            ProcessedRpc = true;
        }
    }
}
