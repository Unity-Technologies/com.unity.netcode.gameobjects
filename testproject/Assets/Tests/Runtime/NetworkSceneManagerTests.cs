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
            InitializeSceneTestInfo();

            // Test loading additive scenes and the associated event messaging and notification pipelines
            ResetWait();
            var result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentScene, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.Started);

            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Test unloading additive scenes and the associated event messaging and notification pipelines
            ResetWait();
            result = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.True(result == SceneEventProgressStatus.Started);

            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

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
        /// For this test, we only need to check the server side for the proper event notifications of loading a scene
        /// as well as the final (more important) event notifications S2C_LoadComplete and S2C_UnloadComplete that signify
        /// the clients have processed through the loading and unloading of the scenes (really server ends up being the only)
        /// one that loads and unloads and the clients pass through or ignore the incoming command if the scene is no longer loaded.
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
