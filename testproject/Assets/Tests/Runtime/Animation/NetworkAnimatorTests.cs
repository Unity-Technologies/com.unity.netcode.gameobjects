
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Components;


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
    public class NetworkAnimatorTests : NetcodeIntegrationTest
    {
        private const string k_AnimatorObjectName = "AnimatorObject";

        protected override int NumberOfClients => 1;
        private GameObject m_AnimationTestPrefab => m_AnimatorObjectPrefab ? m_AnimatorObjectPrefab as GameObject : null;
        private AnimatorTestHelper.ParameterValues m_ParameterValues;
        private Object m_AnimatorObjectPrefab;

        protected override void OnOneTimeSetup()
        {
            m_AnimatorObjectPrefab = Resources.Load(k_AnimatorObjectName);
            Assert.NotNull(m_AnimatorObjectPrefab, $"Failed to load resource AnimatorObject");
            base.OnOneTimeSetup();
        }

        protected override IEnumerator OnSetup()
        {
            AnimatorTestHelper.Initialize();
            TriggerTest.Reset();
            yield return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            m_EnableVerboseDebug = false;
            yield return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
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

        private bool ParameterValuesMatch(OwnerShipMode ownerShipMode, AuthoritativeMode authoritativeMode, bool debugInfo = false)
        {
            var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
            if (!serverParameters.ValuesMatch(m_ParameterValues, debugInfo))
            {
                return false;
            }
            foreach (var animatorTestHelper in AnimatorTestHelper.ClientSideInstances)
            {
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

        public enum AuthoritativeMode
        {
            ServerAuth,
            OwnerAuth
        }

        private GameObject SpawnPrefab(bool isClientOwner, AuthoritativeMode authoritativeMode)
        {
            Assert.NotNull(m_AnimatorObjectPrefab);
            var networkAnimator = (m_AnimatorObjectPrefab as GameObject).GetComponent<NetworkAnimator>();
            networkAnimator.IsServerAuthoritative = authoritativeMode == AuthoritativeMode.ServerAuth;
            var networkManager = isClientOwner ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;
            return SpawnObject(m_AnimatorObjectPrefab as GameObject, networkManager);
        }

        /// <summary>
        /// Verifies that parameters are synchronized with currently connected clients
        /// when no transition or layer change has occurred.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator ParameterUpdateTests([Values] OwnerShipMode ownerShipMode, [Values] AuthoritativeMode authoritativeMode)
        {
            VerboseDebug($" ++++++++++++++++++ Parameter Test [{ownerShipMode}] Starting ++++++++++++++++++ ");

            // Spawn our test animator object
            var objectInstance = SpawnPrefab(ownerShipMode == OwnerShipMode.ClientOwner, authoritativeMode);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (authoritativeMode == AuthoritativeMode.OwnerAuth)
            {
                var objectToUpdate = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
                // Set the new parameter values via the owner
                objectToUpdate.UpdateParameters(m_ParameterValues);
            }
            else
            {
                // Set the new parameter values via the server
                AnimatorTestHelper.ServerSideInstance.UpdateParameters(m_ParameterValues);
            }

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(() => ParameterValuesMatch(ownerShipMode, authoritativeMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");
            VerboseDebug($" ------------------ Parameter Test [{ownerShipMode}] Stopping ------------------ ");
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
                    return false;
                }
            }
            return true;
        }

        private bool WaitForClientsToInitialize()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
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
        public IEnumerator TriggerUpdateTests([Values] OwnerShipMode ownerShipMode, [Values] AuthoritativeMode authoritativeMode)
        {
            VerboseDebug($" ++++++++++++++++++ Trigger Test [{TriggerTest.Iteration}][{ownerShipMode}] Starting ++++++++++++++++++ ");
            TriggerTest.IsVerboseDebug = m_EnableVerboseDebug;
            AnimatorTestHelper.IsTriggerTest = m_EnableVerboseDebug;

            // Spawn our test animator object
            var objectInstance = SpawnPrefab(ownerShipMode == OwnerShipMode.ClientOwner, authoritativeMode);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");
            var animatorTestHelper = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
            if (authoritativeMode == AuthoritativeMode.ServerAuth)
            {
                animatorTestHelper = AnimatorTestHelper.ServerSideInstance;
            }

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
        public IEnumerator LateJoinSynchronizationTest([Values] OwnerShipMode ownerShipMode, [Values] AuthoritativeMode authoritativeMode)
        {
            VerboseDebug($" ++++++++++++++++++ Late-Join Test [{TriggerTest.Iteration}][{ownerShipMode}] Starting ++++++++++++++++++ ");
            TriggerTest.IsVerboseDebug = m_EnableVerboseDebug;
            AnimatorTestHelper.IsTriggerTest = m_EnableVerboseDebug;
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;

            // Spawn our test animator object
            var objectInstance = SpawnPrefab(ownerShipMode == OwnerShipMode.ClientOwner, authoritativeMode);


            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Set the trigger based on the type of test
            if (authoritativeMode == AuthoritativeMode.OwnerAuth)
            {
                var objectToUpdate = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
                // Set the animation trigger via the owner
                objectToUpdate.SetTrigger();
            }
            else
            {
                // Set the animation trigger via the server
                AnimatorTestHelper.ServerSideInstance.SetTrigger();
            }

            // Wait for all triggers to fire
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));
            AssertOnTimeout($"Timed out waiting for all triggers to match!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (authoritativeMode == AuthoritativeMode.OwnerAuth)
            {
                var objectToUpdate = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
                // Set the new parameter values
                objectToUpdate.UpdateParameters(m_ParameterValues);
            }
            else
            {
                // Set the new parameter values
                AnimatorTestHelper.ServerSideInstance.UpdateParameters(m_ParameterValues);
            }

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(() => ParameterValuesMatch(ownerShipMode, authoritativeMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues.ValuesToString()}!");

            yield return CreateAndStartNewClient();

            Assert.IsTrue(m_ClientNetworkManagers.Length == 2, $"Newly created and connected client was not added to {nameof(m_ClientNetworkManagers)}!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the late joining client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Make sure the AnimatorTestHelper client side instances (plus host) is the same as the TotalClients
            Assert.True((AnimatorTestHelper.ClientSideInstances.Count + 1) == TotalClients);

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
            // Now check that the late joining client and all other clients are synchronized to the updated parameter values
            yield return WaitForConditionOrTimeOut(() => ParameterValuesMatch(ownerShipMode, authoritativeMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues.ValuesToString()}!");

            var newlyJoinedClient = m_ClientNetworkManagers[1];
            yield return StopOneClient(newlyJoinedClient);
            VerboseDebug($" ------------------ Late-Join Test [{TriggerTest.Iteration}][{ownerShipMode}] Stopping ------------------ ");
        }
    }
}
