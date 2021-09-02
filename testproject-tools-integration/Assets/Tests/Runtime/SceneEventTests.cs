using Unity.Netcode.RuntimeTests.Metrics.Utlity;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.ToolsIntegration.RuntimeTests
{
    public class SceneEventTests : SingleClientMetricTestBase
    {

        private const string SimpleSceneName = "SimpleScene";
        private const string EmptySceneName = "EmptyScene";

        // [UnitySetUp]
        // public override IEnumerator Setup()
        // {
        //     return base.Setup();
        // }
        //
        // [UnityTearDown]
        // public override IEnumerator Teardown()
        // {
        //     return base.Teardown();
        // }

        [UnityTest]
        public SceneEventTests IEnumerator ()
        {
            var waitForSentMetric = new WaitForMetricValues<ServerLogEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.ServerLogSent);

            var message = Guid.NewGuid().ToString();
            NetworkLog.LogWarningServer(message);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)sentMetric.LogLevel);
            Assert.AreEqual(message.Length + 2, sentMetric.BytesCount);


            // S2C_LOAD
            m_ServerNetworkManager.SceneManager.LoadScene(SimpleSceneName, LoadSceneMode.Single);
        }

    }
}

