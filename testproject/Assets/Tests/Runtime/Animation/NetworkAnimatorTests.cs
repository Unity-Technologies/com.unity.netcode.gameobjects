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
        private GameObject m_AnimationTestPrefab;
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
            TriggerTest.ClientsThatTriggered.Clear();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_AnimationTestPrefab = Object.Instantiate(m_AnimatorObjectPrefab) as GameObject;
            var networkObject = m_AnimationTestPrefab.GetComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_ServerNetworkManager;
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            var networkPrefab = new NetworkPrefab() { Prefab = m_AnimationTestPrefab };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }
            m_AnimationTestPrefab.name = "AnimTest";
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

        /// <summary>
        /// Verifies that parameters are synchronized with currently connected clients
        /// when no transition or layer change has occurred.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator ParameterUpdateTests([Values] OwnerShipMode ownerShipMode)
        {
            var networkAnimator = m_AnimationTestPrefab.GetComponent<NetworkAnimator>();
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;

            // Spawn our test animator object
            var objectInstance = Object.Instantiate(m_AnimationTestPrefab);
            if (isClientOwner)
            {
                objectInstance.GetComponent<NetworkObject>().SpawnWithOwnership(m_ClientNetworkManagers[0].LocalClientId);
            }
            else
            {
                objectInstance.GetComponent<NetworkObject>().Spawn();
            }

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
            if (ownerShipMode == OwnerShipMode.ClientOwner)
            {
                var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
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

        /// <summary>
        /// Verifies that triggers are synchronized with currently connected clients
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator TriggerUpdateTests([Values] OwnerShipMode ownerShipMode)
        {
            var networkAnimator = m_AnimationTestPrefab.GetComponent<NetworkAnimator>();
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;

            // Determine the owner based on the mode
            var networkManagerOwner = (NetworkManager)null;
            if (isClientOwner)
            {
                networkManagerOwner = m_ClientNetworkManagers[0];
            }
            else
            {
                networkManagerOwner = m_ServerNetworkManager;
            }

            // Spawn our test animator object
            var objectInstance = SpawnObject(m_AnimationTestPrefab, networkManagerOwner);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            yield return new WaitForSeconds(1.0f);
            // Set the trigger based on the type of test
            if (isClientOwner && networkAnimator.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId)
            {
                AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId].SetTrigger();
            }
            else
            {
                AnimatorTestHelper.ServerSideInstance.SetTrigger();
            }

            // Wait for all triggers to fire
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));
            AssertOnTimeout($"Timed out waiting for all triggers to match!");
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
            var networkAnimator = m_AnimationTestPrefab.GetComponent<NetworkAnimator>();
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;

            // Determine the owner based on the mode
            var networkManagerOwner = (NetworkManager)null;
            if (isClientOwner)
            {
                networkManagerOwner = m_ClientNetworkManagers[0];
            }
            else
            {
                networkManagerOwner = m_ServerNetworkManager;
            }

            // Spawn our test animator object
            var objectInstance = SpawnObject(m_AnimationTestPrefab, networkManagerOwner);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout($"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Let things run for a small period of time
            yield return new WaitForSeconds(1.0f);

            // Set the trigger based on the type of test
            if (isClientOwner)
            {
                AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId].SetTrigger();
            }
            else
            {
                AnimatorTestHelper.ServerSideInstance.SetTrigger();
            }

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

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(() => ClientSideValuesMatch(ownerShipMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues.ValuesToString()}!");

            yield return CreateAndStartNewClient();

            Assert.IsTrue(m_ClientNetworkManagers.Length == 2, $"Newly created and connected client was not added to {nameof(m_ClientNetworkManagers)}!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[1].LocalClientId));
            AssertOnTimeout($"Timed out waiting for the late joining client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Make sure the AnimatorTestHelper client side instances (plus host) is the same as the TotalClients
            Assert.True((AnimatorTestHelper.ClientSideInstances.Count + 1) == TotalClients);

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
