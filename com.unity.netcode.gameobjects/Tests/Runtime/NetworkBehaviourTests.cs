using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkBehaviourTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_NetworkObjectPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_NetworkObjectPrefab = CreateNetworkObjectPrefab("NetworkObject");
            m_NetworkObjectPrefab.AddComponent<TestNetworkBehaviour>();

            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator GetNetworkObjectAndNetworkBehaviourFromNetworkBehaviourTest()
        {
            var authority = GetAuthorityNetworkManager();
            var gameObject = SpawnObject(m_NetworkObjectPrefab, authority);

            yield return WaitForSpawnedOnAllOrTimeOut(gameObject);
            AssertOnTimeout("Timed out waiting for NetworkBehaviour to spawn on all clients.");

            var networkBehaviour = gameObject.GetComponent<TestNetworkBehaviour>();

            Assert.That(networkBehaviour, Is.Not.Null);
            var networkObject = gameObject.GetComponent<NetworkObject>();
            Assert.That(networkObject, Is.EqualTo(networkBehaviour.NetworkObject));

            foreach (var manager in m_NetworkManagers)
            {
                Assert.True(manager.SpawnManager.SpawnedObjects.TryGetValue(networkObject.NetworkObjectId, out var localObject));

                var localBehaviour = localObject.GetComponent<TestNetworkBehaviour>();
                Assert.That(localBehaviour.NetworkObjectWithID(networkObject.NetworkObjectId), Is.EqualTo(localObject));
                Assert.That(localBehaviour.GetBehaviourAtId(networkBehaviour.NetworkBehaviourId), Is.EqualTo(localBehaviour));
            }
        }

        internal class TestNetworkBehaviour : NetworkBehaviour
        {
            // Use protected class so it doesn't look unused, and so it's tested.
            public NetworkBehaviour GetBehaviourAtId(ushort networkBehaviourId)
            {
                return GetNetworkBehaviour(networkBehaviourId);
            }

            public NetworkObject NetworkObjectWithID(ulong networkObjectId)
            {
                return GetNetworkObject(networkObjectId);
            }
        }
    }

}
