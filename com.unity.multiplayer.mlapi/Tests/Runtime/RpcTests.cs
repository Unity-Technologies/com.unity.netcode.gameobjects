using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode.RuntimeTests
{
    public class RpcTests : BaseMultiInstanceTest
    {
        public class RpcTestNB : NetworkBehaviour
        {
            public event Action OnServer_Rpc;
            public event Action OnClient_Rpc;

            [ServerRpc]
            public void MyServerRpc()
            {
                OnServer_Rpc();
            }

            [ClientRpc]
            public void MyClientRpc()
            {
                OnClient_Rpc();
            }
        }

        protected override int NbClients => 1;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, playerPrefab =>
            {
                playerPrefab.AddComponent<RpcTestNB>();
            });
        }

        [UnityTest]
        public IEnumerator TestRpcs()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));

            // Setup state
            bool hasReceivedServerRpc = false;
            bool hasReceivedClientRpcRemotely = false;
            bool hasReceivedClientRpcLocally = false;

            clientClientPlayerResult.Result.GetComponent<RpcTestNB>().OnClient_Rpc += () =>
            {
                Debug.Log("ClientRpc received on client object");
                hasReceivedClientRpcRemotely = true;
            };

            clientClientPlayerResult.Result.GetComponent<RpcTestNB>().OnServer_Rpc += () =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Assert.Fail("ServerRpc invoked locally. Weaver failure?");
            };

            serverClientPlayerResult.Result.GetComponent<RpcTestNB>().OnServer_Rpc += () =>
            {
                Debug.Log("ServerRpc received on server object");
                hasReceivedServerRpc = true;
            };

            serverClientPlayerResult.Result.GetComponent<RpcTestNB>().OnClient_Rpc += () =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Debug.Log("ClientRpc received on server object");
                hasReceivedClientRpcLocally = true;
            };

            // Send ServerRpc
            clientClientPlayerResult.Result.GetComponent<RpcTestNB>().MyServerRpc();

            // Send ClientRpc
            serverClientPlayerResult.Result.GetComponent<RpcTestNB>().MyClientRpc();

            // Wait for RPCs to be received
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => hasReceivedServerRpc && hasReceivedClientRpcLocally && hasReceivedClientRpcRemotely));

            Assert.True(hasReceivedServerRpc, "ServerRpc was not received");
            Assert.True(hasReceivedClientRpcLocally, "ClientRpc was not locally received on the server");
            Assert.True(hasReceivedClientRpcRemotely, "ClientRpc was not remotely received on the client");
        }
    }
}
