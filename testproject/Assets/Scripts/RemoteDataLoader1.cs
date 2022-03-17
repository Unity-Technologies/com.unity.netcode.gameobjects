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
using UnityEngine.SceneManagement;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class RemoteDataLoader1 : MonoBehaviour
    {
        public RemoteConfig RemoteConfig;
        public MultiprocessConfig MultiprocessConfig;
        public static GameObject NetworkManagerGameObject { get; set; }
        public static NetworkManager NetworkManagerObject { get; set; }

        private int m_UpdateCounter;
        private bool m_PlatformSupportsLocalFiles;
        private List<Task> m_ListOfAsyncTasks;
        private bool m_MatchingConfigFound;
        private bool m_SceneLoadPending;
        private string m_LocalGitHash;
        private Task m_RemoteConfigTask;
        private Task m_CheckForMatchingRemoteConfigTask;
        private TextAsset m_TextConfig;
        public Scene NewScene;
        public static RemoteDataLoader1 Instance;
        public static PostMessageStatus PostMessageStatus;

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
                Debug.Log($"Destroying previously loaded gameobject {objs.Length}");
                Destroy(objs[0]);
            }

            DontDestroyOnLoad(gameObject);

            m_MatchingConfigFound = false;
            m_SceneLoadPending = false;
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
            Debug.Log($"IsActiveAndEnabled: {isActiveAndEnabled}");
        }
        // Start is called before the first frame update
        public void Start()
        {
            m_TextConfig = Resources.Load<TextAsset>("Text/config");
            var githash = Resources.Load<TextAsset>("Text/githash");
            if (githash == null && m_PlatformSupportsLocalFiles)
            {
                Debug.Log("Resource file didn't have githash, computing");
                // If the githash file has not been generated try to compute it
                ComputeLocalGitHash();
                Debug.Log("Done call to ComputeLocalGitHash");
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

            // Create a task to poll for a matching config
            Debug.Log("Calling PollForMatchingConfig");
            PollForMatchingConfig();
        }

        // Update is called once per frame
        public void Update()
        {
            m_UpdateCounter++;

            if (!SceneManager.GetActiveScene().name.Equals("RemoteConfigScene") || m_SceneLoadPending)
            {
                Debug.Log(SceneManager.GetActiveScene().name);
                return;
            }
            
            if (m_MatchingConfigFound)
            {
                // Claim the config
                // RemoteConfigUtils.PostBasicAsync(JsonUtility.ToJson(RemoteConfig), "/claim");
                
                var networkConfig = new NetworkConfig();
                NetworkManagerObject.NetworkConfig = networkConfig;

                MultiprocessConfig = JsonUtility.FromJson<MultiprocessConfig>(RemoteConfig.AdditionalJsonConfig);
                if (RemoteConfig.TransportName.Equals("UNET"))
                {
                    var transport = NetworkManagerGameObject.AddComponent<UNetTransport>();                    
                    NetworkManagerObject.NetworkConfig.NetworkTransport = transport;
                    transport.ConnectAddress = RemoteConfig.HostIp;
                }
                else if (RemoteConfig.TransportName.Equals("UTP"))
                {
                    var transport = NetworkManagerGameObject.AddComponent<UnityTransport>();
                    NetworkManagerObject.NetworkConfig.NetworkTransport = transport;
                    transport.ConnectionData.Address = RemoteConfig.HostIp;
                    transport.ConnectionData.ServerListenAddress = RemoteConfig.HostIp;
                }

                if (MultiprocessConfig.MultiplayerMode == MultiplayerMode.Host)
                {
                    NetworkManagerObject.StartHost();
                }
                else if (MultiprocessConfig.MultiplayerMode == MultiplayerMode.Server)
                {
                    NetworkManagerObject.StartServer();
                }
                else if (MultiprocessConfig.MultiplayerMode == MultiplayerMode.Client)
                {
                    NetworkManagerObject.StartClient();
                }
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.LoadScene(MultiprocessConfig.SceneName, LoadSceneMode.Additive);
                m_SceneLoadPending = true;
            }
            else
            {
                string status = "";
                foreach (var task in m_ListOfAsyncTasks)
                {
                    status += " status:" + task.Status;
                    if (task.Status == TaskStatus.Faulted)
                    {
                        Debug.Log(task.Exception.Message);
                        Debug.Log(task.Exception.StackTrace);
                    }
                }

                UnityEngine.UI.Text textObject = FindObjectOfType<UnityEngine.UI.Text>();

                textObject.text = $"{m_UpdateCounter}\n" +
                    $"Async tasks {m_ListOfAsyncTasks.Count} {status}\n" +
                    $"Local githash: {m_LocalGitHash} \n" +
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

        private Task PollForMatchingConfig()
        {
            Debug.Log("PollForMatchingConfig - Start");
            Debug.Log($"m_MatchingConfigFound {m_MatchingConfigFound} and Time.realtimeSinceStartup {Time.realtimeSinceStartup}");
            Task t = Task.Factory.StartNew(() =>
            {
                Debug.Log("Starting while loop");
                while (!m_MatchingConfigFound)
                {
                    RemoteConfig remoteConfig = null;
                    if (m_LocalGitHash != null)
                    {
                        remoteConfig = RemoteConfigUtils.GetRemoteConfig(Version.v1, m_LocalGitHash, m_TextConfig);
                    }
                    if (remoteConfig == null)
                    {
                        Debug.Log($"{DateTime.Now:f} remoteConfig was null, so no match was found, localGitHash was {m_LocalGitHash}");
                    }
                    else
                    {

                        RemoteConfig = remoteConfig;
                        m_MatchingConfigFound = true;
                    }
                    
                    Thread.Sleep(1234);
                }
                Debug.Log("ending while loop");

            });
            Debug.Log($"Adding task to list to keep it alive, currently it is {t.Status}");
            m_ListOfAsyncTasks.Add(t);
            return t;
        }

        private Task ComputeLocalGitHash()
        {
            Debug.Log("ComputeLocalGitHash");
            Task t = Task.Factory.StartNew(() =>
            {
                var workerProcess = CallGitToGetHash();
                workerProcess.Start();
                workerProcess.WaitForExit();
                Task<string> outputTask = workerProcess.StandardOutput.ReadToEndAsync();
                outputTask.Wait();
                m_LocalGitHash = outputTask.Result.Trim();
                Debug.Log($"ComputeLocalGitHash - {m_LocalGitHash}");
            });
            m_ListOfAsyncTasks.Add(t);
            return t;
        }

        private async void CheckForMatchingRemoteConfig()
        {
            if (m_LocalGitHash == null)
            {
                Debug.LogWarning("local githash was null so there's nothing to match to, hence returning");
            }

            else if (m_CheckForMatchingRemoteConfigTask == null ||
                m_CheckForMatchingRemoteConfigTask.Status == TaskStatus.Canceled  ||
                m_CheckForMatchingRemoteConfigTask.Status == TaskStatus.RanToCompletion ||
                m_CheckForMatchingRemoteConfigTask.Status == TaskStatus.Faulted)
            {
                Debug.Log("---> Creating Task to Check for Matching Remote Config");
                m_CheckForMatchingRemoteConfigTask = Task.Factory.StartNew(() =>
                {
                    RemoteConfigUtils.GetRemoteConfig(Version.v1, m_LocalGitHash, m_TextConfig);
                    
                    Debug.Log($"Remote GitHash was {RemoteConfig.GitHash} and local {m_LocalGitHash}");
                    if (RemoteConfig.GitHash.Equals(m_LocalGitHash))
                    {
                        m_MatchingConfigFound = true;
                    }
                    else
                    {
                        Debug.Log("Non matching config");
                    }
                });
                m_ListOfAsyncTasks.Add(m_CheckForMatchingRemoteConfigTask);
                m_RemoteConfigTask = m_CheckForMatchingRemoteConfigTask;
            }

            await m_CheckForMatchingRemoteConfigTask;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            NewScene = scene;
            // Once we load the new scene the RemoteDataLoader scene no longer needs to run
            if (scene.name.Equals(MultiprocessConfig.SceneName))
            {
                // m_SceneLoadPending = false;
                SceneManager.SetActiveScene(scene);
                // TODO: Determine if the following line somehow causes this process to fail
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
    }

    public class RemoteConfigUtils
    {
        public static string GetWebConfigResult;
        public static List<Task> WebTaskList;
        public static Task GetRemoteConfigTask;
        public static Task<string> GetStringTask;

        static RemoteConfigUtils()
        {
            WebTaskList = new List<Task>();
        }

        
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
                Debug.Log($"server_config file is found at {Path.Combine(Application.streamingAssetsPath, "server_config")}");
                string s = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "server_config"));
                Debug.Log(s);
                var config = JsonUtility.FromJson<RemoteConfig>(s);
                var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(config.AdditionalJsonConfig);
                return config;
            }
            else if (GetRemoteConfigTask != null)
            {
                Debug.Log("Not starting new task because " + GetRemoteConfigTask.Status + " with result " + GetWebConfigResult);
                if (GetStringTask != null)
                {
                    Debug.Log($"GetStringTask status {GetStringTask.Status}");
                    if (GetStringTask.Status == TaskStatus.WaitingForActivation)
                    {
                        
                    }
                }
            }
            else
            {
                Debug.Log("-----> Before GetWebConfig_v2");
                configData = GetWebConfig_v2();
                Debug.Log("-----> After GetWebConfig_v2");

                var remoteConfigList = new RemoteConfigList();
                JsonUtility.FromJsonOverwrite(configData, remoteConfigList);
                
                foreach (var config in remoteConfigList.JobQueueItems)
                {
                    
                    if (config.GitHash != null && config.GitHash.Equals(localGitHash)
                        && (RuntimePlatform)config.PlatformId == Application.platform)
                    {
                        Debug.Log("Found a match");
                        if (config.AdditionalJsonConfig != null)
                        {
                            var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(config.AdditionalJsonConfig);
                            if (mpConfig != null && mpConfig.SceneName != null)
                            {
                                return config;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No match found");
                        Debug.Log($"config.PlatformId = {config.PlatformId}, Application.platform = {Application.platform}");
                        Debug.Log($"config.GitHash {config.GitHash} localGitHash {localGitHash}");
                    }
                }
            }
            return null;
        }

        public static string GetWebConfig_v2()
        {
            using var client = new HttpClient();
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var responseTask = client.GetAsync("https://multiprocess-log-event-manager.test.cds.internal.unity3d.com/api/JobWithFile",
                HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token);
            try
            {
                Debug.Log("-----> Before Wait");
                responseTask.Wait();
                Debug.Log("-----> After Wait");
            }
            catch (Exception e)
            {
                Debug.Log("-----> After Wait in exception catch block");
                Debug.Log(e.Message + e.StackTrace);
            }
            var response = responseTask.Result;
            var contentTask = response.Content.ReadAsStringAsync();
            contentTask.Wait();
            Debug.Log(contentTask.Result.Length);
            return contentTask.Result;
        }

        public static async Task<object> GetWebConfig()
        {
            try
            {
                Debug.Log("GetWebConfig - Start");
                using var client = new HttpClient();

                var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                Debug.Log("GetWebConfig - Before await");
                var responseTask = await client.GetAsync("https://multiprocess-log-event-manager.test.cds.unity3d.com/api/JobWithFile",
                    HttpCompletionOption.ResponseContentRead, cancelAfterDelay.Token);
                Debug.Log("GetWebConfig - after await");

                var response = responseTask;
                var contentTask = response.Content.ReadAsStringAsync();
                contentTask.Wait();
                Debug.Log("GetWebConfig - End");
                GetWebConfigResult = contentTask.Result;
                return GetWebConfigResult;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + e.StackTrace);
            }
            return null;
        }

        public static async void PostBasicAsync(string content, string path = "")
        {
            RemoteDataLoader1.PostMessageStatus = PostMessageStatus.Creating;
            using var client = new HttpClient();
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://multiprocess-log-event-manager.test.cds.internal.unity3d.com/api/JobWithFile{path}");
            using var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            request.Content = stringContent;
            RemoteDataLoader1.PostMessageStatus = PostMessageStatus.Invoking;
            var response = await client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token);
            Debug.Log("PostBasicAsync - after await");
            RemoteDataLoader1.PostMessageStatus = PostMessageStatus.GettingOutput;
            var contentTask = response.Content.ReadAsStringAsync();
            contentTask.Wait();
            Debug.Log(contentTask.Result);
            RemoteDataLoader1.PostMessageStatus = PostMessageStatus.Complete;
            // return contentTask.Result;
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
        public string CreatedBy;
        public string UpdatedBy;
        public DateTime UpdatedDate;
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

    public enum PostMessageStatus
    {
        Creating,
        Invoking,
        GettingOutput,
        Complete
    }
}
