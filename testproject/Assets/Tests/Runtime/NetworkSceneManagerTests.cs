using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode.RuntimeTests;
using Unity.Netcode;

namespace TestProject.RuntimeTests
{
    public class NetworkSceneManagerTests: BaseMultiInstanceTest
    {
        protected override int NbClients => 9;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            m_ShouldWaitList = new List<SceneTestInfo>();
            NetworkSceneManager.IsUnitTesting = true;
            return base.Setup();
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            NetworkSceneManager.IsUnitTesting = false;
            return base.Teardown();
        }

        private class SceneTestInfo
        {
            public bool ShouldWait;
            public bool ProcessedEvent;
            public ulong ClientId;
        }

        private float m_TimeOutMarker;
        private bool m_TimedOut;
        private string m_CurrentScene;
        private List<SceneTestInfo> m_ShouldWaitList;


        [UnityTest]
        public IEnumerator SceneLoadingAndNotifications()
        {
            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            m_CurrentScene = "AdditiveScene1";

            // Check that we cannot call LoadScene when EnableSceneManagement is false (from previous legacy test)
            var threwException = false;
            try
            {
                m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;
                m_ServerNetworkManager.SceneManager.LoadScene("SomeSceneNane", LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling this method."))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);

            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = true;

            // Check that a client cannot call LoadScene
            threwException = false;
            try
            {
                m_ClientNetworkManagers[0].SceneManager.LoadScene("SomeSceneNane", LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"Only the server can start a scene event!"))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);


            // Now prepare for the loading and unloading additive scene testing
            InitializeSceneTestInfo();

            // Test loading additive scenes and the associated event messaging and notification pipelines
            ResetWait();
            var result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentScene, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Check error status for trying to load during an already in progress scene event
            result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentScene, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.SceneEventInProgress);

            // Wait for all clients to load the scene
            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Test unloading additive scenes and the associated event messaging and notification pipelines
            ResetWait();

            // Check that a client cannot call UnloadScene
            threwException = false;
            try
            {
                m_ClientNetworkManagers[0].SceneManager.UnloadScene(m_CurrentScene);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"Only the server can start a scene event!"))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);

            result = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.True(result == SceneEventProgressStatus.Started);

            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Check error status for trying to unloading something not loaded
            ResetWait();
            result = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.True(result == SceneEventProgressStatus.SceneNotLoaded);

            // Check error status for trying to load an invalid scene name
            result = m_ServerNetworkManager.SceneManager.LoadScene("SomeInvalidSceneName", LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.InvalidSceneName);

            yield break;
        }

        /// <summary>
        /// Initializes the m_ShouldWaitList
        /// </summary>
        private void InitializeSceneTestInfo()
        {
            m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = m_ServerNetworkManager.ServerClientId, ShouldWait = false });
            if (!m_ServerNetworkManager.NetworkConfig.RegisteredScenes.Contains(m_CurrentScene))
            {
                m_ServerNetworkManager.NetworkConfig.AllowRuntimeSceneChanges = true;
                m_ServerNetworkManager.SceneManager.AddRuntimeSceneName(m_CurrentScene, (uint)(m_ServerNetworkManager.NetworkConfig.RegisteredScenes.Count + 1));
            }

            foreach (var manager in m_ClientNetworkManagers)
            {
                m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = manager.LocalClientId, ShouldWait = false });
                if (!manager.NetworkConfig.RegisteredScenes.Contains(m_CurrentScene))
                {
                    manager.NetworkConfig.AllowRuntimeSceneChanges = true;
                    manager.SceneManager.AddRuntimeSceneName(m_CurrentScene, (uint)(manager.NetworkConfig.RegisteredScenes.Count + 1));
                }
            }
        }

        /// <summary>
        /// Resets each SceneTestInfo entry
        /// </summary>
        private void ResetWait()
        {
            foreach (var entry in m_ShouldWaitList)
            {
                entry.ShouldWait = true;
                entry.ProcessedEvent = false;
            }

            m_TimeOutMarker = Time.realtimeSinceStartup + 4.0f;
            m_TimedOut = false;
        }

        /// <summary>
        /// Wait until all clients have processed the event and the server has determined the event is completed
        /// Will bail if it takes too long via m_TimeOutMarker
        /// </summary>
        /// <returns></returns>
        private bool ShouldWait()
        {
            m_TimedOut = m_TimeOutMarker < Time.realtimeSinceStartup;
            return (m_ShouldWaitList.Select(c => c).Where(c => c.ProcessedEvent != true && c.ShouldWait == true).Count() > 0) && !m_TimedOut;
        }

        /// <summary>
        /// Determines if the clientId is valid
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private bool ContainsClient(ulong clientId)
        {
            return m_ShouldWaitList.Select(c => c.ClientId).Where(c => c == clientId).Count() > 0;
        }

        /// <summary>
        /// Sets the specific clientId entry as having processed the current event
        /// </summary>
        /// <param name="clientId"></param>
        private void SetClientProcessedEvent(ulong clientId)
        {
            m_ShouldWaitList.Select(c => c).Where(c => c.ClientId == clientId).First().ProcessedEvent = true;
        }

        /// <summary>
        /// Sets all known clients' ShouldWait value to false
        /// </summary>
        /// <param name="clientId"></param>
        private void SetClientWaitDone(List<ulong> clients)
        {
            foreach (var clientId in clients)
            {
                m_ShouldWaitList.Select(c => c).Where(c => c.ClientId == clientId).First().ShouldWait = false;
            }
        }

        /// <summary>
        /// Makes sure the ClientsThatCompleted in the scene event complete notification match the known client identifiers
        /// </summary>
        /// <param name="clients">list of client identifiers (ClientsThatCompleted) </param>
        /// <returns>true or false</returns>
        private bool ContainsAllClients(List<ulong> clients)
        {
            // First, make sure we have the expected client count
            if(clients.Count != m_ShouldWaitList.Count)
            {
                return false;
            }

            // Next, make sure we have all client identifiers
            foreach(var sceneTestInfo in m_ShouldWaitList)
            {
                if(!clients.Contains(sceneTestInfo.ClientId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// This test only needs to check the server side for the proper event notifications of loading a scene, each
        /// client response that it loaded the scene, and the final  event notifications S2C_LoadComplete and S2C_UnloadComplete
        /// that signify all clients have processed through the loading and unloading process.
        /// </summary>
        /// <param name="sceneEvent"></param>
        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            switch(sceneEvent.SceneEventType)
            {
                case SceneEventData.SceneEventTypes.S2C_Load:
                case SceneEventData.SceneEventTypes.S2C_Unload:
                    {
                        Assert.AreEqual(sceneEvent.SceneName,m_CurrentScene);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
                        Assert.IsNotNull(sceneEvent.AsyncOperation);
                        break;
                    }
                case SceneEventData.SceneEventTypes.C2S_LoadComplete:
                case SceneEventData.SceneEventTypes.C2S_UnloadComplete:
                    {
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentScene);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
                        SetClientProcessedEvent(sceneEvent.ClientId);
                        break;
                    }
                case SceneEventData.SceneEventTypes.S2C_LoadComplete:
                case SceneEventData.SceneEventTypes.S2C_UnLoadComplete:
                    {
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentScene);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
                        Assert.IsTrue(ContainsAllClients(sceneEvent.ClientsThatCompleted));
                        SetClientWaitDone(sceneEvent.ClientsThatCompleted);
                        break;
                    }
            }
        }
    }
}
