#if UNIFIED_NETCODE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates the hybrid prefab registrations a <see cref="NetworkManager"/> rejects:<br />
    /// - Any hybrid prefab in a distributed authority session.<br />
    /// - A NetworkPrefab override with a hybrid source or target prefab, in any topology.<br />
    /// A start with a rejected registration fails before anything is initialized, and a rejected registration added during the session is not registered.<br />
    /// </summary>
    /// <remarks>
    /// The fixture's own session only provides the hybrid prefab.<br />
    /// Each test runs a separate <see cref="NetworkManager"/> with a <see cref="MockTransport"/>.<br />
    /// </remarks>
    [TestFixture(HostOrServer.UnifiedHost)]
    internal class UnifiedHybridPrefabValidationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private GameObject m_HybridPrefab;
        private NetworkManager m_StandaloneNetworkManager;
        private readonly List<Object> m_CreatedObjects = new List<Object>();

        public UnifiedHybridPrefabValidationTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("ValidationHybrid");
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            DestroyStandaloneNetworkManager();
            foreach (var createdObject in m_CreatedObjects)
            {
                Object.DestroyImmediate(createdObject);
            }
            m_CreatedObjects.Clear();
            yield return base.OnTearDown();
        }

        private GameObject CreateNonHybridPrefab(string name)
        {
            var prefab = new GameObject(name);
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(prefab.AddComponent<NetworkObject>());
            m_CreatedObjects.Add(prefab);
            return prefab;
        }

        private void CreateStandaloneNetworkManager(NetworkTopologyTypes topology)
        {
            var gameObject = new GameObject($"{topology}NetworkManager");
            m_StandaloneNetworkManager = gameObject.AddComponent<NetworkManager>();
            m_StandaloneNetworkManager.NetworkConfig = new NetworkConfig()
            {
                NetworkTransport = gameObject.AddComponent<MockTransport>(),
                NetworkTopology = topology,
                ForceSamePrefabs = false,
            };
        }

        private void DestroyStandaloneNetworkManager()
        {
            if (m_StandaloneNetworkManager == null)
            {
                return;
            }
            if (m_StandaloneNetworkManager.IsListening)
            {
                m_StandaloneNetworkManager.Shutdown();
            }
            Object.DestroyImmediate(m_StandaloneNetworkManager.gameObject);
            m_StandaloneNetworkManager = null;
        }

        private IEnumerator ShutdownStandaloneNetworkManager(string context)
        {
            m_StandaloneNetworkManager.Shutdown();
            yield return WaitForConditionOrTimeOut(() => !m_StandaloneNetworkManager.IsListening);
            AssertOnTimeout($"{context} The session did not shut down!");
        }

        [UnityTest]
        public IEnumerator StartFailsWithHybridPrefabRegistered()
        {
            CreateStandaloneNetworkManager(NetworkTopologyTypes.DistributedAuthority);
            m_StandaloneNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            var transport = m_StandaloneNetworkManager.NetworkConfig.NetworkTransport;

            var startMethods = new (string Name, Func<bool> Start)[]
            {
                (nameof(NetworkManager.StartServer), m_StandaloneNetworkManager.StartServer),
                (nameof(NetworkManager.StartHost), m_StandaloneNetworkManager.StartHost),
                (nameof(NetworkManager.StartClient), m_StandaloneNetworkManager.StartClient),
            };

            foreach (var startMethod in startMethods)
            {
                LogAssert.Expect(LogType.Error, new Regex($"{Regex.Escape(NetworkPrefabs.DistributedAuthorityHybridPrefabError)}.*{m_HybridPrefab.name}"));
                Assert.IsFalse(startMethod.Start(), $"{startMethod.Name} started a distributed authority session with a hybrid prefab registered!");
                Assert.IsFalse(m_StandaloneNetworkManager.IsListening, $"{startMethod.Name} left the {nameof(NetworkManager)} listening!");
                Assert.AreSame(transport, m_StandaloneNetworkManager.NetworkConfig.NetworkTransport, $"{startMethod.Name} replaced the transport!");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HybridPrefabAddedDuringSessionIsRejected()
        {
            CreateStandaloneNetworkManager(NetworkTopologyTypes.DistributedAuthority);

            // A hybrid prefab removed before the start does not block it.
            m_StandaloneNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            m_StandaloneNetworkManager.RemoveNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(m_StandaloneNetworkManager.StartHost(), "Failed to start a distributed authority session without hybrid prefabs!");
            yield return null;

            var prefabs = m_StandaloneNetworkManager.NetworkConfig.Prefabs;
            LogAssert.Expect(LogType.Error, new Regex($"{Regex.Escape(NetworkPrefabs.DistributedAuthorityHybridPrefabError)}.*{m_HybridPrefab.name}"));
            m_StandaloneNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsFalse(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was registered during a distributed authority session!");
            Assert.IsFalse(prefabs.HasGhostPrefabs, $"{nameof(NetworkPrefabs.HasGhostPrefabs)} was set during a distributed authority session!");

            yield return ShutdownStandaloneNetworkManager("[DistributedAuthority]");

            m_StandaloneNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(prefabs.Contains(m_HybridPrefab), "The hybrid prefab was rejected after the distributed authority session ended!");
        }

        [UnityTest]
        public IEnumerator FailedStartDoesNotRejectHybridPrefabs()
        {
            CreateStandaloneNetworkManager(NetworkTopologyTypes.DistributedAuthority);

            // StartServer is not valid in a distributed authority session and fails after the hybrid prefab check.
            LogAssert.Expect(LogType.Error, new Regex("distributed authority mode"));
            Assert.IsFalse(m_StandaloneNetworkManager.StartServer(), "StartServer succeeded in a distributed authority session!");
            // SetRole keeps IsServer set when it rejects the start, which makes the NetworkManager throw when destroyed.
            m_StandaloneNetworkManager.ConnectionManager.LocalClient.SetRole(false, false);
            yield return null;

            m_StandaloneNetworkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.ClientServer;
            m_StandaloneNetworkManager.AddNetworkPrefab(m_HybridPrefab);
            Assert.IsTrue(m_StandaloneNetworkManager.NetworkConfig.Prefabs.Contains(m_HybridPrefab), "The hybrid prefab was rejected after a failed distributed authority start!");
        }

        [UnityTest]
        public IEnumerator PrefabListAddDuringSessionRegistersOnce()
        {
            CreateStandaloneNetworkManager(NetworkTopologyTypes.DistributedAuthority);
            var prefabList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            m_CreatedObjects.Add(prefabList);
            m_StandaloneNetworkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);
            Assert.IsTrue(m_StandaloneNetworkManager.StartHost(), "Failed to start a distributed authority session!");
            yield return null;

            var prefab = CreateNonHybridPrefab("ValidationListPrefab");

            // A second subscription to the list would register the prefab twice and log a duplicate registration error.
            prefabList.Add(new NetworkPrefab() { Prefab = prefab });
            Assert.IsTrue(m_StandaloneNetworkManager.NetworkConfig.Prefabs.Contains(prefab), "The prefab added to the list was not registered!");
        }

        /// <summary>
        /// Validates that a NetworkPrefab override with a hybrid target, in either topology:<br />
        /// - Sets <see cref="NetworkPrefabs.HasGhostPrefabs"/>.<br />
        /// - Fails the start, with the distributed authority error as well when in that topology.<br />
        /// - Is rejected when added during the session.<br />
        /// </summary>
        [UnityTest]
        public IEnumerator HybridOverrideIsRejected()
        {
            var source = CreateNonHybridPrefab("ValidationOverrideSource");
            var sourceHash = source.GetComponent<NetworkObject>().GlobalObjectIdHash;
            var overrides = new[]
            {
                new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = source, OverridingTargetPrefab = m_HybridPrefab },
                new NetworkPrefab() { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = sourceHash, OverridingTargetPrefab = m_HybridPrefab },
            };
            var topologies = new[] { NetworkTopologyTypes.ClientServer, NetworkTopologyTypes.DistributedAuthority };
            var overrideError = new Regex($"{Regex.Escape(NetworkPrefabs.HybridPrefabOverrideError)}.*overridden by {m_HybridPrefab.name}");
            var distributedAuthorityError = new Regex($"{Regex.Escape(NetworkPrefabs.DistributedAuthorityHybridPrefabError)}.*overridden by {m_HybridPrefab.name}");

            foreach (var topology in topologies)
            {
                var isDistributedAuthority = topology == NetworkTopologyTypes.DistributedAuthority;
                foreach (var hybridOverride in overrides)
                {
                    var context = $"[{topology}][{hybridOverride.Override}]";
                    CreateStandaloneNetworkManager(topology);
                    var prefabs = m_StandaloneNetworkManager.NetworkConfig.Prefabs;

                    Assert.IsTrue(prefabs.Add(hybridOverride), $"{context} The override was not registered!");
                    Assert.IsTrue(prefabs.HasGhostPrefabs, $"{context} An override targeting a hybrid prefab did not set {nameof(NetworkPrefabs.HasGhostPrefabs)}!");

                    LogAssert.Expect(LogType.Error, overrideError);
                    if (isDistributedAuthority)
                    {
                        LogAssert.Expect(LogType.Error, distributedAuthorityError);
                    }
                    Assert.IsFalse(m_StandaloneNetworkManager.StartHost(), $"{context} A session started with a hybrid override registered!");

                    prefabs.Remove(hybridOverride);
                    Assert.IsTrue(m_StandaloneNetworkManager.StartHost(), $"{context} Failed to start once the override was removed!");
                    yield return null;

                    LogAssert.Expect(LogType.Error, isDistributedAuthority ? distributedAuthorityError : overrideError);
                    Assert.IsFalse(prefabs.Add(hybridOverride), $"{context} The override was registered during the session!");

                    yield return ShutdownStandaloneNetworkManager(context);
                    DestroyStandaloneNetworkManager();
                }
            }
        }
    }
}
#endif
