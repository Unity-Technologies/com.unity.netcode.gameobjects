using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;


namespace TestProject.RuntimeTests
{
    [TestFixture(NetworkManagerTestType.HostHasLocalClient)]
    [TestFixture(NetworkManagerTestType.ObjectsNotDestroyedOnShutdown)]
    public class NetworkManagerTests : NetcodeIntegrationTest
    {
        public enum NetworkManagerTestType
        {
            HostHasLocalClient,
            ObjectsNotDestroyedOnShutdown,
        }

        protected override int NumberOfClients => 1;

        private const string k_TestScene = "InSceneNetworkObject";
        private const string k_SceneObjectName = "InSceneObject";
        private Scene m_TestScene;

        private NetworkManagerTestType m_NetworkManagerTestType;

        public NetworkManagerTests(NetworkManagerTestType networkManagerTestType)
        {
            m_NetworkManagerTestType = networkManagerTestType;
        }

        protected override IEnumerator OnSetup()
        {
            switch (m_NetworkManagerTestType)
            {
                case NetworkManagerTestType.ObjectsNotDestroyedOnShutdown:
                    {
                        // We need to switch to UTP because SIP will error out if a server is not present
                        m_NetworkTransport = NetcodeIntegrationTestHelpers.InstanceTransport.UTP;
                        break;
                    }
            }

            return base.OnSetup();
        }

        protected override bool CanStartServerAndClients()
        {
            var canStart = base.CanStartServerAndClients();
            switch (m_NetworkManagerTestType)
            {
                case NetworkManagerTestType.ObjectsNotDestroyedOnShutdown:
                    {
                        canStart = false;
                        break;
                    }
            }
            return canStart;
        }

        protected override void OnServerAndClientsCreated()
        {
            switch (m_NetworkManagerTestType)
            {
                case NetworkManagerTestType.ObjectsNotDestroyedOnShutdown:
                    {
                        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                        SceneManager.LoadSceneAsync(k_TestScene, LoadSceneMode.Additive);
                        break;
                    }
            }
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == k_TestScene)
            {
                m_TestScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator UnityTestEntryPoint()
        {
            switch (m_NetworkManagerTestType)
            {
                case NetworkManagerTestType.HostHasLocalClient:
                    {
                        VerifyHostHasLocalClient();
                        break;
                    }
                case NetworkManagerTestType.ObjectsNotDestroyedOnShutdown:
                    {
                        yield return SceneObjectsNotDestroyedDuringShutdown();
                        break;
                    }
            }
        }

        /// <summary>
        /// Verifies a host will have a local client assigned
        /// </summary>
        protected void VerifyHostHasLocalClient()
        {
            Assert.IsTrue(m_ServerNetworkManager.LocalClient != null);
        }

        /// <summary>
        /// Verifies that when there are in-scene placed NetworkObjects and a client
        /// fails to connect that the in-scene placed NetworkObjects do not get destroyed.
        /// </summary>
        protected IEnumerator SceneObjectsNotDestroyedDuringShutdown()
        {
            // Wait for the scene with the in-scene placed NetworkObject to be loaded.
            yield return WaitForConditionOrTimeOut(() => m_TestScene.IsValid() && m_TestScene.isLoaded);

            var loadedInSceneObject = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.name == k_SceneObjectName).FirstOrDefault();

            Assert.IsNotNull(loadedInSceneObject, $"Failed to find {k_SceneObjectName} before starting client!");

            // Only start the client
            m_ClientNetworkManagers[0].StartClient();

            // Wait for a tick
            yield return s_DefaultWaitForTick;

            // Shutdown the client
            m_ClientNetworkManagers[0].Shutdown();

            // Wait for a tick
            yield return s_DefaultWaitForTick;

            // Find the same object
            loadedInSceneObject = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.name == k_SceneObjectName).FirstOrDefault();

            // Verify it still exists
            Assert.IsNotNull(loadedInSceneObject, $"Failed to find {k_SceneObjectName} after starting client!");
        }
    }
}
