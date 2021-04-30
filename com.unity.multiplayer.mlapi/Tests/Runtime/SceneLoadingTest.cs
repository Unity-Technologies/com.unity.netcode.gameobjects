using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using UnityEditor;
using MLAPI.SceneManagement;

namespace MLAPI.RuntimeTests
{
    [TestFixture]
    public class SceneLoadingTest
    {
        private NetworkManager m_NetworkManager;

        private bool m_SceneLoaded;
        private bool m_HadErrors;
        private string m_TargetSceneNameToLoad;
        private Scene m_LoadedScene;

        [UnityTest]
        public IEnumerator SceneLoading()
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
            var scenePath = Path.Combine(packagePath, "Tests/Runtime/TestScenes/SceneLoadingTest.unity");           

            yield return new WaitForSeconds(0.1f);
            m_TargetSceneNameToLoad = scenePath;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));

            while(!m_SceneLoaded && !m_HadErrors)
            {
                yield return new WaitForSeconds(0.01f);
            }

            Assert.IsTrue(m_SceneLoaded);
            Assert.IsTrue(!m_HadErrors);

            if (SceneManager.GetActiveScene().name != "SceneLoadingTest")
            {
                Debug.Log($"Loaded scene not active, activating scene: {scenePath}");
                SceneManager.SetActiveScene(m_LoadedScene);
            }

            var gameObject = GameObject.Find("NetworkManager");

            Assert.IsNotNull(gameObject);

            m_NetworkManager = gameObject.GetComponent<NetworkManager>();

            Assert.IsNotNull(m_NetworkManager);

            if (m_NetworkManager)
            {
                m_NetworkManager.NetworkConfig.AllowRuntimeSceneChanges = true;
                m_NetworkManager.StartHost();
            }
            m_NetworkManager.SceneManager.AddRuntimeSceneName("SceneToLoad", (uint)m_NetworkManager.SceneManager.RegisteredSceneNames.Count);

           yield return new WaitForSeconds(0.1f);
            m_NetworkManager.SceneManager.OverrideLoadSceneAsync = TestRunerSceneLoadingOverride;
            m_NetworkManager.SceneManager.SwitchScene("SceneToLoad");
            m_SceneLoaded = false;
            m_HadErrors = false;

            while (!m_SceneLoaded && !m_HadErrors)
            {
                yield return new WaitForSeconds(0.01f);
            }

            Assert.IsTrue(m_SceneLoaded);
            Assert.IsTrue(!m_HadErrors);

        }

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

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if(arg0 != null && m_TargetSceneNameToLoad.Contains(arg0.name))
            {
                m_SceneLoaded = true;
                m_LoadedScene = arg0;
            }
        }
    }
}
