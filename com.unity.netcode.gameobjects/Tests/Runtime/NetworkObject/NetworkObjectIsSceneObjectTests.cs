using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// This class overrides NetworkSceneManager so that it does scene-placed object discovery when it's constructed
    /// This gives the ability to add in-scene-placed objects in tests by simply creating them before
    /// calling StartServer()/StartClient()/StartHost()
    /// </summary>
    public class TestNetworkSceneManager : NetworkSceneManager
    {
        public TestNetworkSceneManager(NetworkManager networkManager) : base(networkManager)
        {
            PopulateScenePlacedObjects(SceneManager.GetActiveScene());
        }

    }
    public class NetworkObjectIsSceneObjectTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        public GameObject m_Prefab;
        public GameObject m_Prefab2;
        public GameObject m_Prefab3;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(false, NbClients, _ =>
            {
                m_Prefab = new GameObject("SceneObject");
                var networkObject = m_Prefab.AddComponent<NetworkObject>();
                MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

                m_Prefab2 = new GameObject("NotSceneObject");
                var networkObject2 = m_Prefab2.AddComponent<NetworkObject>();
                MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject2);

                m_Prefab3 = new GameObject("AlsoNotSceneObject");
                var networkObject3 = m_Prefab3.AddComponent<NetworkObject>();
                MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject3);

                var validNetworkPrefab = new NetworkPrefab();
                validNetworkPrefab.Prefab = m_Prefab;
                var validNetworkPrefab2 = new NetworkPrefab();
                validNetworkPrefab2.Prefab = m_Prefab2;
                var validNetworkPrefab3 = new NetworkPrefab();
                validNetworkPrefab3.Prefab = m_Prefab3;
                m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
                m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab2);
                m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab3);
                m_ServerNetworkManager.CreateNetworkSceneManager = (networkManager => new TestNetworkSceneManager(networkManager));
                foreach (var client in m_ClientNetworkManagers)
                {
                    client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
                    client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab2);
                    client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab3);
                    client.CreateNetworkSceneManager = (networkManager => new TestNetworkSceneManager(networkManager));
                }

                // Create a "scene object"
                var serverObject = Object.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
                NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
                serverNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
                serverNetworkObject.IsSceneObject = null;

                // Create the client "scene object"
                var clientObject = Object.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
                NetworkObject clientNetworkObject = clientObject.GetComponent<NetworkObject>();
                clientNetworkObject.NetworkManagerOwner = m_ClientNetworkManagers[0];
                clientNetworkObject.IsSceneObject = null;
            });
        }

        protected override void OnBeforeClientStart()
        {
            // Spawn a "runtime object" on the server before the client connects
            var serverObject2 = Object.Instantiate(m_Prefab2, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject2 = serverObject2.GetComponent<NetworkObject>();
            serverNetworkObject2.NetworkManagerOwner = m_ServerNetworkManager;
            serverNetworkObject2.Spawn();
        }

        [UnityTest]
        public IEnumerator WhenConnectingToServer_SceneObjectsOnClientsAreMarkedAsSceneObjects()
        {
            var clientSceneObjectResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.gameObject.name == "SceneObject(Clone)"), m_ClientNetworkManagers[0], clientSceneObjectResult));

            Assert.IsTrue(clientSceneObjectResult.Result.IsSceneObject);
        }

        [UnityTest]
        public IEnumerator WhenConnectingToServer_NonSceneObjectsOnClientsAreNotMarkedAsSceneObjects()
        {
            var clientNotSceneObjectResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.gameObject.name == "NotSceneObject(Clone)"), m_ClientNetworkManagers[0], clientNotSceneObjectResult));

            Assert.IsFalse(clientNotSceneObjectResult.Result.IsSceneObject);
        }

        [UnityTest]
        public IEnumerator WhenSpawningAnObjectAfterConnecting_ItIsNotMarkedAsASceneObject()
        {
            var serverObject3 = Object.Instantiate(m_Prefab3, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject3 = serverObject3.GetComponent<NetworkObject>();
            serverNetworkObject3.NetworkManagerOwner = m_ServerNetworkManager;
            serverNetworkObject3.Spawn();

            var clientNotSceneObjectResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.gameObject.name == "AlsoNotSceneObject(Clone)"), m_ClientNetworkManagers[0], clientNotSceneObjectResult));

            Assert.IsFalse(clientNotSceneObjectResult.Result.IsSceneObject);
        }
    }
}
