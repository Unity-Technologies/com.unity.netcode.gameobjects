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
        //Keep track of the original scene
        Scene originalScene = SceneManager.GetActiveScene();

        // Load the first scene with the predefined NetworkManager
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

        //Verify it loaded
        Assert.IsFalse(m_TimedOut);
        Assert.IsTrue(m_SceneLoaded);
        Assert.NotNull(m_LoadedScene);

        // Keep track of the scene we just loaded
        Scene primaryScene = m_LoadedScene;

        // Set the scene to be active if it is not the active scene.
        // (This is to assure spawned objects instantiate in the newly loaded scene)
        if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
        {
            primaryScene = m_LoadedScene;
            Debug.Log($"Loaded scene not active, activating scene {m_TargetSceneNameToLoad}");
            SceneManager.SetActiveScene(m_LoadedScene);
            Assert.IsTrue(SceneManager.GetActiveScene().name == m_LoadedScene.name);
        }

        // No longer need to be notified of this event
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

        // [Reference]: Add any additional scenes we want to load here (just create the next index number for the index value)
        //m_NetworkManager.SceneManager.AddRuntimeSceneName("SecondSceneToLoad", (uint)m_NetworkManager.SceneManager.RegisteredSceneNames.Count);

        // Next, we want to do a scene transition using NetworkSceneManager
        m_TargetSceneNameToLoad = "SecondSceneToLoad";

        // Override NetworkSceneManager's scene loading method
        // (this is a temporary work around for NetworkSceneManager expecting just the name)
        // (this issue will be addressed when we overhaul the NetworkSceneManager)
        m_NetworkManager.SceneManager.OverrideLoadSceneAsync = TestRunnerSceneLoadingOverride;

        // Store off the currently active scene so we can unload it
        primaryScene = SceneManager.GetActiveScene();

        // Switch the scene using NetworkSceneManager
        var switchSceneProgress = m_NetworkManager.SceneManager.SwitchScene(m_TargetSceneNameToLoad);
        switchSceneProgress.OnComplete += SwitchSceneProgress_OnComplete;
        m_SceneLoaded = false;

        // Wait for the scene to load
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

        // Make sure we didn't time out and the scene loaded
        Assert.IsFalse(m_TimedOut);
        Assert.IsTrue(m_SceneLoaded);
        Assert.NotNull(m_LoadedScene);

        switchSceneProgress.OnComplete -= SwitchSceneProgress_OnComplete;

        // Set the scene to be active if it is not the active scene
        // (This is to assure spawned objects instantiate in the newly loaded scene)
        if (SceneManager.GetActiveScene().name != m_LoadedScene.name)
        {
            Debug.Log($"Loaded scene not active, activating scene {m_TargetSceneNameToLoad}");
            SceneManager.SetActiveScene(m_LoadedScene);
            Assert.IsTrue(SceneManager.GetActiveScene().name == m_LoadedScene.name);         
        }

        // Now unload the previous scene
        SceneManager.UnloadSceneAsync(primaryScene).completed += UnloadAsync_completed;

        // Now track the newly loaded and currently active scene
        primaryScene = SceneManager.GetActiveScene();

        // Wait for the previous scene to unload
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

        // We are now done with the NetworkSceneManager switch scene test so stop the host
        m_NetworkManager.StopHost();           

        // Set the original Test Runner Scene to be the active scene
        SceneManager.SetActiveScene(originalScene);

        // Unload the previously active scene
        SceneManager.UnloadSceneAsync(primaryScene).completed += UnloadAsync_completed;
        
        // Wait for the scene to unload
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
        // Done!
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
            sceneToBeLoaded = $"Assets/Tests/Manual/SceneTransitioning/{targetscene}.unity";
        }
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
