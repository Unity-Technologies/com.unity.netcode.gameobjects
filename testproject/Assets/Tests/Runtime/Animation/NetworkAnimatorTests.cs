using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Components;


namespace TestProject.RuntimeTests
{
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
            yield return WaitForConditionOrTimeOut(()=>ClientSideValuesMatch(authoritativeMode));
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
    }
}
