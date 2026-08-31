using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Covers the authored <see cref="NetworkConfig.TransformSyncMode"/> reaching the runtime, and staying
    /// fixed for the duration of a session.
    /// </summary>
    /// <remarks>
    /// Every other fixture reaches the mode through the same assignment the harness makes, so none of them
    /// can see a delivery path that never ran.
    /// </remarks>
    // These tests do not need to run against the Rust server, and batching is client-server only.
    [IgnoreIfServiceEnvironmentVariableSet]
    [TestFixture(TransformSyncModes.PerInstance)]
    [TestFixture(TransformSyncModes.Batched)]
    internal class NetworkTransformSyncModeConfigurationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private readonly TransformSyncModes m_SyncMode;
        private GameObject m_MoverPrefab;

        public NetworkTransformSyncModeConfigurationTests(TransformSyncModes syncMode)
        {
            m_SyncMode = syncMode;
        }

        internal override TransformSyncModes OnGetSyncMode()
        {
            return m_SyncMode;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_MoverPrefab = CreateNetworkObjectPrefab("SyncModeMover");
            var networkTransform = m_MoverPrefab.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;

            base.OnServerAndClientsCreated();
        }

        private NetworkTransform SpawnMover()
        {
            var instance = Object.Instantiate(m_MoverPrefab);
            var networkObject = instance.GetComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_ServerNetworkManager;
            networkObject.Spawn();
            return networkObject.GetComponent<NetworkTransform>();
        }

        /// <summary>
        /// A batched instance is registered with the <see cref="NetworkTransformStateManager"/> and a per
        /// instance one is not, so the index is the observable proof of which path an instance took.
        /// </summary>
        private bool IsRegisteredForBatching(NetworkTransform networkTransform)
        {
            return networkTransform.StateManagerIndex >= 0;
        }

        /// <summary>
        /// The authored mode has to reach every <see cref="NetworkManager"/> and route the instances that
        /// spawn under it.
        /// </summary>
        [UnityTest]
        public IEnumerator AuthoredModeReachesTheRuntime()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                Assert.AreEqual(m_SyncMode, networkManager.NetworkConfig.ActiveTransformSyncMode,
                    $"{networkManager.name} started with {networkManager.NetworkConfig.ActiveTransformSyncMode} rather than the authored {m_SyncMode}!");
            }

            var mover = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => IsRegisteredForBatching(mover) == (m_SyncMode == TransformSyncModes.Batched));
            AssertOnTimeout($"Authority instance has StateManagerIndex {mover.StateManagerIndex} under {m_SyncMode}!");
        }

        /// <summary>
        /// Writing the mode while a session is running applies to the next session, not this one.
        /// </summary>
        [UnityTest]
        public IEnumerator ModeChangedDuringASessionDoesNotAffectIt()
        {
            var alreadySpawned = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => IsRegisteredForBatching(alreadySpawned) == (m_SyncMode == TransformSyncModes.Batched));
            AssertOnTimeout($"Authority instance has StateManagerIndex {alreadySpawned.StateManagerIndex} under {m_SyncMode}!");

            var configHash = m_ServerNetworkManager.NetworkConfig.GetConfig(false);
            var otherMode = m_SyncMode == TransformSyncModes.Batched ? TransformSyncModes.PerInstance : TransformSyncModes.Batched;

            foreach (var networkManager in m_NetworkManagers)
            {
                networkManager.NetworkConfig.TransformSyncMode = otherMode;
            }

            foreach (var networkManager in m_NetworkManagers)
            {
                Assert.AreEqual(m_SyncMode, networkManager.NetworkConfig.ActiveTransformSyncMode,
                    $"{networkManager.name} changed to {networkManager.NetworkConfig.ActiveTransformSyncMode} while its session was running!");
            }

            Assert.AreEqual(configHash, m_ServerNetworkManager.NetworkConfig.GetConfig(false),
                "The connection configuration hash changed while the session was running!");

            // An instance spawning after the write still follows the mode the session started with.
            var afterChange = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => IsRegisteredForBatching(afterChange) == (m_SyncMode == TransformSyncModes.Batched));
            AssertOnTimeout($"An instance spawned after the change has StateManagerIndex {afterChange.StateManagerIndex} under {m_SyncMode}!");
        }
    }
}
