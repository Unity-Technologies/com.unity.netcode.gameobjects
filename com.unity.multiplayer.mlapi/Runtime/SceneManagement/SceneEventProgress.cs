using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AsyncOperation = UnityEngine.AsyncOperation;

namespace Unity.Netcode
{
    /// <summary>
    /// Used by <see cref="NetworkSceneManager"/> to determine if a server invoked scene event has started and stored
    /// within the <see cref="SceneEventProgress"/>
    /// Note: This was formally known as SwitchSceneProgress which contained the <see cref="AsyncOperation"/>.
    /// The <see cref="AsyncOperation"/> is now delivered via the <see cref="NetworkSceneManager.OnSceneEvent"/> event through
    /// the <see cref="SceneEvent"/> parameter.
    /// Status Values:
    /// None - No status
    /// Started - Success in starting a load or unload scene event
    /// SceneNotLoaded - Returned if you attempt to unload a scene that is not loaded
    /// SceneEventInProgress - Returned if you attempt to start a new scene event (load or unload) during an existing scene event
    /// InvalidSceneName - Returned if the scene name you specified does not exist
    /// </summary>
    public enum SceneEventProgressStatus
    {
        None,
        Started,
        SceneNotLoaded,
        SceneEventInProgress,
        InvalidSceneName,
    }

    /// <summary>
    /// Class for tracking scene switching progress by server and clients.
    /// </summary>
    public class SceneEventProgress
    {
        /// <summary>
        /// List of clientIds of those clients that is done loading the scene.
        /// </summary>
        public List<ulong> DoneClients { get; } = new List<ulong>();

        /// <summary>
        /// The NetworkTime at the moment the scene switch was initiated by the server.
        /// </summary>
        public NetworkTime TimeAtInitiation { get; }

        /// <summary>
        /// Delegate type for when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        public delegate void OnCompletedDelegate(bool timedOut);

        /// <summary>
        /// The callback invoked when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        public event OnCompletedDelegate OnComplete;

        /// <summary>
        /// Is this scene switch progresses completed, all clients are done loading the scene or a timeout has occurred.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// If all clients are done loading the scene, at the moment of completed.
        /// </summary>
        public bool IsAllClientsDoneLoading { get; private set; }

        /// <summary>
        /// Delegate type for when a client is done loading the scene.
        /// </summary>
        public delegate void OnClientLoadedSceneDelegate(ulong clientId);

        /// <summary>
        /// The callback invoked when a client is done loading the scene.
        /// </summary>
        public event OnClientLoadedSceneDelegate OnClientLoadedScene;

        internal Guid Guid { get; } = Guid.NewGuid();

        private Coroutine m_TimeOutCoroutine;
        private AsyncOperation m_SceneLoadOperation;

        private NetworkManager m_NetworkManager { get; }

        internal SceneEventProgressStatus Status { get; set; }

        internal SceneEventProgress(NetworkManager networkManager, SceneEventProgressStatus status = SceneEventProgressStatus.Started)
        {
            if(status == SceneEventProgressStatus.Started)
            {
                m_NetworkManager = networkManager;
                m_TimeOutCoroutine = m_NetworkManager.StartCoroutine(TimeOutSceneEventProgress());
                TimeAtInitiation = networkManager.LocalTime;
            }
            Status = status;
        }

        internal IEnumerator TimeOutSceneEventProgress()
        {
            yield return new WaitForSecondsRealtime(m_NetworkManager.NetworkConfig.LoadSceneTimeOut);
            SetTimedOut();
        }

        internal void AddClientAsDone(ulong clientId)
        {
            DoneClients.Add(clientId);
            OnClientLoadedScene?.Invoke(clientId);
            CheckCompletion();
        }

        internal void RemoveClientAsDone(ulong clientId)
        {
            DoneClients.Remove(clientId);
            CheckCompletion();
        }

        internal void SetSceneLoadOperation(AsyncOperation sceneLoadOperation)
        {
            m_SceneLoadOperation = sceneLoadOperation;
            m_SceneLoadOperation.completed += operation => CheckCompletion();
        }

        internal void CheckCompletion()
        {
            if (!IsCompleted && DoneClients.Count == m_NetworkManager.ConnectedClientsList.Count && m_SceneLoadOperation.isDone)
            {
                IsCompleted = true;
                IsAllClientsDoneLoading = true;
                m_NetworkManager.SceneManager.SceneSwitchProgresses.Remove(Guid);
                OnComplete?.Invoke(false);

                m_NetworkManager.StopCoroutine(m_TimeOutCoroutine);
            }
        }

        internal void SetTimedOut()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                m_NetworkManager.SceneManager.SceneSwitchProgresses.Remove(Guid);
                OnComplete?.Invoke(true);
            }
        }
    }
}
