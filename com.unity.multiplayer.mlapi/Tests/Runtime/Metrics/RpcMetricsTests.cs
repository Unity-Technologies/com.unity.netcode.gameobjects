using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.Utility;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
    public class RpcMetricsTests
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
        public IEnumerator TrackRpcSentMetricOnServer()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Server, clientPlayer));
            
            var waitForMetricValues = new WaitForMetricValues<RpcEvent>(m_ServerMetrics.Dispatcher, MetricNames.RpcSent);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyClientRpc();

            yield return waitForMetricValues.WaitForMetricsReceived();

            var serverRpcSentValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, serverRpcSentValues.Count); // Server will receive this, since it's host
            
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.Name == nameof(RpcTestComponent.MyClientRpc)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.BytesCount != 0));
            Assert.Contains(m_Server.LocalClientId, serverRpcSentValues.Select(x => x.Connection.Id).ToArray());
            Assert.Contains(m_Client.LocalClientId, serverRpcSentValues.Select(x => x.Connection.Id).ToArray());
        }

        [UnityTest]
        public IEnumerator TrackRpcSentMetricOnClient()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Client, clientPlayer));
            
            var waitForClientMetricsValues = new WaitForMetricValues<RpcEvent>(m_ClientMetrics.Dispatcher, MetricNames.RpcSent);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyServerRpc();

            yield return waitForClientMetricsValues.WaitForMetricsReceived();

            var clientRpcSentValues = waitForClientMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, clientRpcSentValues.Count);

            var rpcSent = clientRpcSentValues.First();
            Assert.AreEqual(m_Server.LocalClientId, rpcSent.Connection.Id);
            Assert.AreEqual(nameof(RpcTestComponent.MyServerRpc), rpcSent.Name);
            Assert.AreNotEqual(0, rpcSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackRpcReceivedMetricOnServer()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Client, clientPlayer));

            var waitForServerMetricsValues = new WaitForMetricValues<RpcEvent>(m_ServerMetrics.Dispatcher, MetricNames.RpcReceived);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyServerRpc();

            yield return waitForServerMetricsValues.WaitForMetricsReceived();

            var serverRpcReceivedValues = waitForServerMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverRpcReceivedValues.Count);

            var rpcReceived = serverRpcReceivedValues.First();
            Assert.AreEqual(m_Client.LocalClientId, rpcReceived.Connection.Id);
            Assert.AreEqual(nameof(RpcTestComponent.MyServerRpc), rpcReceived.Name);
            Assert.AreNotEqual(0, rpcReceived.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackRpcReceivedMetricOnClient()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_Client.LocalClientId), m_Server, clientPlayer));
            
            var waitForServerMetricsValues = new WaitForMetricValues<RpcEvent>(m_ServerMetrics.Dispatcher, MetricNames.RpcReceived);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyClientRpc();

            yield return waitForServerMetricsValues.WaitForMetricsReceived();

            var clientRpcReceivedValues = waitForServerMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, clientRpcReceivedValues.Count);

            var rpcReceived = clientRpcReceivedValues.First();
            Assert.AreEqual(m_Server.LocalClientId, rpcReceived.Connection.Id);
            Assert.AreEqual(nameof(RpcTestComponent.MyClientRpc), rpcReceived.Name);
            Assert.AreNotEqual(0, rpcReceived.BytesCount);
        }
    }
}
