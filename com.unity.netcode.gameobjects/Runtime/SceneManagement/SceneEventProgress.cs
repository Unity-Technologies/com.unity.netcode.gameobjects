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
        internal Dictionary<ulong, bool> ClientsProcessingSceneEvent { get; } = new Dictionary<ulong, bool>();
        internal List<ulong> ClientsThatDisconnected = new List<ulong>();

        /// <summary>
        /// This is when the current scene event will have timed out
        /// </summary>
        internal float WhenSceneEventHasTimedOut;

        /// <summary>
        /// Delegate type for when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        internal delegate bool OnCompletedDelegate(SceneEventProgress sceneEventProgress);

        /// <summary>
        /// The callback invoked when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        internal OnCompletedDelegate OnComplete;

        internal Action<uint> OnSceneEventCompleted;

        /// <summary>
        /// This will make sure that we only have timed out if we never completed
        /// </summary>
        internal bool HasTimedOut()
        {
            return WhenSceneEventHasTimedOut <= Time.realtimeSinceStartup;
        }

        /// <summary>
        /// The hash value generated from the full scene path
        /// </summary>
        internal uint SceneHash { get; set; }

        internal Guid Guid { get; } = Guid.NewGuid();
        internal uint SceneEventId;

        private Coroutine m_TimeOutCoroutine;
        private AsyncOperation m_AsyncOperation;

        private NetworkManager m_NetworkManager { get; }

        internal SceneEventProgressStatus Status { get; set; }

        internal SceneEventType SceneEventType { get; set; }

        internal LoadSceneMode LoadSceneMode;

        internal List<ulong> GetClientsWithStatus(bool completedSceneEvent)
        {
            var clients = new List<ulong>();
            foreach (var clientStatus in ClientsProcessingSceneEvent)
            {
                if (clientStatus.Value == completedSceneEvent)
                {
                    clients.Add(clientStatus.Key);
                }
            }

            // If we are getting the list of clients that have not completed the
            // scene event, then add any clients that disconnected during this
            // scene event.
            if (!completedSceneEvent)
            {
                clients.AddRange(ClientsThatDisconnected);
            }
            return clients;
        }

        internal SceneEventProgress(NetworkManager networkManager, SceneEventProgressStatus status = SceneEventProgressStatus.Started)
        {
            if (status == SceneEventProgressStatus.Started)
            {
                m_NetworkManager = networkManager;

                if (networkManager.IsServer)
                {
                    m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
                    // Track the clients that were connected when we started this event
                    foreach (var connectedClientId in networkManager.ConnectedClientsIds)
                    {
                        ClientsProcessingSceneEvent.Add(connectedClientId, false);
                    }

                    WhenSceneEventHasTimedOut = Time.realtimeSinceStartup + networkManager.NetworkConfig.LoadSceneTimeOut;
                    m_TimeOutCoroutine = m_NetworkManager.StartCoroutine(TimeOutSceneEventProgress());
                }
            }
            Status = status;
        }

        /// <summary>
        /// Remove the client from the clients processing the current scene event
        /// Add this client to the clients that disconnected list
        /// </summary>
        private void OnClientDisconnectCallback(ulong clientId)
        {
            if (ClientsProcessingSceneEvent.ContainsKey(clientId))
            {
                ClientsThatDisconnected.Add(clientId);
                ClientsProcessingSceneEvent.Remove(clientId);
            }
        }

        /// <summary>
        /// Coroutine that checks to see if the scene event is complete every network tick period.
        /// This will handle completing the scene event when one or more client(s) disconnect(s)
        /// during a scene event and if it does not complete within the scene loading time out period
        /// it will time out the scene event.
        /// </summary>
        internal IEnumerator TimeOutSceneEventProgress()
        {
            var waitForNetworkTick = new WaitForSeconds(1.0f / m_NetworkManager.NetworkConfig.TickRate);
            while (!HasTimedOut())
            {
                yield return waitForNetworkTick;

                TryFinishingSceneEventProgress();
            }
        }

        /// <summary>
        /// Sets the client's scene event progress to finished/true
        /// </summary>
        internal void ClientFinishedSceneEvent(ulong clientId)
        {
            if (ClientsProcessingSceneEvent.ContainsKey(clientId))
            {
                ClientsProcessingSceneEvent[clientId] = true;
                TryFinishingSceneEventProgress();
            }
        }

        /// <summary>
        /// Determines if the scene event has finished for both
        /// client(s) and server.
        /// </summary>
        /// <remarks>
        /// The server checks if all known clients processing this scene event
        /// have finished and then it returns its local AsyncOperation status.
        /// Clients finish when their AsyncOperation finishes.
        /// </remarks>
        private bool HasFinished()
        {
            // If the network session is terminated/terminating then finish tracking
            // this scene event
            if (!IsNetworkSessionActive())
            {
                return true;
            }

            // Clients skip over this
            foreach (var clientStatus in ClientsProcessingSceneEvent)
            {
                if (!clientStatus.Value)
                {
                    return false;
                }
            }

            // Return the local scene event's AsyncOperation status
            return m_AsyncOperation.isDone;
        }

        /// <summary>
        /// Sets the AsyncOperation for the scene load/unload event
        /// </summary>
        internal void SetAsyncOperation(AsyncOperation asyncOperation)
        {
            m_AsyncOperation = asyncOperation;
            m_AsyncOperation.completed += new Action<AsyncOperation>(asyncOp2 =>
            {
                // Don't invoke the callback if the network session is disconnected
                // during a SceneEventProgress
                if (IsNetworkSessionActive())
                {
                    OnSceneEventCompleted?.Invoke(SceneEventId);
                }

                // Go ahead and try finishing even if the network session is terminated/terminating
                // as we might need to stop the coroutine
                TryFinishingSceneEventProgress();
            });
        }


        internal bool IsNetworkSessionActive()
        {
            return m_NetworkManager != null && m_NetworkManager.IsListening && !m_NetworkManager.ShutdownInProgress;
        }

        /// <summary>
        /// Will try to finish the current scene event in progress as long as
        /// all conditions are met.
        /// </summary>
        internal void TryFinishingSceneEventProgress()
        {
            if (HasFinished() || HasTimedOut())
            {
                // Don't attempt to finalize this scene event if we are no longer listening or a shutdown is in progress
                if (IsNetworkSessionActive())
                {
                    OnComplete?.Invoke(this);
                    m_NetworkManager.SceneManager.SceneEventProgressTracking.Remove(Guid);
                    m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                }

                if (m_TimeOutCoroutine != null)
                {
                    m_NetworkManager.StopCoroutine(m_TimeOutCoroutine);
                }
            }
        }
    }
}
