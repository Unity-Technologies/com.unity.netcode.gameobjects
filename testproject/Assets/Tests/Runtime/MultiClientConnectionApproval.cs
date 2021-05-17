using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MLAPI.RuntimeTests;
using MLAPI;
using Debug = UnityEngine.Debug;

namespace TestProject.RuntimeTests
{
    public class MultiClientConnectionApproval
    {
        private string m_ConnectionToken;
        private uint m_SuccessfulConnections;
        private uint m_FailedConnections;
        private uint m_PrefabOverrideGlobalObjectIdHash;

        /// <summary>
        /// Tests connection approval and connection approval failure
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ConnectionApproval()
        {
            m_ConnectionToken = "ThisIsTheRightPassword";
            return ConnectionApprovalHandler(6,2);
        }

        /// <summary>
        /// Tests player prefab overriding, connection approval, and connection approval failure
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ConnectionApprovalPrefabOverride()
        {
            m_ConnectionToken = "PrefabOverrideCorrectPassword";
            return ConnectionApprovalHandler(4, 1);
        }


        /// <summary>
        /// Allows for several connection approval related configurations 
        /// </summary>
        /// <param name="numClients">total number of clients (excluding the host)</param>
        /// <param name="failureTestCount">how many clients are expected to fail</param>
        /// <param name="prefabOverride">if we are also testing player prefab overrides</param>
        /// <returns></returns>
        private IEnumerator ConnectionApprovalHandler(int numClients, int failureTestCount = 1, bool prefabOverride = false)
        {
            m_SuccessfulConnections = 0;
            m_FailedConnections = 0;
            Assert.IsTrue(numClients >= failureTestCount);

            // Create Host and (numClients) clients 
            Assert.True(MultiInstanceHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));

            // Create a default player GameObject to use
            var playerPrefab = new GameObject("Player");
            var networkObject = playerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            // Create the player prefab override if set
            if (prefabOverride)
            {
                // Create a default player GameObject to use
                var playerPrefabOverride = new GameObject("PlayerPrefabOverride");
                var networkObjectOverride = playerPrefab.AddComponent<NetworkObject>();
                MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObjectOverride);
                m_PrefabOverrideGlobalObjectIdHash = networkObjectOverride.GlobalObjectIdHash;
            }
            else
            {
                m_PrefabOverrideGlobalObjectIdHash = 0;
            }

            // [Host-Side] Set the player prefab
            server.NetworkConfig.PlayerPrefab = playerPrefab;
            server.NetworkConfig.ConnectionApproval = true;
            server.ConnectionApprovalCallback += ConnectionApprovalCallback;
            server.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(m_ConnectionToken);

            // [Client-Side] Get all of the RpcQueueManualTests instances relative to each client
            var clientsAdjustedList = new List<NetworkManager>();
            var markedForFailure = 0;
            foreach (var client in clients)
            {
                client.NetworkConfig.PlayerPrefab = playerPrefab;
                client.NetworkConfig.ConnectionApproval = true;
                if (markedForFailure < failureTestCount)
                {
                    client.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes("ThisIsTheWrongPassword");
                    markedForFailure++;
                }
                else
                {
                    client.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(m_ConnectionToken);
                    clientsAdjustedList.Add(client);
                }
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server 
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clientsAdjustedList.ToArray()));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clientsAdjustedList.Count + 1));

            // Validate the number of failed connections is the same as expected
            Assert.IsTrue(m_FailedConnections == failureTestCount);

            // Validate the number of successful connections is the total number of expected clients minus the failed client count
            Assert.IsTrue(m_SuccessfulConnections == (numClients + 1) - failureTestCount);

            // If we are doing player prefab overrides, then check all of the players to make sure they spawned the appropriate NetworkObject
            if (prefabOverride)
            {
                foreach(var networkClient in server.ConnectedClientsList)
                {
                    Assert.IsNotNull(networkClient.PlayerObject);
                    Assert.AreEqual(networkClient.PlayerObject.GlobalObjectIdHash, m_PrefabOverrideGlobalObjectIdHash);
                }
            }

            // Shutdown and clean up both of our NetworkManager instances
            MultiInstanceHelpers.ShutdownAndClean();
        }

        /// <summary>
        /// Delegate handler for the connection approval callback
        /// </summary>
        /// <param name="connectionData">the NetworkConfig.ConnectionData sent from the client being approved</param>
        /// <param name="clientId">the client id being approved</param>
        /// <param name="callback">the callback invoked to handle approval</param>
        private void ConnectionApprovalCallback(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
        {
            string approvalToken = Encoding.ASCII.GetString(connectionData);
            var isApproved = approvalToken == m_ConnectionToken;

            if(isApproved)
            {
                m_SuccessfulConnections++;
            }
            else
            {
                m_FailedConnections++;
            }

            if (m_PrefabOverrideGlobalObjectIdHash == 0)
            {
                callback.Invoke(true, null, isApproved, null, null);
            }
            else
            {
                callback.Invoke(true, m_PrefabOverrideGlobalObjectIdHash, isApproved, null, null);
            }
        }
    }
}
