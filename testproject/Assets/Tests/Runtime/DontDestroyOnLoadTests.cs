using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using TestProject.ManualTests;

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
            if (!MultiInstanceHelpers.Create(4, out NetworkManager server, out NetworkManager[] clients, 60))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_PlayerPrefab = new GameObject("Player");
            var playerNetworkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(playerNetworkObject);

            m_DontDestroyOnLoadObject = new GameObject("DontDestroyOnLoadObject");
            var dontDestroyOnLoadNetworkObject = m_DontDestroyOnLoadObject.AddComponent<NetworkObject>();
            m_DontDestroyOnLoadObject.AddComponent<ObjectToNotDestroyBehaviour>();
            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(dontDestroyOnLoadNetworkObject);

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            // Add our test NetworkObject to be moved into the DontDestroyOnLoad scene
            server.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.None, Prefab = m_DontDestroyOnLoadObject });

            // Apply the same settings for the clients
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
                clients[i].NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.None, Prefab = m_DontDestroyOnLoadObject });
            }

            m_ServerNetworkManager = server;
            m_ClientNetworkManagers = clients;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
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

            var networkObjects = Object.FindObjectsOfType<NetworkObject>();
            foreach (var netObject in networkObjects)
            {
                Object.DestroyImmediate(netObject);
            }
            yield return null;
        }


        [UnityTest]
        public IEnumerator ValidateNetworkObjectSynchronization([Values(true, false)] bool enableNetworkManagerDontDestroy)
        {
            m_ServerNetworkManager.DontDestroy = enableNetworkManagerDontDestroy;
            m_ServerNetworkManager.StartHost();
            var objectInstance = Object.Instantiate(m_DontDestroyOnLoadObject);
            var instanceNetworkObject = objectInstance.GetComponent<NetworkObject>();
            instanceNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            instanceNetworkObject.Spawn();
            var serverobjectToNotDestroyBehaviour = objectInstance.GetComponent<ObjectToNotDestroyBehaviour>();

            int nextFrameNumber = Time.frameCount + 32;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            Assert.IsTrue(objectInstance.scene.name == "DontDestroyOnLoad");

            foreach (var networkManager in m_ClientNetworkManagers)
            {
                networkManager.DontDestroy = enableNetworkManagerDontDestroy;
                networkManager.StartClient();
            }

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(m_ClientNetworkManagers));

            nextFrameNumber = Time.frameCount + 32;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            foreach (var networkManager in m_ClientNetworkManagers)
            {
                foreach (var spawnedObject in networkManager.SpawnManager.SpawnedObjectsList)
                {
                    if (spawnedObject.NetworkManager == networkManager && spawnedObject.gameObject.name.Contains("DontDestroyOnLoadObject"))
                    {
                        Assert.IsTrue(spawnedObject.gameObject.scene.name == "DontDestroyOnLoad");
                        var objectToNotDestroyBehaviour = spawnedObject.gameObject.GetComponent<ObjectToNotDestroyBehaviour>();
                        Assert.Greater(objectToNotDestroyBehaviour.CurrentPing, 0);
                        Assert.AreEqual(serverobjectToNotDestroyBehaviour.CurrentPing, objectToNotDestroyBehaviour.CurrentPing);
                    }
                }
            }
        }
    }
}
