using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public float TimeAtInitiation { get; private set; }
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
        /// Is this scene switch progresses completed, all clients are done loading the scene or a timeout has occured.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsCompleted instead", false)]
        public bool isCompleted => IsCompleted;
        /// <summary>
        /// If all clients are done loading the scene, at the moment of completed.
        /// </summary>
        public bool IsAllClientsDoneLoading { get; private set; }
        /// <summary>
        /// If all clients are done loading the scene, at the moment of completed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsCompleted instead", false)]
        public bool isAllClientsDoneLoading => IsAllClientsDoneLoading;
        /// <summary>
        /// Delegate type for when a client is done loading the scene.
        /// </summary>
        public delegate void OnClientLoadedSceneDelegate(ulong clientId);
        /// <summary>
        /// The callback invoked when a client is done loading the scene.
        /// </summary>
        public event OnClientLoadedSceneDelegate OnClientLoadedScene;

        private NetworkingManager networkingManager;
        
        internal Guid guid { get; } = Guid.NewGuid();

        private Coroutine timeOutCoroutine;
        private AsyncOperation sceneLoadOperation;

        internal SceneSwitchProgress(NetworkingManager manager)
        {
            networkingManager = manager;
            TimeAtInitiation = networkingManager.NetworkTime;
            timeOutCoroutine = networkingManager.StartCoroutine(networkingManager.TimeOutSwitchSceneProgress(this));
        }

        internal void AddClientAsDone(ulong clientId)
        {
            DoneClients.Add(clientId);
            if (OnClientLoadedScene != null)
                OnClientLoadedScene.Invoke(clientId);
            CheckCompletion();
        }

        internal void RemoveClientAsDone(ulong clientId)
        {
            DoneClients.Remove(clientId);
            CheckCompletion();
        }

        internal void SetSceneLoadOperation(AsyncOperation sceneLoadOperation)
        {
            this.sceneLoadOperation = sceneLoadOperation;
            this.sceneLoadOperation.completed += (AsyncOperation operation) => { CheckCompletion(); };
        }

        internal void CheckCompletion()
        {
            if (!IsCompleted && DoneClients.Count == networkingManager.ConnectedClientsList.Count && sceneLoadOperation.isDone)
            {
                IsCompleted = true;
                IsAllClientsDoneLoading = true;
                networkingManager.NetworkSceneManager.sceneSwitchProgresses.Remove(guid);
                if (OnComplete != null)
                    OnComplete.Invoke(false);

                networkingManager.StopCoroutine(timeOutCoroutine);
            }
        }

        internal void SetTimedOut()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                networkingManager.NetworkSceneManager.sceneSwitchProgresses.Remove(guid);
                if (OnComplete != null)
                    OnComplete.Invoke(true);
            }
        }

    }
}
