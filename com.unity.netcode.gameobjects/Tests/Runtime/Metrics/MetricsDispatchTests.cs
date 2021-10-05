#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using NUnit.Framework;
using Unity.Multiplayer.Tools.NetStats;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    public class MetricsDispatchTests
    {
        private class MockMetricsObserver : IMetricObserver
        {
            public Action OnObserve;
            public void Observe(MetricCollection collection) => OnObserve?.Invoke();
        }

        private int m_NumTicks;
        private int m_NumDispatches;

        private NetworkManager m_NetworkManager;
        private NetworkTimeSystem timeSystem => m_NetworkManager.NetworkTimeSystem;
        private NetworkTickSystem tickSystem => m_NetworkManager.NetworkTickSystem;
        private uint m_TickRate = 1;

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(
                out m_NetworkManager,
                NetworkManagerHelper.NetworkManagerOperatingMode.Host,
                new NetworkConfig()
            {
                TickRate = m_TickRate
            }));

            InitNetworkManager();

            var networkMetrics = m_NetworkManager.NetworkMetrics as NetworkMetrics;
            networkMetrics.Dispatcher.RegisterObserver(new MockMetricsObserver()
            {
                OnObserve = ()=> m_NumDispatches++
            });
        }

        private void InitNetworkManager()
        {
            tickSystem.Tick += ()=> m_NumTicks++;
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManagerHelper.ShutdownNetworkManager();
            m_NumTicks = default;
            m_NumDispatches = default;
            m_NetworkManager = default;
        }

        [UnityTest]
        public IEnumerator GivenMultipleTicks_OneDispatchOccurs()
        {
            AdvanceTicks(2);

            // Wait one frame so dispatch occurs
            yield return null;

            Assert.AreEqual(2, m_NumTicks);
            Assert.AreEqual(1, m_NumDispatches);
        }

        [UnityTest]
        public IEnumerator GivenOneTick_OneDispatchOccurs()
        {
            AdvanceTicks(1);

            // Wait one frame so dispatch occurs
            yield return null;

            Assert.AreEqual(1, m_NumTicks);
            Assert.AreEqual(1, m_NumDispatches);
        }

        [UnityTest]
        public IEnumerator GivenZeroTicks_ZeroDispatchesOccur()
        {
            yield return null;

            Assert.AreEqual(0, m_NumTicks);
            Assert.AreEqual(0, m_NumDispatches);
        }

        [UnityTest]
        public IEnumerator GivenReinitializedNetworkManagerAfterOneTickExecuted_WhenFirstTickExecuted_MetricsDispatch()
        {
            AdvanceTicks(1);
            yield return null;

            Assert.AreEqual(1, m_NumTicks);
            Assert.AreEqual(1, m_NumDispatches);

            m_NetworkManager.Shutdown();
            m_NetworkManager.StartHost();
            InitNetworkManager();

            AdvanceTicks(1);
            yield return null;

            Assert.AreEqual(2, m_NumTicks);
            Assert.AreEqual(2, m_NumDispatches);
        }

        private void AdvanceTicks(int numTicks)
        {
            timeSystem.Advance(1f / m_TickRate * (numTicks + 0.1f));
            tickSystem.UpdateTick(timeSystem.LocalTime, timeSystem.ServerTime);
        }

    }
}
#endif
