using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// This test validates PR-3000 where it would invoke
    /// TODO:
    /// We really need to get the service running during tests
    /// so we can validate these issues. While this test does
    /// partially validate it we still need to manually validate
    /// with a service connection.
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class RpcProxyMessageTesting : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private List<RpcProxyText> m_ProxyTestInstances = new List<RpcProxyText>();

        private StringBuilder m_ValidationLogger = new StringBuilder();

        public RpcProxyMessageTesting(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override IEnumerator OnSetup()
        {
            m_ProxyTestInstances.Clear();
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<RpcProxyText>();
            base.OnCreatePlayerPrefab();
        }


        private bool ValidateRpcProxyRpcs()
        {
            m_ValidationLogger.Clear();
            foreach (var proxy in m_ProxyTestInstances)
            {

                // Since we are sending to everyone but the authority, the local instance of each client's player should have zero
                // entries.
                if (proxy.ReceivedRpc.Count != 0)
                {
                    m_ValidationLogger.AppendLine($"Client-{proxy.OwnerClientId} sent itself an Rpc!");
                }
                foreach (var networkManager in m_NetworkManagers)
                {
                    // Skip the local player instance
                    if (networkManager.LocalClientId == proxy.OwnerClientId)
                    {
                        continue;
                    }

                    // Get the cloned player instance of the player based on the player's NetworkObjectId
                    if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(proxy.NetworkObjectId))
                    {
                        m_ValidationLogger.AppendLine($"Client-{networkManager.LocalClientId} does not have a cloned instance for Player-{proxy.OwnerClientId}!");
                    }
                    var clonedPlayer = networkManager.SpawnManager.SpawnedObjects[proxy.NetworkObjectId].GetComponent<RpcProxyText>();
                    // For each cloned player, each client should receive 1 RPC call per cloned player instance.
                    // Example (With 3 clients including session owner):
                    // Client-1 (SO): Sends to NotAuthority
                    // Client-2: Should receive 1 RPC on its clone of Player-1
                    // Client-3: Should receive 1 RPC on its clone of Player-1
                    // Client-2: Sends to NotAuthority
                    // Client-1: Should receive 1 RPC on its clone of Player-2
                    // Client-3: Should receive 1 RPC on its clone of Player-2
                    // Client-3: Sends to NotAuthority
                    // Client-1: Should receive 1 RPC on its clone of Player-3
                    // Client-2: Should receive 1 RPC on its clone of Player-3
                    if (clonedPlayer.ReceivedRpc.Count != 1)
                    {
                        m_ValidationLogger.AppendLine($"[{clonedPlayer.name}] Received ({clonedPlayer.ReceivedRpc.Count}) RPCs when we were expected only 1!");
                    }
                }
            }
            return m_ValidationLogger.Length == 0;
        }

        [UnityTest]
        public IEnumerator ProxyDoesNotInvokeOnSender()
        {
            foreach (var client in m_NetworkManagers)
            {
                m_ProxyTestInstances.Add(client.LocalClient.PlayerObject.GetComponent<RpcProxyText>());
            }

            foreach (var clientProxyTest in m_ProxyTestInstances)
            {
                clientProxyTest.SendToEveryOneButMe();
            }

            yield return WaitForConditionOrTimeOut(ValidateRpcProxyRpcs);
            AssertOnTimeout(m_ValidationLogger.ToString());
        }

        public class RpcProxyText : NetworkBehaviour
        {
            public List<ulong> ReceivedRpc = new List<ulong>();

            public void SendToEveryOneButMe()
            {
                var baseTarget = NetworkManager.DistributedAuthorityMode ? RpcTarget.NotAuthority : RpcTarget.NotMe;
                TestRpc(baseTarget);
            }

            [Rpc(SendTo.SpecifiedInParams)]
            private void TestRpc(RpcParams rpcParams = default)
            {
                ReceivedRpc.Add(rpcParams.Receive.SenderClientId);
            }
        }
    }
}
