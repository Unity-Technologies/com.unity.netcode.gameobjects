using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.Editor.Configuration
{
    /// <summary>
    /// Finds looks at every scene inside the <see cref="EditorBuildSettings"/>
    ///   1. Calls <see cref="NetworkObject.OnValidate"/> on every <see cref="NetworkObject"/> in the scene
    ///   2. Marks the scene dirty and saves it
    ///
    /// This will reserialize and save every NetworkObject that had changes after <see cref="NetworkObject.OnValidate"/> was called.
    /// </summary>
    internal class OnValidateAllOnUpgrade : PackageUpgradeAction
    {
        // This action will do nothing except if the previously serialized version of NGO this project has is lower than this version
        private readonly NgoVersion m_AddedInScenePlacedField = new()
        {
            Major = 2,
            Minor = 7,
            Patch = 0,
        };

        // Internal state tracking
        private enum State
        {
            None,
            Setup,
            Processing,
            Finished,
        }

        private static readonly List<string> k_ScenesToUpdate = new();
        private static readonly HashSet<string> k_ScenesUpdated = new();

        private static State s_State = State.None;

        private int m_Counter;

        protected override bool OnIsFinished()
        {
            Debug.Log($"OnValidateAllOnUpgrade state: {s_State}");
            return s_State == State.Finished;
        }

        protected override void OnProcess()
        {
            // If we haven't started yet, set initial state
            if (s_State == State.None)
            {
                if (PackageVersionNeedsUpgrade(m_AddedInScenePlacedField))
                {
                    s_State = State.Setup;

                    k_ScenesToUpdate.Clear();
                    k_ScenesUpdated.Clear();

                    RegisterAllForUpdate();
                }
                else
                {
                    s_State = State.Finished;
                    LogInfo("No processing needed. Skipping.");
                }

                // Need to return after setup to allow the editor to update
                return;
            }

            // Catches if there were no scenes found in setup
            if (k_ScenesToUpdate.Count == 0)
            {
                FinishedProcessingAll();
                return;
            }

            // Start the processing and start opening scenes
            if (s_State == State.Setup)
            {
                s_State = State.Processing;

                // Open a limited number of initial scenes
                // This limits the compute resources used on large projects
                var openLimit = 5;
                var opened = 0;
                while (opened < openLimit && k_ScenesToUpdate.Count > opened)
                {
                    OpenScene(k_ScenesToUpdate[opened++]);
                }
            }
        }

        private void RegisterAllForUpdate()
        {
            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneSaved += SceneSaved;

            foreach (var editorScene in EditorBuildSettings.scenes)
            {
                // LogInfo($"Adding scene {editorScene.path} to be processed!");
                k_ScenesToUpdate.Add(editorScene.path);
            }
        }

        private static void EditorSceneManager_sceneOpened(Scene scene, OpenSceneMode mode)
        {
            SceneOpened(scene);
        }

        private static void OpenScene(string scenePath)
        {
            LogInfo($"Opening scene {scenePath}");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        private static void SceneSaved(Scene scene) => FinishedProcessingScene(scene);

        private static void SceneOpened(Scene scene)
        {
            if (!k_ScenesUpdated.Add(scene.path))
            {
                return;
            }

            LogInfo($"Scene {scene.name} was opened and is processing!");

            var networkObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

            foreach (var networkObject in networkObjects)
            {
                networkObject.OnValidate();
            }


            if (networkObjects.Length == 0 || !EditorSceneManager.MarkSceneDirty(scene))
            {
                LogInfo($"Scene {scene.name} did not get marked as dirty!");
                FinishedProcessingScene(scene);
            }
            else
            {
                LogInfo($"Changes detected and applied!");
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void FinishedProcessingScene(Scene scene)
        {
            k_ScenesToUpdate.Remove(scene.path);

            if (scene != SceneManager.GetActiveScene())
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            if (k_ScenesToUpdate.Count == 0)
            {
                FinishedProcessingAll();
                return;
            }

            // Open next scene
            var scenePath = k_ScenesToUpdate.First();
            OpenScene(scenePath);
        }

        private static void FinishedProcessingAll()
        {
            // Unregister callbacks
            EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneSaved -= SceneSaved;

            s_State = State.Finished;

        }
    }
}
