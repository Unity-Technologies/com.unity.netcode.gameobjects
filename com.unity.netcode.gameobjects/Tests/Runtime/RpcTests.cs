using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode.RuntimeTests
{
    public class RpcTests : NetcodeIntegrationTest
    {
        public class RpcTestNB : NetworkBehaviour
        {
            public event Action<ulong, ServerRpcParams> OnServer_Rpc;
            public event Action<Vector3, Vector3[], FixedString32Bytes> OnTypedServer_Rpc;
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

            [ServerRpc]
            public void MyTypedServerRpc(Vector3 param1, Vector3[] param2, FixedString32Bytes param3)
            {
                OnTypedServer_Rpc(param1, param2, param3);
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
            bool hasReceivedTypedServerRpc = false;
            bool hasReceivedClientRpcRemotely = false;
            bool hasReceivedClientRpcLocally = false;

            var vector3 = new Vector3(1, 2, 3);
            Vector3[] vector3s = new[] { new Vector3(4, 5, 6), new Vector3(7, 8, 9) };

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

            var str = new FixedString32Bytes("abcdefg");

            serverClientRpcTestNB.OnTypedServer_Rpc += (param1, param2, param3) =>
            {
                Debug.Log("TypedServerRpc received on server object");
                Assert.AreEqual(param1, vector3);
                Assert.AreEqual(param2.Length, vector3s.Length);
                Assert.AreEqual(param2[0], vector3s[0]);
                Assert.AreEqual(param2[1], vector3s[1]);
                Assert.AreEqual(param3, str);
                hasReceivedTypedServerRpc = true;
            };

            // Send ServerRpc
            localClienRpcTestNB.MyServerRpc(m_ClientNetworkManagers[0].LocalClientId);

            // Send TypedServerRpc
            localClienRpcTestNB.MyTypedServerRpc(vector3, vector3s, str);

            // Send ClientRpc
            serverClientRpcTestNB.MyClientRpc();

            // Validate each NetworkManager relative MessagingSystem received each respective RPC
            var messageHookList = new List<MessageHookEntry>();
            var serverMessageHookEntry = new MessageHookEntry(m_ServerNetworkManager);
            serverMessageHookEntry.AssignMessageType<ServerRpcMessage>();
            messageHookList.Add(serverMessageHookEntry);

            var typedServerMessageHookEntry = new MessageHookEntry(m_ServerNetworkManager);
            typedServerMessageHookEntry.AssignMessageType<ServerRpcMessage>();
            messageHookList.Add(typedServerMessageHookEntry);

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
            yield return WaitForConditionOrTimeOut(() => hasReceivedServerRpc && hasReceivedClientRpcLocally && hasReceivedClientRpcRemotely && hasReceivedTypedServerRpc);

            Assert.True(hasReceivedServerRpc, "ServerRpc was not received");
            Assert.True(hasReceivedTypedServerRpc, "TypedServerRpc was not received");
            Assert.True(hasReceivedClientRpcLocally, "ClientRpc was not locally received on the server");
            Assert.True(hasReceivedClientRpcRemotely, "ClientRpc was not remotely received on the client");
        }
    }
}
