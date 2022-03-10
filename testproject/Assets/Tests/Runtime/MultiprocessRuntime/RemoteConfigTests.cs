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
    [TestFixture(ConfigType.Commandline)]
	[TestFixture(ConfigType.Remoteconfig)]
	[TestFixture(ConfigType.Resourcefile)]
    public class RemoteConfigTests
    {
        private string m_FullpathToApp;
        private bool m_HasSceneLoaded;

        [UnitySetUp]
        public IEnumerator UnitySetup()
        {
            Debug.Log("UnitySetup - ");
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
		
        public RemoteConfigTests(ConfigType configType)
        {
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
                m_FullpathToApp = BuildMultiNodePlayer.BuildPath + ".exe";
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
                remoteConfig.HostIp = "0.0.0.0";
                remoteConfig.GitHash = localGitHash;
                remoteConfig.PlatformId = 3;
                remoteConfig.JobStateId = 1;
                remoteConfig.TransportName = "UNET";
                remoteConfig.AdditionalJsonConfig = JsonUtility.ToJson(mpConfig);
                string s = JsonUtility.ToJson(remoteConfig);
                Debug.Log(s);
                Task<HttpResponseMessage> t = RemoteConfigUtils.PostBasicAsync(s);
                t.Wait();
                t.Result.EnsureSuccessStatusCode();

            }
            else if (configType == ConfigType.Resourcefile)
            {
                var remoteConfig = new RemoteConfig();
                var mpConfig = new MultiprocessConfig();
                mpConfig.MultiplayerMode = MultiplayerMode.Host;
                mpConfig.SceneName = "MultiprocessTestScene";
                remoteConfig.HostIp = "0.0.0.0";
                remoteConfig.TransportName = "UNET";
                remoteConfig.AdditionalJsonConfig = JsonUtility.ToJson(mpConfig);
                string s = JsonUtility.ToJson(remoteConfig);
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "server_config"), s);
            }
        }

        [UnityTest]
        public IEnumerator TestConfigIsApplied()
        {
            Debug.Log("Remote config - TestConfigIsApplied");
            // Assert.AreEqual(SceneManager.GetActiveScene().name, "MultiprocessTestScene");
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals("MultiprocessTestScene"));
        }
    }

    public enum ConfigType
    {
        Commandline,
        Remoteconfig,
        Resourcefile
    }
}
