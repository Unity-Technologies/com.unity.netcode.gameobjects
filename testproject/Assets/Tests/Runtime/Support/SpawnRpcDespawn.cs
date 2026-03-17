using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.RuntimeTests.Support
{
    public class SpawnRpcDespawn : NetworkBehaviour, INetworkUpdateSystem
    {
        public static bool VerboseLogging;
        private static NetworkUpdateStage s_TestStage;
        public static NetworkUpdateStage TestStage
        {
            get { return s_TestStage; }
            set
            {
                s_TestStage = value;
            }
        }
        public static int ClientUpdateCount;
        public static int ServerUpdateCount;
        public static bool ClientNetworkSpawnRpcCalled;
        public static bool ExecuteClientRpc;
        public static bool ShutdownInClientRpc;
        public static NetworkUpdateStage StageExecutedByReceiver;

        private bool m_Active = false;

        private void Log(string header, string msg)
        {
            if (!VerboseLogging)
            {
                return;
            }
            Debug.Log($"[{nameof(SpawnRpcDespawn)}][Client-{NetworkManager.LocalClientId}]{header} {msg}");
        }

        private void Log(string msg)
        {
            if (!VerboseLogging)
            {
                return;
            }
            Log(string.Empty, msg);
        }

        [ClientRpc]
        public void SendIncrementUpdateCountClientRpc()
        {
            Assert.AreEqual(NetworkUpdateStage.EarlyUpdate, NetworkUpdateLoop.UpdateStage);

            StageExecutedByReceiver = NetworkUpdateLoop.UpdateStage;
            ++ClientUpdateCount;
            Log($"Client RPC executed at {NetworkUpdateLoop.UpdateStage}; client count to {ClientUpdateCount.ToString()}");
        }

        public void IncrementUpdateCount()
        {
            ++ServerUpdateCount;
            Log($"Server count to {ServerUpdateCount.ToString()}");
            SendIncrementUpdateCountClientRpc();
        }

        public void Activate()
        {
            Log("Activated");
            m_Active = true;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                Log("Client instance spawning!");
                // Asserting that the RPC is not called before OnNetworkSpawn
                Assert.IsFalse(ClientNetworkSpawnRpcCalled);
                return;
            }
            Log($"[Should execute: {ExecuteClientRpc}]", "Server instance spawning");
            if (ExecuteClientRpc)
            {
                Log($"[Executing]", $"Server invoking {nameof(ClientTestRpc)}.");
                ClientTestRpc();
            }
        }

        [Rpc(SendTo.NotMe)]
        private void ClientTestRpc()
        {
            Log($"Received {nameof(ClientTestRpc)} message and processed it!");
            ClientNetworkSpawnRpcCalled = true;
            if (ShutdownInClientRpc)
            {
                NetworkManager.Shutdown();
            }
        }

        protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            NetworkUpdateLoop.RegisterAllNetworkUpdates(this);
            base.OnNetworkPreSpawn(ref networkManager);
        }

        public override void OnDestroy()
        {
            NetworkUpdateLoop.UnregisterAllNetworkUpdates(this);
            base.OnDestroy();
        }

        private void RunTest()
        {
            Debug.Log("Running test...");
            IncrementUpdateCount();
            Destroy(gameObject);
            m_Active = false;
        }

        public void NetworkUpdate(NetworkUpdateStage stage)
        {
            if (IsServer && m_Active && stage == TestStage)
            {
                RunTest();
            }
        }
    }
}
