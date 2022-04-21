using NUnit.Framework;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public class SpawnManagerTests
    {
        [Test]
        public void TestGetGlobalObjectIdHash()
        {
            var prefab = new GameObject();
            prefab.AddComponent<NetworkObject>();

            var go = new GameObject();
            var nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
            {
                Prefab = prefab
            });
            nm.NetworkConfig.NetworkTransport = go.AddComponent<UnityTransport>();

            // Start to populate the SpawnManager
            nm.StartHost();

            Assert.True(nm.SpawnManager.GetGlobalObjectIdHash(prefab) == prefab.GetComponent<NetworkObject>().GlobalObjectIdHash);

            nm.Shutdown();
        }
    }
}
