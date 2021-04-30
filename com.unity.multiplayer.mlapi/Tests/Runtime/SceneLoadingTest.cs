using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace MLAPI.RuntimeTests
{
    public class SceneLoadingTest
    {
        private NetworkManager m_NetworkManager;

        private bool m_SceneLoaded;
        private string m_TargetSceneNameToLoad;
        private Scene m_LoadedScene;

        [UnityTest]
        public IEnumerator SceneLoading()
        {
            // Load the first scene with the predefined NetworkManager
            var execAssembly = Assembly.GetExecutingAssembly();
            var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
            var scenePath = Path.Combine(packagePath, "Tests/Runtime/TestScenes/SceneLoadingTest.unity");           
            m_TargetSceneNameToLoad = scenePath;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));

            //Wait for the scene to load
            var timeOut = Time.realtimeSinceStartup + 2;
            var timedOut = false;
            while(!m_SceneLoaded)
            {
                yield return new WaitForSeconds(0.01f);
                if(timeOut < Time.realtimeSinceStartup)
                {
                    timedOut = true;
                    break;
                }
            }

            Assert.IsFalse(timedOut);
            Assert.IsTrue(m_SceneLoaded);
            Assert.NotNull(m_LoadedScene);

            // Set the scene to be active if it is not the active scene
            if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
            {
                Debug.Log($"Loaded scene not active, activating scene: {scenePath}");
                SceneManager.SetActiveScene(m_LoadedScene);
                Assert.IsTrue(SceneManager.GetActiveScene().name == m_LoadedScene.name);
            }

            // Get the NetworkManager instantiated from the scene
            var gameObject = GameObject.Find("NetworkManager");

            Assert.IsNotNull(gameObject);

            m_NetworkManager = gameObject.GetComponent<NetworkManager>();

            Assert.IsNotNull(m_NetworkManager);

            // Start in host mode
            if (m_NetworkManager)
            {
                m_NetworkManager.StartHost();
            }

            // Add any additional scenes we want to load here (just create the next index number for the index value)
            m_NetworkManager.SceneManager.AddRuntimeSceneName("SceneToLoad", (uint)m_NetworkManager.SceneManager.RegisteredSceneNames.Count);

            // Override NetworkSceneManager's scene loading 
            m_NetworkManager.SceneManager.OverrideLoadSceneAsync = TestRunerSceneLoadingOverride;
            m_NetworkManager.SceneManager.SwitchScene("SceneToLoad");
            m_SceneLoaded = false;

            //Wait for the scene to load
            timeOut = Time.realtimeSinceStartup + 2;
            timedOut = false;
            while (!m_SceneLoaded)
            {
                yield return new WaitForSeconds(0.01f);
                if (timeOut < Time.realtimeSinceStartup)
                {
                    timedOut = true;
                    break;
                }
            }

            //Make sure we didn't time out and the scene loaded
            Assert.IsFalse(timedOut);
            Assert.IsTrue(m_SceneLoaded);
            Assert.NotNull(m_LoadedScene);

            // Set the scene to be active if it is not the active scene
            if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
            {
                Debug.Log($"Loaded scene not active, activating scene: {scenePath}");
                SceneManager.SetActiveScene(m_LoadedScene);
                Assert.IsTrue(SceneManager.GetActiveScene().name == m_LoadedScene.name);
            }
            
        }

        /// <summary>
        /// Overrides what NetworkSceneManager uses to load scenes
        /// </summary>
        /// <param name="targetscene">string</param>
        /// <param name="loadSceneMode">LoadSceneMode</param>
        /// <returns>AsyncOperation</returns>
        public AsyncOperation TestRunerSceneLoadingOverride(string targetscene, LoadSceneMode loadSceneMode)
        {

            var sceneToBeLoaded = targetscene;
            if (!targetscene.Contains("Tests/Runtime/TestScenes/"))
            {
                var execAssembly = Assembly.GetExecutingAssembly();
                var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
                sceneToBeLoaded = Path.Combine(packagePath, $"Tests/Runtime/TestScenes/{targetscene}.unity");                
            }
            m_TargetSceneNameToLoad = sceneToBeLoaded;
            return EditorSceneManager.LoadSceneAsyncInPlayMode(sceneToBeLoaded, new LoadSceneParameters(loadSceneMode));
        }

        /// <summary>
        /// When invoked, makes sure the scene loaded is the correct scene
        /// and then set our scene loaded to true and keep a reference to the loaded scene
        /// </summary>
        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if(scene != null && m_TargetSceneNameToLoad.Contains(scene.name))
            {
                m_SceneLoaded = true;
                m_LoadedScene = scene;
            }
        }
    }
}
