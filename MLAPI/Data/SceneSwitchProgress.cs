using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Components
{
    /// <summary>
    /// Class for tracking scene switching progress by server and clients.
    /// </summary>
    public class SceneSwitchProgress
    {
        /// <summary>
        /// List of clientIds of those clients that is done loading the scene.
        /// </summary>
        public List<uint> DoneClients { get; } = new List<uint>();
        /// <summary>
        /// The NetworkTime time at the moment the scene switch was initiated by the server.
        /// </summary>
        public float TimeAtInitiation { get; } = NetworkingManager.Singleton.NetworkTime;
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
        public bool isCompleted { get; private set; }
        /// <summary>
        /// If all clients are done loading the scene, at the moment of completed.
        /// </summary>
        public bool isAllClientsDoneLoading { get; private set; }
        /// <summary>
        /// Delegate type for when a client is done loading the scene.
        /// </summary>
        public delegate void OnClientLoadedSceneDelegate(uint clientId);
        /// <summary>
        /// The callback invoked when a client is done loading the scene.
        /// </summary>
        public event OnClientLoadedSceneDelegate OnClientLoadedScene;

        internal Guid guid { get; } = Guid.NewGuid();

        private Coroutine timeOutCoroutine;
        private AsyncOperation sceneLoadOperation;

        internal SceneSwitchProgress()
        {
            timeOutCoroutine = NetworkingManager.Singleton.StartCoroutine(NetworkingManager.Singleton.TimeOutSwitchSceneProgress(this));
        }

        internal void AddClientAsDone(uint clientId)
        {
            DoneClients.Add(clientId);
            if (OnClientLoadedScene != null)
                OnClientLoadedScene.Invoke(clientId);
            CheckCompletion();
        }

        internal void RemoveClientAsDone(uint clientId)
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
            if (!isCompleted && DoneClients.Count == NetworkingManager.Singleton.ConnectedClientsList.Count && sceneLoadOperation.isDone)
            {
                isCompleted = true;
                isAllClientsDoneLoading = true;
                NetworkSceneManager.sceneSwitchProgresses.Remove(guid);
                if (OnComplete != null)
                    OnComplete.Invoke(false);

                NetworkingManager.Singleton.StopCoroutine(timeOutCoroutine);
            }
        }

        internal void SetTimedOut()
        {
            if (!isCompleted)
            {
                isCompleted = true;
                NetworkSceneManager.sceneSwitchProgresses.Remove(guid);
                if (OnComplete != null)
                    OnComplete.Invoke(true);
            }
        }

    }
}
