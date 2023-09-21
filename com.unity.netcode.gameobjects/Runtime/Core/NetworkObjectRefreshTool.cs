#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// This is a helper tool to update all in-scene placed instances of a prefab that 
    /// originally did not have a NetworkObject component but one was added to the prefab
    /// later.
    /// </summary>
    internal class NetworkObjectRefreshTool
    {
        private static List<string> s_ScenesToUpdate = new List<string>();
        private static bool s_ProcessScenes;
        private static bool s_CloseScenes;

        internal static Action AllScenesProcessed;

        internal static void ProcessScene(string scenePath, bool processScenes = true)
        {
            if (!s_ScenesToUpdate.Contains(scenePath))
            {
                if (s_ScenesToUpdate.Count == 0)
                {
                    EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
                    EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
                }
                s_ScenesToUpdate.Add(scenePath);
            }
            s_ProcessScenes = processScenes;
        }

        internal static void ProcessActiveScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (s_ScenesToUpdate.Contains(activeScene.path) && s_ProcessScenes)
            {
                SceneOpened(activeScene);
            }
        }

        internal static void ProcessScenes()
        {
            if (s_ScenesToUpdate.Count != 0)
            {
                s_CloseScenes = true;
                var scenePath = s_ScenesToUpdate.First();
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
            else
            {
                s_CloseScenes = false;
                EditorSceneManager.sceneSaved -= EditorSceneManager_sceneSaved;
                EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
                AllScenesProcessed?.Invoke();
            }
        }

        private static void FinishedProcessingScene(Scene scene, bool refreshed = false)
        {
            if (s_ScenesToUpdate.Contains(scene.path))
            {
                // Provide a log of all scenes that were modified to the user
                if (refreshed)
                {
                    Debug.Log($"Refreshed and saved updates to scene: {scene.name}");
                }
                s_ProcessScenes = false;
                s_ScenesToUpdate.Remove(scene.path);

                if (scene != SceneManager.GetActiveScene())
                {
                    EditorSceneManager.CloseScene(scene, s_CloseScenes);
                }
                ProcessScenes();
            }
        }

        private static void EditorSceneManager_sceneSaved(Scene scene)
        {
            FinishedProcessingScene(scene, true);
        }

        private static void SceneOpened(Scene scene)
        {
            if (s_ScenesToUpdate.Contains(scene.path))
            {
                if (s_ProcessScenes)
                {
                    if (!EditorSceneManager.MarkSceneDirty(scene))
                    {
                        Debug.Log($"Scene {scene.name} did not get marked as dirty!");
                        FinishedProcessingScene(scene);
                    }
                    else
                    {
                        EditorSceneManager.SaveScene(scene);
                    }
                }
                else
                {
                    FinishedProcessingScene(scene);
                }
            }
        }

        private static void EditorSceneManager_sceneOpened(Scene scene, OpenSceneMode mode)
        {
            SceneOpened(scene);
        }
    }
}
#endif // UNITY_EDITOR
