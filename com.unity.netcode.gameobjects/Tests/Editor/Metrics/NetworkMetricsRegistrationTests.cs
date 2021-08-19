#if MULTIPLAYER_TOOLS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;

namespace Unity.Netcode.EditorTests.Metrics
{
    public class NetworkMetricsRegistrationTests
    {
        [Test]
        public void ValidateThatAllMetricTypesAreRegistered()
        {
            var allMetricTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.GetInterfaces().Contains(typeof(INetworkMetricEvent)))
                .ToList();
            Assert.AreNotEqual(0, allMetricTypes.Count);

            var dispatcher = new NetworkMetrics().Dispatcher as MetricDispatcher;
            Assert.NotNull(dispatcher);

            var collection = typeof(MetricDispatcher)
                .GetField("m_Collection", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(dispatcher) as MetricCollection;
            Assert.NotNull(collection);

            foreach (var metricType in allMetricTypes)
            {
                Assert.That(
                    collection.Metrics,
                    Has.Exactly(2).Matches<IEventMetric>(
                        eventMetric =>
                        {
                            var eventType = eventMetric.GetType().GetGenericArguments()?.FirstOrDefault();
                            return eventType == metricType;
                        }));
            }
        }
    }
}

#endif