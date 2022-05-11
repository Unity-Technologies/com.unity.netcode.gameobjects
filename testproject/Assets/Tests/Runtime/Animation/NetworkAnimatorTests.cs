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
        protected override int NumberOfClients => 2;
        private GameObject m_AnimationTestPrefab;
        private AnimatorTestHelper.ParameterValues m_ParameterValues;

        protected override IEnumerator OnSetup()
        {
            AnimatorTestHelper.Initialize();
            TriggerTest.ClientsThatTriggered.Clear();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            var loadedObject = Resources.Load(k_AnimatorObjectName);
            Assert.NotNull(loadedObject, $"Failed to load resource AnimatorObject");
            var gameObject = loadedObject as GameObject;
            var networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_ServerNetworkManager;
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            var networkPrefab = new NetworkPrefab() { Prefab = gameObject };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }
            gameObject.name = "AnimTest";
            m_AnimationTestPrefab = gameObject;

            base.OnServerAndClientsCreated();
        }

        private bool ClientSideValuesMatch(AuthoritativeMode authoritativeMode)
        {
            if (authoritativeMode == AuthoritativeMode.Owner)
            {
                var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
                if (!serverParameters.ValuesMatch(m_ParameterValues))
                {
                    return false;
                }
            }
            foreach (var animatorTestHelper in AnimatorTestHelper.ClientSideInstances)
            {
                if (authoritativeMode == AuthoritativeMode.Owner && animatorTestHelper.Value.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId)
                {
                    continue;
                }
                var clientParameters = animatorTestHelper.Value.GetParameterValues();
                if (!clientParameters.ValuesMatch(m_ParameterValues))
                {
                    return false;
                }
            }

            return true;
        }

        public enum AuthoritativeMode
        {
            Server,
            Owner
        }

        /// <summary>
        /// Verifies that parameters are synchronized with currently connected clients
        /// when no transition or layer change has occurred.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator ParameterUpdateTests([Values] AuthoritativeMode authoritativeMode)
        {
            var networkAnimator = m_AnimationTestPrefab.GetComponent<NetworkAnimator>();
            networkAnimator.OwnerAuthoritative = authoritativeMode == AuthoritativeMode.Owner;

            // Spawn our test animator object
            var objectInstance = Object.Instantiate(m_AnimationTestPrefab);
            if (networkAnimator.OwnerAuthoritative)
            {
                objectInstance.GetComponent<NetworkObject>().SpawnWithOwnership(m_ClientNetworkManagers[0].LocalClientId);
            }
            else
            {
                objectInstance.GetComponent<NetworkObject>().Spawn();
            }

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (networkAnimator.OwnerAuthoritative)
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
            yield return WaitForConditionOrTimeOut(() => ClientSideValuesMatch(authoritativeMode));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");
        }


        private bool AllTriggersDetected(AuthoritativeMode authoritativeMode)
        {
            if (authoritativeMode == AuthoritativeMode.Owner)
            {
                var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
                if (!TriggerTest.ClientsThatTriggered.Contains(m_ServerNetworkManager.LocalClientId))
                {
                    return false;
                }
            }
            foreach (var animatorTestHelper in AnimatorTestHelper.ClientSideInstances)
            {
                if (!animatorTestHelper.Value.IsSpawned)
                {
                    continue;
                }
                if (authoritativeMode == AuthoritativeMode.Owner && animatorTestHelper.Value.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId)
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
        public IEnumerator TriggerUpdateTests([Values] AuthoritativeMode authoritativeMode)
        {
            var networkAnimator = m_AnimationTestPrefab.GetComponent<NetworkAnimator>();
            networkAnimator.OwnerAuthoritative = authoritativeMode == AuthoritativeMode.Owner;

            // Spawn our test animator object
            var objectInstance = Object.Instantiate(m_AnimationTestPrefab);
            if (networkAnimator.OwnerAuthoritative)
            {
                objectInstance.GetComponent<NetworkObject>().SpawnWithOwnership(m_ClientNetworkManagers[0].LocalClientId);
            }
            else
            {
                objectInstance.GetComponent<NetworkObject>().Spawn();
            }

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Set set the trigger based on the type of test
            if (networkAnimator.OwnerAuthoritative)
            {
                AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId].SetTrigger();
            }
            else
            {
                AnimatorTestHelper.ServerSideInstance.SetTrigger();
            }

            // Wait for all triggers to fire
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(authoritativeMode));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");
        }

        /// <summary>
        /// Verifies that late joining clients are synchronized to an
        /// animator's state.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator LateJoinSynchronizationTest([Values] AuthoritativeMode authoritativeMode)
        {
            // Stop the 2nd client (will join after the animator's state is changed)
            NetcodeIntegrationTestHelpers.StopOneClient(m_ClientNetworkManagers[1], false);

            var networkAnimator = m_AnimationTestPrefab.GetComponent<NetworkAnimator>();
            networkAnimator.OwnerAuthoritative = authoritativeMode == AuthoritativeMode.Owner;

            // Spawn our test animator object
            var objectInstance = Object.Instantiate(m_AnimationTestPrefab);
            if (networkAnimator.OwnerAuthoritative)
            {
                objectInstance.GetComponent<NetworkObject>().SpawnWithOwnership(m_ClientNetworkManagers[0].LocalClientId);
            }
            else
            {
                objectInstance.GetComponent<NetworkObject>().Spawn();
            }

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Set set the trigger based on the type of test
            if (networkAnimator.OwnerAuthoritative)
            {
                AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId].SetTrigger();
            }
            else
            {
                AnimatorTestHelper.ServerSideInstance.SetTrigger();
            }

            // Wait for all triggers to fire
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(authoritativeMode));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (networkAnimator.OwnerAuthoritative)
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
            yield return WaitForConditionOrTimeOut(() => ClientSideValuesMatch(authoritativeMode));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");

            // Start the 2nd client to verify its local animator's state is synchronized to the changes
            NetcodeIntegrationTestHelpers.StartOneClient(m_ClientNetworkManagers[1]);

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[1].LocalClientId));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the late joining client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Make sure the AnimatorTestHelper client side instances (plus host) is the same as the TotalClients
            Assert.True((AnimatorTestHelper.ClientSideInstances.Count + 1) == TotalClients);

            // Now check that the late joining client and all other clients are synchronized to the trigger
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(authoritativeMode));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");

            // Now check that the late joining client and all other clients are synchronized to the updated parameter values
            yield return WaitForConditionOrTimeOut(() => ClientSideValuesMatch(authoritativeMode));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");
        }
    }
}
