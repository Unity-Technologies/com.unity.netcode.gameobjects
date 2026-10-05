#if UNIFIED_NETCODE
using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that a distributed authority session rejects hybrid prefabs:<br />
    /// - A start with a hybrid prefab registered fails before anything is initialized.<br />
    /// - A hybrid prefab added during the session is not registered.<br />
    /// </summary>
    /// <remarks>
    /// The fixture's own session only provides the hybrid prefab.<br />
    /// Each test runs a separate distributed authority <see cref="NetworkManager"/> with a <see cref="MockTransport"/>.<br />
    /// </remarks>
    [TestFixture(HostOrServer.UnifiedHost)]
    internal class UnifiedHybridPrefabDistributedAuthorityTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private GameObject m_HybridPrefab;
        private NetworkManager m_DistributedAuthorityNetworkManager;

        public UnifiedHybridPrefabDistributedAuthorityTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("DistributedAuthorityHybrid");
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            if (m_DistributedAuthorityNetworkManager != null)
            {
                if (m_DistributedAuthorityNetworkManager.IsListening)
                {
                    m_DistributedAuthorityNetworkManager.Shutdown();
                }
                Object.DestroyImmediate(m_DistributedAuthorityNetworkManager.gameObject);
                m_DistributedAuthorityNetworkManager = null;
            }
            yield return base.OnTearDown();
        }

        private void CreateDistributedAuthorityNetworkManager()
        {
            var gameObject = new GameObject("DistributedAuthorityNetworkManager");
            m_DistributedAuthorityNetworkManager = gameObject.AddComponent<NetworkManager>();
            m_DistributedAuthorityNetworkManager.NetworkConfig = new NetworkConfig()
            {
                NetworkTransport = gameObject.AddComponent<MockTransport>(),
                NetworkTopology = NetworkTopologyTypes.DistributedAuthority,
                ForceSamePrefabs = false,
            };
        }

        [UnityTest]
        public IEnumerator StartFailsWithHybridPrefabRegistered()
        {
            CreateDistributedAuthorityNetworkManager();
            m_DistributedAuthorityNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            var transport = m_DistributedAuthorityNetworkManager.NetworkConfig.NetworkTransport;

            var startMethods = new (string Name, Func<bool> Start)[]
            {
                (nameof(NetworkManager.StartServer), m_DistributedAuthorityNetworkManager.StartServer),
                (nameof(NetworkManager.StartHost), m_DistributedAuthorityNetworkManager.StartHost),
                (nameof(NetworkManager.StartClient), m_DistributedAuthorityNetworkManager.StartClient),
            };

            foreach (var startMethod in startMethods)
            {
                LogAssert.Expect(LogType.Error, new Regex($"{Regex.Escape(NetworkPrefabs.DistributedAuthorityHybridPrefabError)}.*{m_HybridPrefab.name}"));
                Assert.IsFalse(startMethod.Start(), $"{startMethod.Name} started a distributed authority session with a hybrid prefab registered!");
                Assert.IsFalse(m_DistributedAuthorityNetworkManager.IsListening, $"{startMethod.Name} left the {nameof(NetworkManager)} listening!");
                Assert.AreSame(transport, m_DistributedAuthorityNetworkManager.NetworkConfig.NetworkTransport, $"{startMethod.Name} replaced the transport!");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HybridPrefabAddedDuringSessionIsRejected()
        {
            CreateDistributedAuthorityNetworkManager();
            Assert.IsTrue(m_DistributedAuthorityNetworkManager.StartHost(), "Failed to start a distributed authority session without hybrid prefabs!");
            yield return null;

            var prefabs = m_DistributedAuthorityNetworkManager.NetworkConfig.Prefabs;
            LogAssert.Expect(LogType.Error, new Regex($"{Regex.Escape(NetworkPrefabs.DistributedAuthorityHybridPrefabError)}.*{m_HybridPrefab.name}"));
            m_DistributedAuthorityNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsFalse(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was registered during a distributed authority session!");
            Assert.IsFalse(prefabs.HasGhostPrefabs, $"{nameof(NetworkPrefabs.HasGhostPrefabs)} was set during a distributed authority session!");

            m_DistributedAuthorityNetworkManager.Shutdown();
            yield return WaitForConditionOrTimeOut(() => !m_DistributedAuthorityNetworkManager.IsListening);
            AssertOnTimeout("The distributed authority session did not shut down!");

            m_DistributedAuthorityNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was rejected after the distributed authority session ended!");
        }
    }
}
#endif
