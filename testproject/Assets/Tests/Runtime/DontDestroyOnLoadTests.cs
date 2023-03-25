using System.Collections;
using NUnit.Framework;
using TestProject.ManualTests;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class DontDestroyOnLoadTests
    {
        private NetworkManager m_ServerNetworkManager;
        private NetworkManager[] m_ClientNetworkManagers;
        private GameObject m_PlayerPrefab;
        private GameObject m_DontDestroyOnLoadObject;


        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Create multiple NetworkManager instances
            if (!NetcodeIntegrationTestHelpers.Create(4, out NetworkManager server, out NetworkManager[] clients, 60))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_PlayerPrefab = new GameObject("Player");
            var playerNetworkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(playerNetworkObject);

            m_DontDestroyOnLoadObject = new GameObject("DontDestroyOnLoadObject");
            var dontDestroyOnLoadNetworkObject = m_DontDestroyOnLoadObject.AddComponent<NetworkObject>();
            m_DontDestroyOnLoadObject.AddComponent<ObjectToNotDestroyBehaviour>();
            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(dontDestroyOnLoadNetworkObject);

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            // Add our test NetworkObject to be moved into the DontDestroyOnLoad scene
            server.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.None, Prefab = m_DontDestroyOnLoadObject });

            // Apply the same settings for the clients
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
                clients[i].NetworkConfig.Prefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.None, Prefab = m_DontDestroyOnLoadObject });
            }

            m_ServerNetworkManager = server;
            m_ClientNetworkManagers = clients;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            NetcodeIntegrationTestHelpers.CleanUpHandlers();

            m_ServerNetworkManager.Shutdown();
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                networkManager.Shutdown();
            }
            int nextFrameNumber = Time.frameCount + 4;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            Object.Destroy(m_PlayerPrefab);
            Object.Destroy(m_DontDestroyOnLoadObject);
            Object.Destroy(m_ServerNetworkManager);
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                Object.Destroy(networkManager);
            }

            m_ServerNetworkManager = null;
            m_ClientNetworkManagers = null;

#if UNITY_2023_1_OR_NEWER
            var networkObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID);
#else
            var networkObjects = Object.FindObjectsOfType<NetworkObject>();
#endif

            foreach (var netObject in networkObjects)
            {
                Object.DestroyImmediate(netObject);
            }
            yield return null;
        }


        [UnityTest]
        public IEnumerator ValidateNetworkObjectSynchronization()
        {
            m_ServerNetworkManager.StartHost();
            NetcodeIntegrationTestHelpers.RegisterHandlers(m_ServerNetworkManager);
            var objectInstance = Object.Instantiate(m_DontDestroyOnLoadObject);
            var instanceNetworkObject = objectInstance.GetComponent<NetworkObject>();
            instanceNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            instanceNetworkObject.Spawn();
            var serverobjectToNotDestroyBehaviour = objectInstance.GetComponent<ObjectToNotDestroyBehaviour>();
            var waitForTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            yield return waitForTick;

            Assert.IsTrue(objectInstance.scene.name == "DontDestroyOnLoad");

            foreach (var networkManager in m_ClientNetworkManagers)
            {
                networkManager.StartClient();
                NetcodeIntegrationTestHelpers.RegisterHandlers(networkManager);
            }

            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(m_ClientNetworkManagers);

            yield return waitForTick;
            var timeOut = Time.realtimeSinceStartup + 2.0f;
            var timedOut = false;
            while (!timedOut)
            {
                var allClientConditionsHaveBeenReached = true;
                foreach (var networkManager in m_ClientNetworkManagers)
                {
                    foreach (var spawnedObject in networkManager.SpawnManager.SpawnedObjectsList)
                    {
                        if (spawnedObject.NetworkManager == networkManager && spawnedObject.gameObject.name.Contains("DontDestroyOnLoadObject"))
                        {
                            if (spawnedObject.gameObject.scene.name != "DontDestroyOnLoad")
                            {
                                allClientConditionsHaveBeenReached = false;
                                break;
                            }
                            var objectToNotDestroyBehaviour = spawnedObject.gameObject.GetComponent<ObjectToNotDestroyBehaviour>();
                            if (objectToNotDestroyBehaviour.CurrentPing == 0 || serverobjectToNotDestroyBehaviour.CurrentPing != objectToNotDestroyBehaviour.CurrentPing)
                            {
                                allClientConditionsHaveBeenReached = false;
                                break;
                            }
                        }
                    }
                }

                if (allClientConditionsHaveBeenReached)
                {
                    break;
                }

                yield return waitForTick;

                timedOut = timeOut < Time.realtimeSinceStartup;
            }
            Assert.False(timedOut, "Timed out while waiting for all client conditions to be reached!");
        }
    }
}
