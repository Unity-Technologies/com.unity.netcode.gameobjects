using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Logs to console all NetworkSceneManager Scene Events
    /// This is only meant to be used as a tool to detect all notifications that occur during
    /// scene events.  This was created to detect the edge case scene event scenarios during
    /// single mode scene loading events and can be used as a manual test to verify additive
    /// scenes are being intentionally unloaded during a single mode scene loading event.
    /// Note: this should be added to the parent GameObject of your NetworkManager component
    /// and will only work if NetworkManager's "Don't Destroy" is true.
    /// </summary>
    public class SceneEventNotificationQueue : MonoBehaviour
    {
        public bool LogToConsole;

        [Range(1, 30)]
        public float TimeToLive = 10.0f;

        private bool m_IsInitialized;

        private class SceneEventNotification
        {
            public float TimeToLive;
            public string SceneEvent;
        }

        private Queue<SceneEventNotification> m_SceneEvents = new Queue<SceneEventNotification>();

        private NetworkManager m_NetworkManager;

        private void Start()
        {
            m_NetworkManager = gameObject.GetComponent<NetworkManager>();
        }

        private void DeregisterFromOnSceneEvent()
        {
            if (m_IsInitialized && m_NetworkManager != null && m_NetworkManager.SceneManager != null)
            {
                m_IsInitialized = false;
                m_NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }

        private void OnApplicationQuit()
        {
            DeregisterFromOnSceneEvent();
        }

        private void OnDisable()
        {
            DeregisterFromOnSceneEvent();
        }

        /// <summary>
        /// Invoked on all local scene event notifications
        /// </summary>
        /// <param name="sceneEvent"></param>
        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            var sceneEventMsg = $"({NetworkManager.Singleton.LocalClientId})-[{sceneEvent.ClientId} | {sceneEvent.SceneEventType} | {sceneEvent.SceneName}";
            if (sceneEvent.SceneEventType == SceneEventType.Load || sceneEvent.SceneEventType == SceneEventType.LoadComplete)
            {
                sceneEventMsg += $" | {sceneEvent.LoadSceneMode}";
                if (sceneEvent.Scene.IsValid() && sceneEvent.SceneEventType == SceneEventType.LoadComplete)
                {
                    sceneEventMsg += $" | SHID: {sceneEvent.Scene.handle}";
                }
            }

            if (sceneEvent.SceneEventType == SceneEventType.UnloadEventCompleted || sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                sceneEventMsg += $" | Loaded ({sceneEvent.ClientsThatCompleted.Count}) : (";
                foreach (var clientId in sceneEvent.ClientsThatCompleted)
                {
                    sceneEventMsg += $"{clientId}, ";
                }
                sceneEventMsg += $") | TimedOut ({sceneEvent.ClientsThatTimedOut.Count}) : (";
                foreach (var clientId in sceneEvent.ClientsThatTimedOut)
                {
                    sceneEventMsg += $"{clientId}, ";
                }
                sceneEventMsg += ")";
            }
            sceneEventMsg += "]";

            m_SceneEvents.Enqueue(new SceneEventNotification() { SceneEvent = sceneEventMsg, TimeToLive = Time.realtimeSinceStartup + TimeToLive });
            if (LogToConsole)
            {
                Debug.Log(sceneEventMsg);
            }
        }

        /// <summary>
        /// Returns the current scene event notifications
        /// </summary>
        /// <returns></returns>
        public List<string> GetCurrentNotifications()
        {
            return m_SceneEvents.Select(c => c.SceneEvent).ToList();
        }

        private void Update()
        {
            if (m_NetworkManager != null && m_NetworkManager.IsListening)
            {
                if (!m_IsInitialized)
                {
                    m_NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                    m_IsInitialized = true;
                }

                if (m_SceneEvents.Count() > 0 && m_SceneEvents.Peek().TimeToLive < Time.realtimeSinceStartup)
                {
                    m_SceneEvents.Dequeue();
                }
            }
        }
    }
}
