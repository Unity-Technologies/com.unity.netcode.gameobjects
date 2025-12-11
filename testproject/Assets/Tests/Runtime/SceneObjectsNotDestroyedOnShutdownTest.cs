using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    public class SceneObjectsNotDestroyedOnShutdownTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private const string k_TestScene = "InSceneNetworkObject";
        private const string k_SceneObjectName = "InSceneObject";
        private Scene m_TestScene;
        private WaitForSeconds m_DefaultWaitForTick = new(1.0f / 30);

        public SceneObjectsNotDestroyedOnShutdownTest(NetworkTopologyTypes topology) : base(topology) { }

        [UnityTest]
        public IEnumerator SceneObjectsNotDestroyedOnShutdown()
        {
            var authority = GetAuthorityNetworkManager();
            authority.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            authority.SceneManager.LoadScene(k_TestScene, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(() => m_TestScene.IsValid() && m_TestScene.isLoaded);
            AssertOnTimeout($"Timed out waiting for scene {k_TestScene} to load!");
            var loadedInSceneObject = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name.Contains(k_SceneObjectName)).FirstOrDefault();
            Assert.IsNotNull(loadedInSceneObject, $"Failed to find {k_SceneObjectName} before starting client!");

            AssertOnTimeout($"Timed out waiting to find {k_SceneObjectName} after scene load and before starting client!\"");

            var lateJoin = CreateNewClient();
            yield return StartClient(lateJoin);

            var loadedInSceneObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name.Contains(k_SceneObjectName));
            Assert.IsTrue(loadedInSceneObjects.Count() > 1, $"Only found one instance of {k_SceneObjectName} after client connected!");
            lateJoin.Shutdown();
            yield return m_DefaultWaitForTick;
            loadedInSceneObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name.Contains(k_SceneObjectName));

            Assert.IsTrue(loadedInSceneObjects.Count() > 1, $"Only found one instance of {k_SceneObjectName} after client shutdown!");
        }

        [UnityTest]
        public IEnumerator ChildSceneObjectsDoNotDestroyOnShutdown()
        {
            var authority = GetAuthorityNetworkManager();
            authority.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            authority.SceneManager.LoadScene(k_TestScene, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(() => m_TestScene.IsValid() && m_TestScene.isLoaded);
            AssertOnTimeout($"Timed out waiting for scene {k_TestScene} to load!");

            var loadedInSceneObject = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name.Contains(k_SceneObjectName)).FirstOrDefault();
            Assert.IsNotNull(loadedInSceneObject, $"Failed to find {k_SceneObjectName} before starting client!");

            var lateJoin = CreateNewClient();
            yield return StartClient(lateJoin);

            var clientId = lateJoin.LocalClientId;
            Assert.IsTrue(loadedInSceneObject.TrySetParent(m_PlayerNetworkObjects[authority.LocalClientId][clientId]), $"Failed to parent in-scene object under client player");

            yield return WaitForConditionOrTimeOut(() => PlayerHasChildren(clientId));
            AssertOnTimeout($"Client-{clientId} player never parented {k_SceneObjectName}!");

            var loadedInSceneObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name.Contains(k_SceneObjectName));
            Assert.IsTrue(loadedInSceneObjects.Count() > 1, $"Only found one instance of {k_SceneObjectName} after client connected!");
            lateJoin.Shutdown();
            yield return m_DefaultWaitForTick;

            // Sanity check to make sure the client's player no longer has any children
            yield return WaitForConditionOrTimeOut(() => PlayerNoLongerExistsWithChildren(clientId));
            AssertOnTimeout($"Client-{clientId} player still exits with children after client shutdown!");

            loadedInSceneObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where(o => o.name.Contains(k_SceneObjectName)).ToArray();
            // Make sure any in-scene placed NetworkObject instantiated has no parent
            foreach (var inSceneObject in loadedInSceneObjects)
            {
                Assert.IsTrue(inSceneObject.transform.parent == null, $"{inSceneObject.name} is still parented!");
            }

            // We should have exactly 2 in-scene placed NetworkObjects remaining:
            // One instance on host side and one on the disconnected client side.
            Assert.IsTrue(loadedInSceneObjects.Count() == 2, $"Only found one instance of {k_SceneObjectName} after client shutdown!");
        }

        private bool PlayerHasChildren(ulong clientId)
        {
            if (m_PlayerNetworkObjects[clientId].ContainsKey(clientId) && m_PlayerNetworkObjects[clientId][clientId] != null)
            {
                return m_PlayerNetworkObjects[clientId][clientId].transform.childCount > 0;
            }
            return false;
        }

        private bool PlayerNoLongerExistsWithChildren(ulong clientId)
        {
            var authorityId = GetAuthorityNetworkManager().LocalClientId;
            if (m_PlayerNetworkObjects[authorityId].ContainsKey(clientId) && m_PlayerNetworkObjects[authorityId][clientId] != null)
            {
                return m_PlayerNetworkObjects[authorityId][clientId].transform.childCount == 0;
            }
            return true;
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId != GetAuthorityNetworkManager().LocalClientId)
            {
                return;
            }

            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && sceneEvent.SceneName == k_TestScene)
            {
                m_TestScene = sceneEvent.Scene;
            }
        }
    }
}
