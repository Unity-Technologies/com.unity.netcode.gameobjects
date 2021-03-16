using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;

namespace MLAPI.RuntimeTests
{
    /// <summary>
    /// Helper class to instantiate a NetworkManager
    /// This also provides the ability to:
    /// --- instantiate GameObjects with NetworkObject components that returns a Guid for accessing it later.
    /// --- add NetworkBehaviour components to the instantiated GameObjects
    /// --- spawn a NetworkObject using its parent GameObject's Guid
    /// Call StartNetworkManager in the constructor of your runtime unit test class.
    /// Call ShutdownNetworkManager in the destructor  of your runtime unit test class.
    ///
    /// Includes a useful "BuffersMatch" method that allows you to compare two buffers (returns true if they match false if not)
    /// </summary>
    public static class NetworkManagerHelper
    {
        public static Transports.Tasks.SocketTasks s_StartHostSocketTasks { get; internal set; }
        public static GameObject s_NetworkManagerObject { get; internal set; }

        internal static Dictionary<Guid,GameObject> s_InstantiatedGameObjects = new Dictionary<Guid, GameObject>();

        internal static Dictionary<Guid,NetworkObject> s_InstantiatedNetworkObjects = new Dictionary<Guid, NetworkObject>();

        internal static NetworkManagerOperatingMode s_CurrentNetworkManagerMode;

        /// <summary>
        /// This provides the ability to start NetworkManager in various modes
        /// </summary>
        public enum NetworkManagerOperatingMode
        {
            None,
            Host,
            Server,
            Client,
        }

        /// <summary>
        /// Called upon the RpcQueueTests being instantiated.
        /// This creates an instance of the NetworkManager to be used during unit tests.
        /// Currently, the best method to run unit tests is by starting in host mode as you can
        /// send messages to yourself (i.e. Host-Client to Host-Server and vice versa).
        /// As such, the default setting is to start in Host mode.
        /// </summary>
        /// <param name="managerMode">parameter to specify which mode you want to start the NetworkManager</param>
        /// <returns>true if it was instantiated or is already instantiate otherwise false means it failed to instantiate</returns>
        public static bool StartNetworkManager(NetworkManagerOperatingMode managerMode = NetworkManagerOperatingMode.Host)
        {
            if (s_CurrentNetworkManagerMode != managerMode)
            {
                StopNetworkManagerMode();
            }

            if (NetworkManager.Singleton == null)
            {
                s_NetworkManagerObject = new GameObject(nameof(NetworkManager));
                var networkManagerComponen = s_NetworkManagerObject.AddComponent<NetworkManager>();
                if (networkManagerComponen == null)
                {
                    return false;
                }

                Debug.Log($"{nameof(NetworkManager)} Instantiated.");

                var unetTransport = s_NetworkManagerObject.AddComponent<UNetTransport>();

                networkManagerComponen.NetworkConfig = new Configuration.NetworkConfig
                {
                    CreatePlayerPrefab = false,
                    AllowRuntimeSceneChanges = true,
                    EnableSceneManagement = false
                };
                unetTransport.ConnectAddress = "127.0.0.1";
                unetTransport.ConnectPort = 7777;
                unetTransport.ServerListenPort = 7777;
                unetTransport.MessageBufferSize = 65535;
                unetTransport.MaxConnections = 100;
                unetTransport.MessageSendMode = UNetTransport.SendMode.Immediately;
                networkManagerComponen.NetworkConfig.NetworkTransport = unetTransport;

                var currentActiveScene = SceneManager.GetActiveScene();

                //Add our test scene name
                NetworkSceneManager.AddRuntimeSceneName(currentActiveScene.name, 0);

                //Starts the network manager in the mode specified
                StartNetworkManagerMode(managerMode);
            }
            return true;
        }

        /// <summary>
        /// Add a GameObject with a NetworkObject component
        /// </summary>
        /// <param name="nameOfGameObject">the name of the object</param>
        /// <returns></returns>
        public static Guid AddGameNetworkObject(string nameOfGameObject)
        {
            Guid gameObjectId = Guid.NewGuid();

            //Create the player object that we will spawn as a host
            var gameObject = new GameObject(nameOfGameObject);

            Assert.IsNotNull(gameObject);

            var networkObject = gameObject.AddComponent<NetworkObject>();

            Assert.IsNotNull(networkObject);

            Assert.IsFalse(s_InstantiatedGameObjects.ContainsKey(gameObjectId));
            Assert.IsFalse(s_InstantiatedNetworkObjects.ContainsKey(gameObjectId));

            s_InstantiatedGameObjects.Add(gameObjectId, gameObject);
            s_InstantiatedNetworkObjects.Add(gameObjectId, networkObject);

            return gameObjectId;
        }

        /// <summary>
        /// Helper class to add a component to the GameObject with a NetoworkObject component
        /// </summary>
        /// <typeparam name="T">NetworkBehaviour component being added to the GameObject</typeparam>
        /// <param name="gameObjectIdentifier">ID returned to reference the game object</param>
        /// <returns></returns>
        public static T AddComponentToObject<T>(Guid gameObjectIdentifier) where T : NetworkBehaviour
        {
            Assert.IsTrue(s_InstantiatedGameObjects.ContainsKey(gameObjectIdentifier));
            return s_InstantiatedGameObjects[gameObjectIdentifier].AddComponent<T>();
        }

        /// <summary>
        /// Spawn the NetworkObject, so Rpcs can flow
        /// </summary>
        /// <param name="gameObjectIdentifier">ID returned to reference the game object</param>
        public static void SpawnNetworkObject(Guid gameObjectIdentifier)
        {
            Assert.IsTrue(s_InstantiatedNetworkObjects.ContainsKey(gameObjectIdentifier));
            if (!s_InstantiatedNetworkObjects[gameObjectIdentifier].IsSpawned)
            {
                s_InstantiatedNetworkObjects[gameObjectIdentifier].Spawn();
            }
        }

        /// <summary>
        /// Starts the NetworkManager in the current mode specified by managerMode
        /// </summary>
        /// <param name="managerMode">the mode to start the NetworkManager as</param>
        private static void StartNetworkManagerMode(NetworkManagerOperatingMode managerMode)
        {
            s_CurrentNetworkManagerMode = managerMode;
            switch(s_CurrentNetworkManagerMode)
            {
                case NetworkManagerOperatingMode.Host:
                    {
                        //Starts the host
                        NetworkManager.Singleton.StartHost();
                        Debug.Log("Host Started.");
                        break;
                    }
                case NetworkManagerOperatingMode.Server:
                    {
                        //Starts the server
                        NetworkManager.Singleton.StartServer();
                        Debug.Log("Server Started.");
                        break;
                    }
                case NetworkManagerOperatingMode.Client:
                    {
                        //Starts the client
                        NetworkManager.Singleton.StartClient();
                        Debug.Log("Client Started.");
                        break;
                    }
            }
        }

        /// <summary>
        /// Stops the current mode of the NetworkManager
        /// </summary>
        private static void StopNetworkManagerMode()
        {
            switch(s_CurrentNetworkManagerMode)
            {
                case NetworkManagerOperatingMode.Host:
                    {
                        //Stop the host
                        NetworkManager.Singleton.StopHost();
                        Debug.Log("Host Stopped.");
                        break;
                    }
                case NetworkManagerOperatingMode.Server:
                    {
                        //Stop the server
                        NetworkManager.Singleton.StopServer();
                        Debug.Log("Server Stopped.");
                        break;
                    }
                case NetworkManagerOperatingMode.Client:
                    {
                        //Stop the client
                        NetworkManager.Singleton.StopClient();
                        Debug.Log("Client Stopped.");
                        break;
                    }
            }
            s_CurrentNetworkManagerMode = NetworkManagerOperatingMode.None;
        }

        //This is called, even if we assert and exit early from a test
        public static void ShutdownNetworkManager()
        {
            //clean up any game objects created with custom unit testing components
            foreach (var entry in s_InstantiatedGameObjects)
            {
                GameObject.Destroy(entry.Value);
            }

            s_InstantiatedGameObjects.Clear();

            if (s_NetworkManagerObject != null)
            {
                StopNetworkManagerMode();

                //Shutdown the NetworkManager
                NetworkManager.Singleton.Shutdown();

                Debug.Log($"{nameof(NetworkManager)} shutdown.");

                GameObject.Destroy(s_NetworkManagerObject);

                Debug.Log($"{nameof(NetworkManager)} destroyed.");
            }
        }

        public static bool BuffersMatch(int indexOffset, long targetSize, byte[] sourceArray, byte[] originalArray)
        {
            long largeInt64Blocks = targetSize >> 3; //Divide by 8
            int originalArrayOffset = 0;
            //process by 8 byte blocks if we can
            for (long i = 0; i < largeInt64Blocks; i++)
            {
                if (BitConverter.ToInt64(sourceArray, indexOffset) != BitConverter.ToInt64(originalArray, originalArrayOffset))
                {
                    return false;
                }
                indexOffset += 8;
                originalArrayOffset += 8;
            }

            long offset = largeInt64Blocks * 8;
            long remainder = targetSize - offset;

            //4 byte block
            if (remainder >= 4)
            {
                if (BitConverter.ToInt32(sourceArray, indexOffset) != BitConverter.ToInt32(originalArray, originalArrayOffset))
                {
                    return false;
                }
                indexOffset += 4;
                originalArrayOffset += 4;
                offset += 4;
            }

            //Remainder of bytes < 4
            if (targetSize - offset > 0)
            {
                for (long i = 0; i < (targetSize - offset); i++)
                {
                    if (sourceArray[indexOffset + i] != originalArray[originalArrayOffset + i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
