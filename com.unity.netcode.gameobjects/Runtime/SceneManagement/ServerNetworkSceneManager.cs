using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    // Server (or host) only code for NetworkSceneManager
    public partial class NetworkSceneManager
    {
        internal const int InvalidSceneNameOrPath = -1;

        // Used to be able to turn re-synchronization off
        internal static bool DisableReSynchronization;

        internal readonly Dictionary<Guid, SceneEventProgress> SceneEventProgressTracking = new Dictionary<Guid, SceneEventProgress>();

        /// <summary>
        /// <b>LoadSceneMode.Single:</b> All currently loaded scenes on the client will be unloaded and
        /// the server's currently active scene will be loaded in single mode on the client
        /// unless it was already loaded.<br/>
        /// <b>LoadSceneMode.Additive:</b> All currently loaded scenes are left as they are and any newly loaded
        /// scenes will be loaded additively.  Users need to determine which scenes are valid to load via the
        /// <see cref="VerifySceneBeforeLoading"/> method.
        /// </summary>
        public LoadSceneMode ClientSynchronizationMode { get; internal set; }

        /// <summary>
        /// This will change how clients are initially synchronized.<br/>
        /// <b>LoadSceneMode.Single:</b> All currently loaded scenes on the client will be unloaded and
        /// the server's currently active scene will be loaded in single mode on the client
        /// unless it was already loaded. <br/>
        /// <b>LoadSceneMode.Additive:</b> All currently loaded scenes are left as they are and any newly loaded
        /// scenes will be loaded additively.  Users need to determine which scenes are valid to load via the
        /// <see cref="VerifySceneBeforeLoading"/> method.
        /// </summary>
        /// <param name="mode"><see cref="LoadSceneMode"/> for initial client synchronization</param>
        public void SetClientSynchronizationMode(LoadSceneMode mode)
        {
            ClientSynchronizationMode = mode;
        }

        /// <summary>
        /// Server Side:
        /// This is used for players that have just had their connection approved and will assure they are synchronized
        /// properly if they are late joining
        /// Note: We write out all of the scenes to be loaded first and then all of the NetworkObjects that need to be
        /// synchronized.
        /// </summary>
        /// <param name="clientId">newly joined client identifier</param>
        internal void SynchronizeNetworkObjects(ulong clientId)
        {
            // Update the clients
            m_NetworkManager.SpawnManager.UpdateObservedNetworkObjects(clientId);

            var sceneEventData = BeginSceneEvent();

            sceneEventData.InitializeForSynch();
            sceneEventData.TargetClientId = clientId;
            sceneEventData.LoadSceneMode = ClientSynchronizationMode;
            var activeScene = SingleScene;// SceneManager.GetActiveScene();
            sceneEventData.SceneEventType = SceneEventType.Synchronize;

            // Organize how (and when) we serialize our NetworkObjects
            foreach (Scene scene in ScenesLoaded.Values)
            {
                // NetworkSceneManager does not synchronize scenes that are not loaded by NetworkSceneManager
                // unless the scene in question is the currently active scene.
                if (ExcludeSceneFromSychronization != null && !ExcludeSceneFromSychronization(scene))
                {
                    continue;
                }

                var sceneHash = SceneHashFromNameOrPath(scene.path);

                // This would depend upon whether we are additive or not
                // If we are the base scene, then we set the root scene index;
                if (activeScene == scene)
                {
                    if (!ValidateSceneBeforeLoading(sceneHash, sceneEventData.LoadSceneMode))
                    {
                        continue;
                    }
                    sceneEventData.SceneHash = sceneHash;
                    sceneEventData.SceneHandle = scene.handle;
                }
                else if (!ValidateSceneBeforeLoading(sceneHash, LoadSceneMode.Additive))
                {
                    continue;
                }
                sceneEventData.AddSceneToSynchronize(sceneHash, scene.handle);
            }

            sceneEventData.AddSpawnedNetworkObjects();
            sceneEventData.AddDespawnedInSceneNetworkObjects();

            var message = new SceneEventMessage
            {
                EventData = sceneEventData
            };
            var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, clientId);
            m_NetworkManager.NetworkMetrics.TrackSceneEventSent(clientId, (uint)sceneEventData.SceneEventType, "", size);

            // Notify the local server that the client has been sent the synchronize event
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = sceneEventData.SceneEventType,
                ClientId = clientId
            });

            OnSynchronize?.Invoke(clientId);

            EndSceneEvent(sceneEventData.SceneEventId);
        }

        /// <summary>
        /// <b>Server side:</b>
        /// Loads the scene name in either additive or single loading mode.
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="sceneName">the name of the scene to be loaded</param>
        /// <param name="loadSceneMode">how the scene will be loaded (single or additive mode)</param>
        /// <returns><see cref="SceneEventProgressStatus"/> (<see cref="SceneEventProgressStatus.Started"/> means it was successful)</returns>
        public SceneEventProgressStatus LoadScene(string sceneName, LoadSceneMode loadSceneMode)
        {
            var sceneEventProgress = ValidateSceneEventLoading(sceneName);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            // This will be the message we send to everyone when this scene event sceneEventProgress is complete
            sceneEventProgress.SceneEventType = SceneEventType.LoadEventCompleted;
            sceneEventProgress.LoadSceneMode = loadSceneMode;

            var sceneEventData = BeginSceneEvent();

            // Now set up the current scene event
            sceneEventData.SceneEventProgressId = sceneEventProgress.Guid;
            sceneEventData.SceneEventType = SceneEventType.Load;
            sceneEventData.SceneHash = SceneHashFromNameOrPath(sceneName);
            sceneEventData.LoadSceneMode = loadSceneMode;
            var sceneEventId = sceneEventData.SceneEventId;
            // This both checks to make sure the scene is valid and if not resets the active scene event
            m_IsSceneEventActive = ValidateSceneBeforeLoading(sceneEventData.SceneHash, loadSceneMode);
            if (!m_IsSceneEventActive)
            {
                EndSceneEvent(sceneEventId);
                return SceneEventProgressStatus.SceneFailedVerification;
            }

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Destroy current scene objects before switching.
                m_NetworkManager.SpawnManager.ServerDestroySpawnedSceneObjects();

                // Preserve the objects that should not be destroyed during the scene event
                MoveObjectsToDontDestroyOnLoad();

                // Now Unload all currently additively loaded scenes
                UnloadAdditivelyLoadedScenes(sceneEventId);

                // Register the active scene for unload scene event notifications
                SceneUnloadEventHandler.RegisterScene(this, SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }

            // Now start loading the scene
            var sceneEventAction = new ISceneManagerHandler.SceneEventAction() { SceneEventId = sceneEventId, EventAction = OnSceneLoaded };
            var sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, loadSceneMode, sceneEventAction);
            // If integration testing, IntegrationTestSceneHandler returns null
            if (sceneLoad == null)
            {
                sceneEventProgress.SetSceneLoadOperation(sceneEventAction);
            }
            else
            {
                sceneEventProgress.SetSceneLoadOperation(sceneLoad);
            }

            // Notify the local server that a scene loading event has begun
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneLoad,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = NetworkManager.ServerClientId
            });

            OnLoad?.Invoke(NetworkManager.ServerClientId, sceneName, sceneEventData.LoadSceneMode, sceneLoad);

            //Return our scene progress instance
            return sceneEventProgress.Status;
        }

        /// <summary>
        /// Both Client and Server: Incoming scene event entry point
        /// </summary>
        /// <param name="clientId">client who sent the scene event</param>
        /// <param name="reader">data associated with the scene event</param>
        internal void HandleSceneEvent(ulong clientId, FastBufferReader reader)
        {
            if (m_NetworkManager != null)
            {
                var sceneEventData = BeginSceneEvent();

                sceneEventData.Deserialize(reader);

                m_NetworkManager.NetworkMetrics.TrackSceneEventReceived(
                    clientId, (uint)sceneEventData.SceneEventType, SceneNameFromHash(sceneEventData.SceneHash), reader.Length);

                if (sceneEventData.IsSceneEventClientSide())
                {
                    HandleClientSceneEvent(sceneEventData.SceneEventId);
                }
                else
                {
                    HandleServerSceneEvent(sceneEventData.SceneEventId, clientId);
                }
            }
            else
            {
                Debug.LogError($"{nameof(NetworkSceneManager.HandleSceneEvent)} was invoked but {nameof(NetworkManager)} reference was null!");
            }
        }

        /// <summary>
        /// <b>Server Side:</b>
        /// Unloads an additively loaded scene.  If you want to unload a <see cref="LoadSceneMode.Single"/> mode loaded scene load another <see cref="LoadSceneMode.Single"/> scene.
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via the <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="scene"></param>
        /// <returns><see cref="SceneEventProgressStatus"/> (<see cref="SceneEventProgressStatus.Started"/> means it was successful)</returns>
        public SceneEventProgressStatus UnloadScene(Scene scene)
        {
            var sceneName = scene.name;
            var sceneHandle = scene.handle;
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"{nameof(UnloadScene)} was called, but the scene {scene.name} is not currently loaded!");
                return SceneEventProgressStatus.SceneNotLoaded;
            }

            var sceneEventProgress = ValidateSceneEventUnLoading(scene);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            if (!ScenesLoaded.ContainsKey(sceneHandle))
            {
                Debug.LogError($"{nameof(UnloadScene)} internal error! {sceneName} with handle {scene.handle} is not within the internal scenes loaded dictionary!");
                return SceneEventProgressStatus.InternalNetcodeError;
            }

            if (scene == SingleScene)
            {
                throw new NotSupportedException("Cannot unload the primary scene.");
            }

            var sceneEventData = BeginSceneEvent();
            sceneEventData.SceneEventProgressId = sceneEventProgress.Guid;
            sceneEventData.SceneEventType = SceneEventType.Unload;
            sceneEventData.SceneHash = SceneHashFromNameOrPath(sceneName);
            sceneEventData.LoadSceneMode = LoadSceneMode.Additive; // The only scenes unloaded are scenes that were additively loaded
            sceneEventData.SceneHandle = sceneHandle;

            // This will be the message we send to everyone when this scene event sceneEventProgress is complete
            sceneEventProgress.SceneEventType = SceneEventType.UnloadEventCompleted;

            ScenesLoaded.Remove(scene.handle);
            var sceneEventAction = new ISceneManagerHandler.SceneEventAction() { SceneEventId = sceneEventData.SceneEventId, EventAction = OnSceneUnloaded };
            var sceneUnload = SceneManagerHandler.UnloadSceneAsync(scene, sceneEventAction);

            // If integration testing, IntegrationTestSceneHandler returns null
            if (sceneUnload == null)
            {
                sceneEventProgress.SetSceneLoadOperation(sceneEventAction);
            }
            else
            {
                sceneEventProgress.SetSceneLoadOperation(sceneUnload);
            }

            // Notify local server that a scene is going to be unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneUnload,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = NetworkManager.ServerClientId  // Server can only invoke this
            });

            OnUnload?.Invoke(NetworkManager.ServerClientId, sceneName, sceneUnload);

            //Return the status
            return sceneEventProgress.Status;
        }

        /// <summary>
        /// Server side:
        /// On scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnServerLoadedScene(uint sceneEventId, Scene scene)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            // Register in-scene placed NetworkObjects with spawn manager
            foreach (var keyValuePairByGlobalObjectIdHash in ScenePlacedObjects)
            {
                foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                {
                    if (!keyValuePairBySceneHandle.Value.IsPlayerObject)
                    {
                        // All in-scene placed NetworkObjects default to being owned by the server
                        m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(keyValuePairBySceneHandle.Value,
                            m_NetworkManager.SpawnManager.GetNetworkObjectId(), true, false, NetworkManager.ServerClientId, true);
                    }
                }
            }

            // Set the server's scene's handle so the client can build a look up table
            sceneEventData.SceneHandle = scene.handle;

            // Send all clients the scene load event
            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                var clientId = m_NetworkManager.ConnectedClientsList[j].ClientId;
                if (clientId != NetworkManager.ServerClientId)
                {
                    sceneEventData.TargetClientId = clientId;
                    var message = new SceneEventMessage
                    {
                        EventData = sceneEventData
                    };
                    var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, clientId);
                    m_NetworkManager.NetworkMetrics.TrackSceneEventSent(clientId, (uint)sceneEventData.SceneEventType, scene.name, size);
                }
            }

            m_IsSceneEventActive = false;
            //First, notify local server that the scene was loaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventType.LoadComplete,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                ClientId = NetworkManager.ServerClientId,
                Scene = scene,
            });

            OnLoadComplete?.Invoke(NetworkManager.ServerClientId, SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode);

            //Second, only if we are a host do we want register having loaded for the associated SceneEventProgress
            if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId) && m_NetworkManager.IsHost)
            {
                SceneEventProgressTracking[sceneEventData.SceneEventProgressId].AddClientAsDone(NetworkManager.ServerClientId);
            }
            EndSceneEvent(sceneEventId);
        }

        /// <summary>
        /// Entry method for scene loading validation
        /// </summary>
        /// <param name="sceneName">scene name to load</param>
        /// <returns></returns>
        private SceneEventProgress ValidateSceneEventLoading(string sceneName)
        {
            if (!m_NetworkManager.IsServer)
            {
                throw new NotServerException("Only server can start a scene event!");
            }
            if (!m_NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                //Log message about enabling SceneManagement
                throw new Exception($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. " +
                    $"Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling " +
                    $"{nameof(NetworkSceneManager.LoadScene)} or {nameof(NetworkSceneManager.UnloadScene)}.");
            }

            return ValidateSceneEvent(sceneName);
        }

        /// <summary>
        /// Entry method for scene unloading validation
        /// </summary>
        /// <param name="scene">the scene to be unloaded</param>
        /// <returns></returns>
        private SceneEventProgress ValidateSceneEventUnLoading(Scene scene)
        {
            if (!m_NetworkManager.IsServer)
            {
                throw new NotServerException("Only server can start a scene event!");
            }

            if (!m_NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                //Log message about enabling SceneManagement
                throw new Exception($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. " +
                    $"Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling " +
                    $"{nameof(NetworkSceneManager.LoadScene)} or {nameof(NetworkSceneManager.UnloadScene)}.");
            }

            if (!scene.isLoaded)
            {
                Debug.LogWarning($"{nameof(UnloadScene)} was called, but the scene {scene.name} is not currently loaded!");
                return new SceneEventProgress(null, SceneEventProgressStatus.SceneNotLoaded);
            }

            return ValidateSceneEvent(scene.name, true);
        }

        /// <summary>
        /// Validates the new scene event request by the server-side code.
        /// This also initializes some commonly shared values as well as SceneEventProgress
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns><see cref="SceneEventProgress"/> that should have a <see cref="SceneEventProgress.Status"/> of <see cref="SceneEventProgressStatus.Started"/> otherwise it failed.</returns>
        private SceneEventProgress ValidateSceneEvent(string sceneName, bool isUnloading = false)
        {
            // Return scene event already in progress if one is already in progress
            if (m_IsSceneEventActive)
            {
                return new SceneEventProgress(null, SceneEventProgressStatus.SceneEventInProgress);
            }

            // Return invalid scene name status if the scene name is invalid
            if (SceneUtility.GetBuildIndexByScenePath(sceneName) == InvalidSceneNameOrPath)
            {
                Debug.LogError($"Scene '{sceneName}' couldn't be loaded because it has not been added to the build settings scenes in build list.");
                return new SceneEventProgress(null, SceneEventProgressStatus.InvalidSceneName);
            }

            var sceneEventProgress = new SceneEventProgress(m_NetworkManager)
            {
                SceneHash = SceneHashFromNameOrPath(sceneName)
            };

            SceneEventProgressTracking.Add(sceneEventProgress.Guid, sceneEventProgress);

            if (!isUnloading)
            {
                // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
                // they need to be moved into the do not destroy temporary scene
                // When it is set: Just before starting the asynchronous loading call
                // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
                // not destroy temporary scene are moved into the active scene
                IsSpawnedObjectsPendingInDontDestroyOnLoad = true;
            }

            m_IsSceneEventActive = true;

            // Set our callback delegate handler for completion
            sceneEventProgress.OnComplete = OnSceneEventProgressCompleted;

            return sceneEventProgress;
        }

        /// <summary>
        /// Callback for the <see cref="SceneEventProgress.OnComplete"/> <see cref="SceneEventProgress.OnCompletedDelegate"/> handler
        /// </summary>
        /// <param name="sceneEventProgress"></param>
        private bool OnSceneEventProgressCompleted(SceneEventProgress sceneEventProgress)
        {
            var sceneEventData = BeginSceneEvent();
            sceneEventData.SceneEventProgressId = sceneEventProgress.Guid;
            sceneEventData.SceneHash = sceneEventProgress.SceneHash;
            sceneEventData.SceneEventType = sceneEventProgress.SceneEventType;
            sceneEventData.ClientsCompleted = sceneEventProgress.DoneClients;
            sceneEventData.LoadSceneMode = sceneEventProgress.LoadSceneMode;
            sceneEventData.ClientsTimedOut = sceneEventProgress.ClientsThatStartedSceneEvent.Except(sceneEventProgress.DoneClients).ToList();

            var message = new SceneEventMessage
            {
                EventData = sceneEventData
            };
            var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, m_NetworkManager.ConnectedClientsIds);

            m_NetworkManager.NetworkMetrics.TrackSceneEventSent(
                m_NetworkManager.ConnectedClientsIds,
                (uint)sceneEventProgress.SceneEventType,
                SceneNameFromHash(sceneEventProgress.SceneHash),
                size);

            // Send a local notification to the server that all clients are done loading or unloading
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = sceneEventProgress.SceneEventType,
                SceneName = SceneNameFromHash(sceneEventProgress.SceneHash),
                ClientId = NetworkManager.ServerClientId,
                LoadSceneMode = sceneEventProgress.LoadSceneMode,
                ClientsThatCompleted = sceneEventProgress.DoneClients,
                ClientsThatTimedOut = m_NetworkManager.ConnectedClients.Keys.Except(sceneEventProgress.DoneClients).ToList(),
            });

            if (sceneEventData.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                OnLoadEventCompleted?.Invoke(SceneNameFromHash(sceneEventProgress.SceneHash), sceneEventProgress.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
            }
            else
            {
                OnUnloadEventCompleted?.Invoke(SceneNameFromHash(sceneEventProgress.SceneHash), sceneEventProgress.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
            }

            EndSceneEvent(sceneEventData.SceneEventId);
            return true;
        }

        /// <summary>
        /// Gets the associated hash value for the scene name or path
        /// </summary>
        internal uint SceneHashFromNameOrPath(string sceneNameOrPath)
        {
            var buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneNameOrPath);
            if (buildIndex >= 0)
            {
                if (BuildIndexToHash.ContainsKey(buildIndex))
                {
                    return BuildIndexToHash[buildIndex];
                }
                else
                {
                    throw new Exception($"Scene '{sceneNameOrPath}' has a build index of {buildIndex} that does not exist in the {nameof(BuildIndexToHash)} table!");
                }
            }
            else
            {
                throw new Exception($"Scene '{sceneNameOrPath}' couldn't be loaded because it has not been added to the build settings scenes in build list.");
            }
        }
    }
}
