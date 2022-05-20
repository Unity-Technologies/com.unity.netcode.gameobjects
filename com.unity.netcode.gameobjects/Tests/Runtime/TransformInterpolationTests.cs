using System.Collections;
using Unity.Netcode.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;


namespace Unity.Netcode.RuntimeTests
{
    public class TransformInterpolationObject : NetworkBehaviour
    {

        public bool IsFixed;

        private float m_StartTime;
        private float m_StopTime;
        private bool m_IsMoving;
        private bool m_CheckPosition;
        private float m_TimeDelta
        {
            get
            {
                return Time.realtimeSinceStartup - m_StartTime;
            }
        }

        // [NSS] Just set this to true to see the timing for both server and client
        private bool m_DebugInfo = false;

        public bool IsTesting
        {
            get
            {
                if (m_DebugInfo)
                {
                    var isTesting = m_StopTime > Time.realtimeSinceStartup;
                    if (isTesting)
                    {
                        if (m_CheckPosition)
                        {
                            Debug.Log($"[Client-Side][{m_StopTime}][{Time.realtimeSinceStartup}]--[{m_TimeDelta}]");
                        }
                        else if (m_IsMoving)
                        {
                            Debug.Log($"[Server-Side][{m_StopTime}][{Time.realtimeSinceStartup}]--[{m_TimeDelta}]");
                        }
                    }
                    return isTesting;
                }
                else
                {
                    return m_StopTime > Time.realtimeSinceStartup;
                }
            }
        }

        public void StartTest(float duration)
        {
            m_StartTime = Time.realtimeSinceStartup;
            m_StopTime = Time.realtimeSinceStartup + duration;
            if (IsServer)
            {
                m_IsMoving = true;
            }
            else
            {
                m_CheckPosition = true;
            }
        }

        private void Update()
        {
            // Since the local position is transformed from local to global and vice-versa on the server and client
            // it may accumulate some error. We allow an error of 0.01 over the range of 1000 used in this test.
            // This requires precision to 5 digits, so it doesn't weaken the test, while preventing spurious failures
            const float maxRoundingError = 0.01f;

            // Check the position of the nested object on the client
            if (m_CheckPosition && IsTesting)
            {
                if (transform.position.y < -maxRoundingError || transform.position.y > 100.0f + maxRoundingError)
                {
                    Debug.LogError($"Interpolation failure. transform.position.y is {transform.position.y}. Should be between 0.0 and 100.0");
                }
            }

            if (m_IsMoving && IsTesting)            // Move the nested object on the server
            {
                // change the space between local and global every second
                GetComponent<NetworkTransform>().InLocalSpace = ((int)m_TimeDelta % 2 == 0);

                transform.position = new Vector3(0.0f, m_TimeDelta * 10, 0.0f);
            }

            // On the server, make sure to keep the parent object at a fixed position
            if (IsFixed)
            {
                transform.position = new Vector3(1000.0f, 1000.0f, 1000.0f);
            }
        }
    }

    public class TransformInterpolationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_PrefabToSpawn;

        private NetworkObject m_SpawnedAsNetworkObject;
        private NetworkObject m_SpawnedObjectOnClient;

        private NetworkObject m_BaseAsNetworkObject;
        private NetworkObject m_BaseOnClient;


        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("InterpTestObject");
            m_PrefabToSpawn.AddComponent<NetworkTransform>();
            m_PrefabToSpawn.AddComponent<TransformInterpolationObject>();
        }

        private IEnumerator RefreshNetworkObjects()
        {
            var clientId = m_ClientNetworkManagers[0].LocalClientId;
            yield return WaitForConditionOrTimeOut(() => s_GlobalNetworkObjects.ContainsKey(clientId) &&
            s_GlobalNetworkObjects[clientId].ContainsKey(m_BaseAsNetworkObject.NetworkObjectId) &&
            s_GlobalNetworkObjects[clientId].ContainsKey(m_SpawnedAsNetworkObject.NetworkObjectId));

            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client side {nameof(NetworkObject)} ID of {m_SpawnedAsNetworkObject.NetworkObjectId}");

            m_BaseOnClient = s_GlobalNetworkObjects[clientId][m_BaseAsNetworkObject.NetworkObjectId];
            // make sure the objects are set with the right network manager
            m_BaseOnClient.NetworkManagerOwner = m_ClientNetworkManagers[0];

            m_SpawnedObjectOnClient = s_GlobalNetworkObjects[clientId][m_SpawnedAsNetworkObject.NetworkObjectId];
            // make sure the objects are set with the right network manager
            m_SpawnedObjectOnClient.NetworkManagerOwner = m_ClientNetworkManagers[0];
        }

        [UnityTest]
        public IEnumerator TransformInterpolationTest()
        {
            // create an object
            var spawnedObject = Object.Instantiate(m_PrefabToSpawn);
            var baseObject = Object.Instantiate(m_PrefabToSpawn);
            baseObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            baseObject.GetComponent<NetworkObject>().Spawn();

            m_SpawnedAsNetworkObject = spawnedObject.GetComponent<NetworkObject>();
            m_SpawnedAsNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;

            m_BaseAsNetworkObject = baseObject.GetComponent<NetworkObject>();
            m_BaseAsNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;


            m_SpawnedAsNetworkObject.TrySetParent(baseObject);

            m_SpawnedAsNetworkObject.Spawn();

            yield return RefreshNetworkObjects();

            m_SpawnedAsNetworkObject.TrySetParent(baseObject);

            baseObject.GetComponent<TransformInterpolationObject>().IsFixed = true;
            var serverSideTestComponent = spawnedObject.GetComponent<TransformInterpolationObject>();
            serverSideTestComponent.StartTest(10.0f);

            const float maxPlacementError = 0.01f;

            // Wait for the base object to place itself on both instances
            while (m_BaseOnClient.transform.position.y < 1000 - maxPlacementError ||
                   m_BaseOnClient.transform.position.y > 1000 + maxPlacementError ||
                   baseObject.transform.position.y < 1000 - maxPlacementError ||
                   baseObject.transform.position.y > 1000 + maxPlacementError)
            {
                yield return new WaitForSeconds(0.01f);
            }
            var clientSideTestComponent = m_SpawnedObjectOnClient.GetComponent<TransformInterpolationObject>();
            clientSideTestComponent.StartTest(10.0f);

            // Test that interpolation works correctly for 10 seconds
            // Increasing this duration gives you the opportunity to go check in the Editor how the objects are setup
            // and how they move

            var timeoutHelper = new TimeoutHelper(11.0f);
            yield return WaitForConditionOrTimeOut(() => !clientSideTestComponent.IsTesting, timeoutHelper);
            AssertOnTimeout("Timed out waiting for the test to complete!", timeoutHelper);
        }
    }
}
