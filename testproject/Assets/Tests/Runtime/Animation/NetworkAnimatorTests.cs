using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;


namespace TestProject.RuntimeTests
{
    public class NetworkAnimatorTests : NetcodeIntegrationTest
    {
        private const string k_AnimatorObjectName = "AnimatorObject";
        protected override int NumberOfClients => 1;
        private GameObject m_AnimationTestPrefab;
        private AnimatorTestHelper m_ClientAnimatorTestHelper;
        private AnimatorTestHelper.ParameterValues m_ParameterValues;

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

        private bool ClientSideValuesMatch()
        {
            var clientParameters = m_ClientAnimatorTestHelper.GetParameterValues();
            return clientParameters.ValuesMatch(m_ParameterValues);
        }

        [UnityTest]
        public IEnumerator ParameterUpdateTests()
        {
            // Spawn our test animator object
            var objectInstance = Object.Instantiate(m_AnimationTestPrefab);
            objectInstance.GetComponent<NetworkObject>().Spawn();

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the server-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side instance of {m_AnimationTestPrefab.name} to be spawned!");

            // Get the client animator test helper instance
            m_ClientAnimatorTestHelper = AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId];

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };
            // Set the new parameter values
            AnimatorTestHelper.ServerSideInstance.UpdateParameters(m_ParameterValues);

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(ClientSideValuesMatch);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");
        }
    }
}
