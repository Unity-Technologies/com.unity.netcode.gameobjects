using System.Collections.Generic;
using MLAPI.Configuration;
using MLAPI.Messaging;
using MLAPI.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAPI.EditorTests
{
    // todo test dontdestroyonload static scene object (object already in scene that sets itself as dontdestroyonload
    // todo test shuting down and restarting network manager while in same scene (check static scene objects if they are dontdestroyonload
    public class NetworkManagerSceneManagerTests
    {
        [Test]
        public void SceneManagerAssigned()
        {
            var gameObject = new GameObject(nameof(SceneManagerAssigned));
            var networkManager = gameObject.AddComponent<NetworkManager>();
            var transport = gameObject.AddComponent<DummyTransport>();

            // MLAPI sets this in validate
            networkManager.NetworkConfig = new NetworkConfig()
            {
                // Set the current scene to prevent unexpected log messages which would trigger a failure
                RegisteredScenes = new List<string>() { SceneManager.GetActiveScene().name }
            };

            // Set dummy transport that does nothing
            networkManager.NetworkConfig.NetworkTransport = transport;

            NetworkSceneManager preManager = networkManager.SceneManager;

            // Start server to cause init
            networkManager.StartServer();

            Debug.Assert(preManager == null);
            Debug.Assert(networkManager.SceneManager != null);

            Object.DestroyImmediate(gameObject);
        }
    }
}
