using System.Collections;
using NUnit.Framework;
using TestProject.RuntimeTests.Support;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class NetworkVariableInitializationOnNetworkSpawnTest
    {
        private GameObject m_Prefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Make sure these static values are reset
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnClient = false;
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnServer = false;
            NetworkVariableInitOnNetworkSpawn.OnValueChangedCalledOnClient = false;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            // Shutdown and clean up both of our NetworkManager instances
            if (m_Prefab)
            {
                MultiInstanceHelpers.Destroy();
                Object.Destroy(m_Prefab);
                m_Prefab = null;
            }
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnClient = false;
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnServer = false;
            NetworkVariableInitOnNetworkSpawn.OnValueChangedCalledOnClient = false;
            yield break;
        }

        private IEnumerator RunTest()
        {
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnServer = false;
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnClient = false;
            NetworkVariableInitOnNetworkSpawn.OnValueChangedCalledOnClient = false;
            NetworkVariableInitOnNetworkSpawn.ExpectedSpawnValueOnClient = 5;

            const int numClients = 1;
            Assert.True(MultiInstanceHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));
            m_Prefab = new GameObject("Object");
            var networkObject = m_Prefab.AddComponent<NetworkObject>();
            m_Prefab.AddComponent<NetworkVariableInitOnNetworkSpawn>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            var validNetworkPrefab = new NetworkPrefab();
            validNetworkPrefab.Prefab = m_Prefab;
            server.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            foreach (var client in clients)
            {
                client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 512));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512));

            var serverObject = Object.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = server;
            serverNetworkObject.Spawn();

            // Wait until all objects have spawned.
            const int maxFrames = 240;
            var doubleCheckTime = Time.realtimeSinceStartup + 5.0f;
            while (!NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnClient)
            {
                if (Time.frameCount > maxFrames)
                {
                    // This is here in the event a platform is running at a higher
                    // frame rate than expected
                    if (doubleCheckTime < Time.realtimeSinceStartup)
                    {
                        Assert.Fail("Did not successfully spawn all expected NetworkObjects");
                        break;
                    }
                }
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }
        }

        [UnityTest]
        [Description("When a network variable is initialized in OnNetworkSpawn on the server, the spawned object's NetworkVariable on the client is initialized with the same value.")]
        public IEnumerator WhenANetworkVariableIsInitializedInOnNetworkSpawnOnTheServer_TheSpawnedObjectsNetworkVariableOnTheClientIsInitializedWithTheSameValue()
        {
            yield return RunTest();
            Assert.IsTrue(NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnServer);
            Assert.IsTrue(NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnClient);
        }

        [UnityTest]
        [Description("When a network variable is initialized in OnNetworkSpawn on the server, OnValueChanged is not called")]
        public IEnumerator WhenANetworkVariableIsInitializedInOnNetworkSpawnOnTheServer_OnValueChangedIsNotCalled()
        {
            yield return RunTest();
            Assert.IsTrue(NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnServer);
            Assert.IsTrue(NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnClient);
        }

        [UnityTest]
        [Description("When a network variable is changed just after OnNetworkSpawn on the server, OnValueChanged is called after OnNetworkSpawn")]
        public IEnumerator WhenANetworkVariableIsInitializedJustAfterOnNetworkSpawnOnTheServer_OnValueChangedIsCalledAfterOnNetworkSpawn()
        {
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnServer = false;
            NetworkVariableInitOnNetworkSpawn.NetworkSpawnCalledOnClient = false;
            NetworkVariableInitOnNetworkSpawn.OnValueChangedCalledOnClient = false;

            const int numClients = 1;
            Assert.True(MultiInstanceHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));
            m_Prefab = new GameObject("Object");
            var networkObject = m_Prefab.AddComponent<NetworkObject>();
            m_Prefab.AddComponent<NetworkVariableInitOnNetworkSpawn>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            var validNetworkPrefab = new NetworkPrefab();
            validNetworkPrefab.Prefab = m_Prefab;
            server.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            foreach (var client in clients)
            {
                client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 512));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512));

            var serverObject = Object.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = server;

            serverNetworkObject.Spawn();
            serverNetworkObject.GetComponent<NetworkVariableInitOnNetworkSpawn>().Variable.Value = 10;

            Assert.IsFalse(NetworkVariableInitOnNetworkSpawn.OnValueChangedCalledOnClient);

            // Expect the value to spawn with the already updated value
            NetworkVariableInitOnNetworkSpawn.ExpectedSpawnValueOnClient = 10;

            // Wait until all objects have spawned.
            //const int expectedNetworkObjects = numClients + 2; // +2 = one for prefab, one for server.
            const int maxFrames = 240;
            var doubleCheckTime = Time.realtimeSinceStartup + 5.0f;
            while (!NetworkVariableInitOnNetworkSpawn.OnValueChangedCalledOnClient)
            {
                if (Time.frameCount > maxFrames)
                {
                    // This is here in the event a platform is running at a higher
                    // frame rate than expected
                    if (doubleCheckTime < Time.realtimeSinceStartup)
                    {
                        Assert.Fail("Did not successfully spawn all expected NetworkObjects");
                        break;
                    }
                }
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }
            // Test of ordering is handled by an assert within OnNetworkSpawn
            Assert.IsTrue(NetworkVariableInitOnNetworkSpawn.OnValueChangedCalledOnClient);
        }
    }
}
