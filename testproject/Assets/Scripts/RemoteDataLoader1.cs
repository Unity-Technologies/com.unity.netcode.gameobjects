using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RemoteDataLoader1 : MonoBehaviour
{

    private RemoteConfig m_RemoteConfig;
    private int m_UpdateCounter;
    private bool m_PlatformSupportsLocalFiles;
    private List<Task> m_ListOfAsyncTasks;
    private bool m_MatchingConfigFound;
    private string m_LocalGitHash;
    private Task m_RemoteConfigTask;

    public void Awake()
    {
        m_MatchingConfigFound = false;
        m_ListOfAsyncTasks = new List<Task>();
        var panelObject = GetComponentInParent<Canvas>();
        Debug.Log(panelObject);
        var textObject = GetComponent<UnityEngine.UI.Text>();
        Debug.Log(textObject.text);

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
    }
    // Start is called before the first frame update
    public void Start()
    {
        Debug.Log("in Start - calling get remote config");
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
            Debug.Log($"{m_UpdateCounter} Checking SceneName from remote config");
            var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(m_RemoteConfig.AdditionalJsonConfig);
            Debug.Log($"Checking SceneName - found {mpConfig.SceneName}, loading scene with LoadSceneMode.Single");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mpConfig.SceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            string status = "";
            foreach (var task in m_ListOfAsyncTasks)
            {
                status += " " + task.IsCompleted;
            }
            var textObject = GetComponent<UnityEngine.UI.Text>();
            textObject.text = $"{m_UpdateCounter}\n" +
                $"Async tasks {m_ListOfAsyncTasks.Count} {status}\n" +
                $"Local githash: {m_LocalGitHash} remote githash: {PlayerPrefs.GetString("GitHash")}\n" +
                $"{PlayerPrefs.GetString("HostIp")}\n" +
                $"{PlayerPrefs.GetString("JobId")}";
        }
    }

    private Task ComputeLocalGitHash()
    {
        Task t = Task.Factory.StartNew(() =>
        {
            var workerProcess = new System.Diagnostics.Process();
            workerProcess.StartInfo.UseShellExecute = false;
            workerProcess.StartInfo.RedirectStandardError = true;
            workerProcess.StartInfo.RedirectStandardOutput = true;
            workerProcess.StartInfo.FileName = "git";
            workerProcess.StartInfo.Arguments = "rev-parse HEAD";
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
            m_RemoteConfig = RemoteConfigUtils.GetRemoteConfig(Version.v1, m_LocalGitHash);
            Debug.Log($"Remote GitHash was {m_RemoteConfig.GitHash} and local {m_LocalGitHash}");
            m_MatchingConfigFound = true;
            if (m_RemoteConfig.GitHash.Equals(PlayerPrefs.GetString("GitHash")))
            {
                // If the githash and platform are a match then we can pick up this job
                // TODO:
                PlayerPrefs.SetInt("JobId", m_RemoteConfig.JobId);
                PlayerPrefs.SetString("HostIp", m_RemoteConfig.HostIp);
            }
        });
        m_ListOfAsyncTasks.Add(t);
        return t;
    }
}

public class RemoteConfigUtils
{
    public static RemoteConfig GetRemoteConfig(Version version, string localGitHash)
    {
        // There are three sources of information
        // 1. Command Line
        // 2. Web config
        // 3. Local file config
        // This needs to be ordered carefully as this also represents the order of priority, when one is found the next item on the list is not regarded

        string configData = " x ";

        // -m(?) | Start network in one of 3 modes: client, host, server
        bool isCommandLine = Environment.GetCommandLineArgs().Any(value => value == "-m");
        if (isCommandLine)
        {
        }
        else
        {

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
