using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if USING_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif


namespace Unity.Netcode
{
    /// <summary>
    ///  The default SceneManagerHandler that interfaces between the SceneManager and NetworkSceneManager
    /// </summary>
    internal class DefaultSceneManagerHandler : ISceneManagerHandler
    {
        private Scene m_InvalidScene = new Scene();

        internal struct SceneEntry
        {
            public bool IsAssigned;
            public Scene Scene;
        }
        public bool IsIntegrationTest() { return false; }

        internal Dictionary<string, Dictionary<NetworkSceneHandle, SceneEntry>> SceneNameToSceneHandles = new();

        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            sceneEventProgress.SetAsyncOperation(operation);
            return operation;
        }

        public AsyncOperation UnloadSceneAsync(Scene scene, SceneEventProgress sceneEventProgress)
        {
            var operation = SceneManager.UnloadSceneAsync(scene);
            sceneEventProgress.SetAsyncOperation(operation);
            return operation;
        }

#if USING_ADDRESSABLES
        /// <summary>
        /// Tracks Addressable-loaded scenes by their scene handle so they can be unloaded (and their
        /// handles released) later. A single loaded scene handle is unique per loaded scene instance.
        /// </summary>
        internal Dictionary<NetworkSceneHandle, AsyncOperationHandle<SceneInstance>> AddressableSceneHandles = new();

        // Maps an Addressable address to the actual runtime Scene.name once the scene has loaded.
        // The name is not knowable from the address alone, so it is recorded on load completion.
        internal Dictionary<string, string> AddressToLoadedSceneName = new();

        // Maps a loaded scene handle back to the Addressable address it was loaded from. This is required
        // to resolve a loaded Scene to its wire hash during client synchronization (the loaded scene's
        // path/name does not necessarily match its Addressable address).
        internal Dictionary<NetworkSceneHandle, string> SceneHandleToAddress = new();

        public ISceneEventOperation LoadAddressableSceneAsync(string address, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress)
        {
            // activateOnLoad:true preserves the existing non-deferred behavior of the default (build-settings) path.
            var handle = Addressables.LoadSceneAsync(address, loadSceneMode);

            // Subscribe the bookkeeping callback BEFORE wrapping the handle in the operation so that,
            // when the handle completes, the scene handle and name are recorded before the scene event
            // progress completion (OnSceneLoaded) runs and looks the scene up by name.
            handle.Completed += completedHandle =>
            {
                if (completedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var loadedScene = completedHandle.Result.Scene;
                    AddressableSceneHandles[loadedScene.handle] = completedHandle;
                    AddressToLoadedSceneName[address] = loadedScene.name;
                    SceneHandleToAddress[loadedScene.handle] = address;
                }
            };

            var sceneOperation = new AddressableSceneOperation(handle);
            sceneEventProgress.SetSceneEventOperation(sceneOperation);
            return sceneOperation;
        }

        public ISceneEventOperation UnloadAddressableSceneAsync(Scene scene, SceneEventProgress sceneEventProgress)
        {
            if (!AddressableSceneHandles.TryGetValue(scene.handle, out var handle))
            {
                throw new Exception($"[{nameof(DefaultSceneManagerHandler)}] Attempted to unload Addressable scene {scene.name} (handle {scene.handle}) that is not tracked as an Addressable scene!");
            }

            if (SceneHandleToAddress.TryGetValue(scene.handle, out var loadedAddress))
            {
                AddressToLoadedSceneName.Remove(loadedAddress);
            }
            AddressableSceneHandles.Remove(scene.handle);
            SceneHandleToAddress.Remove(scene.handle);

            // autoReleaseHandle:true releases the load handle when the unload completes.
            var unloadHandle = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);
            var sceneOperation = new AddressableSceneOperation(unloadHandle);
            sceneEventProgress.SetSceneEventOperation(sceneOperation);
            return sceneOperation;
        }

        public bool IsAddressableSceneLoaded(Scene scene)
        {
            return AddressableSceneHandles.ContainsKey(scene.handle);
        }

        public bool TryGetAddressableSceneName(string address, out string sceneName)
        {
            return AddressToLoadedSceneName.TryGetValue(address, out sceneName);
        }

        public bool TryGetAddressableSceneAddress(Scene scene, out string address)
        {
            return SceneHandleToAddress.TryGetValue(scene.handle, out address);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Works around an upstream Addressables bug that throws
        /// "Attempting to use an invalid operation handle" when stopping play mode with an
        /// Addressable scene still loaded (renders scene materials magenta on the way out).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When an Addressable scene loads, <c>AddressablesImpl</c> tracks the same scene handle in
        /// <b>two</b> internal collections: <c>m_resultToHandle</c> and <c>m_SceneInstances</c>. On
        /// play-mode exit, <c>AddressablesImpl.Dispose()</c> iterates <b>both</b> collections and
        /// releases every handle it finds. A freshly loaded scene handle has a reference count of 1,
        /// so the first release destroys it and the second release throws on the now-invalid handle.
        /// </para>
        /// <para>
        /// Clearing <c>m_SceneInstances</c> immediately before <c>Dispose()</c> runs lets the
        /// <c>m_resultToHandle</c> pass release each scene handle exactly once (which is the correct
        /// behavior). This hook is registered on editor load so it runs earlier in the
        /// <see cref="UnityEditor.EditorApplication.playModeStateChanged"/> invocation list than
        /// Addressables' own cleanup (which subscribes on play-mode enter). Editor-only and
        /// play-exit-only; builds are unaffected.
        /// </para>
        /// </remarks>
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterAddressablesTeardownWorkaround()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChangedAddressablesTeardown;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChangedAddressablesTeardown;
        }

        private static void OnPlayModeStateChangedAddressablesTeardown(UnityEditor.PlayModeStateChange change)
        {
            if (change != UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            try
            {
                var instanceProperty = typeof(Addressables).GetProperty("Instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var addressablesImpl = instanceProperty?.GetValue(null);
                if (addressablesImpl == null)
                {
                    return;
                }

                var sceneInstancesField = addressablesImpl.GetType().GetField("m_SceneInstances", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (sceneInstancesField?.GetValue(addressablesImpl) is HashSet<AsyncOperationHandle> sceneInstances)
                {
                    // Leave m_resultToHandle intact so Dispose() still releases each scene handle once.
                    sceneInstances.Clear();
                }
            }
            catch (Exception)
            {
                // Best-effort, editor-only teardown workaround. If Addressables' internals change and
                // reflection fails, silently fall back to the (harmless, cosmetic) upstream behavior
                // rather than surfacing an exception during play-mode exit.
            }
        }
#endif
#else
        public ISceneEventOperation LoadAddressableSceneAsync(string address, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress)
        {
            throw new NotSupportedException($"Addressable scene loading requires the com.unity.addressables package. Cannot load '{address}'.");
        }

        public ISceneEventOperation UnloadAddressableSceneAsync(Scene scene, SceneEventProgress sceneEventProgress)
        {
            throw new NotSupportedException("Addressable scene unloading requires the com.unity.addressables package.");
        }

        public bool IsAddressableSceneLoaded(Scene scene)
        {
            return false;
        }

        public bool TryGetAddressableSceneName(string address, out string sceneName)
        {
            sceneName = null;
            return false;
        }

        public bool TryGetAddressableSceneAddress(Scene scene, out string address)
        {
            address = null;
            return false;
        }
#endif

        /// <summary>
        /// Resets scene tracking
        /// </summary>
        public void ClearSceneTracking(NetworkManager networkManager)
        {
            SceneNameToSceneHandles.Clear();
        }

        /// <summary>
        /// Stops tracking a specific scene
        /// </summary>
        public void StopTrackingScene(NetworkSceneHandle handle, string name, NetworkManager networkManager)
        {
            if (SceneNameToSceneHandles.ContainsKey(name))
            {
                if (SceneNameToSceneHandles[name].ContainsKey(handle))
                {
                    SceneNameToSceneHandles[name].Remove(handle);
                    if (SceneNameToSceneHandles[name].Count == 0)
                    {
                        SceneNameToSceneHandles.Remove(name);
                    }
                }
            }
        }

        /// <summary>
        /// Starts tracking a specific scene
        /// </summary>
        public void StartTrackingScene(Scene scene, bool assigned, NetworkManager networkManager)
        {
            if (!SceneNameToSceneHandles.ContainsKey(scene.name))
            {
                SceneNameToSceneHandles.Add(scene.name, new Dictionary<NetworkSceneHandle, SceneEntry>());
            }

            if (!SceneNameToSceneHandles[scene.name].ContainsKey(scene.handle))
            {
                var sceneEntry = new SceneEntry()
                {
                    IsAssigned = true,
                    Scene = scene
                };
                SceneNameToSceneHandles[scene.name].Add(scene.handle, sceneEntry);
            }
            else
            {
                throw new Exception($"[Duplicate Handle] Scene {scene.name} already has scene handle {scene.handle} registered!");
            }
        }

        /// <summary>
        /// Determines if there is an existing scene loaded that matches the scene name but has not been assigned
        /// </summary>
        public bool DoesSceneHaveUnassignedEntry(string sceneName, NetworkManager networkManager)
        {
            var scenesWithSceneName = new List<Scene>();

            // Get all loaded scenes with the same name
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName)
                {
                    scenesWithSceneName.Add(scene);
                }
            }

            // If there are no scenes of this name loaded then we have no loaded scenes
            // to use
            if (scenesWithSceneName.Count == 0)
            {
                return false;
            }

            // If we have 1 or more scenes with the name and we have no entries, then we do have
            // a scene to use
            if (scenesWithSceneName.Count > 0 && !SceneNameToSceneHandles.ContainsKey(sceneName))
            {
                return true;
            }

            // Determine if any of the loaded scenes has been used for synchronizing
            foreach (var scene in scenesWithSceneName)
            {
                // If we don't have the handle, then we can use that scene
                if (!SceneNameToSceneHandles[scene.name].ContainsKey(scene.handle))
                {
                    return true;
                }

                // If we have an entry, but it is not yet assigned (i.e. preloaded)
                // then we can use that.
                if (!SceneNameToSceneHandles[scene.name][scene.handle].IsAssigned)
                {
                    return true;
                }
            }
            // If none were found, then we have no available scene (which most likely means one will get loaded)
            return false;
        }

        /// <summary>
        /// This will find any scene entry that hasn't been used/assigned, set the entry to assigned, and
        /// return the associated scene. If none are found it returns an invalid scene.
        /// </summary>
        public Scene GetSceneFromLoadedScenes(string sceneName, NetworkManager networkManager)
        {
            if (SceneNameToSceneHandles.ContainsKey(sceneName))
            {
                foreach (var sceneHandleEntry in SceneNameToSceneHandles[sceneName])
                {
                    if (!sceneHandleEntry.Value.IsAssigned)
                    {
                        var sceneEntry = sceneHandleEntry.Value;
                        sceneEntry.IsAssigned = true;
                        SceneNameToSceneHandles[sceneName][sceneHandleEntry.Key] = sceneEntry;
                        return sceneEntry.Scene;
                    }
                }
            }
            // If we found nothing return an invalid scene
            return m_InvalidScene;
        }

        /// <summary>
        /// Only invoked is client synchronization is additive, this will generate the scene tracking table
        /// in order to re-use the same scenes the server is synchronizing instead of having to unload the
        /// scenes and reload them when synchronizing (i.e. client disconnects due to external reason, the
        /// same application instance is still running, the same scenes are still loaded on the client, and
        /// upon reconnecting the client doesn't have to unload the scenes and then reload them)
        /// </summary>
        public void PopulateLoadedScenes(ref Dictionary<NetworkSceneHandle, Scene> scenesLoaded, NetworkManager networkManager)
        {
            SceneNameToSceneHandles.Clear();
            var sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!SceneNameToSceneHandles.ContainsKey(scene.name))
                {
                    SceneNameToSceneHandles.Add(scene.name, new Dictionary<NetworkSceneHandle, SceneEntry>());
                }

                if (!SceneNameToSceneHandles[scene.name].ContainsKey(scene.handle))
                {
                    var sceneEntry = new SceneEntry()
                    {
                        IsAssigned = false,
                        Scene = scene
                    };
                    SceneNameToSceneHandles[scene.name].Add(scene.handle, sceneEntry);
                    if (!scenesLoaded.ContainsKey(scene.handle))
                    {
                        scenesLoaded.Add(scene.handle, scene);
                    }
                }
                else
                {
                    throw new Exception($"[Duplicate Handle] Scene {scene.name} already has scene handle {scene.handle} registered!");
                }
            }
        }

        private List<Scene> m_ScenesToUnload = new List<Scene>();

        /// <summary>
        /// Unloads any scenes that have not been assigned.
        /// </summary>
        /// <param name="networkManager"></param>
        public void UnloadUnassignedScenes(NetworkManager networkManager)
        {
            var sceneManager = networkManager.SceneManager;
            SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;
            foreach (var sceneEntry in SceneNameToSceneHandles)
            {
                var scenHandleEntries = SceneNameToSceneHandles[sceneEntry.Key];
                foreach (var sceneHandleEntry in scenHandleEntries)
                {
                    if (!sceneHandleEntry.Value.IsAssigned)
                    {
                        if (sceneManager.VerifySceneBeforeUnloading == null || sceneManager.VerifySceneBeforeUnloading.Invoke(sceneHandleEntry.Value.Scene))
                        {
                            m_ScenesToUnload.Add(sceneHandleEntry.Value.Scene);
                        }
                    }
                }
            }
            foreach (var sceneToUnload in m_ScenesToUnload)
            {
                SceneManager.UnloadSceneAsync(sceneToUnload);
                // Update the ScenesLoaded when we unload scenes
                if (sceneManager.ScenesLoaded.ContainsKey(sceneToUnload.handle))
                {
                    sceneManager.ScenesLoaded.Remove(sceneToUnload.handle);
                }
            }
        }

        private void SceneManager_SceneUnloaded(Scene scene)
        {
            if (SceneNameToSceneHandles.ContainsKey(scene.name))
            {
                if (SceneNameToSceneHandles[scene.name].ContainsKey(scene.handle))
                {
                    SceneNameToSceneHandles[scene.name].Remove(scene.handle);
                }
                if (SceneNameToSceneHandles[scene.name].Count == 0)
                {
                    SceneNameToSceneHandles.Remove(scene.name);
                }
                m_ScenesToUnload.Remove(scene);
                if (m_ScenesToUnload.Count == 0)
                {
                    SceneManager.sceneUnloaded -= SceneManager_SceneUnloaded;
                }
            }
        }

        /// <summary>
        /// Handles determining if a client should attempt to load a scene during synchronization.
        /// </summary>
        /// <param name="sceneName">name of the scene to be loaded</param>
        /// <param name="isPrimaryScene">when in client synchronization mode single, this determines if the scene is the primary active scene</param>
        /// <param name="clientSynchronizationMode">the current client synchronization mode</param>
        /// <param name="networkManager"><see cref="NetworkManager"/> instance</param>
        /// <returns></returns>
        public bool ClientShouldPassThrough(string sceneName, bool isPrimaryScene, LoadSceneMode clientSynchronizationMode, NetworkManager networkManager)
        {
            var shouldPassThrough = clientSynchronizationMode == LoadSceneMode.Single ? false : DoesSceneHaveUnassignedEntry(sceneName, networkManager);
            var activeScene = SceneManager.GetActiveScene();

            // If shouldPassThrough is not yet true and the scene to be loaded is the currently active scene
            if (!shouldPassThrough && sceneName == activeScene.name)
            {
                // In additive mode we always pass through, but in LoadSceneMode.Single we only pass through if the currently active scene
                // is the primary scene to be loaded
                if (clientSynchronizationMode == LoadSceneMode.Additive || (isPrimaryScene && clientSynchronizationMode == LoadSceneMode.Single))
                {
                    // don't try to reload this scene and pass through to post load processing.
                    shouldPassThrough = true;
                }
            }
            return shouldPassThrough;
        }

        /// <summary>
        /// Handles migrating dynamically spawned NetworkObjects to the DDOL when a scene is unloaded
        /// </summary>
        /// <param name="networkManager"><see cref="NetworkManager"/>relative instance</param>
        /// <param name="scene">scene being unloaded</param>
        public void MoveObjectsFromSceneToDontDestroyOnLoad(ref NetworkManager networkManager, Scene scene)
        {
            bool isActiveScene = scene == SceneManager.GetActiveScene();
            // Create a local copy of the spawned objects list since the spawn manager will adjust the list as objects
            // are despawned.
            var localSpawnedObjectsHashSet = new HashSet<NetworkObject>(networkManager.SpawnManager.SpawnedObjectsList);
            var distributedAuthority = networkManager.DistributedAuthorityMode;
            foreach (var networkObject in localSpawnedObjectsHashSet)
            {
                if (networkObject == null || (networkObject != null && networkObject.gameObject.scene.handle != scene.handle))
                {
                    continue;
                }

                // Check to determine if we need to allow destroying a non-authority instance
                if (distributedAuthority && networkObject.DestroyWithScene && !networkObject.HasAuthority)
                {
                    networkObject.DestroyPendingSceneEvent = true;
                }

                // Only NetworkObjects marked to not be destroyed with the scene and are not already in the DDOL are preserved
                if (!networkObject.DestroyWithScene && networkObject.gameObject.scene != networkManager.SceneManager.DontDestroyOnLoadScene)
                {
                    // Only move dynamically spawned NetworkObjects with no parent as the children will follow
                    if (networkObject.gameObject.transform.parent == null && !networkObject.InScenePlaced)
                    {
                        UnityEngine.Object.DontDestroyOnLoad(networkObject.gameObject);
                    }
                }
                else if (networkObject.HasAuthority)
                {
                    // We know this instance is going to be destroyed (when it receives the destroy object message).
                    // We have to invoke this prior to invoking despawn in order to know that we are de-spawning in
                    // preparation of being destroyed by the SceneManager.
                    networkObject.SetIsDestroying();
                    // This sends a de-spawn message prior to the scene event.
                    networkObject.Despawn();
                }
                else // We are a client, migrate the object into the DDOL temporarily until it receives the destroy command from the server
                {
                    // We know this instance is going to be destroyed (when it receives the destroy object message).
                    // We have to invoke this prior to invoking despawn in order to know that we are de-spawning in
                    // preparation of being destroyed by the SceneManager.
                    networkObject.SetIsDestroying();
                    UnityEngine.Object.DontDestroyOnLoad(networkObject.gameObject);
                }
            }
        }

        /// <summary>
        /// Sets the client synchronization mode which impacts whether both the server or client take into consideration scenes loaded before
        /// starting the <see cref="NetworkManager"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="LoadSceneMode.Single"/>: Does not take preloaded scenes into consideration
        /// <see cref="LoadSceneMode.Single"/>: Does take preloaded scenes into consideration
        /// </remarks>
        /// <param name="networkManager">relative <see cref="NetworkManager"/> instance</param>
        /// <param name="mode"><see cref="LoadSceneMode.Single"/> or <see cref="LoadSceneMode.Additive"/></param>
        public void SetClientSynchronizationMode(ref NetworkManager networkManager, LoadSceneMode mode)
        {
            var sceneManager = networkManager.SceneManager;
            // In client-server, we don't let client's set this value.
            // In distributed authority, since session owner can be promoted clients can set this value
            if (!networkManager.DistributedAuthorityMode && !networkManager.IsServer)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Clients should not set this value as it is automatically synchronized with the server's setting!");
                }
                return;
            }
            else // Warn users if they are changing this after there are clients already connected and synchronized
            if (!networkManager.DistributedAuthorityMode && networkManager.ConnectedClientsIds.Count > (networkManager.IsHost ? 1 : 0) && sceneManager.ClientSynchronizationMode != mode)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server is changing client synchronization mode after clients have been synchronized! It is recommended to do this before clients are connected!");
                }
            }

            // For additive client synchronization, we take into consideration scenes
            // already loaded.
            if (mode == LoadSceneMode.Additive)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);

                    // If using scene verification
                    if (sceneManager.VerifySceneBeforeLoading != null)
                    {
                        // Determine if we should take this scene into consideration
                        if (!sceneManager.VerifySceneBeforeLoading.Invoke(scene.buildIndex, scene.name, LoadSceneMode.Additive))
                        {
                            continue;
                        }
                    }

                    // If the scene is not already in the ScenesLoaded list, then add it
                    if (!sceneManager.ScenesLoaded.ContainsKey(scene.handle))
                    {
                        sceneManager.ScenesLoaded.Add(scene.handle, scene);
                    }
                }
            }
            // Set the client synchronization mode
            sceneManager.ClientSynchronizationMode = mode;
        }
    }
}
