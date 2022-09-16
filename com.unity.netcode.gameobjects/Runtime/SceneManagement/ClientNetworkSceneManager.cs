using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    // Client-only code for NetworkSceneManager
    public partial class NetworkSceneManager
    {
        /// <summary>
        /// Client side:
        /// On scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnClientLoadedScene(uint sceneEventId, Scene scene)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            sceneEventData.DeserializeScenePlacedObjects();

            sceneEventData.SceneEventType = SceneEventType.LoadComplete;
            SendSceneEventData(sceneEventId, new ulong[] { NetworkManager.ServerClientId });
            m_IsSceneEventActive = false;

            // Notify local client that the scene was loaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventType.LoadComplete,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                ClientId = m_NetworkManager.LocalClientId,
                Scene = scene,
            });

            OnLoadComplete?.Invoke(m_NetworkManager.LocalClientId, SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode);

            EndSceneEvent(sceneEventId);
        }

        /// <summary>
        /// Client Side:
        /// Handles both forms of scene loading
        /// </summary>
        /// <param name="objectStream">Stream data associated with the event</param>
        private void OnClientSceneLoadingEvent(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneName = SceneNameFromHash(sceneEventData.SceneHash);

            // Run scene validation before loading a scene
            if (!ValidateSceneBeforeLoading(sceneEventData.SceneHash, sceneEventData.LoadSceneMode))
            {
                EndSceneEvent(sceneEventId);
                return;
            }

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Move ALL NetworkObjects to the temp scene
                MoveObjectsToDontDestroyOnLoad();

                // Now Unload all currently additively loaded scenes
                UnloadAdditivelyLoadedScenes(sceneEventData.SceneEventId);
            }

            // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
            // they need to be moved into the do not destroy temporary scene
            // When it is set: Just before starting the asynchronous loading call
            // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
            // not destroy temporary scene are moved into the active scene
            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

                // Register the active scene for unload scene event notifications
                SceneUnloadEventHandler.RegisterScene(this, SceneManager.GetActiveScene(), LoadSceneMode.Single);

            }

            var sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, sceneEventData.LoadSceneMode,
                new ISceneManagerHandler.SceneEventAction() { SceneEventId = sceneEventId, EventAction = OnSceneLoaded });

            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneLoad,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId
            });

            OnLoad?.Invoke(m_NetworkManager.LocalClientId, sceneName, sceneEventData.LoadSceneMode, sceneLoad);
        }

        /// <summary>
        /// <b>Client Side:</b>
        /// Handles <see cref="SceneEventType.Unload"/> scene events.
        /// </summary>
        private void OnClientUnloadScene(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneName = SceneNameFromHash(sceneEventData.SceneHash);

            if (!ServerSceneHandleToClientSceneHandle.ContainsKey(sceneEventData.SceneHandle))
            {
                Debug.Log($"Client failed to unload scene {sceneName} " +
                    $"because we are missing the client scene handle due to the server scene handle {sceneEventData.SceneHandle} not being found.");
                EndSceneEvent(sceneEventId);
                return;
            }

            var sceneHandle = ServerSceneHandleToClientSceneHandle[sceneEventData.SceneHandle];

            if (!ScenesLoaded.ContainsKey(sceneHandle))
            {
                // Error scene handle not found!
                throw new Exception($"Client failed to unload scene {sceneName} " +
                    $"because the client scene handle {sceneHandle} was not found in ScenesLoaded!");
            }
            m_IsSceneEventActive = true;

            var sceneUnload = SceneManagerHandler.UnloadSceneAsync(ScenesLoaded[sceneHandle],
                new ISceneManagerHandler.SceneEventAction() { SceneEventId = sceneEventData.SceneEventId, EventAction = OnSceneUnloaded });

            ScenesLoaded.Remove(sceneHandle);

            // Remove our server to scene handle lookup
            ServerSceneHandleToClientSceneHandle.Remove(sceneEventData.SceneHandle);

            // Notify the local client that a scene is going to be unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneUnload,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = LoadSceneMode.Additive,     // The only scenes unloaded are scenes that were additively loaded
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId   // Server sent this message to the client, but client is executing it
            });

            OnUnload?.Invoke(m_NetworkManager.LocalClientId, sceneName, sceneUnload);
        }

        /// <summary>
        /// Client Side Only:
        /// This takes a server scene handle that is written by the server before the scene relative
        /// NetworkObject is serialized and converts the server scene handle to a local client handle
        /// so it can set the appropriate SceneBeingSynchronized.
        /// Note: This is now part of the soft synchronization process and is needed for the scenario
        /// where a user loads the same scene additively that has an in-scene placed NetworkObject
        /// which means each scene relative in-scene placed NetworkObject will have the identical GlobalObjectIdHash
        /// value.  Scene handles are used to distinguish between in-scene placed NetworkObjects under this situation.
        /// </summary>
        /// <param name="serverSceneHandle"></param>
        internal void SetTheSceneBeingSynchronized(int serverSceneHandle)
        {
            var clientSceneHandle = serverSceneHandle;
            if (ServerSceneHandleToClientSceneHandle.ContainsKey(serverSceneHandle))
            {
                clientSceneHandle = ServerSceneHandleToClientSceneHandle[serverSceneHandle];
                // If we were already set, then ignore
                if (SceneBeingSynchronized.IsValid() && SceneBeingSynchronized.isLoaded && SceneBeingSynchronized.handle == clientSceneHandle)
                {
                    return;
                }

                // Get the scene currently being synchronized
                SceneBeingSynchronized = ScenesLoaded.ContainsKey(clientSceneHandle) ? ScenesLoaded[clientSceneHandle] : new Scene();

                if (!SceneBeingSynchronized.IsValid() || !SceneBeingSynchronized.isLoaded)
                {
                    // Let's go ahead and use the currently active scene under the scenario where a NetworkObject is determined to exist in a scene that the NetworkSceneManager is not aware of
                    SceneBeingSynchronized = SceneManager.GetActiveScene();

                    // Keeping the warning here in the event we cannot find the scene being synchronized
                    Debug.LogWarning($"[{nameof(NetworkSceneManager)}- {nameof(ScenesLoaded)}] Could not find the appropriate scene to set as being synchronized! Using the currently active scene.");
                }
            }
            else
            {
                // Most common scenario for DontDestroyOnLoad is when NetworkManager is set to not be destroyed
                if (serverSceneHandle == DontDestroyOnLoadScene.handle)
                {
                    SceneBeingSynchronized = m_NetworkManager.gameObject.scene;
                    return;
                }
                else
                {
                    // Let's go ahead and use the currently active scene under the scenario where a NetworkObject is determined to exist in a scene that the NetworkSceneManager is not aware of
                    // or the NetworkObject has yet to be moved to that specific scene (i.e. no DontDestroyOnLoad scene exists yet).
                    SceneBeingSynchronized = SceneManager.GetActiveScene();

                    // This could be the scenario where NetworkManager.DontDestroy is false and we are creating the first NetworkObject (client side) to be in the DontDestroyOnLoad scene
                    // Otherwise, this is some other specific scenario that we might not be handling currently.
                    Debug.LogWarning($"[{nameof(SceneEventData)}- Scene Handle Mismatch] {nameof(serverSceneHandle)} could not be found in {nameof(ServerSceneHandleToClientSceneHandle)}. Using the currently active scene.");
                }
            }
        }

        /// <summary>
        /// Client Side:
        /// Handles incoming Scene_Event messages for clients
        /// </summary>
        /// <param name="stream">data associated with the event</param>
        private void HandleClientSceneEvent(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            switch (sceneEventData.SceneEventType)
            {
                case SceneEventType.Load:
                    {
                        OnClientSceneLoadingEvent(sceneEventId);
                        break;
                    }
                case SceneEventType.Unload:
                    {
                        OnClientUnloadScene(sceneEventId);
                        break;
                    }
                case SceneEventType.Synchronize:
                    {
                        if (!sceneEventData.IsDoneWithSynchronization())
                        {
                            OnClientBeginSync(sceneEventId);
                        }
                        else
                        {
                            // Include anything in the DDOL scene
                            PopulateScenePlacedObjects(DontDestroyOnLoadScene, false);
                            // Synchronize the NetworkObjects for this scene
                            sceneEventData.SynchronizeSceneNetworkObjects(m_NetworkManager);

                            sceneEventData.SceneEventType = SceneEventType.SynchronizeComplete;
                            SendSceneEventData(sceneEventId, new ulong[] { NetworkManager.ServerClientId });

                            // All scenes are synchronized, let the server know we are done synchronizing
                            m_NetworkManager.IsConnectedClient = true;

                            // Client is now synchronized and fully "connected".  This also means the client can send "RPCs" at this time
                            m_NetworkManager.InvokeOnClientConnectedCallback(m_NetworkManager.LocalClientId);

                            // Notify the client that they have finished synchronizing
                            OnSceneEvent?.Invoke(new SceneEvent()
                            {
                                SceneEventType = sceneEventData.SceneEventType,
                                ClientId = m_NetworkManager.LocalClientId, // Client sent this to the server
                            });

                            OnSynchronizeComplete?.Invoke(m_NetworkManager.LocalClientId);

                            EndSceneEvent(sceneEventId);
                        }
                        break;
                    }
                case SceneEventType.ReSynchronize:
                    {
                        // Notify the local client that they have been re-synchronized after being synchronized with an in progress game session
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = sceneEventData.SceneEventType,
                            ClientId = NetworkManager.ServerClientId,  // Server sent this to client
                        });

                        EndSceneEvent(sceneEventId);
                        break;
                    }
                case SceneEventType.LoadEventCompleted:
                case SceneEventType.UnloadEventCompleted:
                    {
                        // Notify the local client that all clients have finished loading or unloading
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = sceneEventData.SceneEventType,
                            LoadSceneMode = sceneEventData.LoadSceneMode,
                            SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                            ClientId = NetworkManager.ServerClientId,
                            ClientsThatCompleted = sceneEventData.ClientsCompleted,
                            ClientsThatTimedOut = sceneEventData.ClientsTimedOut,
                        });

                        if (sceneEventData.SceneEventType == SceneEventType.LoadEventCompleted)
                        {
                            OnLoadEventCompleted?.Invoke(SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
                        }
                        else
                        {
                            OnUnloadEventCompleted?.Invoke(SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
                        }

                        EndSceneEvent(sceneEventId);

                        break;
                    }
                default:
                    {
                        Debug.LogWarning($"{sceneEventData.SceneEventType} is not currently supported!");
                        break;
                    }
            }
        }

        /// <summary>
        /// This is called when the client receives the <see cref="SceneEventType.Synchronize"/> event
        /// Note: This can recurse one additional time by the client if the current scene loaded by the client
        /// is already loaded.
        /// </summary>
        private void OnClientBeginSync(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneHash = sceneEventData.GetNextSceneSynchronizationHash();
            var sceneHandle = sceneEventData.GetNextSceneSynchronizationHandle();
            var sceneName = SceneNameFromHash(sceneHash);
            var activeScene = SceneManager.GetActiveScene();

            var loadSceneMode = sceneHash == sceneEventData.SceneHash ? sceneEventData.LoadSceneMode : LoadSceneMode.Additive;

            // Store the sceneHandle and hash
            sceneEventData.NetworkSceneHandle = sceneHandle;
            sceneEventData.ClientSceneHash = sceneHash;

            // If this is the beginning of the synchronization event, then send client a notification that synchronization has begun
            if (sceneHash == sceneEventData.SceneHash)
            {
                OnSceneEvent?.Invoke(new SceneEvent()
                {
                    SceneEventType = SceneEventType.Synchronize,
                    ClientId = m_NetworkManager.LocalClientId,
                });

                OnSynchronize?.Invoke(m_NetworkManager.LocalClientId);

                // Clear the in-scene placed NetworkObjects when we load the first scene in our synchronization process
                ScenePlacedObjects.Clear();
            }

            // Always check to see if the scene needs to be validated
            if (!ValidateSceneBeforeLoading(sceneHash, loadSceneMode))
            {
                HandleClientSceneEvent(sceneEventId);
                if (m_NetworkManager.LogLevel == LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"Client declined to load the scene {sceneName}, continuing with synchronization.");
                }
                return;
            }

            var shouldPassThrough = false;
            var sceneLoad = (AsyncOperation)null;

            // Check to see if the client already has loaded the scene to be loaded
            // if (sceneName == activeScene.name)
            // {
            //     // If the client is already in the same scene, then pass through and
            //     // don't try to reload it.
            //     shouldPassThrough = true;
            // }

            if (!shouldPassThrough)
            {
                // If not, then load the scene
                sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, loadSceneMode,
                new ISceneManagerHandler.SceneEventAction() { SceneEventId = sceneEventId, EventAction = ClientLoadedSynchronization });

                // Notify local client that a scene load has begun
                OnSceneEvent?.Invoke(new SceneEvent()
                {
                    AsyncOperation = sceneLoad,
                    SceneEventType = SceneEventType.Load,
                    LoadSceneMode = loadSceneMode,
                    SceneName = sceneName,
                    ClientId = m_NetworkManager.LocalClientId,
                });

                OnLoad?.Invoke(m_NetworkManager.LocalClientId, sceneName, loadSceneMode, sceneLoad);
            }
            else
            {
                // If so, then pass through
                ClientLoadedSynchronization(sceneEventId, activeScene);
            }
        }

        /// <summary>
        /// Part of the initial client synchronization code, called instead of OnSceneLoaded
        ///
        /// Once a scene is loaded ( or if it was already loaded) this gets called.
        /// This handles all of the in-scene and dynamically spawned NetworkObject synchronization
        /// </summary>
        /// <param name="sceneIndex">Netcode scene index that was loaded</param>
        private void ClientLoadedSynchronization(uint sceneEventId, Scene scene)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneName = SceneNameFromHash(sceneEventData.ClientSceneHash);
            var nextScene = scene; //GetAndAddNewlyLoadedSceneByName(sceneName);

            if (!nextScene.isLoaded || !nextScene.IsValid())
            {
                throw new Exception($"Failed to find valid scene internal Unity.Netcode for {nameof(GameObject)}s error!");
            }

            ScenesLoaded.Add(nextScene.handle, nextScene);

            var loadSceneMode = (sceneEventData.ClientSceneHash == sceneEventData.SceneHash ? sceneEventData.LoadSceneMode : LoadSceneMode.Additive);

            // For now, during a synchronization event, we will make the first scene the "base/master" scene that denotes a "complete scene switch"
            if (loadSceneMode == LoadSceneMode.Single)
            {
                SingleScene = nextScene;
                SceneManager.SetActiveScene(nextScene);
            }

            if (!ServerSceneHandleToClientSceneHandle.ContainsKey(sceneEventData.NetworkSceneHandle))
            {
                ServerSceneHandleToClientSceneHandle.Add(sceneEventData.NetworkSceneHandle, nextScene.handle);
            }
            else
            {
                // If the exact same handle exists then there are problems with using handles
                throw new Exception($"Server Scene Handle ({sceneEventData.SceneHandle}) already exist!  Happened during scene load of {nextScene.name} with Client Handle ({nextScene.handle})");
            }

            // Apply all in-scene placed NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene, false);

            // Send notification back to server that we finished loading this scene
            var responseSceneEventData = BeginSceneEvent();
            responseSceneEventData.LoadSceneMode = loadSceneMode;
            responseSceneEventData.SceneEventType = SceneEventType.LoadComplete;
            responseSceneEventData.SceneHash = sceneEventData.ClientSceneHash;


            var message = new SceneEventMessage
            {
                EventData = responseSceneEventData
            };
            var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, NetworkManager.ServerClientId);

            m_NetworkManager.NetworkMetrics.TrackSceneEventSent(NetworkManager.ServerClientId, (uint)responseSceneEventData.SceneEventType, sceneName, size);

            EndSceneEvent(responseSceneEventData.SceneEventId);

            // Send notification to local client that the scene has finished loading
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventType.LoadComplete,
                LoadSceneMode = loadSceneMode,
                SceneName = sceneName,
                Scene = nextScene,
                ClientId = m_NetworkManager.LocalClientId,
            });

            OnLoadComplete?.Invoke(m_NetworkManager.LocalClientId, sceneName, loadSceneMode);

            // Check to see if we still have scenes to load and synchronize with
            HandleClientSceneEvent(sceneEventId);
        }
    }
}
