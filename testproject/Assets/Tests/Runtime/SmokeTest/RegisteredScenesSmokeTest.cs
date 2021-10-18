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
    public class RegisteredScenesSmokeTest : SmokeTestState
    {
        private const string k_MainMenuScene = "MainMenu";

        private List<string> m_SceneReferenced;
        private List<Scene> m_LoadedScenes;

        private string m_SceneBeingProcessed;
        private bool m_SceneIsProcessed;

        internal delegate void OnCollectedRegisteredScenesDelegateHandler(List<string> registeredSceneNames);
        internal event OnCollectedRegisteredScenesDelegateHandler OnCollectedRegisteredScenes;

        #region Start and Initialize
        private bool StartLoadingScene(string sceneName)
        {
            m_SceneBeingProcessed = sceneName;
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (asyncOp == null)
            {
                return false;
            }
            m_SceneIsProcessed = false;
            return true;
        }

        /// <summary>
        /// Find all scene registrations from the main menu down
        /// </summary>
        protected override IEnumerator OnStartState()
        {
            m_LoadedScenes = new List<Scene>();
            m_SceneReferenced = new List<string>();
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Assert.That(StartLoadingScene(k_MainMenuScene) == true);
            while (!m_SceneIsProcessed)
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
                while (!m_SceneIsProcessed)
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

            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

            yield return base.OnStartState();
        }

        private void SceneManager_sceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            if (loadMode == LoadSceneMode.Additive && sceneLoaded.name.Contains(m_SceneBeingProcessed))
            {
                m_LoadedScenes.Add(sceneLoaded);
                m_SceneIsProcessed = true;
            }
        }
        #endregion

        #region Process/Validate Registered Scenes
        internal string GetSceneNameFromPath(string scenePath)
        {
            var begin = scenePath.LastIndexOf("/", System.StringComparison.Ordinal) + 1;
            var end = scenePath.LastIndexOf(".", System.StringComparison.Ordinal);
            return scenePath.Substring(begin, end - begin);
        }

        private List<string> GetSceneNamesFromBuildSettings()
        {
            var sceneNamesInBuildSettings = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                sceneNamesInBuildSettings.Add(GetSceneNameFromPath(SceneUtility.GetScenePathByBuildIndex(i)));
            }
            return sceneNamesInBuildSettings;
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
            foreach (var sceneName in m_SceneReferenced)
            {
                Assert.That(sceneNamesInBuildSettings.Contains(sceneName));
            }
            return base.OnProcessState();
        }
        #endregion

        #region Finalize and Unload
        private bool StartUnloadingScene(Scene sceneToUnload)
        {
            m_SceneBeingProcessed = sceneToUnload.name;
            var asyncOp = SceneManager.UnloadSceneAsync(sceneToUnload);
            if (asyncOp == null)
            {
                return false;
            }

            m_SceneIsProcessed = false;
            return true;
        }

        private IEnumerator UnloadScenes()
        {
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

            foreach (var sceneToUnload in m_LoadedScenes)
            {
                Assert.That(StartUnloadingScene(sceneToUnload) == true);

                while (!m_SceneIsProcessed)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
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

        private void SceneManager_sceneUnloaded(Scene sceneUnloaded)
        {
            if (sceneUnloaded.name == m_SceneBeingProcessed)
            {
                m_SceneIsProcessed = true;
            }
        }
        #endregion
    }
}
