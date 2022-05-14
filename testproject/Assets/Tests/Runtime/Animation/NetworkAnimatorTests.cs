
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;


namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Tests Various Features of The NetworkAnimator
    /// !! NOTE !!
    /// This test depends upon the following assets:
    /// Assets\Tests\Animation\Resources\AnimatorObject.prefab
    /// Assets\Tests\Manual\NetworkAnimatorTests\CubeAnimatorController.controller (referenced in AnimatorObject)
    /// Possibly we could build this at runtime, but for now it uses the same animator controller as the manual
    /// test does.
    /// </summary>
    [TestFixture(0)]
    [TestFixture(1)]
    [TestFixture(2)]
    [TestFixture(3)]
    [TestFixture(4)]
    [TestFixture(5)]
    [TestFixture(6)]
    [TestFixture(7)]
    [TestFixture(8)]
    [TestFixture(9)]
    public class NetworkAnimatorTests : NetcodeIntegrationTest
    {
        private const string k_AnimatorObjectName = "AnimatorObject";
        private const string k_AnimatorObjectScene = "AnimationTestAsset";
        private const string k_BaseScene = "UnitTestBaseScene";

        protected override int NumberOfClients => 1;
        private GameObject m_AnimationTestPrefab => m_AnimatorObjectPrefab ? m_AnimatorObjectPrefab as GameObject : null;
        private AnimatorTestHelper.ParameterValues m_ParameterValues;
        private Object m_AnimatorObjectPrefab;

        public NetworkAnimatorTests(int number)
        {
            TriggerTest.Iteration = number;
        }
        protected override void OnOneTimeSetup()
        {
            m_AnimatorObjectPrefab = Resources.Load(k_AnimatorObjectName);
            Assert.NotNull(m_AnimatorObjectPrefab, $"Failed to load resource AnimatorObject");
            base.OnOneTimeSetup();
        }

        private Scene m_TempScene;
        private Scene m_BaseScene;

        protected override IEnumerator OnSetup()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            //SceneManager.LoadSceneAsync(k_AnimatorObjectScene, LoadSceneMode.Additive);
            //yield return WaitForConditionOrTimeOut(() => m_TempScene.IsValid() && m_TempScene.isLoaded);
            //AssertOnTimeout("Timed out waiting for animator prefab object to be loaded and instantiated!");
            //var sceneGameObjects = m_TempScene.GetRootGameObjects();
            //var gameObject = sceneGameObjects.Where((c) => c.GetComponent<NetworkAnimator>() != null).First();
            //Assert.NotNull(gameObject);



            //var asyncOperation = SceneManager.UnloadSceneAsync(m_TempScene);
            //yield return WaitForConditionOrTimeOut(() => asyncOperation.isDone);

            //SceneManager.LoadSceneAsync(k_BaseScene, LoadSceneMode.Additive);
            //yield return WaitForConditionOrTimeOut(() => m_BaseScene.IsValid() && m_BaseScene.isLoaded);
            //AssertOnTimeout("Timed out waiting for animator prefab object to be loaded and instantiated!");
            //NetcodeIntegrationTestHelpers.ScenesForServerToIgnore.Add(k_BaseScene);
            AnimatorTestHelper.Initialize();
            TriggerTest.Reset();
            yield return base.OnSetup();
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == k_AnimatorObjectScene)
            {
                m_TempScene = scene;
            }
            else if (scene.name == k_BaseScene)
            {
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                m_BaseScene = scene;
            }
        }

        protected override IEnumerator OnTearDown()
        {
            m_EnableVerboseDebug = false;
            yield return base.OnTearDown();
            //var asyncOperation = SceneManager.UnloadSceneAsync(m_BaseScene);
            //yield return WaitForConditionOrTimeOut(() => asyncOperation.isDone);
        }

        protected override void OnServerAndClientsCreated()
        {
            //m_AnimationTestPrefab = Object.Instantiate(m_AnimatorObjectPrefab) as GameObject;
            var networkObject = (m_AnimatorObjectPrefab as GameObject).GetComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_ServerNetworkManager;
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            var networkPrefab = new NetworkPrefab() { Prefab = networkObject.gameObject };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }
            networkObject.name = "AnimTest";
            base.OnServerAndClientsCreated();
        }

        private bool ClientSideValuesMatch(OwnerShipMode ownerShipMode, bool debugInfo = false)
        {
            if (ownerShipMode == OwnerShipMode.ClientOwner)
            {
                var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
                if (!serverParameters.ValuesMatch(m_ParameterValues, debugInfo))
                {
                    return false;
                }
            }
            foreach (var animatorTestHelper in AnimatorTestHelper.ClientSideInstances)
            {
                if (ownerShipMode == OwnerShipMode.ClientOwner && animatorTestHelper.Value.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId)
                {
                    continue;
                }
                var clientParameters = animatorTestHelper.Value.GetParameterValues();
                if (!clientParameters.ValuesMatch(m_ParameterValues, debugInfo))
                {
                    return false;
                }
            }

            return true;
        }

        public enum OwnerShipMode
        {
            ServerOwner,
            ClientOwner
        }

        private GameObject SpawnPrefab(bool isClientOwner)
        {
            Assert.NotNull(m_AnimatorObjectPrefab);
            var networkManager = isClientOwner ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;
            return SpawnObject(m_AnimatorObjectPrefab as GameObject, networkManager);
        }

        /// <summary>
        /// Verifies that parameters are synchronized with currently connected clients
        /// when no transition or layer change has occurred.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator ParameterUpdateTests([Values] OwnerShipMode ownerShipMode)
        {
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;
            // Spawn our test animator object
            var objectInstance = SpawnPrefab(isClientOwner);


            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (isClientOwner)
            {
                // Set the new parameter values
                AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId].UpdateParameters(m_ParameterValues);
            }
            else
            {
                // Set the new parameter values
                AnimatorTestHelper.ServerSideInstance.UpdateParameters(m_ParameterValues);
            }

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(() => ClientSideValuesMatch(ownerShipMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");
        }


        private bool AllTriggersDetected(OwnerShipMode ownerShipMode)
        {
            var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
            if (ownerShipMode == OwnerShipMode.ClientOwner)
            {
                if (!TriggerTest.ClientsThatTriggered.Contains(m_ServerNetworkManager.LocalClientId))
                {
                    return false;
                }
            }

            foreach (var animatorTestHelper in AnimatorTestHelper.ClientSideInstances)
            {
                if (ownerShipMode == OwnerShipMode.ClientOwner && animatorTestHelper.Value.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId)
                {
                    continue;
                }
                if (!TriggerTest.ClientsThatTriggered.Contains(animatorTestHelper.Value.NetworkManager.LocalClientId))
                {
                    //if (animatorTestHelper.Value.GetCurrentTriggerState())
                    //{
                    //    Debug.Log($"[{animatorTestHelper.Value.name}] Current trigger state is true but has not entered the trigger test!");
                    //    //continue;
                    //}
                    return false;
                }
            }
            return true;
        }

        private bool WaitForClientsToInitialize()
        {
            foreach(var networkManager in m_ClientNetworkManagers)
            {
                var clientId = networkManager.LocalClientId;
                if (!AnimatorTestHelper.ClientSideInstances.ContainsKey(clientId))
                {
                    return false;
                }
                if (!AnimatorTestHelper.ClientSideInstances[clientId].GetComponent<Animator>().isInitialized)
                {
                    return false;
                }
                VerboseDebug($"{networkManager.name} initialized and spawned {AnimatorTestHelper.ClientSideInstances[clientId]}.");
            }
            return true;
        }

        /// <summary>
        /// Verifies that triggers are synchronized with currently connected clients
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator TriggerUpdateTests([Values] OwnerShipMode ownerShipMode)
        {
            VerboseDebug($" ++++++++++++++++++ Trigger Test [{TriggerTest.Iteration}][{ownerShipMode}] Starting ++++++++++++++++++ ");
            TriggerTest.IsVerboseDebug = m_EnableVerboseDebug;
            AnimatorTestHelper.IsTriggerTest = m_EnableVerboseDebug;
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;
            // Spawn our test animator object
            var objectInstance = SpawnPrefab(isClientOwner);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");
            var animatorTestHelper = isClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
            if (m_EnableVerboseDebug)
            {
                var retryTrigger = true;
                var timeOutHelper = new TimeoutHelper(1.0f);
                var count = 0;
                while (retryTrigger)
                {
                    VerboseDebug($"Current Trigger State: {animatorTestHelper.GetCurrentTriggerState()}");
                    VerboseDebug($"Setting Trigger");
                    animatorTestHelper.SetTrigger();
                    VerboseDebug($"New Trigger State: {animatorTestHelper.GetCurrentTriggerState()}");
                    // Wait for all triggers to fire
                    yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode), timeOutHelper);
                    retryTrigger = timeOutHelper.TimedOut;
                    if (retryTrigger)
                    {
                        count++;
                        Debug.LogWarning($"[{ownerShipMode}][{count}] Resending trigger!");
                    }
                }
            }
            else
            {
                animatorTestHelper.SetTrigger();
                // Wait for all triggers to fire
                yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));
                AssertOnTimeout($"Timed out waiting for all triggers to match!");
            }

            AnimatorTestHelper.IsTriggerTest = false;
            VerboseDebug($" ------------------ Trigger Test [{TriggerTest.Iteration}][{ownerShipMode}] Stopping ------------------ ");
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            var networkPrefab = new NetworkPrefab() { Prefab = m_AnimationTestPrefab };
            networkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
        }

        /// <summary>
        /// Verifies that late joining clients are synchronized to an
        /// animator's state.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator LateJoinSynchronizationTest([Values] OwnerShipMode ownerShipMode)
        {
            m_EnableVerboseDebug = true;
            TriggerTest.IsVerboseDebug = m_EnableVerboseDebug;
            AnimatorTestHelper.IsTriggerTest = m_EnableVerboseDebug;
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;
            // Spawn our test animator object
            var objectInstance = SpawnPrefab(isClientOwner);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Let things run for a small period of time
            yield return new WaitForSeconds(0.5f);

            // Set the trigger based on the type of test
            if (isClientOwner)
            {
                AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId].SetTrigger();
            }
            else
            {
                AnimatorTestHelper.ServerSideInstance.SetTrigger();
            }

            //yield return new WaitForSeconds(0.5f);
            // Wait for all triggers to fire
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));
            AssertOnTimeout($"Timed out waiting for all triggers to match!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (isClientOwner)
            {
                // Set the new parameter values
                AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId].UpdateParameters(m_ParameterValues);
            }
            else
            {
                // Set the new parameter values
                AnimatorTestHelper.ServerSideInstance.UpdateParameters(m_ParameterValues);
            }

            //yield return new WaitForSeconds(0.5f);

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(() => ClientSideValuesMatch(ownerShipMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues.ValuesToString()}!");

            yield return CreateAndStartNewClient();

            Assert.IsTrue(m_ClientNetworkManagers.Length == 2, $"Newly created and connected client was not added to {nameof(m_ClientNetworkManagers)}!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the late joining client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Make sure the AnimatorTestHelper client side instances (plus host) is the same as the TotalClients
            Assert.True((AnimatorTestHelper.ClientSideInstances.Count + 1) == TotalClients);

            //yield return new WaitForSeconds(0.5f);

            // Now check that the late joining client and all other clients are synchronized to the updated parameter values
            yield return WaitForConditionOrTimeOut(() => ClientSideValuesMatch(ownerShipMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues.ValuesToString()}!");

            // Now check that the late joining client and all other clients are synchronized to the trigger
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));

            var message = string.Empty;
            if (s_GlobalTimeoutHelper.TimedOut)
            {
                message = "\n Clients that triggered:";
                foreach (var id in TriggerTest.ClientsThatTriggered)
                {
                    message += $" ({id})";
                }
            }
            AssertOnTimeout($"Timed out waiting for the late joining client's triggers to match!{message}", s_GlobalTimeoutHelper);
            var newlyJoinedClient = m_ClientNetworkManagers[1];
            yield return StopOneClient(newlyJoinedClient);
        }
    }
}
