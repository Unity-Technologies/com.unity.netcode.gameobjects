using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.SceneManagement;
using NUnit;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

using UnityEditor;

namespace MLAPI.RuntimeTests
{
    [TestFixture]
    public class SceneLoadingTest : IPrebuildSetup, IPostBuildCleanup
    {
        // Setup the test
        public void Setup()
        {

            var scenes = new List<EditorBuildSettingsScene>();
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/UnitTestScenes" });
            if (guids != null)
            {
                foreach (string guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        var scene = new EditorBuildSettingsScene(path, true);
                        scenes.Add(scene);
                    }
                }
            }
            Debug.Log("Adding test scenes to build settings:\n" + string.Join("\n", scenes.Select(scene => scene.path)));
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Union(scenes).ToArray();

        }

        private NetworkManager m_NetworkManager;

        [SetUp]
        public void SetUpTest()
        {
            //Create, instantiate, and host
            //NetworkManagerHelper.StartNetworkManager(out NetworkManager networkManager, NetworkManagerHelper.NetworkManagerOperatingMode.None);
            
        }

        private bool m_SceneLoaded;
        private bool m_HadErrors;
        private Scene m_TargetScene;

        [UnityTest]
        public IEnumerator SceneLoading()
        {
            Debug.Log($"Scenes in BuildSettings:");
            foreach (var entry in EditorBuildSettings.scenes)
            {
                Debug.Log($"Scene {entry.path}");
            }

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;            
            SceneManager.LoadScene("SceneLoadingTest");

            while(!m_SceneLoaded && !m_HadErrors)
            {
                yield return new WaitForSeconds(0.01f);
            }

            if (SceneManager.GetActiveScene().name != "SceneLoadingTest")
            {
                SceneManager.SetActiveScene(m_TargetScene);
            }

            var gameObject = GameObject.Find("NetworkManager");

            Assert.IsNotNull(gameObject);

            m_NetworkManager = gameObject.GetComponent<NetworkManager>();

            Assert.IsNotNull(m_NetworkManager);


            Assert.IsNotNull(m_NetworkManager);
            if (m_NetworkManager)
            {
                m_NetworkManager.StartHost();
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if(arg0 != null && arg0.name == "SceneLoadingTest")
            {
                m_SceneLoaded = true;
                m_TargetScene = arg0;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if(m_NetworkManager != null)
            {
                m_NetworkManager.StopHost();
            }
        }

        // Cleanup the test
        public void Cleanup()
        {

            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(scene => !scene.path.StartsWith("Assets/UnitTestScenes")).ToArray();
        }
    }
}
