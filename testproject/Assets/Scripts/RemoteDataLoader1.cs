using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode.Transports.UNET;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class RemoteDataLoader1 : MonoBehaviour
    {
        public RemoteConfig RemoteConfig;
        public MultiprocessConfig MultiprocessConfig;
        public static GameObject NetworkManagerGameObject { get; internal set; }
        public static NetworkManager NetworkManagerObject { get; internal set; }

        private int m_UpdateCounter;
        private bool m_PlatformSupportsLocalFiles;
        private List<Task> m_ListOfAsyncTasks;
        private bool m_MatchingConfigFound;
        private string m_LocalGitHash;
        private Task m_RemoteConfigTask;
        private TextAsset m_TextConfig;
        public static RemoteDataLoader1 Instance;

        public void Awake()
        {
            Debug.Log($"Awake {Application.streamingAssetsPath}");
            if (NetworkManagerGameObject == null)
            {
                NetworkManagerGameObject = new GameObject(nameof(NetworkManager));
                NetworkManagerObject = NetworkManagerGameObject.AddComponent<NetworkManager>();
                
            }
            GameObject[] objs = GameObject.FindGameObjectsWithTag("RemoteDataLoader");

            if (objs.Length > 1)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);

            m_MatchingConfigFound = false;
            m_ListOfAsyncTasks = new List<Task>();
            
            Application.targetFrameRate = 5;
            QualitySettings.vSyncCount = 0;

            // There are three categories of platform: desktop, mobile, console
            // Each category has certain constraints so it is important to know
            // which one we are
            var platform = Application.platform;
            switch (platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.WindowsEditor:
                    // Files allowed
                    m_PlatformSupportsLocalFiles = true;
                    break;
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    // No command line and no local files
                    m_PlatformSupportsLocalFiles = false;
                    break;
                default:
                    break;
            }
            Instance = this;
        }
        // Start is called before the first frame update
        public void Start()
        {
            m_TextConfig = Resources.Load<TextAsset>("Text/config");
            var githash = Resources.Load<TextAsset>("Text/githash");
            if (githash == null && m_PlatformSupportsLocalFiles)
            {
                Debug.Log("Resource file didn't have githash");
                // If the githash file has not been generated try to compute it
                ComputeLocalGitHash();
            }
            else if (githash != null && githash.text != null)
            {
                Debug.Log($"Resource file githash >{githash.text}<");
                m_LocalGitHash = githash.text;
            }
            else
            {
                if (githash == null)
                {
                    Debug.Log($"What condition is this? githash: {githash == null}");
                }
                else
                {
                    Debug.Log($"What condition is this? githash.text: {githash.text == null}");
                }
            }
        }

        // Update is called once per frame
        public void Update()
        {
            m_UpdateCounter++;
            if (m_LocalGitHash != null)
            {
                if (m_RemoteConfigTask == null || m_RemoteConfigTask.IsCompleted)
                {
                    m_RemoteConfigTask = CheckForMatchingRemoteConfig();
                }
            }

            if (m_MatchingConfigFound)
            {
                MultiprocessConfig = JsonUtility.FromJson<MultiprocessConfig>(RemoteConfig.AdditionalJsonConfig);
                PlayerPrefs.SetString("Transport", RemoteConfig.TransportName);
                if (RemoteConfig.TransportName.Equals("UNET"))
                {
                    var transport = NetworkManagerGameObject.AddComponent<UNetTransport>();
                    var networkConfig = new NetworkConfig();
                    
                    NetworkManagerObject.NetworkConfig = networkConfig;
                    NetworkManagerObject.NetworkConfig.NetworkTransport = transport;
                }
                

                UnityEngine.SceneManagement.SceneManager.LoadScene(MultiprocessConfig.SceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                string status = "";
                foreach (var task in m_ListOfAsyncTasks)
                {
                    status += " " + task.IsCompleted;
                }

                UnityEngine.UI.Text textObject = FindObjectOfType<UnityEngine.UI.Text>();

                textObject.text = $"{m_UpdateCounter}\n" +
                    $"Async tasks {m_ListOfAsyncTasks.Count} {status}\n" +
                    $"Local githash: {m_LocalGitHash} remote githash: {PlayerPrefs.GetString("GitHash")}\n" +
                    $"{PlayerPrefs.GetString("HostIp")}\n" +
                    $"{PlayerPrefs.GetString("JobId")}";
            }
        }

        public static System.Diagnostics.Process CallGitToGetHash()
        {
            var workerProcess = new System.Diagnostics.Process();
            workerProcess.StartInfo.UseShellExecute = false;
            workerProcess.StartInfo.RedirectStandardError = true;
            workerProcess.StartInfo.RedirectStandardOutput = true;
            workerProcess.StartInfo.FileName = "git";
            workerProcess.StartInfo.Arguments = "rev-parse HEAD";
            return workerProcess;
        }

        private Task ComputeLocalGitHash()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                var workerProcess = CallGitToGetHash();
                workerProcess.Start();
                workerProcess.WaitForExit();
                Task<string> outputTask = workerProcess.StandardOutput.ReadToEndAsync();
                outputTask.Wait();
                m_LocalGitHash = outputTask.Result.Trim();
                Debug.Log(m_LocalGitHash);
            });
            m_ListOfAsyncTasks.Add(t);
            return t;
        }

        private Task CheckForMatchingRemoteConfig()
        {
            if (m_LocalGitHash == null)
            {
                Debug.LogWarning("local githash was null so there's nothing to match to, hence returning");
                return null;
            }

            var t = Task.Factory.StartNew(() =>
            {
                Debug.Log("Task to Get Remote Config");
                RemoteConfig = RemoteConfigUtils.GetRemoteConfig(Version.v1, m_LocalGitHash, m_TextConfig);
                Debug.Log($"Remote GitHash was {RemoteConfig.GitHash} and local {m_LocalGitHash}");
                m_MatchingConfigFound = true;
                if (RemoteConfig.GitHash.Equals(PlayerPrefs.GetString("GitHash")))
                {
                // If the githash and platform are a match then we can pick up this job
                // TODO:
                    PlayerPrefs.SetInt("JobId", RemoteConfig.JobId);
                    PlayerPrefs.SetString("HostIp", RemoteConfig.HostIp);
                }
            });
            m_ListOfAsyncTasks.Add(t);
            return t;
        }
    }

    public class RemoteConfigUtils
    {
        public static RemoteConfig GetRemoteConfig(Version version, string localGitHash, TextAsset textAsset)
        {
            Debug.Log("GetRemoteConfig");
            // There are three sources of information
            // 1. Command Line
            // 2. Local file config
            // 3. Web config

            // This needs to be ordered carefully as this also represents the order of priority, when one is found the next item on the list is not regarded

            string configData = " x ";

            // -m(?) | Start network in one of 3 modes: client, host, server
            bool isCommandLine = Environment.GetCommandLineArgs().Any(value => value == "-m");

            
            Debug.Log($"isCommandLine {isCommandLine}");

            if (isCommandLine)
            {
                Debug.Log("Command line config");
            }
            else if (textAsset != null)
            {
                Debug.Log($"Config file is not null {textAsset.text}");
            }
            else if (File.Exists(Path.Combine(Application.streamingAssetsPath, "server_config")))
            {
                string s = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "server_config"));
                Debug.Log(s);
                var config = JsonUtility.FromJson<RemoteConfig>(s);
                var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(config.AdditionalJsonConfig);
                return config;
            }
            else
            {
                Debug.Log("There was no command line and no config file");
                // Try to get config from web resource
                configData = GetWebConfig();
                var remoteConfigList = new RemoteConfigList();
                JsonUtility.FromJsonOverwrite(configData, remoteConfigList);

                foreach (var config in remoteConfigList.JobQueueItems)
                {
                    if (config.GitHash != null && config.GitHash.Equals(localGitHash))
                    {
                        if (config.AdditionalJsonConfig != null)
                        {
                            var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(config.AdditionalJsonConfig);
                            if (mpConfig != null && mpConfig.SceneName != null)
                            {
                                return config;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static string GetWebConfig()
        {
            using var client = new HttpClient();

            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var responseTask = client.GetAsync("https://multiprocess-log-event-manager.cds.internal.unity3d.com/api/JobWithFile",
                HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token);

            responseTask.Wait();
            var response = responseTask.Result;
            var contentTask = response.Content.ReadAsStringAsync();
            contentTask.Wait();

            return contentTask.Result;
        }

        public static Task<HttpResponseMessage> PostBasicAsync(string content)
        {
            using var client = new HttpClient();
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://multiprocess-log-event-manager.cds.internal.unity3d.com/api/JobWithFile");
            using var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            request.Content = stringContent;
            Task<HttpResponseMessage> t = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token);
            return t;
        }
    }

    public enum Version
    {
        v1,
        v2
    }

    [Serializable]
    public class RemoteConfig
    {
        public int Id;
        public string GitHash;
        public int PlatformId;
        public int JobId;
        public string HostIp;
        public int JobStateId;
        public string TransportName;
        public string AdditionalJsonConfig;
    }

    [Serializable]
    public class RemoteConfigList
    {
        public List<RemoteConfig> JobQueueItems;
    }

    [Serializable]
    public class MultiprocessConfig
    {
        public string SceneName;
        public MultiplayerMode MultiplayerMode;
    }

    [Serializable]
    public enum MultiplayerMode
    {
        Host,
        Server,
        Client
    }

    public class CommandLineDataLoader
    {
        private Dictionary<string, string> m_CommandLineArguments = new Dictionary<string, string>();

        public CommandLineDataLoader()
        {
            LoadCommandLineData();
        }

        private void LoadCommandLineData()
        {
            string[] args = Environment.GetCommandLineArgs();
            m_CommandLineArguments = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i].ToLower();
                if (arg.StartsWith("-"))
                {
                    var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;

                    m_CommandLineArguments.Add(arg, value);
                }
            }
        }
    }

    public class RemoteHttpUtils
    {

    }
}
