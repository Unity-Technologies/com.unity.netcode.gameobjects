using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.Utility;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
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
            var initializer = new SingleClientMetricTestInitializer(MetricTestInitializer.CreateAndAssignPlayerPrefabs);

            yield return initializer.Initialize();

            m_Server = initializer.Server;
            m_Client = initializer.Client;
            m_ClientMetrics = initializer.ClientMetrics;
            m_ServerMetrics = initializer.ServerMetrics;
        }

        [TearDown]
        public void TearDown()
        {
            MultiInstanceHelpers.Destroy();
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
            yield return waitForClientMetricsValues.WaitForMetricsReceived();

            var clientMetricSentValues = waitForClientMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, clientMetricSentValues.Count);

            var clientMetric = clientMetricSentValues.First();
            Assert.AreEqual(m_Server.LocalClientId, clientMetric.Connection.Id);
            Assert.AreEqual("MyServerRpc", clientMetric.Name);

            // Server Check
            yield return waitForServerMetricsValues.WaitForMetricsReceived();

            var serverMetricReceivedValues = waitForServerMetricsValues.AssertMetricValuesHaveBeenFound();
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

            yield return waitForServerMetricsValues.WaitForMetricsReceived();

            var serverMetricReceivedValues = waitForServerMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverMetricReceivedValues.Count);

            var serverMetric = serverMetricReceivedValues.First();
            Assert.AreEqual(m_Server.LocalClientId, serverMetric.Connection.Id);
            Assert.AreEqual("MyClientRpc", serverMetric.Name);
        }
    }
}
