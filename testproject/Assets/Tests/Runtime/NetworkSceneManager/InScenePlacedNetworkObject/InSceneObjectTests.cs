using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;



namespace TestProject.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, HostOrServer.DAHost)]
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.Host)]
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.Server)]
    public class InSceneObjectTests : InSceneObjectBase
    {
        protected override int NumberOfClients => 2;

        public InSceneObjectTests(NetworkTopologyTypes networkTopologyType, HostOrServer hostOrServer) : base(networkTopologyType, hostOrServer) { }


        private int m_NumberOfInstancesCheck;


        /// <summary>
        /// This validates that users can despawn in-scene placed NetworkObjects and disable the
        /// associated GameObject when OnNetworkDespawn is invoked while still being able to
        /// re-spawn the same in-scene placed NetworkObject.
        /// This test validates this for:
        /// - Currently connected clients
        /// - Late joining client
        /// - Scene switching and having the server despawn the NetworkObject the first time it is spawned.
        /// </summary>
        [UnityTest]
        public IEnumerator EnableDisableInSceneObjectTests()
        {
            NetworkObjectTestComponent.ServerNetworkObjectInstance = null;
            // Enabled disabling the NetworkObject when it is despawned
            NetworkObjectTestComponent.DisableOnDespawn = true;
            var authority = GetAuthorityNetworkManager();

            authority.SceneManager.LoadScene(SceneToLoad, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(HaveAllClientsLoadedScene);
            AssertOnTimeout($"Timed out waiting for {SceneToLoad} scene to be loaded on all clients!");

            // Verify all connected clients spawned the in-scene placed NetworkObject
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawnedInSceneObject);
            AssertOnTimeout($"Timed out waiting for all instances to be spawned and enabled!");

            var serverInSceneObjectInstance = NetworkObjectTestComponent.ServerNetworkObjectInstance;
            Assert.IsNotNull(serverInSceneObjectInstance, $"Could not get the server-side registration of {nameof(NetworkObjectTestComponent)}!");

            // Test #1: Despawn the in-scene placed NetworkObject and verify it is despawned and disabled on the clients
            serverInSceneObjectInstance.Despawn(false);

            yield return WaitForConditionOrTimeOut(HaveAllClientsDespawnedInSceneObject);
            AssertOnTimeout($"[Test #1] Timed out waiting for all instances to be despawned and disabled!");

            // Test #2: Late-join a client and re-verify that all in-scene placed object instances are still disabled
            var newlyJoinedClient = CreateNewClient();
            yield return StartClient(newlyJoinedClient);

            yield return WaitForConditionOrTimeOut(HaveAllClientsDespawnedInSceneObject);
            AssertOnTimeout($"[Test #2] Timed out waiting for all instances to be despawned and disabled!");

            // Test #3: Now spawn the same in-scene placed NetworkObject
            serverInSceneObjectInstance.gameObject.SetActive(true);
            serverInSceneObjectInstance.Spawn();
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawnedInSceneObject);

            // Test #4: Now unload the in-scene object's scene and scene switch to the same scene while
            // also having the server-side disable the in-scene placed NetworkObject and verify all
            // connected clients completed the scene switch and that all in-scene placed NetworkObjects
            // are despawned and disabled.

            NetworkObjectTestComponent.ServerNetworkObjectInstance = null;
            NetworkObjectTestComponent.DisableOnSpawn = true;

            authority.SceneManager.UnloadScene(GetLoadedScene());

            yield return WaitForConditionOrTimeOut(HaveAllClientsUnloadedScene);
            AssertOnTimeout($"Timed out waiting for {SceneToLoad} scene to be unloaded on all clients!");

            // Verify the spawned instances list is empty
            Assert.True(NetworkObjectTestComponent.SpawnedInstances.Count == 0, $"There are {NetworkObjectTestComponent.SpawnedInstances.Count} that did not despawn when the scene was unloaded!");

            // Go ahead and clear out the despawned instances list
            NetworkObjectTestComponent.DespawnedInstances.Clear();

            // Now scene switch (LoadSceneMode.Single) into the scene with the in-scene placed NetworkObject we have been testing
            authority.SceneManager.LoadScene(SceneToLoad, LoadSceneMode.Single);

            yield return WaitForConditionOrTimeOut(HaveAllClientsLoadedScene);
            AssertOnTimeout($"Timed out waiting for {SceneToLoad} scene to be loaded on all clients!");

            // Verify all client instances are disabled and despawned when done scene switching
            yield return WaitForConditionOrTimeOut(HaveAllClientsDespawnedInSceneObject);
            AssertOnTimeout($"[Test #4] Timed out waiting for all instances to be despawned and disabled!");

            serverInSceneObjectInstance = NetworkObjectTestComponent.ServerNetworkObjectInstance;
            Assert.IsNotNull(serverInSceneObjectInstance, $"[Test #4] Could not get the server-side registration of {nameof(NetworkObjectTestComponent)}!");

            // Test #5: Now spawn the in-scene placed NetworkObject
            serverInSceneObjectInstance.gameObject.SetActive(true);
            serverInSceneObjectInstance.Spawn();

            // Verify all clients spawned their in-scene NetworkObject relative instance
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawnedInSceneObject);
            AssertOnTimeout("[Test #2] Timed out waiting for all instances to be enabled and spawned!");
        }
    }

    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    internal class InSceneObjectClientTests : NetcodeIntegrationTest
    {
        private const string k_SceneToLoad = "InSceneNetworkObject";

        protected override int NumberOfClients => 0;

        private Scene m_Scene;

        public InSceneObjectClientTests(NetworkTopologyTypes topology) : base(topology)
        {
        }

        protected override IEnumerator OnSetup()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);
            return base.OnSetup();
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == k_SceneToLoad && loadSceneMode == LoadSceneMode.Additive)
            {
                m_Scene = scene;
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            }
        }

        protected override IEnumerator OnTearDown()
        {
            if (m_Scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(m_Scene);
            }
            return base.OnTearDown();
        }

        [UnityTest]
        public IEnumerator DespawnAndDestroyNetworkObjects()
        {
            // Simulate a client disconnecting early by just invoking DespawnAndDestroyNetworkObjects to assure
            // this method does not destroy in-scene placed NetworkObjects.
            GetAuthorityNetworkManager().SpawnManager.DespawnAndDestroyNetworkObjects();

            yield return s_DefaultWaitForTick;

            var inSceneObject = GameObject.Find("InSceneObject");
            Assert.IsNotNull(inSceneObject, $"Could not find the in-scene placed {nameof(NetworkObject)}: InSceneObject!");
        }
    }
}
