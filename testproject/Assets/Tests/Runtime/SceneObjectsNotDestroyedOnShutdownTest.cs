
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    public class SceneObjectsNotDestroyedOnShutdownTest
    {
        private const string k_TestScene = "InSceneNetworkObject";
        private const string k_SceneObjectName = "InSceneObject";
        private Scene m_TestScene;
        private NetworkManager m_ClientNetworkManager;
        private GameObject m_NetworkManagerGameObject;
        private WaitForSeconds m_DefaultWaitForTick = new WaitForSeconds(1.0f / 30);

        [SetUp]
        public void Setup()
        {
            m_NetworkManagerGameObject = new GameObject();
            m_ClientNetworkManager = m_NetworkManagerGameObject.AddComponent<NetworkManager>();
            m_ClientNetworkManager.NetworkConfig = new NetworkConfig();
            m_ClientNetworkManager.NetworkConfig.NetworkTransport = m_NetworkManagerGameObject.AddComponent<BlankTestingTransport>();
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.LoadSceneAsync(k_TestScene, LoadSceneMode.Additive);
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == k_TestScene)
            {
                m_TestScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator SceneObjectsNotDestroyedOnShutdown()
        {
            var timeoutHelper = new TimeoutHelper(2);

            // Wait for the scene with the in-scene placed NetworkObject to be loaded.
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => m_TestScene.IsValid() && m_TestScene.isLoaded, timeoutHelper);
            Assert.False(timeoutHelper.TimedOut, "Timed out waiting for scene to load!");

#if UNITY_2023_1_OR_NEWER
            var loadedInSceneObject = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name == k_SceneObjectName).FirstOrDefault();
#else
            var loadedInSceneObject = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.name == k_SceneObjectName).FirstOrDefault();
#endif

            Assert.IsNotNull(loadedInSceneObject, $"Failed to find {k_SceneObjectName} before starting client!");

            // Only start the client
            m_ClientNetworkManager.StartClient();

            // Wait for a tick
            yield return m_DefaultWaitForTick;

            // Shutdown the client
            m_ClientNetworkManager.Shutdown();

            // Wait for a tick
            yield return m_DefaultWaitForTick;

            // Find the same object
#if UNITY_2023_1_OR_NEWER
            loadedInSceneObject = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.name == k_SceneObjectName).FirstOrDefault();
#else
            loadedInSceneObject = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.name == k_SceneObjectName).FirstOrDefault();
#endif

            // Verify it still exists
            Assert.IsNotNull(loadedInSceneObject, $"Failed to find {k_SceneObjectName} after starting client!");
        }

        [TearDown]
        public void TearDown()
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

            if (m_TestScene.IsValid() && m_TestScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(m_TestScene);
            }

            if (m_NetworkManagerGameObject != null)
            {
                Object.DestroyImmediate(m_NetworkManagerGameObject);
            }
        }

        internal class BlankTestingTransport : TestingNetworkTransport
        {
            public override ulong ServerClientId { get; } = 0;
            public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
            {
            }

            public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
            {
                clientId = 0;
                payload = new ArraySegment<byte>();
                receiveTime = 0;
                return NetworkEvent.Nothing;
            }

            public override bool StartClient()
            {
                return true;
            }

            public override bool StartServer()
            {
                return true;
            }

            public override void DisconnectRemoteClient(ulong clientId)
            {
            }

            public override void DisconnectLocalClient()
            {
            }

            public override ulong GetCurrentRtt(ulong clientId)
            {
                return 0;
            }

            public override void Shutdown()
            {
            }

            public override void Initialize(NetworkManager networkManager = null)
            {
            }
        }
    }
}
