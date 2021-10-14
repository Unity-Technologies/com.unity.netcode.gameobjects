#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class RpcMetricsTests : SingleClientMetricTestBase
    {
        protected override Action<GameObject> UpdatePlayerPrefab => prefab => prefab.AddComponent<RpcTestComponent>();

        [UnityTest]
        public IEnumerator TrackRpcSentMetricOnServer()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == Client.LocalClientId, Server, clientPlayer));

            var waitForMetricValues = new WaitForMetricValues<RpcEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.RpcSent);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyClientRpc();

            yield return waitForMetricValues.WaitForMetricsReceived();

            var serverRpcSentValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, serverRpcSentValues.Count); // Server will receive this, since it's host

            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.Name == nameof(RpcTestComponent.MyClientRpc)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.NetworkBehaviourName == nameof(RpcTestComponent)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.BytesCount != 0));
            Assert.Contains(Server.LocalClientId, serverRpcSentValues.Select(x => x.Connection.Id).ToArray());
            Assert.Contains(Client.LocalClientId, serverRpcSentValues.Select(x => x.Connection.Id).ToArray());
        }

        [UnityTest]
        public IEnumerator TrackRpcSentMetricOnClient()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == Client.LocalClientId, Client, clientPlayer));

            var waitForClientMetricsValues = new WaitForMetricValues<RpcEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.RpcSent);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyServerRpc();

            yield return waitForClientMetricsValues.WaitForMetricsReceived();

            var clientRpcSentValues = waitForClientMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, clientRpcSentValues.Count);

            var rpcSent = clientRpcSentValues.First();
            Assert.AreEqual(Server.LocalClientId, rpcSent.Connection.Id);
            Assert.AreEqual(nameof(RpcTestComponent.MyServerRpc), rpcSent.Name);
            Assert.AreEqual(nameof(RpcTestComponent), rpcSent.NetworkBehaviourName);
            Assert.AreNotEqual(0, rpcSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackRpcReceivedMetricOnServer()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == Client.LocalClientId, Client, clientPlayer));

            var waitForServerMetricsValues = new WaitForMetricValues<RpcEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.RpcReceived);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyServerRpc();

            yield return waitForServerMetricsValues.WaitForMetricsReceived();

            var serverRpcReceivedValues = waitForServerMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverRpcReceivedValues.Count);

            var rpcReceived = serverRpcReceivedValues.First();
            Assert.AreEqual(Client.LocalClientId, rpcReceived.Connection.Id);
            Assert.AreEqual(nameof(RpcTestComponent.MyServerRpc), rpcReceived.Name);
            Assert.AreEqual(nameof(RpcTestComponent), rpcReceived.NetworkBehaviourName);
            Assert.AreNotEqual(0, rpcReceived.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackRpcReceivedMetricOnClient()
        {
            var clientPlayer = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == Client.LocalClientId, Server, clientPlayer));

            var waitForServerMetricsValues = new WaitForMetricValues<RpcEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.RpcReceived);

            clientPlayer.Result.GetComponent<RpcTestComponent>().MyClientRpc();

            yield return waitForServerMetricsValues.WaitForMetricsReceived();

            var clientRpcReceivedValues = waitForServerMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, clientRpcReceivedValues.Count);

            var rpcReceived = clientRpcReceivedValues.First();
            Assert.AreEqual(Server.LocalClientId, rpcReceived.Connection.Id);
            Assert.AreEqual(nameof(RpcTestComponent.MyClientRpc), rpcReceived.Name);
            Assert.AreEqual(nameof(RpcTestComponent), rpcReceived.NetworkBehaviourName);
            Assert.AreNotEqual(0, rpcReceived.BytesCount);
        }
    }
}
#endif
