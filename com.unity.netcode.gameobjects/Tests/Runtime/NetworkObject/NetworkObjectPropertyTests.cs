using NUnit.Framework;
using System.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Tests properties of NetworkObject for proper functinality.
    /// </summary>
    public class NetworkObjectPropertyTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private NetworkPrefab m_TestPrefab;

        protected override void OnServerAndClientsCreated()
        {
            // create prefab
            var gameObject = new GameObject("TestObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            m_TestPrefab = new NetworkPrefab() { Prefab = gameObject };

            m_ServerNetworkManager.NetworkConfig.Prefabs.Add(m_TestPrefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.Prefabs.Add(m_TestPrefab);
            }
        }

        /// <summary>
        /// Tests PrefabHashId returns corectly when the NetworkObject is not a prefab.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestPrefabHashIdPropertyNotAPrefab()
        {
            const uint kInvalidPrefabHashId = 0;

            var gameObject = new GameObject("TestObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();

            yield return null;
            Assert.AreEqual(kInvalidPrefabHashId, networkObject.PrefabHashId);
        }

        /// <summary>
        /// Tests PrefabHashId returns corectly when the NetworkObject is a prefab.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestPrefabHashIdPropertyIsAPrefab()
        {
            var networkObject = m_TestPrefab.Prefab.GetComponent<NetworkObject>();

            yield return null;
            Assert.AreEqual(networkObject.GlobalObjectIdHash, networkObject.PrefabHashId);
        }
    }
}
