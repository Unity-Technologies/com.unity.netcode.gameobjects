using System;
using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.NetworkVariables;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.Ownership
{
    public class NetworkMetricsOwnershipChangeReceptionTests
    {
        NetworkManager m_Server;
        NetworkManager m_Client;
        NetworkMetrics m_ClientMetrics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!MultiInstanceHelpers.Create(1, out m_Server, out var clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }
            
            var playerPrefab = new GameObject("Player");
            var networkObject = playerPrefab.AddComponent<NetworkObject>();
            playerPrefab.AddComponent<NetworkVariableComponent>();

            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            m_Server.NetworkConfig.PlayerPrefab = playerPrefab;

            foreach (var client in clients)
            {
                client.NetworkConfig.PlayerPrefab = playerPrefab;
            }

            if (!MultiInstanceHelpers.Start(true, m_Server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_Server));

            m_Client = clients.First();
            m_ClientMetrics = m_Client.NetworkMetrics as NetworkMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackOwnershipChangeReceivedMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(m_ClientMetrics.Dispatcher, MetricNames.OwnershipChangeReceived);

            var gameObject = new GameObject(Guid.NewGuid().ToString());
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;
            networkObject.Spawn();

            yield return new WaitForSeconds(0.5f);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForAFewFrames();

            var metricValues = waitForMetricValues.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, metricValues.Count);
        }
    }
}