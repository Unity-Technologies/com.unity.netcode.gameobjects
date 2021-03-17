using System;
using System.Collections.Generic;
using UnityEngine;
using AsyncOperation = UnityEngine.AsyncOperation;

namespace MLAPI.SceneManagement
{
    /// <summary>
    /// Class for tracking scene switching progress by server and clients.
    /// </summary>
    public class SceneSwitchProgress
    {
        /// <summary>
        /// List of clientIds of those clients that is done loading the scene.
        /// </summary>
        public List<ulong> DoneClients { get; } = new List<ulong>();

        /// <summary>
        /// The NetworkTime time at the moment the scene switch was initiated by the server.
        /// </summary>
        public float TimeAtInitiation { get; } = NetworkManager.Singleton.NetworkTime;

        /// <summary>
        /// Delegate type for when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        public delegate void OnCompletedDelegate(bool timedOut);

        /// <summary>
        /// The callback invoked when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        public event OnCompletedDelegate OnComplete;

        /// <summary>
        /// Is this scene switch progresses completed, all clients are done loading the scene or a timeout has occured.
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

        internal SceneSwitchProgress()
        {
            m_TimeOutCoroutine = NetworkManager.Singleton.StartCoroutine(NetworkManager.Singleton.TimeOutSwitchSceneProgress(this));
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
            if (!IsCompleted && DoneClients.Count == NetworkManager.Singleton.ConnectedClientsList.Count && m_SceneLoadOperation.isDone)
            {
                IsCompleted = true;
                IsAllClientsDoneLoading = true;
                NetworkSceneManager.SceneSwitchProgresses.Remove(Guid);
                OnComplete?.Invoke(false);

                NetworkManager.Singleton.StopCoroutine(m_TimeOutCoroutine);
            }
        }

        internal void SetTimedOut()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                NetworkSceneManager.SceneSwitchProgresses.Remove(Guid);
                OnComplete?.Invoke(true);
            }
        }
    }
}