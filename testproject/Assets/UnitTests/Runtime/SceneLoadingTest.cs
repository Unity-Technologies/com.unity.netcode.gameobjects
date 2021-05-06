using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using MLAPI;

public class SceneLoadingTest
{
    private NetworkManager m_NetworkManager;

    private bool m_SceneLoaded;
    private bool m_timedOut;
    private string m_TargetSceneNameToLoad;
    private Scene m_LoadedScene;

    [UnityTest]
    public IEnumerator SceneLoading()
    {
        // Load the first scene with the predefined NetworkManager
        //var execAssembly = Assembly.GetExecutingAssembly();
        //var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
        //var scenePath = Path.Combine(packagePath, "Assets/ManualTests/SceneTransitioning/SceneTransitioningTest.unity");           
        //m_TargetSceneNameToLoad = scenePath;
        m_TargetSceneNameToLoad = "Assets/ManualTests/SceneTransitioning/SceneTransitioningTest.unity";
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.LoadScene(m_TargetSceneNameToLoad);
        //EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));

        //Wait for the scene to load
        var timeOut = Time.realtimeSinceStartup + 5;
        m_timedOut = false;
        while(!m_SceneLoaded)
        {
            yield return new WaitForSeconds(0.01f);
            if(timeOut < Time.realtimeSinceStartup)
            {
                m_timedOut = true;
                break;
            }
        }

        Assert.IsFalse(m_timedOut);
        Assert.IsTrue(m_SceneLoaded);
        Assert.NotNull(m_LoadedScene);

        // Set the scene to be active if it is not the active scene
        if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
        {
            Debug.Log($"Loaded scene not active, activating scene {m_TargetSceneNameToLoad}");
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
        //m_NetworkManager.SceneManager.AddRuntimeSceneName("SecondSceneToLoad", (uint)m_NetworkManager.SceneManager.RegisteredSceneNames.Count);
        m_TargetSceneNameToLoad = "SecondSceneToLoad"; 
        // Override NetworkSceneManager's scene loading 
        m_NetworkManager.SceneManager.OverrideLoadSceneAsync = TestRunnerSceneLoadingOverride;
        var switchSceneProgress = m_NetworkManager.SceneManager.SwitchScene(m_TargetSceneNameToLoad);
        switchSceneProgress.OnComplete += SwitchSceneProgress_OnComplete;
        m_SceneLoaded = false;

        //Wait for the scene to load
        timeOut = Time.realtimeSinceStartup + 5;
        m_timedOut = false;
        while (!m_SceneLoaded)
        {
            yield return new WaitForSeconds(0.01f);
            if (timeOut < Time.realtimeSinceStartup)
            {
                m_timedOut = true;
                break;
            }
        }

        //Make sure we didn't time out and the scene loaded
        Assert.IsFalse(m_timedOut);
        Assert.IsTrue(m_SceneLoaded);
        Assert.NotNull(m_LoadedScene);

        // Set the scene to be active if it is not the active scene
        if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
        {
            Debug.Log($"Loaded scene not active, activating scene {m_TargetSceneNameToLoad}");
            SceneManager.SetActiveScene(m_LoadedScene);
            Assert.IsTrue(SceneManager.GetActiveScene().name == m_LoadedScene.name);
        }
            
    }

    private void SwitchSceneProgress_OnComplete(bool timedOut)
    {
        m_timedOut = timedOut;
        if (!m_timedOut)
        {
            var scene = SceneManager.GetActiveScene();

            if (scene != null && m_TargetSceneNameToLoad.Contains(scene.name))
            {
                m_SceneLoaded = true;
                m_LoadedScene = scene;
            }
        }
    }

    /// <summary>
    /// Overrides what NetworkSceneManager uses to load scenes
    /// </summary>
    /// <param name="targetscene">string</param>
    /// <param name="loadSceneMode">LoadSceneMode</param>
    /// <returns>AsyncOperation</returns>
    public AsyncOperation TestRunnerSceneLoadingOverride(string targetscene, LoadSceneMode loadSceneMode)
    {

        var sceneToBeLoaded = targetscene;
        if (!targetscene.Contains("Assets/ManualTests/SceneTransitioning/"))
        {
            //var execAssembly = Assembly.GetExecutingAssembly();
            //var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
            sceneToBeLoaded = $"Assets/ManualTests/SceneTransitioning/{targetscene}.unity";
        }
        //m_TargetSceneNameToLoad = sceneToBeLoaded;
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
