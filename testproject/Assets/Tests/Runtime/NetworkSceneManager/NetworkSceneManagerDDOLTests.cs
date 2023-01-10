using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    public class NetworkSceneManagerDDOLTests
    {
        private NetworkManager m_ServerNetworkManager;
        private GameObject m_NetworkManagerGameObject;
        private GameObject m_DDOL_ObjectToSpawn;

        protected float m_ConditionMetFrequency = 0.1f;

        [UnitySetUp]
        protected IEnumerator SetUp()
        {
            m_NetworkManagerGameObject = new GameObject("NetworkManager - Host");
            m_ServerNetworkManager = m_NetworkManagerGameObject.AddComponent<NetworkManager>();

            m_DDOL_ObjectToSpawn = new GameObject();
            var networkObject = m_DDOL_ObjectToSpawn.AddComponent<NetworkObject>();
            m_DDOL_ObjectToSpawn.AddComponent<DDOLBehaviour>();
            networkObject.DontDestroyWithOwner = true;
            networkObject.DestroyWithScene = false;

            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            var unityTransport = m_NetworkManagerGameObject.AddComponent<UnityTransport>();

            var prefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            prefabs.Add(new NetworkPrefab { Prefab = m_DDOL_ObjectToSpawn });
            m_ServerNetworkManager.NetworkConfig = new NetworkConfig()
            {
                ConnectionApproval = false,
                Prefabs = new NetworkPrefabs { NetworkPrefabsLists = new List<NetworkPrefabsList> { prefabs } },
                NetworkTransport = unityTransport
            };
            m_ServerNetworkManager.StartHost();
            yield break;
        }

        [UnityTearDown]
        protected IEnumerator TearDown()
        {
            m_ServerNetworkManager.Shutdown();

            Object.Destroy(m_NetworkManagerGameObject);
            Object.Destroy(m_DDOL_ObjectToSpawn);

            yield break;
        }

        public enum DefaultState
        {
            IsEnabled,
            IsDisabled
        }

        public enum MovedIntoDDOLBy
        {
            User,
            NetworkSceneManager
        }

        public enum NetworkObjectType
        {
            InScenePlaced,
            DynamicallySpawned
        }

        /// <summary>
        /// Tests to make sure NetworkObjects moved into the DDOL will
        /// restore back to their currently active state when a full
        /// scene transition is complete.
        /// This tests both in-scene placed and dynamically spawned NetworkObjects
        [UnityTest]
        public IEnumerator InSceneNetworkObjectState([Values(DefaultState.IsEnabled, DefaultState.IsDisabled)] DefaultState activeState,
            [Values(MovedIntoDDOLBy.User, MovedIntoDDOLBy.NetworkSceneManager)] MovedIntoDDOLBy movedIntoDDOLBy,
            [Values(NetworkObjectType.InScenePlaced, NetworkObjectType.DynamicallySpawned)] NetworkObjectType networkObjectType)
        {
            var waitForFullNetworkTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            var isActive = activeState == DefaultState.IsEnabled ? true : false;
            var isInScene = networkObjectType == NetworkObjectType.InScenePlaced ? true : false;
            var objectInstance = Object.Instantiate(m_DDOL_ObjectToSpawn);

            var networkObject = objectInstance.GetComponent<NetworkObject>();
            var ddolBehaviour = objectInstance.GetComponent<DDOLBehaviour>();

            // All tests require this to be false
            networkObject.DestroyWithScene = false;

            if (movedIntoDDOLBy == MovedIntoDDOLBy.User)
            {
                ddolBehaviour.MoveToDDOL();
            }

            // Sets whether we are in-scene or dynamically spawned NetworkObject
            ddolBehaviour.SetInScene(isInScene);

            networkObject.Spawn();
            yield return waitForFullNetworkTick;

            Assert.That(networkObject.IsSpawned);

            objectInstance.SetActive(isActive);
            m_ServerNetworkManager.SceneManager.MoveObjectsToDontDestroyOnLoad();

            yield return waitForFullNetworkTick;

            // It should be isActive when MoveObjectsToDontDestroyOnLoad is called.
            Assert.That(networkObject.isActiveAndEnabled == isActive);

            m_ServerNetworkManager.SceneManager.MoveObjectsFromDontDestroyOnLoadToScene(SceneManager.GetActiveScene());

            yield return waitForFullNetworkTick;

            // It should be isActive when MoveObjectsFromDontDestroyOnLoadToScene is called.
            Assert.That(networkObject.isActiveAndEnabled == isActive);

            //Done
            networkObject.Despawn();
        }

        public class DDOLBehaviour : NetworkBehaviour
        {
            public void MoveToDDOL()
            {
                DontDestroyOnLoad(gameObject);
            }

            public override void OnNetworkSpawn()
            {
                NetworkObject.DestroyWithScene = false;
                base.OnNetworkSpawn();
            }

            public void SetInScene(bool isInScene)
            {
                var networkObject = GetComponent<NetworkObject>();
                networkObject.IsSceneObject = isInScene;
            }
        }

    }
}
