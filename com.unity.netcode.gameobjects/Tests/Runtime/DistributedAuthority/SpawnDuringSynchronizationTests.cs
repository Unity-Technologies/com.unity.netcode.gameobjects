using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(NetworkBehaviourSpawnTimes.OnNetworkSpawn, SceneManagement.Enabled)]
    [TestFixture(NetworkBehaviourSpawnTimes.OnNetworkPostSpawn, SceneManagement.Enabled)]
    [TestFixture(NetworkBehaviourSpawnTimes.OnSynchronized, SceneManagement.Enabled)]
    [TestFixture(NetworkBehaviourSpawnTimes.OnNetworkSpawn, SceneManagement.Disabled)]
    [TestFixture(NetworkBehaviourSpawnTimes.OnNetworkPostSpawn, SceneManagement.Disabled)]
    [TestFixture(NetworkBehaviourSpawnTimes.OnSynchronized, SceneManagement.Disabled)]
    internal class SpawnDuringSynchronizationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private List<NetworkManager> m_AllNetworkManagers = new List<NetworkManager>();
        private StringBuilder m_ErrorLog = new StringBuilder();

        private NetworkBehaviourSpawnTimes m_SpawnTime;
        private bool m_EnableSceneManagement;
        internal enum NetworkBehaviourSpawnTimes
        {
            OnNetworkSpawn,
            OnNetworkPostSpawn,
            OnSynchronized
        }

        internal enum SceneManagement
        {
            Enabled,
            Disabled,
        }

        internal class SpawnTestComponent : NetworkBehaviour
        {
            public GameObject PrefabToSpawn;
            public NetworkObject SpawnedObject { get; private set; }
            public NetworkBehaviourSpawnTimes SpawnTime;

            private void SpawnObject(NetworkBehaviourSpawnTimes spawnTime)
            {
                if (IsOwner && SpawnTime == spawnTime)
                {
                    SpawnedObject = NetworkObject.InstantiateAndSpawn(PrefabToSpawn, NetworkManager, OwnerClientId);
                }
            }

            public override void OnNetworkSpawn()
            {
                SpawnObject(NetworkBehaviourSpawnTimes.OnNetworkSpawn);
                base.OnNetworkSpawn();
            }

            protected override void OnNetworkPostSpawn()
            {
                SpawnObject(NetworkBehaviourSpawnTimes.OnNetworkPostSpawn);
                base.OnNetworkPostSpawn();
            }

            protected override void OnNetworkSessionSynchronized()
            {
                SpawnObject(NetworkBehaviourSpawnTimes.OnSynchronized);
                base.OnNetworkSessionSynchronized();
            }
        }


        public SpawnDuringSynchronizationTests(NetworkBehaviourSpawnTimes spawnTime, SceneManagement sceneManagement) : base(NetworkTopologyTypes.DistributedAuthority, HostOrServer.DAHost)
        {
            m_SpawnTime = spawnTime;
            m_EnableSceneManagement = sceneManagement == SceneManagement.Enabled;
        }

        protected override void OnCreatePlayerPrefab()
        {
            var spawnTestComponent = m_PlayerPrefab.AddComponent<SpawnTestComponent>();
            spawnTestComponent.SpawnTime = m_SpawnTime;
            spawnTestComponent.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.None);
            base.OnCreatePlayerPrefab();
        }

        protected override void OnServerAndClientsCreated()
        {
            var spawnTestComponent = m_PlayerPrefab.GetComponent<SpawnTestComponent>();
            spawnTestComponent.PrefabToSpawn = CreateNetworkObjectPrefab("ObjToSpawn");
            spawnTestComponent.PrefabToSpawn.GetComponent<NetworkObject>().SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            if (!UseCMBService())
            {
                m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = m_EnableSceneManagement;
            }
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                networkManager.NetworkConfig.EnableSceneManagement = m_EnableSceneManagement;
            }
            base.OnServerAndClientsCreated();
        }

        private bool AllClientsSpawnedObject()
        {
            m_ErrorLog.Clear();
            var spawnTestComponent = (SpawnTestComponent)null;
            foreach (var networkManager in m_AllNetworkManagers)
            {
                spawnTestComponent = networkManager.LocalClient.PlayerObject.GetComponent<SpawnTestComponent>();
                if (spawnTestComponent.SpawnedObject == null || !spawnTestComponent.SpawnedObject.IsSpawned)
                {
                    m_ErrorLog.AppendLine($"{networkManager.name}'s player failed to spawn the network prefab!");
                    break;
                }
                foreach (var networkManagerToCheck in m_AllNetworkManagers)
                {
                    if (networkManagerToCheck == networkManager)
                    {
                        continue;
                    }
                    if (!networkManagerToCheck.SpawnManager.SpawnedObjects.ContainsKey(spawnTestComponent.NetworkObjectId))
                    {
                        m_ErrorLog.AppendLine($"{networkManager.name}'s player failed to spawn the network prefab!");
                    }
                }
            }
            return m_ErrorLog.Length == 0;
        }

        /// <summary>
        /// Validates that a client can spawn network prefabs during OnNetworkSpawn, OnNetworkPostSpawn, and OnNetworkSessionSynchronized.
        /// </summary>
        [UnityTest]
        public IEnumerator SpawnDuringSynchronization()
        {
            m_AllNetworkManagers.Clear();
            m_AllNetworkManagers.AddRange(m_ClientNetworkManagers);
            if (!UseCMBService())
            {
                m_AllNetworkManagers.Add(m_ServerNetworkManager);
            }

            yield return WaitForConditionOrTimeOut(AllClientsSpawnedObject);
            AssertOnTimeout(m_ErrorLog.ToString());
        }
    }
}
