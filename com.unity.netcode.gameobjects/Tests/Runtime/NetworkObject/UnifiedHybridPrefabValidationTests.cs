#if UNIFIED_NETCODE
using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates the hybrid prefab registrations that are rejected:<br />
    /// - Any hybrid prefab in a distributed authority session. The start fails, and one added during the session is not registered.<br />
    /// - A hybrid prefab added during a session that started without hybrid prefabs. It is not registered.<br />
    /// - A NetworkPrefab override with a hybrid source or target prefab, in any topology. The entry is ignored.<br />
    /// </summary>
    /// <remarks>
    /// The fixture's managers are not started. Each test creates its own <see cref="NetworkManager"/> that registers only the prefabs it needs.
    /// </remarks>
    [TestFixture(HostOrServer.UnifiedHost)]
    internal class UnifiedHybridPrefabValidationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        protected override bool m_UseMockTransport => true;

        private GameObject m_HybridPrefab;
        private GameObject m_Prefab;
        private NetworkManager m_NetworkManager;

        public UnifiedHybridPrefabValidationTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("ValidationHybrid");
            m_Prefab = CreateNetworkObjectPrefab("ValidationPrefab", false);
            base.OnServerAndClientsCreated();
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            // Each test registers only the prefabs it needs.
        }

        protected override IEnumerator OnTearDown()
        {
            if (m_NetworkManager != null)
            {
                yield return StopOneClient(m_NetworkManager, true);
                m_NetworkManager = null;
            }
            yield return base.OnTearDown();
        }

        private NetworkManager CreateNetworkManager(NetworkTopologyTypes topology)
        {
            m_NetworkManager = CreateNewClient();
            m_NetworkManager.NetworkConfig.NetworkTopology = topology;
            // Allows prefabs to be added during the session.
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            return m_NetworkManager;
        }

        [UnityTest]
        public IEnumerator StartFailsWithHybridPrefabRegistered()
        {
            var networkManager = CreateNetworkManager(NetworkTopologyTypes.DistributedAuthority);
            networkManager.AddNetworkPrefab(m_HybridPrefab);
            var prefabs = networkManager.NetworkConfig.Prefabs;
            var transport = networkManager.NetworkConfig.NetworkTransport;
            var hybridPrefabError = new Regex($"{Regex.Escape(NetworkPrefabs.DistributedAuthorityHybridPrefabError)}.*{m_HybridPrefab.name}");

            var startMethods = new (string Name, Func<bool> Start)[]
            {
                (nameof(NetworkManager.StartServer), networkManager.StartServer),
                (nameof(NetworkManager.StartHost), networkManager.StartHost),
                (nameof(NetworkManager.StartClient), networkManager.StartClient),
            };

            foreach (var startMethod in startMethods)
            {
                LogAssert.Expect(LogType.Error, hybridPrefabError);
                Assert.IsFalse(startMethod.Start(), $"{startMethod.Name} started a distributed authority session with a hybrid prefab registered!");
                Assert.IsFalse(networkManager.IsListening, $"{startMethod.Name} left the {nameof(NetworkManager)} listening!");
                Assert.AreSame(transport, networkManager.NetworkConfig.NetworkTransport, $"{startMethod.Name} replaced the transport!");
            }

            networkManager.RemoveNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(networkManager.StartHost(), "Failed to start a distributed authority session once the hybrid prefab was removed!");

            LogAssert.Expect(LogType.Error, hybridPrefabError);
            networkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsFalse(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was registered during a distributed authority session!");
            Assert.IsFalse(prefabs.HasGhostPrefabs, $"{nameof(NetworkPrefabs.HasGhostPrefabs)} was set during a distributed authority session!");

            networkManager.Shutdown();
            yield return WaitForConditionOrTimeOut(() => !networkManager.IsListening);
            AssertOnTimeout("The distributed authority session did not shut down!");

            networkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was rejected after the distributed authority session ended!");
        }

        [Test]
        public void FailedStartDoesNotRejectHybridPrefabs()
        {
            var networkManager = CreateNetworkManager(NetworkTopologyTypes.DistributedAuthority);

            // StartServer is not valid in a distributed authority session and fails after the hybrid prefab check.
            LogAssert.Expect(LogType.Error, new Regex("distributed authority mode"));
            Assert.IsFalse(networkManager.StartServer(), "StartServer succeeded in a distributed authority session!");
            // SetRole keeps IsServer set when it rejects the start, which makes the NetworkManager throw when destroyed.
            networkManager.ConnectionManager.LocalClient.SetRole(false, false);

            networkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.ClientServer;
            networkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(networkManager.NetworkConfig.Prefabs.Contains(m_HybridPrefab), "The hybrid prefab was rejected after a failed distributed authority start!");
        }

        [UnityTest]
        public IEnumerator HybridPrefabAddedAfterStartIsRejected()
        {
            var networkManager = CreateNetworkManager(NetworkTopologyTypes.ClientServer);
            var prefabs = networkManager.NetworkConfig.Prefabs;
            Assert.IsTrue(networkManager.StartHost(), "Failed to start the session!");

            LogAssert.Expect(LogType.Error, new Regex($"{Regex.Escape(NetworkPrefabs.HybridPrefabAfterStartError)}.*{m_HybridPrefab.name}"));
            networkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsFalse(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was registered during a session that started without hybrid prefabs!");
            Assert.IsFalse(prefabs.HasGhostPrefabs, $"{nameof(NetworkPrefabs.HasGhostPrefabs)} was set during the session!");

            networkManager.Shutdown();
            yield return WaitForConditionOrTimeOut(() => !networkManager.IsListening);
            AssertOnTimeout("The session did not shut down!");

            networkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was rejected after the session ended!");
        }

        [Test]
        public void PrefabListAddDuringSessionRegistersOnce()
        {
            var networkManager = CreateNetworkManager(NetworkTopologyTypes.ClientServer);
            var prefabList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);
            Assert.IsTrue(networkManager.StartHost(), "Failed to start the session!");

            // A second subscription to the list would register the prefab twice and log a duplicate registration error.
            prefabList.Add(new NetworkPrefab() { Prefab = m_Prefab });
            Assert.IsTrue(networkManager.NetworkConfig.Prefabs.Contains(m_Prefab), "The prefab added to the list was not registered!");
            UnityEngine.Object.Destroy(prefabList);
        }

        /// <summary>
        /// Validates that an override with a hybrid source or target is ignored, with an error naming it.
        /// </summary>
        [Test]
        public void HybridOverrideIsRejected()
        {
            var prefabs = CreateNetworkManager(NetworkTopologyTypes.ClientServer).NetworkConfig.Prefabs;
            var overrides = new[]
            {
                new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = m_Prefab, OverridingTargetPrefab = m_HybridPrefab },
                new NetworkPrefab() { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = m_Prefab.GetComponent<NetworkObject>().GlobalObjectIdHash, OverridingTargetPrefab = m_HybridPrefab },
                new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = m_HybridPrefab, OverridingTargetPrefab = m_Prefab },
            };

            foreach (var hybridOverride in overrides)
            {
                var debugName = hybridOverride.GetDebugName();
                LogAssert.Expect(LogType.Error, new Regex($"{Regex.Escape(NetworkPrefab.HybridPrefabOverrideError)}.*{Regex.Escape(debugName)}"));
                Assert.IsFalse(prefabs.Add(hybridOverride), $"[{debugName}] The override was registered!");
                Assert.IsFalse(prefabs.HasGhostPrefabs, $"[{debugName}] {nameof(NetworkPrefabs.HasGhostPrefabs)} was set by a rejected override!");
            }
        }
    }
}
#endif
