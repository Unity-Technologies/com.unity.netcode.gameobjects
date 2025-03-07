using System.Collections;
using System.Collections.Generic;
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
    public class SceneObjectsNotDestroyedOnShutdownTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private const string k_TestScene = "InSceneNetworkObject";
        private const string k_SceneObjectName = "InSceneObject";
        private Scene m_TestScene;
        private WaitForSeconds m_DefaultWaitForTick = new WaitForSeconds(1.0f / 30);

        private NetworkObject m_LoadedSceneObject;
        private List<NetworkObject> m_LoadedSceneOBjects = new List<NetworkObject>();

        private bool FoundLoadedSceneObject()
        {
            m_LoadedSceneObject = null;
#if UNITY_2023_1_OR_NEWER
            m_LoadedSceneObject = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name.Contains(k_SceneObjectName)).FirstOrDefault();
#else
            m_LoadedSceneObject = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.name.Contains(k_SceneObjectName)).FirstOrDefault();
#endif
            return m_LoadedSceneObject != null;
        }

        private bool FoundLoadedSceneObjects()
        {
            m_LoadedSceneOBjects.Clear();
#if UNITY_2023_1_OR_NEWER
            var loadedInSceneObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name.Contains(k_SceneObjectName));
#else
            var loadedInSceneObjects = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.name.Contains(k_SceneObjectName));
#endif
            if (loadedInSceneObjects.Count() == 0)
            {
                return false;
            }
            m_LoadedSceneOBjects.AddRange(loadedInSceneObjects);
            return true;
        }

        [UnityTest]
        public IEnumerator SceneObjectsNotDestroyedOnShutdown()
        {
            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            m_ServerNetworkManager.SceneManager.LoadScene(k_TestScene, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(() => m_TestScene.IsValid() && m_TestScene.isLoaded);
            AssertOnTimeout($"Timed out waiting for scene {k_TestScene} to load!");

            yield return WaitForConditionOrTimeOut(FoundLoadedSceneObject);
            AssertOnTimeout($"Failed to find {k_SceneObjectName} before starting client!");
            yield return CreateAndStartNewClient();

            yield return WaitForConditionOrTimeOut(FoundLoadedSceneObjects);
            AssertOnTimeout($"Failed to find {k_SceneObjectName} objects after starting Client-{m_ClientNetworkManagers[0].LocalClientId}!");

            Assert.IsTrue(m_LoadedSceneOBjects.Count() > 1, $"Only found one instance of {k_SceneObjectName} after Client-{m_ClientNetworkManagers[0].LocalClientId} connected!");
            m_ClientNetworkManagers[0].Shutdown();
            yield return m_DefaultWaitForTick;
            yield return WaitForConditionOrTimeOut(FoundLoadedSceneObjects);
            AssertOnTimeout($"Failed to find {k_SceneObjectName} objects after shutting down client!");
            Assert.IsTrue(m_LoadedSceneOBjects.Count() > 1, $"Only found one instance of {k_SceneObjectName} after client shutdown!");
        }


        [UnityTest]
        public IEnumerator ChildSceneObjectsDoNotDestroyOnShutdown()
        {
            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            m_ServerNetworkManager.SceneManager.LoadScene(k_TestScene, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(() => m_TestScene.IsValid() && m_TestScene.isLoaded);
            AssertOnTimeout($"Timed out waiting for scene {k_TestScene} to load!");

            yield return WaitForConditionOrTimeOut(FoundLoadedSceneObject);
            AssertOnTimeout($"Failed to find {k_SceneObjectName} before starting client!");

            yield return CreateAndStartNewClient();

            var clientId = m_ClientNetworkManagers[0].LocalClientId;
            Assert.IsTrue(m_LoadedSceneObject.TrySetParent(m_PlayerNetworkObjects[0][clientId]), $"Failed to parent in-scene object under client player");

            yield return WaitForConditionOrTimeOut(() => PlayerHasChildren(clientId));
            AssertOnTimeout($"Client-{clientId} player never parented {k_SceneObjectName}!");

            yield return WaitForConditionOrTimeOut(FoundLoadedSceneObjects);
            AssertOnTimeout($"Failed to find {k_SceneObjectName} objects before shutting down Client-{m_ClientNetworkManagers[0].LocalClientId}!");
            Assert.IsTrue(m_LoadedSceneOBjects.Count() > 1, $"Only found one instance of {k_SceneObjectName} after client connected!");
            m_ClientNetworkManagers[0].Shutdown();
            yield return m_DefaultWaitForTick;

            // Sanity check to make sure the client's player no longer has any children
            yield return WaitForConditionOrTimeOut(() => PlayerNoLongerExistsWithChildren(clientId));
            AssertOnTimeout($"Client-{clientId} player still exits with children after client shutdown!");

            yield return WaitForConditionOrTimeOut(FoundLoadedSceneObjects);
            AssertOnTimeout($"Failed to find {k_SceneObjectName} objects after shutting down client!");

            // Make sure any in-scene placed NetworkObject instantiated has no parent
            foreach (var insceneObject in m_LoadedSceneOBjects)
            {
                Assert.IsTrue(insceneObject.transform.parent == null, $"{insceneObject.name} is still parented!");
            }

            // We should have exactly 2 in-scene placed NetworkObjects remaining:
            // One instance on host side and one on the disconnected client side.
            Assert.IsTrue(m_LoadedSceneOBjects.Count() == 2, $"Only found one instance of {k_SceneObjectName} after client shutdown!");
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
            if (m_PlayerNetworkObjects[0].ContainsKey(clientId) && m_PlayerNetworkObjects[0][clientId] != null)
            {
                return m_PlayerNetworkObjects[0][clientId].transform.childCount == 0;
            }
            return true;
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId != m_ServerNetworkManager.LocalClientId)
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
