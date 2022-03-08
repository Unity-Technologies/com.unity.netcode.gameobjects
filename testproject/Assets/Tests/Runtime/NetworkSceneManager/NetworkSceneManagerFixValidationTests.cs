using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Use this test group for validating NetworkSceneManager fixes.
    /// </summary>
    public class NetworkSceneManagerFixValidationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        /// <summary>
        /// This validation test verifies that the NetworkSceneManager will not crash if
        /// the SpawnManager.SpawnedObjectsList contains destroyed and invalid NetworkObjects.
        /// </summary>
        [Test]
        public void DDOLPopulateWithNullNetworkObjectsValidation([Values] bool useHost)
        {
            var gameObject = new GameObject();
            var networkObject = gameObject.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = gameObject });

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = gameObject });
            }

            // Start the host and clients
            if (!NetcodeIntegrationTestHelpers.Start(useHost, m_ServerNetworkManager, m_ClientNetworkManagers))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Spawn some NetworkObjects
            var spawnedNetworkObjects = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                var instance = Object.Instantiate(gameObject);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                instanceNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
                instanceNetworkObject.Spawn();
                spawnedNetworkObjects.Add(instance);
            }

            // Add a bogus entry to the SpawnManager
            m_ServerNetworkManager.SpawnManager.SpawnedObjectsList.Add(null);

            // Verify moving all NetworkObjects into the DDOL when some might be invalid will not crash
            m_ServerNetworkManager.SceneManager.MoveObjectsToDontDestroyOnLoad();

            // Verify moving all NetworkObjects from DDOL back into the active scene will not crash even if some are invalid
            m_ServerNetworkManager.SceneManager.MoveObjectsFromDontDestroyOnLoadToScene(SceneManager.GetActiveScene());

            // Now remove the invalid object
            m_ServerNetworkManager.SpawnManager.SpawnedObjectsList.Remove(null);

            // As long as there are no exceptions this test passes
        }
    }
}
