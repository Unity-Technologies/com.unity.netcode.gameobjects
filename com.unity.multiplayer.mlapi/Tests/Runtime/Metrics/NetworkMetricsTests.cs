using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
#if true
    public class NetworkMetricsTests
    {
        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNamedMessageSent_TracksNamedMessageSentMetric()
        {
            NetworkManagerHelper.StartNetworkManager(out var networkManager);
            var networkMetrics = networkManager.NetworkMetrics as NetworkMetrics;
            var messageName = Guid.NewGuid().ToString();

            networkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<NamedMessageEvent>(collection, MetricNames.NamedMessageSent);
                Assert.AreEqual(1, namedMessageSentMetric.Values.Count);

                var namedMessageSent = namedMessageSentMetric.Values.First();
                Assert.AreEqual(messageName, namedMessageSent.Name);
                Assert.AreEqual(networkManager.LocalClientId, namedMessageSent.Connection.Id);
            }));

            networkManager.CustomMessagingManager.SendNamedMessage(messageName, 100, Stream.Null);

            yield return WaitForMetricsDispatch();

            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNamedMessageSentToMultipleClients_TracksNamedMessageSentMetric()
        {
            NetworkManagerHelper.StartNetworkManager(out var networkManager);
            var networkMetrics = networkManager.NetworkMetrics as NetworkMetrics;
            var messageName = Guid.NewGuid().ToString();

            networkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<NamedMessageEvent>(collection, MetricNames.NamedMessageSent);
                Assert.AreEqual(1, namedMessageSentMetric.Values.Count);

                var namedMessageSent = namedMessageSentMetric.Values.First();
                Assert.AreEqual(messageName, namedMessageSent.Name);
                Assert.AreEqual(networkManager.LocalClientId, namedMessageSent.Connection.Id);
            }));

            networkManager.CustomMessagingManager.SendNamedMessage(messageName, new List<ulong> { 100, 200, 300 }, Stream.Null);

            yield return WaitForMetricsDispatch();

            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNamedMessageReceived_TracksNamedMessageReceivedMetric()
        {
            var createServerAndSingleClient = new CreateServerAndSingleClient();
            yield return createServerAndSingleClient.Run();
            var server = createServerAndSingleClient.Server;
            var client = createServerAndSingleClient.Client;
            var clientMetrics = client.NetworkMetrics as NetworkMetrics;

            var messageName = Guid.NewGuid().ToString();
            LogAssert.Expect(LogType.Log, $"Received from {server.LocalClientId}");
            client.CustomMessagingManager.RegisterNamedMessageHandler(messageName, (sender, payload) =>
            {
                Debug.Log($"Received from {sender}");
            });

            var found = false;
            clientMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = collection.Metrics.SingleOrDefault(x => x.Name == MetricNames.NamedMessageReceived);
                Assert.NotNull(namedMessageSentMetric);

                var typedMetric = namedMessageSentMetric as IEventMetric<NamedMessageEvent>;
                Assert.NotNull(typedMetric);
                if (typedMetric.Values.Any()) // We always get the metric, but when it has values, something has been tracked
                {
                    Assert.AreEqual(1, typedMetric.Values.Count);

                    var namedMessageSent = typedMetric.Values.First();
                    Assert.AreEqual(messageName, namedMessageSent.Name);
                    Assert.AreEqual(client.LocalClientId, namedMessageSent.Connection.Id);

                    found = true;
                }
            }));

            server.CustomMessagingManager.SendNamedMessage(messageName, client.LocalClientId, Stream.Null);

            yield return WaitForAFewFrames(); // Client does not receive message synchronously

            MultiInstanceHelpers.Destroy();

            Assert.True(found);
        }

        private IEnumerator WaitForMetricsDispatch()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }

        private IEnumerator WaitForAFewFrames()
        {
            yield return new WaitForSeconds(0.5f);
        }

        private IEventMetric<TEvent> AssertSingleMetricEventOfType<TEvent>(MetricCollection collection, string name)
        {
            var namedMessageSentMetric = collection.Metrics.SingleOrDefault(x => x.Name == name);
            Assert.NotNull(namedMessageSentMetric);

            var typedMetric = namedMessageSentMetric as IEventMetric<TEvent>;
            Assert.NotNull(typedMetric);
            Assert.IsNotEmpty(typedMetric.Values);

            return typedMetric;
        }

        private class TestObserver : IMetricObserver
        {
            private readonly Action<MetricCollection> m_Assertion;

            public TestObserver(Action<MetricCollection> assertion)
            {
                m_Assertion = assertion;
            }

            public void Observe(MetricCollection collection)
            {
                m_Assertion.Invoke(collection);
            }
        }

        public class CreateServerAndSingleClient
        {
            public NetworkManager Server { get; private set; }

            public NetworkManager Client { get; private set; }

            public IEnumerator Run()
            {
                if (!MultiInstanceHelpers.Create(1, out var server, out NetworkManager[] clients))
                {
                    Debug.LogError("Failed to create instances");
                    Assert.Fail("Failed to create instances");
                }

                if (!MultiInstanceHelpers.Start(true, server, clients))
                {
                    Debug.LogError("Failed to start instances");
                    Assert.Fail("Failed to start instances");
                }

                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server));

                Server = server;
                Client = clients.SingleOrDefault();
            }
        }
    }
#endif
}
