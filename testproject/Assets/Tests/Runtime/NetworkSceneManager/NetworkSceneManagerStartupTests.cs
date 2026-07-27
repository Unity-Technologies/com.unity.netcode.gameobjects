using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Validates <see cref="NetworkSceneManager"/> client synchronization.
    /// </summary>
    /// <remarks>
    /// This includes both client synchronization mode passes along with verifying
    /// that <see cref="NetworkObject"/>s migrated into the DDOL will still be spawned
    /// and preserve their <see cref="NetworkObject.InScenePlaced"/> value.
    /// </remarks>
    internal class NetworkSceneManagerStartupTests : NetcodeIntegrationTest
    {
        private const string k_ActiveScene = "SessionSynchronize";
        private const string k_AdditionalScene = "InSceneNetworkObjectMovesToDDOL";

        private readonly List<NetworkObject> m_ObjectsInScenes = new List<NetworkObject>();
        private Scene m_OriginalActiveScene;
        private Scene m_SceneLoaded;
        private bool m_CanStart = false;

        // Used with scene pre-loading
        private string m_SceneToLoad;
        private bool m_SceneWasLoaded;

        #region NetcodeIntegrationTest overrides
        protected override int NumberOfClients => 0;
        protected override bool CanStartServerAndClients() => m_CanStart;

        protected override void OnOneTimeSetup()
        {
            // Get the active scene prior to any interation running through the OnSetup.
            m_OriginalActiveScene = SceneManager.GetActiveScene();
            base.OnOneTimeSetup();
        }

        protected override IEnumerator OnSetup()
        {
            // Always reset
            m_CanStart = false;
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            // Avoid trying to spawn this
            Object.DontDestroyOnLoad(m_PlayerPrefab);
            base.OnCreatePlayerPrefab();
        }

        protected override IEnumerator OnTearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            m_ObjectsInScenes.Clear();
            // Restore the integration test scene as the active scene.
            SceneManager.SetActiveScene(m_OriginalActiveScene);

            // Unload everything else.
            for (int i = 0; i < SceneManager.sceneCount - 1; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == m_OriginalActiveScene)
                {
                    continue;
                }
                SceneManager.UnloadSceneAsync(scene);
                yield return WaitForConditionOrTimeOut(() => !scene.isLoaded);
            }
            yield return base.OnTearDown();
        }
        #endregion

        /// <summary>
        /// Validates things migrated into the DDOL will be included when synchronizing clients.
        /// </summary>
        /// <param name="clientSynchronizationMode">The client synchronization mode to use for the current pass.</param>
        [UnityTest]
        public IEnumerator AllExistingObjectsAreSpawnedAtStartup([Values] LoadSceneMode clientSynchronizationMode)
        {
            LogAssert.ignoreFailingMessages = true;
            yield return PreLoadScene(k_ActiveScene);
            yield return PreLoadScene(k_AdditionalScene);
            SceneManager.SetActiveScene(m_SceneLoaded);

            var existingObjects = new List<NetworkObject>();
            var dontDestroyOnLoadCount = 0;

            // Now get everything migrated into the DDOL and the DDOL scene itself.
            var ddolScene = GetNetworkObjectsInDDOL();
            // Validate NetworkObjects in DDOL
            foreach (var obj in m_ObjectsInScenes)
            {
                Assert.IsFalse(obj.IsSpawned, $"NetworkObject {obj.name} should not have been spawned!");

                existingObjects.Add(obj);
                if (obj.gameObject.scene.name == ddolScene.name)
                {
                    dontDestroyOnLoadCount++;
                }
            }

            Assert.IsNotEmpty(existingObjects, $"Found no existing {nameof(NetworkObject)}s!");
            Assert.That(dontDestroyOnLoadCount, Is.GreaterThan(0), "Found no {nameof(NetworkObject)}s in the DDOL scene!");

            // Now enable starting server and clients and start the server
            m_CanStart = true;
            yield return StartServerAndClients();

            // Apply the test's client synchronization mode
            GetAuthorityNetworkManager().SceneManager.SetClientSynchronizationMode(clientSynchronizationMode);

            // Validate the existing objects
            foreach (var existingObject in existingObjects)
            {
                Assert.IsFalse(existingObject == null, "Expected existing object to still exist!");
                Assert.IsTrue(existingObject.IsSpawned, $"NetworkObject {existingObject.name} in scene {existingObject.gameObject.scene.name} was not spawned!");
                Assert.IsTrue(existingObject.InScenePlaced, $"NetworkObject {existingObject.name} in scene {existingObject.gameObject.scene.name} was not inScenePlaced!");
            }

            // If additive client synchronization mode, load the scenes that are already loaded
            // on the scene authority instance so they will be used during client synchronization.
            if (clientSynchronizationMode == LoadSceneMode.Additive)
            {
                yield return PreLoadScene(k_ActiveScene);
                yield return PreLoadScene(k_AdditionalScene);
            }

            // Late join a client
            yield return CreateAndStartNewClient();

            // Wait for all existing objects to spawn on the client
            yield return WaitForSpawnedOnAllOrTimeOut(existingObjects);
            AssertOnTimeout("Timed out waiting for objects to spawn on all clients!");
        }

        #region Scene loading and related methods

        /// <summary>
        /// Uses the <see cref="NetworkManager.Singleton"/>'s current scene which should be
        /// the DDOL scene.
        /// </summary>
        /// <returns>The DDOL scene</returns>
        private Scene GetNetworkObjectsInDDOL()
        {
            // This does catch any newly instantiated in-scene placed NetworkObjects moved into DDOL
            // during awake.
            var sceneToUse = NetworkManager.Singleton.gameObject.scene;
            Assert.IsTrue(sceneToUse.IsValid() && sceneToUse.name == "DontDestroyOnLoad", $"[{NetworkManager.Singleton.name}] Is not in the DDOL! Is this being invoked too early?");

            foreach (var rootObject in sceneToUse.GetRootGameObjects())
            {
                foreach (var networkObject in rootObject.GetComponentsInChildren<NetworkObject>())
                {
                    if (!m_ObjectsInScenes.Contains(networkObject) && networkObject.InScenePlaced)
                    {
                        m_ObjectsInScenes.Add(networkObject);
                    }
                }
            }
            return sceneToUse;
        }

        private IEnumerator PreLoadScene(string sceneName)
        {
            m_SceneToLoad = sceneName;
            m_SceneWasLoaded = false;
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            yield return WaitForConditionOrTimeOut(() => m_SceneWasLoaded);
            AssertOnTimeout("Timed out waiting for scene to load!");
            SceneManager.sceneLoaded -= OnSceneLoad;
        }

        private void TrackObjectsInScene(Scene scene)
        {
            // This does not catch things moved into the DDOL during awake.
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                foreach (var networkObject in rootObject.GetComponentsInChildren<NetworkObject>())
                {
                    m_ObjectsInScenes.Add(networkObject);
                }
            }
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (m_SceneToLoad == scene.name)
            {
                m_SceneWasLoaded = true;
                m_SceneLoaded = scene;
                TrackObjectsInScene(scene);
            }
        }
        #endregion
    }
}
