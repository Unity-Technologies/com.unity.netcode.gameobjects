using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(NetworkSpawnTypes.OnNetworkSpawn)]
    [TestFixture(NetworkSpawnTypes.OnNetworkPostSpawn)]
    internal class ParentingDuringSpawnTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 2;

        public enum NetworkSpawnTypes
        {
            OnNetworkSpawn,
            OnNetworkPostSpawn,
        }

        private NetworkSpawnTypes m_NetworkSpawnType;

        private GameObject m_ParentPrefab;
        private GameObject m_ChildPrefab;
        private NetworkObject m_AuthorityInstance;
        private List<NetworkManager> m_NetworkManagers = new List<NetworkManager>();
        private StringBuilder m_Errors = new StringBuilder();

        public class ParentDuringSpawnBehaviour : NetworkBehaviour
        {
            public GameObject ChildToSpawn;

            public NetworkSpawnTypes NetworkSpawnType;

            public Transform ChildSpawnPoint;

            private void SpawnThenParent()
            {
                var child = NetworkObject.InstantiateAndSpawn(ChildToSpawn, NetworkManager, position: ChildSpawnPoint.position, rotation: ChildSpawnPoint.rotation);
                if (!child.TrySetParent(NetworkObject))
                {
                    var errorMessage = $"[{ChildToSpawn}] Failed to parent child {child.name} under parent {gameObject.name}!";
                    Debug.LogError(errorMessage);
                }
            }

            public override void OnNetworkSpawn()
            {
                if (IsServer && NetworkSpawnType == NetworkSpawnTypes.OnNetworkSpawn)
                {
                    SpawnThenParent();
                }

                base.OnNetworkSpawn();
            }

            protected override void OnNetworkPostSpawn()
            {
                if (IsServer && NetworkSpawnType == NetworkSpawnTypes.OnNetworkPostSpawn)
                {
                    SpawnThenParent();
                }
                base.OnNetworkPostSpawn();
            }
        }

        public ParentingDuringSpawnTests(NetworkSpawnTypes networkSpawnType) : base()
        {
            m_NetworkSpawnType = networkSpawnType;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ParentPrefab = CreateNetworkObjectPrefab("Parent");
            m_ChildPrefab = CreateNetworkObjectPrefab("Child");
            var parentComponet = m_ParentPrefab.AddComponent<ParentDuringSpawnBehaviour>();
            parentComponet.ChildToSpawn = m_ChildPrefab;
            var spawnPoint = new GameObject();
            parentComponet.ChildSpawnPoint = spawnPoint.transform;
            parentComponet.ChildSpawnPoint.position = GetRandomVector3(-5.0f, 5.0f);
            var rotation = parentComponet.ChildSpawnPoint.rotation;
            rotation.eulerAngles = GetRandomVector3(-180.0f, 180.0f);
            parentComponet.ChildSpawnPoint.rotation = rotation;
            base.OnServerAndClientsCreated();
        }

        private bool NonAuthorityInstancesSpawnedParent()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthorityInstance.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool NonAuthorityInstancesParentedChild()
        {
            m_Errors.Clear();
            if (m_AuthorityInstance.transform.childCount == 0)
            {
                return false;
            }
            var authorityChildObject = m_AuthorityInstance.transform.GetChild(0).GetComponent<NetworkObject>();

            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(authorityChildObject.NetworkObjectId))
                {
                    m_Errors.AppendLine($"{networkManager.name} has not spawned the child {authorityChildObject.name}!");
                    return false;
                }
                var childObject = networkManager.SpawnManager.SpawnedObjects[authorityChildObject.NetworkObjectId];

                if (childObject.transform.parent == null)
                {
                    m_Errors.AppendLine($"{childObject.name} does not have a parent!");
                    return false;
                }

                if (!Approximately(authorityChildObject.transform.position, childObject.transform.position))
                {
                    m_Errors.AppendLine($"{childObject.name} position {GetVector3Values(childObject.transform.position)} does " +
                        $"not match the authority's position {GetVector3Values(authorityChildObject.transform.position)}!");
                    return false;
                }

                if (!Approximately(authorityChildObject.transform.rotation, childObject.transform.rotation))
                {
                    m_Errors.AppendLine($"{childObject.name} rotation {GetVector3Values(childObject.transform.rotation.eulerAngles)} does " +
                        $"not match the authority's position {GetVector3Values(authorityChildObject.transform.rotation.eulerAngles)}!");
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator ParentDuringSpawn()
        {
            m_NetworkManagers.Clear();
            var authorityNetworkManager = m_ServerNetworkManager;

            m_NetworkManagers.AddRange(m_ClientNetworkManagers);
            m_NetworkManagers.Add(m_ServerNetworkManager);

            m_AuthorityInstance = SpawnObject(m_ParentPrefab, authorityNetworkManager).GetComponent<NetworkObject>();

            yield return WaitForConditionOrTimeOut(NonAuthorityInstancesSpawnedParent);
            AssertOnTimeout($"Not all clients spawned the parent {nameof(NetworkObject)}!");

            yield return WaitForConditionOrTimeOut(NonAuthorityInstancesParentedChild);
            AssertOnTimeout($"Non-Authority instance had a mismatched value: \n {m_Errors}");
        }
    }
}
