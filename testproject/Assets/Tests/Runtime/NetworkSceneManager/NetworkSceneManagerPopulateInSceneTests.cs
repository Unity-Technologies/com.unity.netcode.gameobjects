using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    public class NetworkSceneManagerPopulateInSceneTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;
        protected const int k_NumberOfInstances = 5;

        protected Dictionary<uint, GameObject> m_InSceneObjectList = new Dictionary<uint, GameObject>();

        protected override IEnumerator OnSetup()
        {
            m_InSceneObjectList.Clear();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            // Create several prefabs to simulate scene-placed NetworkObjects which
            // are spawned upon the server starting
            for (int i = 0; i < k_NumberOfInstances; i++)
            {
                var inScenePrefab = CreateNetworkObjectPrefab($"NewSceneObject-{i}");
                var networkObject = inScenePrefab.GetComponent<NetworkObject>();
                networkObject.IsSceneObject = null;
                networkObject.NetworkManagerOwner = m_ServerNetworkManager;
                m_InSceneObjectList.Add(networkObject.GlobalObjectIdHash, inScenePrefab);

                inScenePrefab = CreateNetworkObjectPrefab($"SetInSceneObject-{i}");
                networkObject = inScenePrefab.GetComponent<NetworkObject>();
                networkObject.IsSceneObject = true;
                networkObject.NetworkManagerOwner = m_ServerNetworkManager;
                m_InSceneObjectList.Add(networkObject.GlobalObjectIdHash, inScenePrefab);
            }
        }

        [UnityTest]
        public IEnumerator PopulateScenePlacedObjectsTest()
        {
            var activeScene = SceneManager.GetActiveScene();

            m_ServerNetworkManager.SceneManager.PopulateScenePlacedObjects(activeScene, true);
            var scenePlacedNetworkObjects = m_ServerNetworkManager.SceneManager.ScenePlacedObjects;
            foreach (var entry in m_InSceneObjectList)
            {
                Assert.IsTrue(scenePlacedNetworkObjects.ContainsKey(entry.Key), $"Failed to find {nameof(NetworkObject.GlobalObjectIdHash)}({entry.Key}) for " +
                    $"{entry.Value.name} in the {nameof(NetworkSceneManager.ScenePlacedObjects)}!");

                Assert.IsTrue(scenePlacedNetworkObjects[entry.Key].ContainsKey(activeScene.handle), $"Failed to find the scene handle {activeScene.handle} ({activeScene.name}) entry for " +
                    $"{entry.Value.name} in the {nameof(NetworkSceneManager.ScenePlacedObjects)}!");
                var inSceneGameObject = scenePlacedNetworkObjects[entry.Key][activeScene.handle].gameObject;
                Assert.IsTrue(inSceneGameObject == entry.Value, $"{nameof(GameObject)} {entry.Value.name} is not the same as {inSceneGameObject.name}!");
            }

            yield break;
        }

        protected override IEnumerator OnTearDown()
        {
            foreach(var spawnedInstance in m_InSceneObjectList)
            {
                Object.Destroy(spawnedInstance.Value);
            }
            return base.OnTearDown();
        }
    }
}
