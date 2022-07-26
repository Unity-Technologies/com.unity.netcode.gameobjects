using Unity.Netcode;

namespace TestProject.RuntimeTests.Support
{
    public class ShutdownDuringOnNetworkSpawnBehaviour : NetworkBehaviour
    {
        public static int SpawnCount = 0;
        public static int ClientRpcsCalled = 0;
        public static int ServerRpcsCalled = 0;
        public static bool ShutdownImmediately = false;

        public override void OnNetworkSpawn()
        {
            ++SpawnCount;
            if (!IsServer)
            {
                NetworkManager.Shutdown(ShutdownImmediately);
                TestServerRpc();
            }
            else
            {
                TestClientRpc();
            }
        }

        [ClientRpc]
        private void TestClientRpc()
        {
            ++ClientRpcsCalled;
        }

        [ServerRpc(RequireOwnership = false)]
        private void TestServerRpc()
        {
            ++ServerRpcsCalled;
        }
    }
}
