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
        // Set the minimum threshold which we will use as our margin of error
        public const float MinThreshold = 0.005f;

        public bool CheckPosition;
        public bool IsMoving;
        public bool IsFixed;

        private void Update()
        {
            // Check the position of the nested object on the client
            if (CheckPosition)
            {
                if (transform.position.y < -MinThreshold || transform.position.y > 100.0f + MinThreshold)
                {
                    Debug.LogError($"Interpolation failure. transform.position.y is {transform.position.y}. Should be between 0.0 and 100.0. Current threshold is [+/- {MinThreshold}].");
                }
            }

            // Move the nested object on the server
            if (IsMoving)
            {
                var y = Time.realtimeSinceStartup % 10.0f;

                // change the space between local and global every second
                GetComponent<NetworkTransform>().InLocalSpace = ((int)y % 2 == 0);

                transform.position = new Vector3(0.0f, y * 10, 0.0f);
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
            var networkTransform = m_PrefabToSpawn.AddComponent<NetworkTransform>();
            networkTransform.PositionThreshold = TransformInterpolationObject.MinThreshold;
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
            spawnedObject.GetComponent<TransformInterpolationObject>().IsMoving = true;
            spawnedObject.GetComponent<NetworkTransform>().SetMaxInterpolationBound(1.0f);

            const float maxPlacementError = 0.01f;

            // Wait for the base object to place itself on both instances
            while (m_BaseOnClient.transform.position.y < 1000 - maxPlacementError ||
                   m_BaseOnClient.transform.position.y > 1000 + maxPlacementError ||
                   baseObject.transform.position.y < 1000 - maxPlacementError ||
                   baseObject.transform.position.y > 1000 + maxPlacementError)
            {
                yield return new WaitForSeconds(0.01f);
            }

            m_SpawnedObjectOnClient.GetComponent<NetworkTransform>().SetMaxInterpolationBound(1.0f);
            m_SpawnedObjectOnClient.GetComponent<TransformInterpolationObject>().CheckPosition = true;

            // Test that interpolation works correctly for 10 seconds
            // Increasing this duration gives you the opportunity to go check in the Editor how the objects are setup
            // and how they move
            yield return new WaitForSeconds(10.0f);
        }
    }
}
