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
    private bool m_TimedOut;
    private string m_TargetSceneNameToLoad;
    private Scene m_LoadedScene;

    [UnityTest]
    public IEnumerator SceneLoading()
    {
        // Load the first scene with the predefined NetworkManager
        //var execAssembly = Assembly.GetExecutingAssembly();
        //var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
        //var scenePath = Path.Combine(packagePath, "Assets/Tests/ManualTests/SceneTransitioning/SceneTransitioningTest.unity");
        Scene originalScene = SceneManager.GetActiveScene();
        m_TargetSceneNameToLoad = "Assets/Tests/Manual/SceneTransitioning/SceneTransitioningTest.unity";
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.LoadScene(m_TargetSceneNameToLoad,LoadSceneMode.Additive);

        //Wait for the scene to load
        var timeOut = Time.realtimeSinceStartup + 5;
        m_TimedOut = false;
        while(!m_SceneLoaded)
        {
            yield return new WaitForSeconds(0.01f);
            if(timeOut < Time.realtimeSinceStartup)
            {
                m_TimedOut = true;
                break;
            }
        }

        Assert.IsFalse(m_TimedOut);
        Assert.IsTrue(m_SceneLoaded);
        Assert.NotNull(m_LoadedScene);

        Scene primaryScene = m_LoadedScene;
        // Set the scene to be active if it is not the active scene
        if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
        {
            primaryScene = m_LoadedScene;
            Debug.Log($"Loaded scene not active, activating scene {m_TargetSceneNameToLoad}");
            SceneManager.SetActiveScene(m_LoadedScene);
            Assert.IsTrue(SceneManager.GetActiveScene().name == m_LoadedScene.name);
        }
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

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
        primaryScene = SceneManager.GetActiveScene();
        var switchSceneProgress = m_NetworkManager.SceneManager.SwitchScene(m_TargetSceneNameToLoad);
        switchSceneProgress.OnComplete += SwitchSceneProgress_OnComplete;
        m_SceneLoaded = false;

        //Wait for the scene to load
        timeOut = Time.realtimeSinceStartup + 5;
        m_TimedOut = false;
        while (!m_SceneLoaded)
        {
            yield return new WaitForSeconds(0.01f);
            if (timeOut < Time.realtimeSinceStartup)
            {
                m_TimedOut = true;
                break;
            }
        }

        //Make sure we didn't time out and the scene loaded
        Assert.IsFalse(m_TimedOut);
        Assert.IsTrue(m_SceneLoaded);
        Assert.NotNull(m_LoadedScene);
        switchSceneProgress.OnComplete -= SwitchSceneProgress_OnComplete;

        // Set the scene to be active if it is not the active scene
        if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
        {
            Debug.Log($"Loaded scene not active, activating scene {m_TargetSceneNameToLoad}");
            SceneManager.SetActiveScene(m_LoadedScene);
            Assert.IsTrue(SceneManager.GetActiveScene().name == m_LoadedScene.name);         
        }
        if (originalScene != primaryScene)
        {
            SceneManager.UnloadSceneAsync(primaryScene).completed += UnloadAsync_completed;
        }
        primaryScene = SceneManager.GetActiveScene();
        //Wait for the scene to unload
        timeOut = Time.realtimeSinceStartup + 5;
        while (!m_SceneLoaded)
        {
            yield return new WaitForSeconds(0.01f);
            if (timeOut < Time.realtimeSinceStartup)
            {
                m_TimedOut = true;
                break;
            }
        }
        
        m_NetworkManager.DontDestroy = false;
        m_NetworkManager.StopHost();
   
        m_NetworkManager = null;

        SceneManager.SetActiveScene(originalScene);
        var unloadAsync = SceneManager.UnloadSceneAsync(primaryScene);
        unloadAsync.completed += UnloadAsync_completed;
        //Wait for the scene to unload
        timeOut = Time.realtimeSinceStartup + 5;
        while (!m_SceneLoaded)
        {
            yield return new WaitForSeconds(0.01f);
            if (timeOut < Time.realtimeSinceStartup)
            {
                m_TimedOut = true;
                break;
            }
        }

    }

    private void UnloadAsync_completed(AsyncOperation obj)
    {
        m_SceneLoaded = true;
    }

    private void SwitchSceneProgress_OnComplete(bool timedOut)
    {
        m_TimedOut = timedOut;
        if (!m_TimedOut)
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
            sceneToBeLoaded = $"Assets/Tests/Manual/SceneTransitioning/{targetscene}.unity";
        }
        //m_TargetSceneNameToLoad = sceneToBeLoaded;
        return SceneManager.LoadSceneAsync(sceneToBeLoaded, LoadSceneMode.Additive);
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
