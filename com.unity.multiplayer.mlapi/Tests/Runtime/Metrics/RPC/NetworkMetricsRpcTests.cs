using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.RPC
{
    public class NetworkMetricsRpcTests
    {
        NetworkManager m_Server;
        NetworkManager m_Client;
        NetworkMetrics m_ClientMetrics;
        NetworkMetrics m_ServerMetrics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!MultiInstanceHelpers.Create(1, out m_Server, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            var playerPrefab = new GameObject("Player");
            NetworkObject networkObject = playerPrefab.AddComponent<NetworkObject>();
            playerPrefab.AddComponent<RpcTestComponent>();

            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            m_Server.NetworkConfig.PlayerPrefab = playerPrefab;
            m_Client = clients.First();

            m_Client.NetworkConfig.PlayerPrefab = playerPrefab;

            if (!MultiInstanceHelpers.Start(true, m_Server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_Server));

            m_ClientMetrics = m_Client.NetworkMetrics as NetworkMetrics;
            m_ServerMetrics = m_Server.NetworkMetrics as NetworkMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackServerRpcMetrics()
        {
            var waitForClientMetricsValues = new WaitForMetricValues<RpcEvent>(m_ClientMetrics.Dispatcher, MetricNames.RpcSent);
            var waitForServerMetricsValues = new WaitForMetricValues<RpcEvent>(m_ServerMetrics.Dispatcher, MetricNames.RpcReceived);

            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Server, serverClientPlayerResult));

            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Client, clientClientPlayerResult));

            bool hasReceivedServerRpc = false;
            serverClientPlayerResult.Result.GetComponent<RpcTestComponent>().OnServerRpcAction += () =>
            {
                hasReceivedServerRpc = true;
            };

            clientClientPlayerResult.Result.GetComponent<RpcTestComponent>().MyServerRpc();

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => hasReceivedServerRpc));

            // Client Check
            yield return waitForClientMetricsValues.WaitForAFewFrames();

            var clientMetricSentValues = waitForClientMetricsValues.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, clientMetricSentValues.Count);

            var clientMetric = clientMetricSentValues.First();
            Assert.AreEqual(m_Server.LocalClientId, clientMetric.Connection.Id);
            Assert.AreEqual("MyServerRpc", clientMetric.Name);

            // Server Check
            yield return waitForServerMetricsValues.WaitForAFewFrames();

            var serverMetricReceivedValues = waitForServerMetricsValues.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverMetricReceivedValues.Count);

            var serverMetric = serverMetricReceivedValues.First();
            Assert.AreEqual(m_Client.LocalClientId, serverMetric.Connection.Id);
            Assert.AreEqual("MyServerRpc", serverMetric.Name);
        }

        [UnityTest]
        public IEnumerator TrackClientRpcMetrics()
        {
            var waitForServerMetricsValues = new WaitForMetricValues<RpcEvent>(m_ServerMetrics.Dispatcher, MetricNames.RpcReceived);

            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Server, serverClientPlayerResult));

            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Client, clientClientPlayerResult));

            bool hasReceivedClientRpcOnServer = false;
            bool hasReceivedClientRpcRemotely = false;
            serverClientPlayerResult.Result.GetComponent<RpcTestComponent>().OnClientRpcAction += () =>
            {
                hasReceivedClientRpcOnServer = true;
            };
            clientClientPlayerResult.Result.GetComponent<RpcTestComponent>().OnClientRpcAction += () =>
            {
                Debug.Log("ClientRpc received on client object");
                hasReceivedClientRpcRemotely = true;
            };

            serverClientPlayerResult.Result.GetComponent<RpcTestComponent>().MyClientRpc();

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => hasReceivedClientRpcOnServer && hasReceivedClientRpcRemotely));

            yield return waitForServerMetricsValues.WaitForAFewFrames();

            var serverMetricReceivedValues = waitForServerMetricsValues.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverMetricReceivedValues.Count);

            var serverMetric = serverMetricReceivedValues.First();
            Assert.AreEqual(m_Server.LocalClientId, serverMetric.Connection.Id);
            Assert.AreEqual("MyClientRpc", serverMetric.Name);
        }
    }
}
