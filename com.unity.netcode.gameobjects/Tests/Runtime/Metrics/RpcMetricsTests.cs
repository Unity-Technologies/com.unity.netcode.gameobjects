#if MULTIPLAYER_TOOLS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.TestHelpers.Runtime.Metrics;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class RpcMetricsTests : DualClientMetricTestBase
    {
        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<RpcTestComponent>();
            base.OnCreatePlayerPrefab();
        }

        [UnityTest]
        public IEnumerator TrackRpcSentMetricOnServerToOnlyOneClientWithArray()
        {
            var waitForMetricValues = new WaitForEventMetricValues<RpcEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.RpcSent);

            m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][FirstClient.LocalClientId].GetComponent<RpcTestComponent>().MyClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { FirstClient.LocalClientId }
                }
            });

            yield return waitForMetricValues.WaitForMetricsReceived();

            var serverRpcSentValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverRpcSentValues.Count);

            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.Name == nameof(RpcTestComponent.MyClientRpc)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.NetworkBehaviourName == nameof(RpcTestComponent)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.BytesCount != 0));
            Assert.AreEqual(FirstClient.LocalClientId, serverRpcSentValues.First().Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackRpcSentMetricOnServerToOnlyOneClientWithNativeArray()
        {
            var waitForMetricValues = new WaitForEventMetricValues<RpcEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.RpcSent);

            m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][FirstClient.LocalClientId].GetComponent<RpcTestComponent>().MyClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIdsNativeArray = new NativeArray<ulong>(new[] { FirstClient.LocalClientId }, Allocator.Temp)
                }
            });

            yield return waitForMetricValues.WaitForMetricsReceived();

            var serverRpcSentValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverRpcSentValues.Count);

            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.Name == nameof(RpcTestComponent.MyClientRpc)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.NetworkBehaviourName == nameof(RpcTestComponent)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.BytesCount != 0));
            Assert.AreEqual(FirstClient.LocalClientId, serverRpcSentValues.First().Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackRpcSentMetricOnServerToAllClients()
        {
            var waitForMetricValues = new WaitForEventMetricValues<RpcEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.RpcSent);

            m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][FirstClient.LocalClientId].GetComponent<RpcTestComponent>().MyClientRpc();

            yield return waitForMetricValues.WaitForMetricsReceived();

            var serverRpcSentValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(3, serverRpcSentValues.Count); // Server will receive this, since it's host

            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.Name == nameof(RpcTestComponent.MyClientRpc)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.NetworkBehaviourName == nameof(RpcTestComponent)));
            Assert.That(serverRpcSentValues, Has.All.Matches<RpcEvent>(x => x.BytesCount != 0));
            Assert.Contains(Server.LocalClientId, serverRpcSentValues.Select(x => x.Connection.Id).ToArray());
            Assert.Contains(FirstClient.LocalClientId, serverRpcSentValues.Select(x => x.Connection.Id).ToArray());
            Assert.Contains(SecondClient.LocalClientId, serverRpcSentValues.Select(x => x.Connection.Id).ToArray());
        }

        [UnityTest]
        public IEnumerator TrackRpcSentMetricOnClient()
        {
            var waitForClientMetricsValues = new WaitForEventMetricValues<RpcEvent>(FirstClientMetrics.Dispatcher, NetworkMetricTypes.RpcSent);

            m_PlayerNetworkObjects[FirstClient.LocalClientId][FirstClient.LocalClientId].GetComponent<RpcTestComponent>().MyServerRpc();

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
            var waitForServerMetricsValues = new WaitForEventMetricValues<RpcEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.RpcReceived);
            m_PlayerNetworkObjects[FirstClient.LocalClientId][FirstClient.LocalClientId].GetComponent<RpcTestComponent>().MyServerRpc();

            yield return waitForServerMetricsValues.WaitForMetricsReceived();

            var serverRpcReceivedValues = waitForServerMetricsValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, serverRpcReceivedValues.Count);

            var rpcReceived = serverRpcReceivedValues.First();
            Assert.AreEqual(FirstClient.LocalClientId, rpcReceived.Connection.Id);
            Assert.AreEqual(nameof(RpcTestComponent.MyServerRpc), rpcReceived.Name);
            Assert.AreEqual(nameof(RpcTestComponent), rpcReceived.NetworkBehaviourName);
            Assert.AreNotEqual(0, rpcReceived.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackRpcReceivedMetricOnClient()
        {
            var waitForClientMetricsValues = new WaitForEventMetricValues<RpcEvent>(FirstClientMetrics.Dispatcher, NetworkMetricTypes.RpcReceived);

            m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][FirstClient.LocalClientId].GetComponent<RpcTestComponent>().MyClientRpc();

            yield return waitForClientMetricsValues.WaitForMetricsReceived();

            var clientRpcReceivedValues = waitForClientMetricsValues.AssertMetricValuesHaveBeenFound();
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
