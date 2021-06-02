using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.NetworkVariables
{
    public class NetworkVariableMetricsDispatchTests : MetricsTestBase
    {
        NetworkManager m_NetworkManager;
        NetworkMetrics m_NetworkMetrics;

        [SetUp]
        public void SetUp()
        {
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager);
            m_NetworkMetrics = m_NetworkManager.NetworkMetrics as NetworkMetrics;

            var gameObjectId = NetworkManagerHelper.AddGameNetworkObject("NetworkVariableTestComponent");
            NetworkManagerHelper.AddComponentToObject<NetworkVariableComponent>(gameObjectId);
            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNetworkVariableDeltaSent_TracksNetworkVariableDeltaSentMetric()
        {
            var found = false;
            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var networkVariableUpdateMetric = collection.Metrics.SingleOrDefault(x => x.Name == MetricNames.NetworkVariableDeltaSent);
                Assert.NotNull(networkVariableUpdateMetric);

                var typedMetric = networkVariableUpdateMetric as IEventMetric<NetworkVariableEvent>;
                Assert.NotNull(typedMetric);
                
                if (typedMetric.Values.Any() && !found)
                {
                    Assert.AreEqual(1, typedMetric.Values.Count);

                    var networkVariableDeltaSent = typedMetric.Values.First();
                    Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), networkVariableDeltaSent.Name);
                    Assert.AreEqual(m_NetworkManager.LocalClientId, networkVariableDeltaSent.Connection.Id);

                    found = true;
                }
            }));

            yield return WaitForAFewFrames();

            Assert.True(found);
        }
    }
}