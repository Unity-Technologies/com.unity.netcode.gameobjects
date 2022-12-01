using System.Text;
using System.Collections;
using Unity.Netcode.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TestProject.ManualTests;

namespace TestProject.RuntimeTests
{
    public class NestedNetworkTransformTests : IntegrationTestWithApproximation
    {
        private const string k_TestScene = "NestedNetworkTransformTestScene";
        private const string k_PlayerToLoad = "PlayerNestedTransforms";

        protected override int NumberOfClients => 0;

        private Scene m_BaseSceneLoaded;
        private Scene m_OriginalActiveScene;

        private Object m_PlayerPrefabResource;

        protected override void OnOneTimeSetup()
        {
            ChildMover.RandomizeScale = true;
            // Preserve the test runner scene that is currently the active scene
            m_OriginalActiveScene = SceneManager.GetActiveScene();
            // Load our test's scene used by all client players (it gets set as the currently active scene when loaded)
            m_PlayerPrefabResource = Resources.Load(k_PlayerToLoad);
            Assert.NotNull(m_PlayerPrefabResource, $"Failed to load resource {k_PlayerToLoad}");

            // Migrate the resource into the DDOL to prevent the server from thinking it is in-scene placed.
            Object.DontDestroyOnLoad(m_PlayerPrefabResource);
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.LoadScene(k_TestScene, LoadSceneMode.Additive);
            base.OnOneTimeSetup();
        }

        protected override void OnOneTimeTearDown()
        {
            ChildMover.RandomizeScale = false;
            // Set test runner's scene back to the currently active scene
            if (m_OriginalActiveScene.IsValid() && m_OriginalActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(m_OriginalActiveScene);
            }
            // Unload our base scene if it is still loaded
            if (m_BaseSceneLoaded.IsValid() && m_BaseSceneLoaded.isLoaded)
            {
                SceneManager.UnloadSceneAsync(m_BaseSceneLoaded);
            }
            base.OnOneTimeTearDown();
        }

        protected override IEnumerator OnSetup()
        {
            yield return WaitForConditionOrTimeOut(() => m_BaseSceneLoaded.IsValid() && m_BaseSceneLoaded.isLoaded);
            AssertOnTimeout($"Timed out waiting for scene {k_TestScene} to load!");
            yield return base.OnSetup();
        }

        private void SceneManager_sceneLoaded(Scene sceneLoaded, LoadSceneMode loadSceneMode)
        {
            if (loadSceneMode == LoadSceneMode.Additive && sceneLoaded.name == k_TestScene)
            {
                m_BaseSceneLoaded = sceneLoaded;
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                SceneManager.SetActiveScene(sceneLoaded);
            }
        }

        protected override IEnumerator OnTearDown()
        {
            // This prevents us from trying to destroy the resource loaded
            m_PlayerPrefab = null;
            return base.OnTearDown();
        }


        protected override void OnCreatePlayerPrefab()
        {
            // Destroy the default player prefab
            Object.DestroyImmediate(m_PlayerPrefab);
            // Assign the network prefab resource loaded
            m_PlayerPrefab = m_PlayerPrefabResource as GameObject;
            base.OnCreatePlayerPrefab();
        }

        /// <summary>
        /// Prevent the server from telling the clients to load our test scene
        /// </summary>
        private bool VerifySceneServer(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName == k_TestScene)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Increase the threshold as we are just testing that the
        /// NetworkTransform is synchronizing properly and there are
        /// known issues with Euler rotation that will be fixed when
        /// we start synchronizing Quaternions.
        /// </summary>
        protected override float GetDeltaVarianceThreshold()
        {
            return 0.1f;
        }

        private StringBuilder m_ValidationErrors;
        /// <summary>
        /// Validates that all player transforms are approximately the
        /// same when a new client joins.
        /// </summary>
        /// <returns></returns>
        private bool ValidateNetworkTransforms()
        {
            m_ValidationErrors.Clear();
            foreach (var connectedClient in m_ServerNetworkManager.ConnectedClientsIds)
            {
                var playerToValidate = m_PlayerNetworkObjects[connectedClient][connectedClient];
                var playerNetworkTransforms = playerToValidate.GetComponentsInChildren<NetworkTransform>();
                foreach (var playerRelative in m_PlayerNetworkObjects)
                {
                    if (playerRelative.Key == connectedClient)
                    {
                        continue;
                    }
                    var relativeClonedTransforms = playerRelative.Value[connectedClient].GetComponentsInChildren<NetworkTransform>();
                    for (int i = 0; i < playerNetworkTransforms.Length; i++)
                    {
                        if (!Approximately(playerNetworkTransforms[i].transform.position, relativeClonedTransforms[i].transform.position))
                        {
                            m_ValidationErrors.Append($"[Position][Client-{connectedClient} {playerNetworkTransforms[i].transform.position}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} {relativeClonedTransforms[i].transform.position}]");
                        }
                        if (!Approximately(playerNetworkTransforms[i].transform.eulerAngles, relativeClonedTransforms[i].transform.eulerAngles))
                        {
                            m_ValidationErrors.Append($"[Rotation][Client-{connectedClient} {playerNetworkTransforms[i].transform.eulerAngles}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} {relativeClonedTransforms[i].transform.eulerAngles}]");
                        }
                        if (!Approximately(playerNetworkTransforms[i].transform.localScale, relativeClonedTransforms[i].transform.localScale))
                        {
                            m_ValidationErrors.Append($"[Scale][Client-{connectedClient} {playerNetworkTransforms[i].transform.localScale}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} {relativeClonedTransforms[i].transform.localScale}]");
                        }
                    }
                }
            }
            return m_ValidationErrors.Length == 0;
        }

        [UnityTest]
        public IEnumerator NestedNetworkTransformSynchronization()
        {
            m_ValidationErrors = new StringBuilder();
            var waitPeriod = new WaitForSeconds(1.0f);
            m_ServerNetworkManager.SceneManager.VerifySceneBeforeLoading = VerifySceneServer;
            yield return waitPeriod;

            // Spawn 5 more clients over time
            for (int i = 0; i < 5; i++)
            {
                yield return CreateAndStartNewClient();
                // Stop all movement for all players
                AutomatedPlayerMover.StopMovement = true;
                // Validate that the transforms are approximately the same
                yield return WaitForConditionOrTimeOut(ValidateNetworkTransforms);
                AssertOnTimeout($"Timed out waiting for all nested NetworkTransform cloned instances to match:\n {m_ValidationErrors}");
                // Continue player movement
                AutomatedPlayerMover.StopMovement = false;
                // Allow the players to move a bit.
                yield return waitPeriod;
            }

            // Just a final sanity check to make sure position and rotation match
            AutomatedPlayerMover.StopMovement = true;
            yield return WaitForConditionOrTimeOut(ValidateNetworkTransforms);
        }

    }
}

