using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Checks to make sure all scene registrations
    /// for the menu system are included in the
    /// Build Settings Scenes in Build list.
    /// </summary>
    public class RegisteredSceneValidationState : SceneAwareSmokeTestState
    {
        private const string k_MainMenuScene = "MainMenu";

        private List<List<string>> m_SceneReferenced;
        private List<Scene> m_LoadedScenes;

        internal delegate void OnCollectedRegisteredScenesDelegateHandler(List<List<string>> registeredSceneNames);
        internal event OnCollectedRegisteredScenesDelegateHandler OnCollectedRegisteredScenes;

        /// <summary>
        /// Called when a scene is loaded
        /// </summary>
        /// <param name="sceneLoaded"></param>
        /// <param name="loadMode"></param>
        /// <returns></returns>
        public override bool OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            if (loadMode == LoadSceneMode.Additive && sceneLoaded.name.Contains(SceneBeingProcessed))
            {
                m_LoadedScenes.Add(sceneLoaded);
                SceneIsProcessed = true;
            }
            return SceneIsProcessed;
        }

        /// <summary>
        /// Find all scene registrations from the main menu down
        /// </summary>
        protected override IEnumerator OnStartState()
        {
            m_LoadedScenes = new List<Scene>();
            m_SceneReferenced = new List<List<string>>();

            Assert.That(StartLoadingScene(k_MainMenuScene) == true);
            while (!SceneIsProcessed)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.That(ManualTests.MenuManagerReferences.GetMenuMaangers().Count > 0);
            var menuScenes = ManualTests.MenuManagerReferences.GetMenuMaangers()[0].GetAllMenuScenes();
            Assert.That(menuScenes != null && menuScenes.Count > 0);

            yield return UnloadScenes();

            var sceneMenuManagerCount = ManualTests.MenuManagerReferences.GetSceneMenuManagers().Count;
            foreach (var sceneToLoad in menuScenes)
            {
                Assert.That(StartLoadingScene(sceneToLoad) == true);
                while (!SceneIsProcessed)
                {
                    yield return new WaitForSeconds(0.1f);
                }

                // Making sure we are always adding another scene menu manager to the list as we load the
                // sub-menus
                var newCount = ManualTests.MenuManagerReferences.GetSceneMenuManagers().Count;
                Assert.That(newCount > sceneMenuManagerCount);
                var currentCount = m_SceneReferenced.Count;
                foreach (var sceneMenuManager in ManualTests.MenuManagerReferences.GetSceneMenuManagers())
                {
                    m_SceneReferenced.AddRange(sceneMenuManager.GetAllSceneReferences());
                }
                Assert.That(currentCount < m_SceneReferenced.Count);
                yield return UnloadScenes();
            }

            yield return base.OnStartState();
        }

        /// <summary>
        /// Compare the scene registration scenes with the Build Settings
        /// Scenes in Build list to verify all scenes referenced in menus
        /// can be loaded.
        /// </summary>
        /// <returns></returns>
        protected override bool OnProcessState()
        {
            var sceneNamesInBuildSettings = GetSceneNamesFromBuildSettings();
            foreach (var sceneGroup in m_SceneReferenced)
            {
                foreach (var sceneName in sceneGroup)
                {
                    Assert.That(sceneNamesInBuildSettings.Contains(sceneName));
                }
            }
            return base.OnProcessState();
        }

        /// <summary>
        /// Unloads all loaded scenes
        /// </summary>
        private IEnumerator UnloadScenes()
        {
            foreach (var sceneToUnload in m_LoadedScenes)
            {
                Assert.That(StartUnloadingScene(sceneToUnload) == true);
                while (!SceneIsProcessed)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            m_LoadedScenes.Clear();
            yield return null;
        }

        /// <summary>
        /// Unload all of the loaded scenes
        /// </summary>
        protected override IEnumerator OnStopState()
        {
            yield return UnloadScenes();
            OnCollectedRegisteredScenes?.Invoke(m_SceneReferenced);
            m_SceneReferenced.Clear();

            yield return base.OnStopState();
        }

        /// <summary>
        /// Called when a scene is unloaded
        /// </summary>
        /// <param name="sceneUnloaded"></param>
        /// <returns></returns>
        protected override bool OnSceneUnloaded(Scene sceneUnloaded)
        {
            if (sceneUnloaded.name == SceneBeingProcessed)
            {
                SceneIsProcessed = true;
            }
            return SceneIsProcessed;
        }
    }
}
