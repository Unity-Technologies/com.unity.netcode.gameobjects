using System.Net.Http;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(ConfigType.Remoteconfig, "UNET")]
	[TestFixture(ConfigType.Resourcefile, "UNET")]
    [TestFixture(ConfigType.Resourcefile, "UTP")]
    public class RemoteConfigTests
    {
        private bool m_HasSceneLoaded;
        private string m_Transport;
        private Scene m_OriginalScene;
        private static Scene s_InitScene;
        private Task<HttpResponseMessage> m_SetupTask;
        private Task m_WrapperTask;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Debug.Log($"SceneManager current active scene name {SceneManager.GetActiveScene().name}");
        }

        [UnitySetUp]
        public IEnumerator UnitySetup()
        {
            Debug.Log("UnitySetup - Start");
            yield return new WaitUntil(() => RemoteDataLoader1.PostMessageStatus == PostMessageStatus.Complete);
            var startupScene = SceneManager.GetActiveScene();
            
            if (startupScene == null)
            {
                Debug.Log("Start up - Scene is null");
            }
            else
            {
                Debug.Log($"UnitySetup - Startup scene is {startupScene.name} {startupScene.IsValid()} {startupScene}");
                if (startupScene.name.Contains("Init"))
                {
                    s_InitScene = startupScene;
                }
            }

            if (m_OriginalScene != null)
            {
                Debug.Log($"On UnitySetup there was an original scene of {m_OriginalScene.name} " +
                    $" and its active state was {m_OriginalScene.isLoaded} {m_OriginalScene.IsValid()}");
            }
            
            SceneManager.sceneLoaded += RemoteConfigTestsOnSceneLoaded;
            SceneManager.LoadScene("RemoteConfigScene", LoadSceneMode.Additive);
            yield return new WaitUntil(() => m_HasSceneLoaded == true);
        }

        [SetUp]
        public void Setup()
        {
            Debug.Log("Setup - Start");
            
            //SceneManager.sceneLoaded -= RemoteConfigTestsOnSceneLoaded;
        }

        public void RemoteConfigTestsOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"RemoteConfigTestsOnSceneLoaded - Switched to scene: {scene.name}");
            
            if (scene.name.Equals("RemoteConfigScene"))
            {
                SceneManager.SetActiveScene(scene);
                m_HasSceneLoaded = true;
                m_OriginalScene = scene;
            }
            
        }

        public RemoteConfigTests()
        {
            Debug.Log($"Remote config - default constructor");
        }

        private void Init(ConfigType configType, string transport)
        {
            Debug.Log($"Remote config {configType}, {transport}");
            m_Transport = transport;
            Debug.Log($"config Type: {configType}");
            var workerProcess = RemoteDataLoader1.CallGitToGetHash();
            workerProcess.Start();
            workerProcess.WaitForExit();
            Task<string> outputTask = workerProcess.StandardOutput.ReadToEndAsync();
            outputTask.Wait();
            var localGitHash = outputTask.Result.Trim();
            if (configType == ConfigType.Remoteconfig)
            {
                // Path to build
                var server_config_file = new FileInfo(Path.Combine(Application.streamingAssetsPath, "server_config"));
                var server_config_metafile = new FileInfo(Path.Combine(Application.streamingAssetsPath, "server_config.eta"));
                if (server_config_file.Exists)
                {
                    server_config_file.Delete();
                }
                if (server_config_metafile.Exists)
                {
                    server_config_metafile.Delete();
                }
                var remoteConfig = new RemoteConfig();
                var mpConfig = new MultiprocessConfig();
                mpConfig.MultiplayerMode = MultiplayerMode.Host;
                mpConfig.SceneName = "MultiprocessTestScene";
                remoteConfig.CreatedBy = "Zain";
                remoteConfig.UpdatedBy = "Zain";
                remoteConfig.HostIp = "0.0.0.0";
                remoteConfig.GitHash = localGitHash;
                remoteConfig.PlatformId = (int)Application.platform;
                remoteConfig.JobStateId = 1;
                remoteConfig.TransportName = transport;
                remoteConfig.AdditionalJsonConfig = JsonUtility.ToJson(mpConfig);
                string s = JsonUtility.ToJson(remoteConfig);

                Debug.Log($"Calling RemoteConfigUtils.PostBasicAsync\n{s}");
                m_WrapperTask = Task.Factory.StartNew(() =>
                {
                    Debug.Log($"Calling RemoteConfigUtils.PostBasicAsync\n{s}");
                    RemoteConfigUtils.PostBasicAsync(s);
                    
                });
                m_WrapperTask.Wait();
                Debug.Log($"m_WrapperTask.Status {m_WrapperTask.Status}");

            }
            else if (configType == ConfigType.Resourcefile)
            {
                var remoteConfig = new RemoteConfig();
                var mpConfig = new MultiprocessConfig();
                mpConfig.MultiplayerMode = MultiplayerMode.Host;
                mpConfig.SceneName = "MultiprocessTestScene";
                remoteConfig.HostIp = "0.0.0.0";
                remoteConfig.GitHash = localGitHash;
                remoteConfig.PlatformId = (int)Application.platform;
                remoteConfig.TransportName = transport;
                remoteConfig.AdditionalJsonConfig = JsonUtility.ToJson(mpConfig);
                string s = JsonUtility.ToJson(remoteConfig);
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "server_config"), s);
            }
        }

        public RemoteConfigTests(ConfigType configType)
        {
            Init(configType, "UNET");
        }

        public RemoteConfigTests(ConfigType configType, string transport)
        {
            Init(configType, transport);
        }

        [UnityTest]
        public IEnumerator TestSceneIsLoaded()
        {
            Debug.Log("Remote config - TestConfigIsApplied");
            // Assert.AreEqual(SceneManager.GetActiveScene().name, "MultiprocessTestScene");
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals("MultiprocessTestScene"));
        }

        [UnityTest]
        public IEnumerator TestTransportIsSet()
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals("MultiprocessTestScene"));
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            if (m_Transport.Equals("UNET"))
            {
                Assert.AreEqual(transport.ToString(), "NetworkManager (Unity.Netcode.Transports.UNET.UNetTransport)");
            }
            else if (m_Transport.Equals("UTP"))
            {
                Assert.AreEqual(transport.ToString(), "NetworkManager (Unity.Netcode.UnityTransport)");
            }
        }

        [UnityTest]
        public IEnumerator TestServerIsStarted()
        {
            // By the time the test case runs the server should be up and running
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals("MultiprocessTestScene"));
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            Debug.Log($"{transport} {m_Transport}");
            Assert.AreEqual(
                transport.ToString(),
                m_Transport.Equals("UNET") ?
                "NetworkManager (Unity.Netcode.Transports.UNET.UNetTransport)" :
                "NetworkManager (Unity.Netcode.UnityTransport)");
            Assert.IsTrue(NetworkManager.Singleton.IsServer);
        }

        [TearDown]
        public void TearDown()
        {

        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            Debug.Log($"UnityTearDown - isValid: {m_OriginalScene.name} {m_OriginalScene.IsValid()} {SceneManager.GetActiveScene().name}");
            NetworkManager.Singleton.Shutdown(true);

            bool isok = SceneManager.SetActiveScene(s_InitScene);
            if (isok)
            {
                // Releasing these references should end the network manager
                RemoteDataLoader1.NetworkManagerObject = null;
                RemoteDataLoader1.NetworkManagerGameObject = null;
            }

            m_HasSceneLoaded = false;
            yield return new WaitForSeconds(1);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Debug.Log($"OneTimeTearDown - {s_InitScene}");
            if (s_InitScene != null)
            {
                Debug.Log($"OneTimeTearDown - {s_InitScene.name} valid: {s_InitScene.IsValid()} loaded: {s_InitScene.isLoaded}");
            }

            var x = SceneManager.CreateScene("OneTimeTearDownScene");
            Debug.Log($"{x.name} isValid:{x.IsValid()} isLoaded:{x.isLoaded}");
            SceneManager.SetActiveScene(x);

            var activeScene = SceneManager.GetActiveScene();
            Debug.Log($"activeScene: {activeScene.name} {activeScene.IsValid()} {activeScene.isLoaded}");

            var sceneCount = SceneManager.sceneCount;
            Debug.Log($"There are {sceneCount} scenes");

            for (int i = 0; i < sceneCount; i++)
            {
                var iterScene = SceneManager.GetSceneAt(i);
            }

            // SceneManager.sceneLoaded -= RemoteConfigTestsOnSceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
            var unloadOperation = SceneManager.UnloadSceneAsync(m_OriginalScene);
            // var unloadOperation = SceneManager.UnloadSceneAsync(m_OriginalScene);
            Debug.Log($"End of OneTimeTearDown with unloadOperation isnull? {unloadOperation}");
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            Debug.Log("Scene Unloaded: " + arg0.name);
        }
    }

    public enum ConfigType
    {
        Commandline,
        Remoteconfig,
        Resourcefile
    }
}
