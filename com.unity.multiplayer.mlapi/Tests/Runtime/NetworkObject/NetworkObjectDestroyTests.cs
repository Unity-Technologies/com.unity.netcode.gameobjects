using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests
{
    public class NetworkObjectDestroyTests
    {
        private const int k_ClientInstanceCount = 2;

        private NetworkManager m_ServerNetworkManager;
        private NetworkManager[] m_ClientNetworkManagers;
        private GameObject m_PlayerPrefab;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // we need at least 1 client for tests
            Assert.That(k_ClientInstanceCount, Is.GreaterThan(0));

            Assert.That(MultiInstanceHelpers.Create(k_ClientInstanceCount, out m_ServerNetworkManager, out m_ClientNetworkManagers));
            Assert.That(m_ServerNetworkManager, Is.Not.Null);
            Assert.That(m_ClientNetworkManagers, Is.Not.Null);
            Assert.That(m_ClientNetworkManagers.Length, Is.EqualTo(k_ClientInstanceCount));

            m_PlayerPrefab = new GameObject("PlayerPrefabPrototype");
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(m_PlayerPrefab.AddComponent<NetworkObject>());

            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            Assert.That(MultiInstanceHelpers.Start(/* isHost = */ true, m_ServerNetworkManager, m_ClientNetworkManagers));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(m_ClientNetworkManagers));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_ServerNetworkManager));
        }

        [TearDown]
        public void Teardown()
        {
            MultiInstanceHelpers.Destroy();

            if (m_PlayerPrefab != null)
            {
                Object.Destroy(m_PlayerPrefab);
            }
        }

        [UnityTest]
        public IEnumerator ShouldDestroyPlayerObjectOnDisconnect()
        {
            var c0PlayerNetworkObject = m_ServerNetworkManager.ConnectedClientsList[0].PlayerObject;
            var c0PlayerGameObject = c0PlayerNetworkObject.gameObject;
            var c0PlayerObjectId = c0PlayerNetworkObject.NetworkObjectId;
            var c0PlayerClientId = m_ServerNetworkManager.ConnectedClientsList[0].ClientId;

            Assert.That(m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(c0PlayerObjectId));
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                Assert.That(clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(c0PlayerObjectId));
            }

            // m_ServerNetworkManager.DisconnectClient(c0PlayerClientId);
            m_ClientNetworkManagers[0].StopClient();

            int nextFrameNumber = Time.frameCount + 2;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            Assert.That(!m_ServerNetworkManager.ConnectedClients.ContainsKey(c0PlayerClientId));
            Assert.That(!m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(c0PlayerObjectId));
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                Assert.That(!clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(c0PlayerObjectId));
            }
        }

        [UnityTest]
        public IEnumerator ShouldDestroyOwnedObjectsOnDisconnect()
        {
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTest]
        public IEnumerator ShouldNotDestroyOwnedObjectsOnDisconnect()
        {
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTest]
        public IEnumerator ShouldNotDestroyDisownedObjectsOnDisconnect()
        {
            yield return new WaitForSeconds(0.1f);
        }
    }
}
