using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkSceneManagerUsageTests : NetcodeIntegrationTest
    {
        private const string k_AdditiveScene1 = "EmptyScene";

        private bool m_ClientLoadedScene;
        private string m_CurrentSceneName;
        private Scene m_CurrentScene;

        protected override int NumberOfClients => 1;
        public NetworkSceneManagerUsageTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        /// <summary>
        /// Checks that LoadScene cannot be called when EnableSceneManagement is false
        /// </summary>
        [Test]
        public void SceneManagementDisabledException([Values(LoadSceneMode.Single, LoadSceneMode.Additive)] LoadSceneMode loadSceneMode)
        {
            m_CurrentSceneName = k_AdditiveScene1;

            var threwException = false;
            try
            {
                m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;
                m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, loadSceneMode);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. " +
                    $"Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling " +
                    $"{nameof(NetworkSceneManager.LoadScene)} or {nameof(NetworkSceneManager.UnloadScene)}."))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);
        }

        /// <summary>
        /// Checks that a client cannot call LoadScene
        /// </summary>
        [UnityTest]
        public IEnumerator ClientCannotUseException([Values(LoadSceneMode.Single, LoadSceneMode.Additive)] LoadSceneMode loadSceneMode)
        {
            m_CurrentSceneName = k_AdditiveScene1;
            bool threwException = false;
            try
            {
                m_ClientNetworkManagers[0].SceneManager.LoadScene(m_CurrentSceneName, loadSceneMode);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Only server can start a scene event!"))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);

            // Check that a client cannot call UnloadScene
            threwException = false;

            m_ServerNetworkManager.SceneManager.OnSceneEvent += ServerSceneManager_OnSceneEvent;
            m_ClientNetworkManagers[0].SceneManager.OnLoadComplete += ClientSceneManager_OnLoadComplete;
            // Loading additive only because we don't want to unload the
            // Test Runner's scene using LoadSceneMode.Single
            var retStatus = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.AreEqual(retStatus, SceneEventProgressStatus.Started, $"{nameof(NetworkSceneManager.LoadScene)} returned " +
                $"unexpected {nameof(SceneEventProgressStatus)}: {retStatus}!");

            // Wait until we receive the LoadComplete event from the client
            yield return WaitForConditionOrTimeOut(() => m_ClientLoadedScene && m_CurrentScene.isLoaded);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {m_CurrentSceneName} {nameof(SceneEventType.LoadComplete)} event from client!");

            // Now try to unload the scene as a client
            try
            {
                m_ClientNetworkManagers[0].SceneManager.UnloadScene(m_CurrentScene);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Only server can start a scene event!"))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);

            // Loading additive only because we don't want to unload the
            // Test Runner's scene using LoadSceneMode.Single
            retStatus = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.AreEqual(retStatus, SceneEventProgressStatus.Started);
            yield return WaitForConditionOrTimeOut(() => !m_ClientLoadedScene);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {m_CurrentSceneName} {nameof(SceneEventType.UnloadComplete)} event from client!");
        }

        private void ServerSceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId != m_ServerNetworkManager.LocalClientId)
            {
                return;
            }
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        m_CurrentScene = sceneEvent.Scene;
                        break;
                    }
                case SceneEventType.UnloadEventCompleted:
                    {
                        // Only set to false if the scene actually unloaded
                        m_ClientLoadedScene = m_CurrentScene.isLoaded;
                        break;
                    }
            }
        }

        private void ClientSceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (clientId == m_ClientNetworkManagers[0].LocalClientId && sceneName == m_CurrentSceneName)
            {
                m_ClientLoadedScene = true;
            }
        }
    }
}
