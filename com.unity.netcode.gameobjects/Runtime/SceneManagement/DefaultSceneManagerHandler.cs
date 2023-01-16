using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


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

        internal Dictionary<string, Dictionary<int, SceneEntry>> SceneNameToSceneHandles = new Dictionary<string, Dictionary<int, SceneEntry>>();

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
        public void StopTrackingScene(int handle, string name, NetworkManager networkManager)
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
                SceneNameToSceneHandles.Add(scene.name, new Dictionary<int, SceneEntry>());
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
            if (SceneNameToSceneHandles.ContainsKey(sceneName))
            {
                foreach (var sceneHandleEntry in SceneNameToSceneHandles[sceneName])
                {
                    if (!sceneHandleEntry.Value.IsAssigned)
                    {
                        return true;
                    }
                }
            }
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
        public void PopulateLoadedScenes(ref Dictionary<int, Scene> scenesLoaded, NetworkManager networkManager)
        {
            SceneNameToSceneHandles.Clear();
            var sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!SceneNameToSceneHandles.ContainsKey(scene.name))
                {
                    SceneNameToSceneHandles.Add(scene.name, new Dictionary<int, SceneEntry>());
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
    }
}
