using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    internal class InScenePlacedProcessorTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private static readonly string k_DynamicObjectMoverScene = "MoveADynamicObjectInAwake";

        private GameObject m_DynamicSpawnPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_DynamicSpawnPrefab = CreateNetworkObjectPrefab("DynamicSpawnObject");
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator InScenePlacedProcessorSkipsMovedObject()
        {
            var authority = GetAuthorityNetworkManager();
            var spawnedObj = SpawnObject(m_DynamicSpawnPrefab, authority).GetComponent<NetworkObject>();

            yield return WaitForSpawnedOnAllOrTimeOut(spawnedObj);
            AssertOnTimeout("Timed out waiting for object to spawn!");

            Assert.IsFalse(spawnedObj.InScenePlaced, "Dynamically spawned object should not be InScenePlaced!");

            authority.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            var status = authority.SceneManager.LoadScene(k_DynamicObjectMoverScene, LoadSceneMode.Additive);
            Assert.True(status == SceneEventProgressStatus.Started, $"Failed to start lading scene {k_DynamicObjectMoverScene} with status {status}!");
            yield return WaitForConditionOrTimeOut(() => m_SceneLoaded.IsValid());
            AssertOnTimeout("Timed out waiting for scene to load!");

            var movers = FindObjects.ByType<MoveDynamicSpawnInAwake>();
            foreach (var mover in movers)
            {
                var movedObject = mover.MovedObject;
                Assert.IsFalse(movedObject == null);
                Assert.IsFalse(movedObject.InScenePlaced, "Dynamically spawned object should not be re-processed as InScenePlaced!");
                Assert.AreEqual(mover.gameObject.scene, movedObject.gameObject.scene, "Object should have moved scenes!");
            }

            yield return CreateAndStartNewClient();
            AssertOnTimeout("Timed out waiting for late joining client!");

            yield return WaitForSpawnedOnAllOrTimeOut(spawnedObj);
            AssertOnTimeout("Timed out waiting for object to be spawned on late joining client!");
        }

        private Scene m_SceneLoaded;
        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            var authority = GetAuthorityNetworkManager();
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        if (sceneEvent.ClientId == authority.LocalClientId)
                        {
                            m_SceneLoaded = sceneEvent.Scene;
                        }
                        return;
                    }
            }
        }
    }
}
