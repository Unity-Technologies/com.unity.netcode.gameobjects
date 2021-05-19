using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TestProject.ManualTests;
using MLAPI.RuntimeTests;
using MLAPI;
using Debug = UnityEngine.Debug;

namespace TestProject.RuntimeTests
{
    public class RPCTestsAutomated
    {
        private bool m_TimedOut;

        private int m_MaxFrames;

        private GameObject m_PlayerPrefab;

        /// <summary>
        /// Default Mode (Batched RPCs Enabled)
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ManualRpcTestsAutomated()
        {
            return AutomatedRpcTestsHandler(9);
        }

        /// <summary>
        /// Same test with Batched RPC turned off
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ManualRpcTestsAutomatedNoBatching()
        {
            return AutomatedRpcTestsHandler(3, false);
        }


        /// <summary>
        /// This just helps to simplify any further tests that can leverage from
        /// the RpcQueueManualTests' wide array of RPC testing under different
        /// conditions.  
        /// Currently this allows for the adjustment of client count and whether
        /// RPC Batching is enabled or not.
        /// </summary>
        /// <param name="numClients"></param>
        /// <param name="useBatching"></param>
        /// <returns></returns>
        private IEnumerator AutomatedRpcTestsHandler(int numClients, bool useBatching = true)
        {

            Debug.Log($"Application.targetFrameRate = {Application.targetFrameRate}");
            if (Application.targetFrameRate > 60)
            {
                Application.targetFrameRate = 60;
            }

            var startFrameCount = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;

            // Set RpcQueueManualTests into unit testing mode
            RpcQueueManualTests.UnitTesting = true;

            // Create Host and (numClients) clients 
            Assert.True(MultiInstanceHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));

            // Create a default player GameObject to use
            m_PlayerPrefab = new GameObject("Player");
            var networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Add our RpcQueueManualTests component
            m_PlayerPrefab.AddComponent<RpcQueueManualTests>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;


            // Set all of the client's player prefab
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Set the Rpc Batch sending mode
            server.RpcQueueContainer.EnableBatchedRpcs(useBatching);

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].RpcQueueContainer.EnableBatchedRpcs(useBatching);
            }

            // [Client-Side] Wait for a connection to the server 
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 256));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 256));

            // [Host-Side] Get the Host owned instance of the RpcQueueManualTests
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), server, serverClientPlayerResult));

            var serverRpcTests = serverClientPlayerResult.Result.GetComponent<RpcQueueManualTests>();
            Assert.IsNotNull(serverRpcTests);

            // [Host-Side] Set the (unit) testing mode
            serverRpcTests.SetTestingMode(true, 1);

            // [Client-Side] Get all of the RpcQueueManualTests instances relative to each client
            var clientRpcQueueManualTestInstsances = new List<RpcQueueManualTests>();
            foreach (var client in clients)
            {
                var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), client, clientClientPlayerResult));
                var clientRpcTests = clientClientPlayerResult.Result.GetComponent<RpcQueueManualTests>();
                Assert.IsNotNull(clientRpcTests);
                clientRpcQueueManualTestInstsances.Add(clientRpcTests);
            }

            // [Client-Side] Set the (unit) testing mode for each client
            foreach (var rpcClientSideTest in clientRpcQueueManualTestInstsances)
            {
                rpcClientSideTest.SetTestingMode(true, 1);
            }

            // [Host-Side] Begin testing on the host
            serverRpcTests.BeginTest();

            // [Client-Side] Begin testing on each client
            foreach (var rpcClientSideTest in clientRpcQueueManualTestInstsances)
            {
                rpcClientSideTest.BeginTest();
            }

            m_MaxFrames = Time.frameCount + 1000;
            // Use frame counts
            var startFrame = Time.frameCount;
            var doubleCheckTime = Time.realtimeSinceStartup + 10.0f; //Double check that we aren't just running really fast frames?

            m_TimedOut = false;
            while (!serverRpcTests.IsFinishedWithTest())
            {
                if (Time.frameCount > m_MaxFrames)
                {
                    if (doubleCheckTime < Time.realtimeSinceStartup)
                    {
                        m_TimedOut = true;
                        break;
                    }
                }
                // We can still use frame count as our time out metric
                // but still use WaitForSeconds to reduce processing overhead.

                var nextFrameId = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameId);
            }

            Debug.Log($"Frames taken to complete: {Time.frameCount - startFrame}");

            // Verify we didn't time out (i.e. all tests ran and finished)
            Assert.IsFalse(m_TimedOut);

            // Log the output for visual confirmation (Acceptance Test for this test) that all Rpc test types (tracked by counters) executed multiple times
            Debug.Log("Final Host-Server Status Info:");
            Debug.Log(serverRpcTests.GetCurrentServerStatusInfo());

            foreach (var rpcClientSideTest in clientRpcQueueManualTestInstsances)
            {
                Debug.Log($"Final Client {rpcClientSideTest.NetworkManager.LocalClientId} Status Info:");
                Debug.Log(rpcClientSideTest.GetCurrentClientStatusInfo());
            }

            Debug.Log($"Application.targetFrameRate = {Application.targetFrameRate}.");
            Debug.Log($"Total frames updated = {Time.frameCount - startFrameCount} within {Time.realtimeSinceStartup - startTime} seconds.");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_PlayerPrefab != null)
            {
                Object.Destroy(m_PlayerPrefab);
                m_PlayerPrefab = null;
            }
            // Shutdown and clean up both of our NetworkManager instances
            MultiInstanceHelpers.Destroy();
        }
    }
}
