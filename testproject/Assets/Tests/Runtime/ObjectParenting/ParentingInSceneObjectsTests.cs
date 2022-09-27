using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using TestProject.ManualTests;

namespace TestProject.RuntimeTests
{
    public class ParentingInSceneObjectsTests : IntegrationTestWithApproximation
    {
        private const string k_BaseSceneToLoad = "UnitTestBaseScene";
        private const string k_TestSceneToLoad = "ParentingInSceneObjects";
        private const int k_NumIterationsDeparentReparent = 100;
        private const float k_AproximateThresholdValue = 0.001f;

        protected override int NumberOfClients => 2;

        protected override void OnOneTimeSetup()
        {
            NetworkManagerTestDisabler.IsIntegrationTest = true;
            ManualTestAssetsDestroyer.IsIntegrationTest = true;
            base.OnOneTimeSetup();
        }

        protected override void OnOneTimeTearDown()
        {
            NetworkManagerTestDisabler.IsIntegrationTest = false;
            ManualTestAssetsDestroyer.IsIntegrationTest = false;
            base.OnOneTimeTearDown();
        }

        private Scene m_BaseSceneLoaded;

        protected override IEnumerator OnSetup()
        {
            InSceneParentChildHandler.ResetInstancesTracking(m_EnableVerboseDebug);
            return base.OnSetup();
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == k_BaseSceneToLoad)
            {
                m_BaseSceneLoaded = scene;
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            }
        }

        protected override IEnumerator OnTearDown()
        {
            if (m_BaseSceneLoaded.IsValid() && m_BaseSceneLoaded.isLoaded)
            {
                SceneManager.UnloadSceneAsync(m_BaseSceneLoaded);
            }
            yield return base.OnTearDown();
        }

        private bool m_InitialClientsLoadedScene;
        private StringBuilder m_ErrorValidationLog = new StringBuilder(0x2000);

        private void GeneratePositionDoesNotMatch(InSceneParentChildHandler serverHandler, InSceneParentChildHandler clientHandler)
        {
            m_ErrorValidationLog.Append($"[Client-{clientHandler.NetworkManager.LocalClientId}] {nameof(NetworkObject)}-{clientHandler.NetworkObjectId}'s position {clientHandler.transform.position} does not equal the server-side position {serverHandler.transform.position}");
        }

        private void GenerateRotationDoesNotMatch(InSceneParentChildHandler serverHandler, InSceneParentChildHandler clientHandler)
        {
            m_ErrorValidationLog.Append($"[Client-{clientHandler.NetworkManager.LocalClientId}] {nameof(NetworkObject)}-{clientHandler.NetworkObjectId}'s rotation {clientHandler.transform.eulerAngles} does not equal the server-side scale {serverHandler.transform.eulerAngles}");
        }

        private void GenerateScaleDoesNotMatch(InSceneParentChildHandler serverHandler, InSceneParentChildHandler clientHandler)
        {
            m_ErrorValidationLog.Append($"[Client-{clientHandler.NetworkManager.LocalClientId}] {nameof(NetworkObject)}-{clientHandler.NetworkObjectId}'s scale {clientHandler.transform.localScale} does not equal the server-side scale {serverHandler.transform.localScale}");
        }

        private void GenerateParentIsNotCorrect(InSceneParentChildHandler handler, bool shouldHaveParent)
        {
            var serverOrClient = handler.NetworkManager.IsServer ? "Server" : "Client";
            if (!shouldHaveParent)
            {
                m_ErrorValidationLog.Append($"[{serverOrClient }-{handler.NetworkManager.LocalClientId}] {nameof(NetworkObject)}-{handler.NetworkObjectId}'s still has the parent {handler.transform.parent.name} when it should be null!");
            }
            else
            {
                m_ErrorValidationLog.Append($"[{serverOrClient }-{handler.NetworkManager.LocalClientId}] {nameof(NetworkObject)}-{handler.NetworkObjectId}'s does not have a parent when it should!");
            }
        }

        private bool ValidateClientAgainstServerTransformValues()
        {
            // We reset this each time because we are only interested in the last time it checked and failed
            m_ErrorValidationLog.Clear();
            foreach (var instance in InSceneParentChildHandler.ServerRelativeInstances)
            {
                var serverInstanceTransform = instance.Value.transform;
                foreach (var clientInstances in InSceneParentChildHandler.ClientRelativeInstances)
                {
                    Assert.True(clientInstances.Value.ContainsKey(instance.Key), $"Client-{clientInstances.Key} did not spawn NetworkObject-{instance.Key}!");
                    var clientInstance = clientInstances.Value[instance.Key];
                    var clientInstanceTransform = clientInstance.transform;
                    if (!Approximately(serverInstanceTransform.position, clientInstanceTransform.position))
                    {
                        GeneratePositionDoesNotMatch(instance.Value, clientInstance);
                        return false;
                    }

                    if (!Approximately(serverInstanceTransform.eulerAngles, clientInstanceTransform.eulerAngles))
                    {
                        GeneratePositionDoesNotMatch(instance.Value, clientInstance);
                        return false;
                    }

                    if (!Approximately(serverInstanceTransform.localScale, clientInstanceTransform.localScale))
                    {
                        GeneratePositionDoesNotMatch(instance.Value, clientInstance);
                        return false;
                    }
                }
            }
            return true;
        }

        private bool ValidateAllChildrenParentingStatus(bool checkForParent)
        {
            foreach (var instance in InSceneParentChildHandler.ServerRelativeInstances)
            {
                if (!instance.Value.IsRootParent)
                {
                    if (checkForParent && instance.Value.transform.parent == null)
                    {
                        GenerateParentIsNotCorrect(instance.Value, checkForParent);
                        return false;
                    }
                    else if (!checkForParent && instance.Value.transform.parent != null)
                    {
                        GenerateParentIsNotCorrect(instance.Value, checkForParent);
                        return false;
                    }
                }
            }

            foreach (var clientInstances in InSceneParentChildHandler.ClientRelativeInstances)
            {
                foreach (var instance in clientInstances.Value)
                {
                    if (!instance.Value.IsRootParent)
                    {
                        if (checkForParent && instance.Value.transform.parent == null)
                        {
                            GenerateParentIsNotCorrect(instance.Value, checkForParent);
                            return false;
                        }
                        else if (!checkForParent && instance.Value.transform.parent != null)
                        {
                            GenerateParentIsNotCorrect(instance.Value, checkForParent);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        protected override float GetDeltaVarianceThreshold()
        {
            return k_AproximateThresholdValue;
        }

        [UnityTest]
        public IEnumerator InSceneParentingTest()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.LoadScene(k_BaseSceneToLoad, LoadSceneMode.Additive);
            m_InitialClientsLoadedScene = false;
            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

            var sceneEventStartedStatus = m_ServerNetworkManager.SceneManager.LoadScene(k_TestSceneToLoad, LoadSceneMode.Additive);
            Assert.True(sceneEventStartedStatus == SceneEventProgressStatus.Started, $"Failed to load scene {k_TestSceneToLoad} with a return status of {sceneEventStartedStatus}.");
            yield return WaitForConditionOrTimeOut(() => m_InitialClientsLoadedScene);
            AssertOnTimeout($"Timed out waiting for all clients to load scene {k_TestSceneToLoad}!");

            // [Currently Connected Clients]
            // remove the parents, change all transform values, and re-parent
            InSceneParentChildHandler.ServerRootParent.DeparentSetValuesAndReparent();
            yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
            AssertOnTimeout($"Timed out waiting for all clients transform values to match the server transform values!\n {m_ErrorValidationLog}");

            // [Late Join Client #1]
            // Make sure the late joining client synchronizes properly
            yield return CreateAndStartNewClient();
            yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
            AssertOnTimeout($"Timed out waiting for the late joining client's transform values to match the server transform values!\n {m_ErrorValidationLog}");

            // Remove the parents from al of the children
            InSceneParentChildHandler.ServerRootParent.DeparentAllChildren();
            yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
            AssertOnTimeout($"[Late Join 1] Timed out waiting for all clients transform values to match the server transform values!\n {m_ErrorValidationLog}");

            yield return WaitForConditionOrTimeOut(() => ValidateAllChildrenParentingStatus(false));
            AssertOnTimeout($"[Late Join 1] Timed out waiting for all children to be removed from their parent!\n {m_ErrorValidationLog}");

            // [Late Join Client #2]
            // Make sure the late joining client synchronizes properly with all children having their parent removed
            yield return CreateAndStartNewClient();
            yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
            AssertOnTimeout($"[Late Join 2] Timed out waiting for the late joining client's transform values to match the server transform values!\n {m_ErrorValidationLog}");

            // Just a sanity check that late joining client #2 has no child parented
            yield return WaitForConditionOrTimeOut(() => ValidateAllChildrenParentingStatus(false));
            AssertOnTimeout($"[Late Join 2] Timed out waiting for late joined client's children objects to have no parent!\n {m_ErrorValidationLog}");

            // Finally, re-parent all of the children to make sure late joining client #2 synchronizes properly
            InSceneParentChildHandler.ServerRootParent.ReParentAllChildren();
            yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
            AssertOnTimeout($"[Late Join 2] Timed out waiting for all clients transform values to match the server transform values!\n {m_ErrorValidationLog}");
            yield return WaitForConditionOrTimeOut(() => ValidateAllChildrenParentingStatus(true));
            AssertOnTimeout($"[Late Join 2] Timed out waiting for all children to be removed from their parent!\n {m_ErrorValidationLog}");

            // Now run through many iterations where we remove the parents, set the parents, and while
            // the parents are being set the InSceneParentChildHandler assigns new position, rotation, and scale values
            // in the OnNetworkObjectParentChanged overridden method on the server side only
            for (int i = 0; i < k_NumIterationsDeparentReparent; i++)
            {
                InSceneParentChildHandler.ServerRootParent.DeparentSetValuesAndReparent();

                yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
                AssertOnTimeout($"[Final Pass] Timed out waiting for all clients transform values to match the server transform values!\n {m_ErrorValidationLog}");

                yield return WaitForConditionOrTimeOut(() => ValidateAllChildrenParentingStatus(true));
                AssertOnTimeout($"[Final Pass] Timed out waiting for all children to be removed from their parent!\n {m_ErrorValidationLog}");
            }

            // In the final pass, we remove the second generation nested child
            var firstGenChild = InSceneParentChildHandler.ServerRootParent.transform.GetChild(0);
            var secondGenChild = firstGenChild.GetChild(0);
            var secondGenChildNetworkObject = secondGenChild.GetComponent<NetworkObject>();
            Assert.True(secondGenChildNetworkObject.TrySetParent((NetworkObject)null, false), $"[Final Pass] Failed to remove the parent from the second generation child!");

            // Validate all transform values match
            yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
            AssertOnTimeout($"[Final Pass] Timed out waiting for all clients transform values to match the server transform values after the second generation child's parent was removed!\n {m_ErrorValidationLog}");

            // Now run through one last deparent, reparent, and set new values pass to make sure everything still synchronizes
            InSceneParentChildHandler.ServerRootParent.DeparentSetValuesAndReparent();

            yield return WaitForConditionOrTimeOut(ValidateClientAgainstServerTransformValues);
            AssertOnTimeout($"[Final Pass - Last Test] Timed out waiting for all clients transform values to match the server transform values!\n {m_ErrorValidationLog}");

            yield return WaitForConditionOrTimeOut(() => ValidateAllChildrenParentingStatus(true));
            AssertOnTimeout($"[Final Pass - Last Test] Timed out waiting for all children to be removed from their parent!\n {m_ErrorValidationLog}");
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneName != k_TestSceneToLoad)
            {
                return;
            }

            if (sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId && sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                m_InitialClientsLoadedScene = true;
            }
        }
    }
}
