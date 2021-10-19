using System;
using Unity.Netcode;
using NUnit.Framework;
using UnityEngine;

namespace TestProject.RuntimeTests.Support
{
    public class ShutdownDuringOnNetworkSpawnBehaviour : NetworkBehaviour
    {
        public static int SpawnCount = 0;
        
        public override void OnNetworkSpawn()
        {
            ++SpawnCount;
            if (!IsServer)
            {
                NetworkManager.Shutdown();
                return;
            }

            TestClientRpc();
            Destroy(gameObject);
        }

        [ClientRpc]
        private void TestClientRpc()
        {
        }
    }
}
