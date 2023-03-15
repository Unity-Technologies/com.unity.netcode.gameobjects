using System.Text;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TestProject.ManualTests;
using Unity.Netcode.TestHelpers.Runtime;

namespace TestProject.RuntimeTests
{
    [TestFixture(Interpolation.Interpolation, Precision.Full, AuthoritativeModel.Server)]
    [TestFixture(Interpolation.Interpolation, Precision.Full, AuthoritativeModel.Owner)]
    [TestFixture(Interpolation.Interpolation, Precision.Half, AuthoritativeModel.Server)]
    [TestFixture(Interpolation.Interpolation, Precision.Half, AuthoritativeModel.Owner)]
    [TestFixture(Interpolation.Interpolation, Precision.Compressed, AuthoritativeModel.Server)]
    [TestFixture(Interpolation.Interpolation, Precision.Compressed, AuthoritativeModel.Owner)]
    [TestFixture(Interpolation.NoInterpolation, Precision.Full, AuthoritativeModel.Server)]
    [TestFixture(Interpolation.NoInterpolation, Precision.Full, AuthoritativeModel.Owner)]
    [TestFixture(Interpolation.NoInterpolation, Precision.Half, AuthoritativeModel.Server)]
    [TestFixture(Interpolation.NoInterpolation, Precision.Half, AuthoritativeModel.Owner)]
    [TestFixture(Interpolation.NoInterpolation, Precision.Compressed, AuthoritativeModel.Server)]
    [TestFixture(Interpolation.NoInterpolation, Precision.Compressed, AuthoritativeModel.Owner)]
    public class NestedNetworkTransformTests : IntegrationTestWithApproximation
    {
        private const string k_TestScene = "NestedNetworkTransformTestScene";
        private const string k_PlayerToLoad = "PlayerNestedTransforms";

        protected override int NumberOfClients => 0;

        private float m_OriginalVarianceThreshold;

        private Scene m_BaseSceneLoaded;
        private Scene m_OriginalActiveScene;

        private Object m_PlayerPrefabResource;
        private Interpolation m_Interpolation;
        private Precision m_Precision;
        private AuthoritativeModel m_Authority;

        public enum Interpolation
        {
            NoInterpolation,
            Interpolation
        }

        public enum Precision
        {
            Compressed,
            Half,
            Full
        }

        public enum AuthoritativeModel
        {
            Server,
            Owner
        }


        public NestedNetworkTransformTests(Interpolation interpolation, Precision precision, AuthoritativeModel authoritativeModel)
        {
            m_Interpolation = interpolation;
            m_Precision = precision;
            m_Authority = authoritativeModel;
        }

        public NestedNetworkTransformTests()
        {

        }

        protected override void OnOneTimeSetup()
        {
            m_OriginalVarianceThreshold = base.GetDeltaVarianceThreshold();
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

        private void ConfigureNetworkTransform(IntegrationNetworkTransform networkTransform)
        {
            networkTransform.Interpolate = m_Interpolation == Interpolation.Interpolation;
            networkTransform.UseHalfFloatPrecision = m_Precision == Precision.Half || m_Precision == Precision.Compressed;
            networkTransform.UseQuaternionCompression = m_Precision == Precision.Compressed;
            networkTransform.IsServerAuthority = m_Authority == AuthoritativeModel.Server;
        }


        protected override void OnCreatePlayerPrefab()
        {
            // Destroy the default player prefab
            Object.DestroyImmediate(m_PlayerPrefab);
            // Assign the network prefab resource loaded
            m_PlayerPrefab = m_PlayerPrefabResource as GameObject;
            m_PlayerPrefab.name = "Player";

            ConfigureNetworkTransform(m_PlayerPrefab.GetComponent<IntegrationNetworkTransform>());
            var networkTransforms = m_PlayerPrefab.GetComponentsInChildren<IntegrationNetworkTransform>();
            foreach (var networkTransform in networkTransforms)
            {
                ConfigureNetworkTransform(networkTransform);
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

        // Acceptable variances when using half precision
        private const float k_PositionScaleVariance = 0.1255f;
        private const float k_RotationVariance = 0.1255f;
        private const float k_RotationVarianceCompressed = 0.555f;

        private float m_CurrentVariance = k_PositionScaleVariance;
        override protected float GetDeltaVarianceThreshold()
        {
            return m_CurrentVariance;
        }

        private StringBuilder m_ValidationErrors;

        private string GetVector3Values(ref Vector3 vector3)
        {
            return $"({vector3.x.ToString("G6")},{vector3.y.ToString("G6")},{vector3.z.ToString("G6")})";
        }

        /// <summary>
        /// Validates all transform instance values match the authority's
        /// </summary>
        private bool ValidateNetworkTransforms()
        {
            m_ValidationErrors.Clear();
            foreach (var connectedClient in m_ServerNetworkManager.ConnectedClientsIds)
            {
                var authorityId = m_Authority == AuthoritativeModel.Server ? m_ServerNetworkManager.LocalClientId : connectedClient;
                var playerToValidate = m_PlayerNetworkObjects[authorityId][connectedClient];
                var playerNetworkTransforms = playerToValidate.GetComponentsInChildren<IntegrationNetworkTransform>();
                foreach (var playerRelative in m_PlayerNetworkObjects)
                {
                    if (playerRelative.Key == authorityId)
                    {
                        continue;
                    }
                    var relativeClonedTransforms = playerRelative.Value[connectedClient].GetComponentsInChildren<IntegrationNetworkTransform>();
                    m_CurrentVariance = m_Precision == Precision.Full ? m_OriginalVarianceThreshold : k_PositionScaleVariance;
                    m_CurrentVariance += m_Interpolation == Interpolation.Interpolation && m_Precision != Precision.Full ? 0.10f : m_Interpolation == Interpolation.Interpolation ? 0.10f : 0.0f;
                    for (int i = 0; i < playerNetworkTransforms.Length; i++)
                    {
                        var playerCurrentPosition = playerNetworkTransforms[i].PushedPosition;
                        var clonePosition = relativeClonedTransforms[i].GetSpaceRelativePosition();
                        var playerGameObjectName = playerNetworkTransforms[i].gameObject.name;
                        var cloneGameObjectName = relativeClonedTransforms[i].gameObject.name;
                        if (!Approximately(playerCurrentPosition, clonePosition))
                        {
                            m_ValidationErrors.Append($"[Position][Client-{connectedClient}-{playerGameObjectName} {GetVector3Values(ref playerCurrentPosition)}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId}-{cloneGameObjectName} {GetVector3Values(ref clonePosition)}]\n");
                            if (m_EnableVerboseDebug)
                            {
                                if (!Approximately(playerNetworkTransforms[i].LastUpdatedPosition, clonePosition))
                                {
                                    m_ValidationErrors.Append($"[Last-Position][Client-{connectedClient}-{playerGameObjectName}  {playerNetworkTransforms[i].LastUpdatedPosition}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId}-{cloneGameObjectName}{relativeClonedTransforms[i].LastUpdatedPosition}]\n");
                                    if (!Approximately(playerNetworkTransforms[i].PreviousUpdatedPosition, relativeClonedTransforms[i].LastUpdatedPosition))
                                    {
                                        m_ValidationErrors.Append($"[Prev-Position][Client-{connectedClient}-{playerGameObjectName}  {playerNetworkTransforms[i].PreviousUpdatedPosition}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId}-{cloneGameObjectName}{relativeClonedTransforms[i].LastUpdatedPosition}]\n");
                                    }
                                }
                            }
                        }

                        if (!Approximately(playerNetworkTransforms[i].PushedScale, relativeClonedTransforms[i].transform.localScale))
                        {
                            m_ValidationErrors.Append($"[Scale][Client-{connectedClient} {playerNetworkTransforms[i].transform.localScale}][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} {relativeClonedTransforms[i].transform.localScale}]\n");
                        }

                        m_CurrentVariance = m_Precision == Precision.Full ? m_OriginalVarianceThreshold : k_RotationVariance;
                        if (m_Precision == Precision.Compressed)
                        {
                            m_CurrentVariance = k_RotationVarianceCompressed;
                        }
                        m_CurrentVariance += m_Interpolation == Interpolation.Interpolation && m_Precision != Precision.Full ? 0.10333f : 0.0f;
                        var playerCurrentRotation = playerNetworkTransforms[i].PushedRotation;
                        var cloneRotation = relativeClonedTransforms[i].GetSpaceRelativeRotation();
                        if (!ApproximatelyEuler(playerCurrentRotation.eulerAngles, cloneRotation.eulerAngles))
                        {
                            // Double check Euler as quaternions can have inverted Vector4 values from one another but be the same when comparing Euler
                            if (!ApproximatelyEuler(playerCurrentRotation.eulerAngles, cloneRotation.eulerAngles))
                            {
                                var deltaEuler = playerCurrentRotation.eulerAngles - cloneRotation.eulerAngles;
                                var eulerPlayer = playerCurrentRotation.eulerAngles;
                                var eulerClone = cloneRotation.eulerAngles;

                                m_ValidationErrors.Append($"[Rotation][Client-{connectedClient} ({eulerPlayer.x}, {eulerPlayer.y}, {eulerPlayer.z})][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId}-{cloneGameObjectName} ({eulerClone.x}, {eulerClone.y}, {eulerClone.z})]\n");
                                m_ValidationErrors.Append($"[Rotation Delta] ({deltaEuler.x}, {deltaEuler.y}, {deltaEuler.z})\n");
                                if (m_EnableVerboseDebug)
                                {
                                    if (!Approximately(playerNetworkTransforms[i].LastUpdatedRotation, relativeClonedTransforms[i].LastUpdatedRotation))
                                    {
                                        var playerEulerAngles = playerNetworkTransforms[i].LastUpdatedRotation.eulerAngles;
                                        var relativeEulerAngles = relativeClonedTransforms[i].LastUpdatedRotation.eulerAngles;
                                        deltaEuler = playerEulerAngles - relativeEulerAngles;
                                        m_ValidationErrors.Append($"[Last-Rotation][Client-{connectedClient} ({playerEulerAngles.x}, {playerEulerAngles.y}, {playerEulerAngles.z})][Failing on Client-{playerRelative.Key} for Clone-{relativeClonedTransforms[i].OwnerClientId} ({relativeEulerAngles.x}, {relativeEulerAngles.y}, {relativeEulerAngles.z})]\n");
                                        m_ValidationErrors.Append($"[Last-Rotation Delta] ({deltaEuler.x}, {deltaEuler.y}, {deltaEuler.z})\n");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // If we had any variance failures logged, then we failed
            return m_ValidationErrors.Length == 0;
        }
        private const int k_IterationsToTest = 8;
        private const int k_ClientsToSpawn = 2;  // Really it will be 3 including the host

        // Number of failures in a row with no correction in precision for the test to fail
        private const int k_MaximumPrecisionFailures = 4;

        [UnityTest]
        public IEnumerator NestedNetworkTransformSynchronization()
        {
            m_EnableVerboseDebug = true;
            AutomatedPlayerMover.StopMovement = false;
            ChildMoverManager.StopMovement = false;
            m_ValidationErrors = new StringBuilder();
            var waitPeriod = new WaitForSeconds(2f);
            var pausePeriod = new WaitForSeconds(1f);
            var synchTimeOut = new TimeoutHelper(m_Interpolation == Interpolation.Interpolation ? 4f : 2f);

            m_ServerNetworkManager.SceneManager.VerifySceneBeforeLoading = VerifySceneServer;
            yield return pausePeriod;

            var precisionFailures = 0;
            var clientCount = 0;


            // 5 of the iterations are spent spawning
            // All iterations test transform values against the authority and non-authority instances
            for (int i = 0; i < k_IterationsToTest; i++)
            {
                ChildMoverManager.StopMovement = true;
                AutomatedPlayerMover.StopMovement = true;
                yield return WaitForConditionOrTimeOut(ValidateNetworkTransforms, synchTimeOut);
                // Do one last validation pass to make sure one (or more) transforms does not fall within the acceptable ranges
                if (!ValidateNetworkTransforms())
                {
                    // If not, then log errors to console and clear the current pass validation errors
                    if (m_ValidationErrors.Length > 0)
                    {
                        VerboseDebug($"{m_ValidationErrors}");
                    }
                    precisionFailures++;
                    // With half float values, we can sometimes have values that exceed typical thresholds but they
                    // should correct themselves over time. This is to allow enough passes to allow for this correction
                    // to occur for delta position (especially), half float quaternions, and quaternion compression.
                    // If we have 5 precision failures in a row and fail to correct, then fail this test
                    if (precisionFailures > k_MaximumPrecisionFailures)
                    {
                        m_EnableVerboseDebug = true;
                        VerboseDebug($"[{i}][Precision Failure] Exceeded Precision Failure Count ({precisionFailures})");
                        AssertOnTimeout($"Timed out waiting for all nested NetworkTransform cloned instances to match!\n{m_ValidationErrors}", synchTimeOut);
                        Assert.IsTrue(false, $"Timed out waiting for all nested NetworkTransform cloned instances to match:\n{m_ValidationErrors}");
                    }
                    else
                    {
                        VerboseDebug($"[Precision Failure] {precisionFailures}");
                    }
                }
                else
                {
                    if (precisionFailures > 0)
                    {
                        VerboseDebug($"[{i}][Precision Corrected] Synchronized After");
                        precisionFailures = 0;
                    }
                }

                if (clientCount < k_ClientsToSpawn)
                {
                    yield return CreateAndStartNewClient();
                    clientCount++;
                }

                AutomatedPlayerMover.StopMovement = false;
                ChildMoverManager.StopMovement = false;
                // Now let everything move around a bit
                yield return waitPeriod;

                // If we are just about to end this test but there are running precision
                // failures, we need to keep running until it corrects itself or fails
                if (precisionFailures > 0 && i == (k_IterationsToTest - 1))
                {
                    Debug.Log($"Extending test iterations with {precisionFailures} precision failures still pending correction.");
                    i--;
                }
            }
        }

    }
}
