using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(ConfigType.Commandline)]
	[TestFixture(ConfigType.Remoteconfig)]
	[TestFixture(ConfigType.Resourcefile, "UNET")]
    [TestFixture(ConfigType.Resourcefile, "UTP")]
    public class RemoteConfigTests
    {
        private string m_FullpathToApp;
        private bool m_HasSceneLoaded;
        private string m_Transport;

        [UnitySetUp]
        public IEnumerator UnitySetup()
        {
            Debug.Log("UnitySetup - ");

            var remoteConfig = new RemoteConfig();
            var mpConfig = new MultiprocessConfig();
            mpConfig.IsServer = true;
            mpConfig.SceneName = "MultiprocessTestScene";
            remoteConfig.HostIp = "0.0.0.0";
            remoteConfig.TransportName = m_Transport;
            remoteConfig.AdditionalJsonConfig = JsonUtility.ToJson(mpConfig);
            string s = JsonUtility.ToJson(remoteConfig);
            Debug.Log(Application.streamingAssetsPath);
            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "server_config"), s);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene("RemoteConfigScene");
            yield return new WaitUntil(() => m_HasSceneLoaded == true);
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Switched to scene {scene.name}");
            m_HasSceneLoaded = true;
        }

        public RemoteConfigTests()
        {
            Debug.Log($"Remote config - default constructor");
        }
		
        public RemoteConfigTests(ConfigType configType, string transport)
        {
            Debug.Log($"Remote config {configType}, {transport}");
            m_Transport = transport;
            if (configType == ConfigType.Remoteconfig)
            {
                // Path to build
                m_FullpathToApp = BuildMultiNodePlayer.BuildPath + ".exe";
                
            }
        }

        [UnityTest]
        public IEnumerator TestSceneIsLoaded()
        {
            Debug.Log("Remote config - TestConfigIsApplied");
            var f = new FileInfo(m_FullpathToApp);
            Assert.IsTrue(f.Exists);
            // Assert.AreEqual(SceneManager.GetActiveScene().name, "MultiprocessTestScene");
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals("MultiprocessTestScene"));
        }

        [UnityTest]
        public IEnumerator TestServerIsStarted()
        {
            // By the time the test case runs the server should be up and running
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals("MultiprocessTestScene"));
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            Debug.Log(transport);
            Assert.AreEqual(transport, m_Transport);
        }
    }

    public enum ConfigType
    {
        Commandline,
        Remoteconfig,
        Resourcefile
    }
}
