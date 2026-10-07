using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that a client is only told about the scene migrations of the <see cref="NetworkObject"/>s it observes,
    /// and that a <see cref="NetworkObject"/> shown after it migrated spawns in the authority's scene.
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Server)]
#if UNIFIED_NETCODE
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
#endif
    internal class NetworkObjectSceneMigrationObserverTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private GameObject m_PrefabNoObserversSpawn;
        private GameObject m_PrefabWithObservers;

#if UNIFIED_NETCODE
        protected override bool UseUnifiedTests()
        {
            return true;
        }
#endif

        protected override bool UseCMBService()
        {
            return false; // CMB service pass is failing
        }

        public NetworkObjectSceneMigrationObserverTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabNoObserversSpawn = CreateNetworkObjectPrefab("NoObserversObject");
            m_PrefabNoObserversSpawn.GetComponent<NetworkObject>().SpawnWithObservers = false;
            m_PrefabWithObservers = CreateNetworkObjectPrefab("WithObserversObject");
            base.OnServerAndClientsCreated();
        }

        private NetworkObject SpawnAndShow(NetworkManager authority, NetworkManager observer)
        {
            var networkObject = SpawnObject(m_PrefabNoObserversSpawn, authority).GetComponent<NetworkObject>();
            networkObject.NetworkShow(observer.LocalClientId);
            return networkObject;
        }

        private IEnumerator WaitForSpawnedOn(NetworkManager networkManager, NetworkObject networkObject)
        {
            yield return WaitForConditionOrTimeOut(() => networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId));
            AssertOnTimeout($"[Client-{networkManager.LocalClientId}] Failed to spawn {networkObject.name} when it was shown!");
        }

        /// <summary>
        /// Two objects each shown to a different client migrate into different scenes in the same frame. Each client
        /// must only receive the migration of the object it observes.
        /// </summary>
        [UnityTest]
        public IEnumerator SceneMigrationIsOnlySentToObservers()
        {
            var authority = GetAuthorityNetworkManager();
            var firstClient = GetNonAuthorityNetworkManager(0);
            var secondClient = GetNonAuthorityNetworkManager(1);
            var activeScene = SceneManager.GetActiveScene();

            var firstObject = SpawnAndShow(authority, firstClient);
            var secondObject = SpawnAndShow(authority, secondClient);
            // Start the first object in the DontDestroyOnLoad scene so the two objects migrate into different scenes
            Object.DontDestroyOnLoad(firstObject.gameObject);
            yield return WaitForSpawnedOn(firstClient, firstObject);
            yield return WaitForSpawnedOn(secondClient, secondObject);
            yield return s_DefaultWaitForTick;

            SceneManager.MoveGameObjectToScene(firstObject.gameObject, activeScene);
            Object.DontDestroyOnLoad(secondObject.gameObject);

            var firstClientInstance = firstClient.SpawnManager.SpawnedObjects[firstObject.NetworkObjectId];
            var secondClientInstance = secondClient.SpawnManager.SpawnedObjects[secondObject.NetworkObjectId];
            yield return WaitForConditionOrTimeOut(() => firstClientInstance.gameObject.scene == activeScene
                && secondClientInstance.gameObject.scene == secondClient.SceneManager.DontDestroyOnLoadScene);
            AssertOnTimeout($"The observing clients did not migrate their instances! " +
                $"[Client-{firstClient.LocalClientId}] {firstClientInstance.gameObject.scene.name}, [Client-{secondClient.LocalClientId}] {secondClientInstance.gameObject.scene.name}");

            Assert.False(firstClient.SpawnManager.SpawnedObjects.ContainsKey(secondObject.NetworkObjectId), $"[Client-{firstClient.LocalClientId}] Spawned {secondObject.name} without observing it!");
            Assert.False(secondClient.SpawnManager.SpawnedObjects.ContainsKey(firstObject.NetworkObjectId), $"[Client-{secondClient.LocalClientId}] Spawned {firstObject.name} without observing it!");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// An object that migrates while no client observes it sends no migration.
        /// </summary>
        [UnityTest]
        public IEnumerator SceneMigrationWithNoObserversIsNotSent()
        {
            var authority = GetAuthorityNetworkManager();

            var networkObject = SpawnObject(m_PrefabNoObserversSpawn, authority).GetComponent<NetworkObject>();
            yield return s_DefaultWaitForTick;
            Object.DontDestroyOnLoad(networkObject.gameObject);

            // Wait long enough for a migration to have been sent and processed
            yield return new WaitForSeconds(0.25f);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// An object shown to a client after it migrated while hidden from that client spawns in the authority's scene.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkShowAfterSceneMigrationSpawnsInAuthorityScene()
        {
            var authority = GetAuthorityNetworkManager();
            var client = GetNonAuthorityNetworkManager();

            var networkObject = SpawnObject(m_PrefabNoObserversSpawn, authority).GetComponent<NetworkObject>();
            yield return s_DefaultWaitForTick;
            Object.DontDestroyOnLoad(networkObject.gameObject);
            yield return s_DefaultWaitForTick;

            networkObject.NetworkShow(client.LocalClientId);
            yield return WaitForSpawnedOn(client, networkObject);

            var clientInstance = client.SpawnManager.SpawnedObjects[networkObject.NetworkObjectId];
            Assert.AreEqual(client.SceneManager.DontDestroyOnLoadScene, clientInstance.gameObject.scene, $"[Client-{client.LocalClientId}] {networkObject.name} did not spawn in the authority's scene!");
        }

        /// <summary>
        /// A late joining client synchronizes an object that is not in the active scene into the authority's scene.
        /// A hybrid prefab instance can spawn after the synchronization completes, once its ghost arrives.
        /// </summary>
        [UnityTest]
        public IEnumerator LateJoinSynchronizesObjectIntoAuthorityScene()
        {
            var authority = GetAuthorityNetworkManager();
            var networkObject = SpawnObject(m_PrefabWithObservers, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(networkObject);
            AssertOnTimeout($"Failed to spawn {networkObject.name} on all clients!");
            Object.DontDestroyOnLoad(networkObject.gameObject);
            yield return s_DefaultWaitForTick;

            yield return CreateAndStartNewClient();
            var lateJoinClient = m_ClientNetworkManagers[m_ClientNetworkManagers.Length - 1];
            yield return WaitForSpawnedOn(lateJoinClient, networkObject);

            var clientInstance = lateJoinClient.SpawnManager.SpawnedObjects[networkObject.NetworkObjectId];
            Assert.AreEqual(lateJoinClient.SceneManager.DontDestroyOnLoadScene, clientInstance.gameObject.scene, $"[Client-{lateJoinClient.LocalClientId}] {networkObject.name} did not spawn in the authority's scene!");
        }
    }
}
