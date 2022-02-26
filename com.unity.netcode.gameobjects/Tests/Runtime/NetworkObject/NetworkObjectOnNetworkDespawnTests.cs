using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;


namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Tests that check OnNetworkDespawn being invoked
    /// </summary>
    public class NetworkObjectOnNetworkDespawnTests
    {
        private NetworkManager m_ServerHost;
        private NetworkManager[] m_Clients;

        private GameObject m_ObjectToSpawn;
        private NetworkObject m_NetworkObject;

        internal class OnNetworkDespawnTestComponent : NetworkBehaviour
        {
            public bool OnNetworkDespawnCalled { get; internal set; }

            public override void OnNetworkSpawn()
            {
                OnNetworkDespawnCalled = false;
                base.OnNetworkSpawn();
            }

            public override void OnNetworkDespawn()
            {
                OnNetworkDespawnCalled = true;
                base.OnNetworkDespawn();
            }
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.IsTrue(NetcodeIntegrationTestHelpers.Create(1, out m_ServerHost, out m_Clients));

            m_ObjectToSpawn = new GameObject();
            m_NetworkObject = m_ObjectToSpawn.AddComponent<NetworkObject>();
            m_ObjectToSpawn.AddComponent<OnNetworkDespawnTestComponent>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_NetworkObject);

            var networkPrefab = new NetworkPrefab();
            networkPrefab.Prefab = m_ObjectToSpawn;
            m_ServerHost.NetworkConfig.NetworkPrefabs.Add(networkPrefab);

            foreach (var client in m_Clients)
            {
                client.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            // Shutdown and clean up both of our NetworkManager instances
            if (m_ObjectToSpawn)
            {
                Object.Destroy(m_ObjectToSpawn);
                m_ObjectToSpawn = null;
            }
            NetcodeIntegrationTestHelpers.Destroy();
            yield return null;
        }

        public enum InstanceType
        {
            Server,
            Host,
            Client
        }

        /// <summary>
        /// Tests that a spawned NetworkObject's associated NetworkBehaviours will have
        /// their OnNetworkDespawn invoked during NetworkManager shutdown.
        /// </summary>
        [UnityTest]
        public IEnumerator TestNetworkObjectDespawnOnShutdown([Values(InstanceType.Server, InstanceType.Host, InstanceType.Client)] InstanceType despawnCheck)
        {
            var useHost = despawnCheck != InstanceType.Server;
            var networkManager = despawnCheck == InstanceType.Host || despawnCheck == InstanceType.Server ? m_ServerHost : m_Clients[0];

            // Start the instances
            if (!NetcodeIntegrationTestHelpers.Start(useHost, m_ServerHost, m_Clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(m_Clients, null, 512);

            // [Host-Server-Side] Check to make sure all clients are connected
            var clientCount = useHost ? m_Clients.Length + 1 : m_Clients.Length;
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(m_ServerHost, clientCount, null, 512);

            // Spawn the test object
            var spawnedObject = Object.Instantiate(m_NetworkObject);
            var spawnedNetworkObject = spawnedObject.GetComponent<NetworkObject>();
            spawnedNetworkObject.NetworkManagerOwner = m_ServerHost;
            spawnedNetworkObject.Spawn(true);

            // Get the spawned object relative to which NetworkManager instance we are testing.
            var relativeSpawnedObject = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.GetComponent<OnNetworkDespawnTestComponent>() != null), networkManager, relativeSpawnedObject);
            var onNetworkDespawnTestComponent = relativeSpawnedObject.Result.GetComponent<OnNetworkDespawnTestComponent>();

            // Confirm it is not set before shutting down the NetworkManager
            Assert.IsFalse(onNetworkDespawnTestComponent.OnNetworkDespawnCalled);

            // Shutdown the NetworkManager instance we are testing.
            networkManager.Shutdown();

            // Since shutdown is now delayed until the post frame update
            // just wait 2 frames before checking to see if OnNetworkDespawnCalled is true
            var currentFrame = Time.frameCount + 2;
            yield return new WaitUntil(() => Time.frameCount <= currentFrame);

            // Confirm that OnNetworkDespawn is invoked after shutdown
            Assert.IsTrue(onNetworkDespawnTestComponent.OnNetworkDespawnCalled);
        }
    }
}

