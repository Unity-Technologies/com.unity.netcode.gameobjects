#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics.Utility
{
    internal abstract class SingleClientMetricTestBase : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        protected virtual Action<GameObject> UpdatePlayerPrefab => _ => { };

        internal NetworkManager Server { get; private set; }

        internal NetworkMetrics ServerMetrics { get; private set; }

        internal NetworkManager Client { get; private set; }

        internal NetworkMetrics ClientMetrics { get; private set; }

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, UpdatePlayerPrefab);

            Server = m_ServerNetworkManager;
            ServerMetrics = Server.NetworkMetrics as NetworkMetrics;
            Client = m_ClientNetworkManagers[0];
            ClientMetrics = Client.NetworkMetrics as NetworkMetrics;
        }
    }

    public abstract class DualClientMetricTestBase : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        protected virtual Action<GameObject> UpdatePlayerPrefab => _ => { };

        internal NetworkManager Server { get; private set; }

        internal NetworkMetrics ServerMetrics { get; private set; }

        internal NetworkManager FirstClient { get; private set; }

        internal NetworkMetrics FirstClientMetrics { get; private set; }

        internal NetworkManager SecondClient { get; private set; }

        internal NetworkMetrics SecondClientMetrics { get; private set; }

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, UpdatePlayerPrefab);

            Server = m_ServerNetworkManager;
            ServerMetrics = Server.NetworkMetrics as NetworkMetrics;
            FirstClient = m_ClientNetworkManagers[0];
            FirstClientMetrics = FirstClient.NetworkMetrics as NetworkMetrics;
            SecondClient = m_ClientNetworkManagers[0];
            SecondClientMetrics = SecondClient.NetworkMetrics as NetworkMetrics;
        }
    }
}
#endif
