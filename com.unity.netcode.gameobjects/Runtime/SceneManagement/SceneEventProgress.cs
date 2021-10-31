using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AsyncOperation = UnityEngine.AsyncOperation;

namespace Unity.Netcode
{
    /// <summary>
    /// Used by <see cref="NetworkSceneManager"/> to determine if a server invoked scene event has started.
    /// The returned status is stored in the <see cref="SceneEventProgress.Status"/> property.<br/>
    /// <em>Note: This was formally known as SwitchSceneProgress which contained the <see cref="AsyncOperation"/>.
    /// All <see cref="AsyncOperation"/>s are now delivered by the <see cref="NetworkSceneManager.OnSceneEvent"/> event handler
    /// via the <see cref="SceneEvent"/> parameter.</em>
    /// </summary>
    public enum SceneEventProgressStatus
    {
        /// <summary>
        /// No scene event progress status can be used to initialize a variable that will be checked over time.
        /// </summary>
        None,
        /// <summary>
        /// The scene event was successfully started.
        /// </summary>
        Started,
        /// <summary>
        /// Returned if you try to unload a scene that was not yet loaded.
        /// </summary>
        SceneNotLoaded,
        /// <summary>
        /// Returned if you try to start a new scene event before a previous one is finished.
        /// </summary>
        SceneEventInProgress,
        /// <summary>
        /// Returned if the scene name used with <see cref="NetworkSceneManager.LoadScene(string, LoadSceneMode)"/>
        /// or <see cref="NetworkSceneManager.UnloadScene(Scene)"/>is invalid.
        /// </summary>
        InvalidSceneName,
        /// <summary>
        /// Server side: Returned if the <see cref="NetworkSceneManager.VerifySceneBeforeLoading"/> delegate handler returns false
        /// (<em>i.e. scene is considered not valid/safe to load</em>).
        /// </summary>
        SceneFailedVerification,
        /// <summary>
        /// This is used for internal error notifications.<br/>
        /// If you receive this event then it is most likely due to a bug (<em>please open a GitHub issue with steps to replicate</em>).<br/>
        /// </summary>
        InternalNetcodeError,
    }

    /// <summary>
    /// Server side only:
    /// This tracks the progress of clients during a load or unload scene event
    /// </summary>
    internal class SceneEventProgress
    {
        /// <summary>
        /// List of clientIds of those clients that is done loading the scene.
        /// </summary>
        internal List<ulong> DoneClients { get; } = new List<ulong>();

        /// <summary>
        /// The NetworkTime at the moment the scene switch was initiated by the server.
        /// </summary>
        internal NetworkTime TimeAtInitiation { get; }

        /// <summary>
        /// Delegate type for when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        internal delegate bool OnCompletedDelegate(SceneEventProgress sceneEventProgress);

        /// <summary>
        /// The callback invoked when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        internal OnCompletedDelegate OnComplete;

        /// <summary>
        /// Is this scene switch progresses completed, all clients are done loading the scene or a timeout has occurred.
        /// </summary>
        internal bool IsCompleted { get; private set; }

        internal bool TimedOut { get; private set; }

        /// <summary>
        /// If all clients are done loading the scene, at the moment of completed.
        /// </summary>
        internal bool AreAllClientsDoneLoading { get; private set; }

        /// <summary>
        /// The hash value generated from the full scene path
        /// </summary>
        internal uint SceneHash { get; set; }

        internal Guid Guid { get; } = Guid.NewGuid();

        private Coroutine m_TimeOutCoroutine;
        private AsyncOperation m_SceneLoadOperation;

        private NetworkManager m_NetworkManager { get; }

        internal SceneEventProgressStatus Status { get; set; }

        internal SceneEventType SceneEventType { get; set; }

        internal LoadSceneMode LoadSceneMode;

        internal SceneEventProgress(NetworkManager networkManager, SceneEventProgressStatus status = SceneEventProgressStatus.Started)
        {
            if (status == SceneEventProgressStatus.Started)
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
            TimedOut = true;
            CheckCompletion();
        }

        internal void AddClientAsDone(ulong clientId)
        {
            DoneClients.Add(clientId);
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
            if ((!IsCompleted && DoneClients.Count == m_NetworkManager.ConnectedClientsList.Count && m_SceneLoadOperation.isDone) || (!IsCompleted && TimedOut))
            {
                IsCompleted = true;
                AreAllClientsDoneLoading = true;

                // If OnComplete is not registered or it is and returns true then remove this from the progress tracking
                if (OnComplete == null || (OnComplete != null && OnComplete.Invoke(this)))
                {
                    m_NetworkManager.SceneManager.SceneEventProgressTracking.Remove(Guid);
                }
                m_NetworkManager.StopCoroutine(m_TimeOutCoroutine);
            }
        }
    }
}
