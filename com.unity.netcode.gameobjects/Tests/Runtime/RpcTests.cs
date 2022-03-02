using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode.RuntimeTests
{
    public class RpcTests : NetcodeIntegrationTest
    {
        public class RpcTestNB : NetworkBehaviour
        {
            public event Action<ulong, ServerRpcParams> OnServer_Rpc;
            public event Action OnClient_Rpc;

            [ServerRpc]
            public void MyServerRpc(ulong clientId, ServerRpcParams param = default)
            {
                OnServer_Rpc(clientId, param);
            }

            [ClientRpc]
            public void MyClientRpc()
            {
                OnClient_Rpc();
            }
        }

        protected override int NumberOfClients => 1;

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<RpcTestNB>();
        }

        [UnityTest]
        public IEnumerator TestRpcs()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER* RpcTestNB component
            var serverClientRpcTestNB = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][m_ClientNetworkManagers[0].LocalClientId].GetComponent<RpcTestNB>();

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER* RpcTestNB component
            var localClienRpcTestNB = m_PlayerNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][m_ClientNetworkManagers[0].LocalClientId].GetComponent<RpcTestNB>();

            // Setup state
            bool hasReceivedServerRpc = false;
            bool hasReceivedClientRpcRemotely = false;
            bool hasReceivedClientRpcLocally = false;

            localClienRpcTestNB.OnClient_Rpc += () =>
            {
                Debug.Log("ClientRpc received on client object");
                hasReceivedClientRpcRemotely = true;
            };

            localClienRpcTestNB.OnServer_Rpc += (clientId, param) =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Assert.Fail("ServerRpc invoked locally. Weaver failure?");
            };

            serverClientRpcTestNB.OnServer_Rpc += (clientId, param) =>
            {
                Debug.Log("ServerRpc received on server object");
                Assert.True(param.Receive.SenderClientId == clientId);
                hasReceivedServerRpc = true;
            };

            serverClientRpcTestNB.OnClient_Rpc += () =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Debug.Log("ClientRpc received on server object");
                hasReceivedClientRpcLocally = true;
            };

            // Send ServerRpc
            localClienRpcTestNB.MyServerRpc(m_ClientNetworkManagers[0].LocalClientId);

            // Send ClientRpc
            serverClientRpcTestNB.MyClientRpc();

            // Validate each NetworkManager relative MessagingSystem received each respective RPC
            var messageHookList = new List<MessageHookEntry>();
            var serverMessageHookEntry = new MessageHookEntry(m_ServerNetworkManager);
            serverMessageHookEntry.AssignMessageType<ServerRpcMessage>();
            messageHookList.Add(serverMessageHookEntry);
            foreach (var client in m_ClientNetworkManagers)
            {
                var clientMessageHookEntry = new MessageHookEntry(client);
                clientMessageHookEntry.AssignMessageType<ClientRpcMessage>();
                messageHookList.Add(clientMessageHookEntry);
            }
            var rpcMessageHooks = new MessageHooksConditional(messageHookList);
            yield return WaitForConditionOrTimeOut(rpcMessageHooks);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for messages: {rpcMessageHooks.GetHooksStillWaiting()}");

            // Make sure RPCs propagated all the way up and were called on the relative destination class instance
            yield return WaitForConditionOrTimeOut(() => hasReceivedServerRpc && hasReceivedClientRpcLocally && hasReceivedClientRpcRemotely);

            Assert.True(hasReceivedServerRpc, "ServerRpc was not received");
            Assert.True(hasReceivedClientRpcLocally, "ClientRpc was not locally received on the server");
            Assert.True(hasReceivedClientRpcRemotely, "ClientRpc was not remotely received on the client");
        }
    }
}
