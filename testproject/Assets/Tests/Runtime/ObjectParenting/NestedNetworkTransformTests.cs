using System.Text;
using System.Collections;
using Unity.Netcode.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TestProject.ManualTests;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;

namespace TestProject.RuntimeTests
{
    [TestFixture(Interpolation.NoInterpolation)]
    [TestFixture(Interpolation.Interpolation)]
    public class NestedNetworkTransformTests : IntegrationTestWithApproximation
    {
        private const string k_TestScene = "NestedNetworkTransformTestScene";
        private const string k_PlayerToLoad = "PlayerNestedTransforms";

        protected override int NumberOfClients => 0;

        private Scene m_BaseSceneLoaded;
        private Scene m_OriginalActiveScene;

        private Object m_PlayerPrefabResource;
        private Interpolation m_Interpolation;

        public enum Interpolation
        {
            NoInterpolation,
            Interpolation
        }

        public NestedNetworkTransformTests(Interpolation interpolation)
        {
            m_Interpolation = interpolation;
        }

        public NestedNetworkTransformTests()
        {

        }

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
            m_PlayerPrefab.GetComponent<NetworkTransform>().Interpolate = m_Interpolation == Interpolation.Interpolation;
            var networkTransforms = m_PlayerPrefab.GetComponentsInChildren<NetworkTransform>();
            foreach(var networkTransform in networkTransforms)
            {
                networkTransform.Interpolate = m_Interpolation == Interpolation.Interpolation;
            }

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

        private const float k_PositionVariance = 0.01795f;
        private const float k_RotationVariance = 0.05f;

        private float m_CurrentVariance = k_PositionVariance;
        override protected float GetDeltaVarianceThreshold()
        {
            return m_CurrentVariance;
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
                var playerToValidate = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][connectedClient];
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
                        m_CurrentVariance = k_PositionVariance;
                        if (!Approximately(playerNetworkTransforms[i].transform.position, relativeClonedTransforms[i].transform.position))
                        {
                            m_ValidationErrors.Append($"[Position][Client-{connectedClient} {playerNetworkTransforms[i].transform.position}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} {relativeClonedTransforms[i].transform.position}]\n");
                            if (playerNetworkTransforms[i].NetworkManager.IsServer)
                            {
                                var precisionLoss = playerNetworkTransforms[i].GetPecisionLoss();
                                var adjusted = relativeClonedTransforms[i].transform.position + precisionLoss;
                                m_ValidationErrors.Append($"Pending Precision Loss: ({precisionLoss.x}, {precisionLoss.y}, {precisionLoss.z}) | Adjusted: ({adjusted.x}, {adjusted.y}, {adjusted.z})\n");
                            }
                        }

                        m_CurrentVariance = k_RotationVariance;
                        if (!Approximately(playerNetworkTransforms[i].transform.eulerAngles, relativeClonedTransforms[i].transform.eulerAngles))
                        {
                            m_ValidationErrors.Append($"[Rotation][Client-{connectedClient} {playerNetworkTransforms[i].transform.eulerAngles}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} {relativeClonedTransforms[i].transform.eulerAngles}]\n");
                        }
                        if (!Approximately(playerNetworkTransforms[i].transform.localScale, relativeClonedTransforms[i].transform.localScale))
                        {
                            m_ValidationErrors.Append($"[Scale][Client-{connectedClient} {playerNetworkTransforms[i].transform.localScale}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} {relativeClonedTransforms[i].transform.localScale}]\n");
                        }
                    }
                }
            }
            return m_ValidationErrors.Length == 0;
        }

        // TODO: We need to fix the issue within IntegrationNetworkTransform with authority and mapping tick values
        private void PositionValidationCallback(ref NetworkObject networkObject, ref Vector3 position, ref IntegrationNetworkTransform.AuthorityStateUpdate serverStateUpdate)
        {
            //if (!Approximately(position, serverStateUpdate.AuthorityPosition) )
            //{
            //    var authPosition = serverStateUpdate.AuthorityPosition;
            //    m_ValidationErrors.Append($"NonAuthority-({networkObject.OwnerClientId}) Position ({position.x},{position.y},{position.z}) does not match authority position ({authPosition.x},{authPosition.y},{authPosition.z}) for network tick ({serverStateUpdate.Tick})! \n");
            //}
            //if (!Approximately(position, serverStateUpdate.PredictedPosition))
            //{
            //    m_ValidationErrors.Append($"NonAuthority-({networkObject.OwnerClientId}) Position ({position.x},{position.y},{position.z}) does not match authority position ({authPosition.x},{authPosition.y},{authPosition.z}) for network tick ({serverStateUpdate.Tick})! \n");
            //}
        }


        private void AddDebugHandler(NetworkManager networkManager)
        {
            var integrationNetworkTransforms = m_ServerNetworkManager.LocalClient.PlayerObject.GetComponentsInChildren<IntegrationNetworkTransform>();
            foreach(var integrationNetworkTransform in integrationNetworkTransforms)
            {
                integrationNetworkTransform.PositionValidationCallback = PositionValidationCallback;
            }
        }

        [UnityTest]
        public IEnumerator NestedNetworkTransformSynchronization()
        {
            AutomatedPlayerMover.StopMovement = false;
            ChildMoverManager.StopMovement = false;
            m_ValidationErrors = new StringBuilder();
            //var waitPeriod = new WaitForSeconds(s_DefaultWaitForTick.waitTime * m_ServerNetworkManager.NetworkConfig.TickRate);
            var waitPeriod = new WaitForSeconds(3f);
            var pausePeriod = new WaitForSeconds(2f);
            var timeoutHelper = new TimeoutHelper(1f);
            m_ServerNetworkManager.SceneManager.VerifySceneBeforeLoading = VerifySceneServer;
            AddDebugHandler(m_ServerNetworkManager);
            yield return pausePeriod;

            // Spawn 5 more clients over time
            for (int i = 0; i < 5; i++)
            {
                AutomatedPlayerMover.StopMovement = true;
                ChildMoverManager.StopMovement = true;
                m_ValidationErrors.Clear();
                yield return CreateAndStartNewClient();
                AddDebugHandler(m_ClientNetworkManagers[i]);
                yield return WaitForConditionOrTimeOut(ValidateNetworkTransforms, timeoutHelper);

                AssertOnTimeout($"Timed out waiting for all nested NetworkTransform cloned instances to match:\n {m_ValidationErrors}", timeoutHelper);
                AutomatedPlayerMover.StopMovement = false;
                ChildMoverManager.StopMovement = false;
                // Now let everything move around a bit
                yield return waitPeriod;

                // Make sure we didn't have any synchronization issues
                Assert.IsTrue(m_ValidationErrors.Length == 0, $"Synchronization Mismatch Detected!\n {m_ValidationErrors}");


                //AssertOnTimeout($"Timed out waiting for new client's nested NetworkTransform cloned instances to match! {m_ValidationErrors}\n");
                //AutomatedPlayerMover.StopMovement = false;
                //ChildMoverManager.StopMovement = false;

                //yield return s_DefaultWaitForTick;
                //int syncCount = 0;
                //while (syncCount < 3)
                //{
                //    //// Stop all movement for all players
                //    AutomatedPlayerMover.StopMovement = true;
                //    ChildMoverManager.StopMovement = true;

                //    // Validate that the transforms are approximately the same
                //    yield return WaitForConditionOrTimeOut(ValidateNetworkTransforms, timeoutHelper);

                //    // Continue player movement
                //    AutomatedPlayerMover.StopMovement = false;
                //    ChildMoverManager.StopMovement = false;
                //    if (timeoutHelper.TimedOut)
                //    {
                //        syncCount++;
                //        // Allow the players to move a bit.
                //        yield return waitPeriod;
                //    }
                //    else
                //    {
                //        break;
                //    }
                //}
                //AutomatedPlayerMover.StopMovement = true;
                //ChildMoverManager.StopMovement = true;
                //AssertOnTimeout($"Timed out waiting for all nested NetworkTransform cloned instances to match:\n {m_ValidationErrors}", timeoutHelper);
                //// Allow the players to move a bit.
                //yield return waitPeriod;
            }

            // Just a final sanity check to make sure position and rotation match
            //AutomatedPlayerMover.StopMovement = true;
            //ChildMoverManager.StopMovement = true;
            //yield return WaitForConditionOrTimeOut(ValidateNetworkTransforms);

            //AssertOnTimeout($"Timed out waiting for all nested NetworkTransform cloned instances to match:\n {m_ValidationErrors}");
        }

    }
}

