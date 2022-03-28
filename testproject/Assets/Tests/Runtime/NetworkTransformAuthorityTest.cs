using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Components;
using TestProject.ManualTests;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    public class NetworkTransformAuthorityTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private GameObject m_TestPrefab;
        private NetworkObject m_TestPrefabInstance_Server;
        private GenericNetworkObjectBehaviour m_ServerNetworkObjectBehaviour;
        private GenericNetworkObjectBehaviour m_ClientNetworkObjectBehaviour;


        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("AuthorityTest");
            var rigidBody = m_TestPrefab.AddComponent<Rigidbody>();
            // Just to keep the test prefab instances from falling
            rigidBody.useGravity = false;
            m_TestPrefab.AddComponent<NetworkTransform>();
            m_TestPrefab.AddComponent<NetworkRigidbody>();
            m_TestPrefab.AddComponent<MeshRenderer>();
            var genericNetworkObjectBehaviour = m_TestPrefab.AddComponent<GenericNetworkObjectBehaviour>();
            genericNetworkObjectBehaviour.SetRotation(new Vector3(0.0f, 3.33333f, 0.0f));
        }

        /// <summary>
        /// This verifies that a warning message is generated on the client side
        /// if a client tries to modify the transform of a NetworkObject with a
        /// NetworkTransform component.
        /// </summary>
        [UnityTest]
        public IEnumerator VerifyAuthorityWarningOnClient()
        {
            // Spawn the test prefab
            var serverInstance = SpawnObject(m_TestPrefab, m_ServerNetworkManager);
            m_TestPrefabInstance_Server = serverInstance.GetComponent<NetworkObject>();
            m_ServerNetworkObjectBehaviour = serverInstance.GetComponent<GenericNetworkObjectBehaviour>();
            float ang = Random.Range(0.0f, 2 * Mathf.PI);
            m_ServerNetworkObjectBehaviour.SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), 5.0f);

            // Wait for the client side to have spawned and registered the NetworkObject
            var clientId = m_ClientNetworkManagers[0].LocalClientId;
            yield return WaitForConditionOrTimeOut(() => s_GlobalNetworkObjects.ContainsKey(clientId));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to be registered with {nameof(s_GlobalNetworkObjects)} table");
            yield return WaitForConditionOrTimeOut(() => s_GlobalNetworkObjects[clientId].ContainsKey(m_TestPrefabInstance_Server.NetworkObjectId));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to spawn and register {m_TestPrefabInstance_Server.name}");

            // The message requires LogLevel.Developer
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;
            m_ClientNetworkObjectBehaviour = s_GlobalNetworkObjects[clientId][m_TestPrefabInstance_Server.NetworkObjectId].GetComponent<GenericNetworkObjectBehaviour>();

            // Modify the client-side instance's transform position to generate the warning
            m_ClientNetworkObjectBehaviour.transform.position += Vector3.one;
            LogAssert.Expect(LogType.Warning, $"A local change to position without authority detected, reverting back to latest interpolated network state!");

            // Wait a tick while the message is generated
            yield return s_DefaultWaitForTick;

            // Clean up
            m_TestPrefabInstance_Server.Despawn(true);
            Object.Destroy(m_TestPrefab);
        }
    }
}
