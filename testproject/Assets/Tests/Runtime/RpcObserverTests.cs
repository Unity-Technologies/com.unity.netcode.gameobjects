using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class RpcObserverTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 9;

        private GameObject m_TestPrefab;

        private GameObject m_ServerPrefabInstance;
        private RpcObserverObject m_ServerRpcObserverObject;

        public RpcObserverTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnOneTimeSetup()
        {
            m_NetworkTransport = NetcodeIntegrationTestHelpers.InstanceTransport.UTP;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab($"{nameof(RpcObserverObject)}");
            m_TestPrefab.AddComponent<RpcObserverObject>();
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_ServerPrefabInstance = SpawnObject(m_TestPrefab, m_ServerNetworkManager);
            m_ServerRpcObserverObject = m_ServerPrefabInstance.GetComponent<RpcObserverObject>();
            return base.OnServerAndClientsConnected();
        }

        [UnityTest]
        public IEnumerator ClientRpcObserverTest()
        {
            // Wait for all clients to report they have spawned an instance of our test prefab
            yield return WaitForConditionOrTimeOut(m_ServerRpcObserverObject.AllClientsSpawned);

            var nonObservers = new List<ulong>();

            // We start out with all clients being observers of the test prefab
            // Test that all clients receive the RPC
            yield return RunRpcObserverTest(nonObservers);

            // This hides the test prefab from one client and then runs the test
            foreach (var clientId in m_ServerNetworkManager.ConnectedClientsIds)
            {
                if (clientId == m_ServerNetworkManager.LocalClientId)
                {
                    continue;
                }
                // Hide it from the client
                m_ServerRpcObserverObject.NetworkObject.NetworkHide(clientId);
                nonObservers.Add(clientId);
                // Provide 1 tick for the client to hide the NetworkObject
                yield return s_DefaultWaitForTick;

                // Run the test
                yield return RunRpcObserverTest(nonObservers);
            }
        }

        /// <summary>
        /// This will send an RPC to all observers of the test prefab and check
        /// to see if the observer clients receive the message and also confirms
        /// that the non-observer clients did not receive the message.
        /// </summary>
        private IEnumerator RunRpcObserverTest(List<ulong> nonObservers)
        {
            m_ServerRpcObserverObject.ResetTest();

            m_ServerRpcObserverObject.ObserverMessageClientRpc();

            yield return WaitForConditionOrTimeOut(m_ServerRpcObserverObject.AllObserversReceivedRPC);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive message!\n" +
                $"Clients that received the message:{m_ServerRpcObserverObject.GetClientIdsAsString()}");

            Assert.False(m_ServerRpcObserverObject.NonObserversReceivedRPC(nonObservers),$"Non-observers ({m_ServerRpcObserverObject.GetClientIdsAsString(nonObservers)}) received the RPC message!");
        }

    }

    public class RpcObserverObject : NetworkBehaviour
    {
        public readonly List<ulong> ObserversThatReceivedRPC = new List<ulong>();

        public static readonly List<ulong>  ClientInstancesSpawned = new List<ulong>();

        protected bool m_NotifyClientReceivedMessage;

        public string GetClientIdsAsString(List<ulong> clientIds = null)
        {
            if (clientIds == null)
            {
                clientIds = ObserversThatReceivedRPC;
            }
            var clientIdsAsString = string.Empty;
            foreach (var clientId in clientIds)
            {
                clientIdsAsString += $"({clientId})";
            }
            return clientIdsAsString;
        }

        /// <summary>
        /// Returns true if all connected clients have spawned the test prefab
        /// </summary>
        public bool AllClientsSpawned()
        {
            if (!IsServer)
            {
                return false;
            }

            foreach(var clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }

                if (!ClientInstancesSpawned.Contains(clientId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if all observers have received the RPC
        /// </summary>
        public bool AllObserversReceivedRPC()
        {
            if (!IsServer)
            {
                return false;
            }

            foreach (var clientId in NetworkObject.Observers)
            {
                if (clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }
                if (!ObserversThatReceivedRPC.Contains(clientId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if any clientId in the nonObservers list received the RPC
        /// </summary>
        /// <param name="nonObservers">list of clientIds that should not have received the RPC message</param>
        public bool NonObserversReceivedRPC(List<ulong> nonObservers)
        {
            foreach (var clientId in nonObservers)
            {
                // return false if a non-observer received the RPC
                if (ObserversThatReceivedRPC.Contains(clientId))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clears the received
        /// </summary>
        public void ResetTest()
        {
            ObserversThatReceivedRPC.Clear();
        }

        /// <summary>
        /// Called from server-host once per test run
        /// </summary>
        [ClientRpc]
        public void ObserverMessageClientRpc()
        {
            m_NotifyClientReceivedMessage = true;
        }

        /// <summary>
        /// Called by each observer client that received the ObserverMessageClientRpc message
        /// The sender id is added to the ObserversThatReceivedRPC list
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ObserverMessageServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ObserversThatReceivedRPC.Add(serverRpcParams.Receive.SenderClientId);
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                ClientInstancesSpawned.Add(NetworkManager.LocalClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                ClientInstancesSpawned.Remove(NetworkManager.LocalClientId);
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                return;
            }

            if (m_NotifyClientReceivedMessage)
            {
                m_NotifyClientReceivedMessage = false;
                ObserverMessageServerRpc();
            }
        }
    }

}
