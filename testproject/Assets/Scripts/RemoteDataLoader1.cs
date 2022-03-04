using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using UnityEngine;

public class RemoteDataLoader1 : MonoBehaviour
{

    public static RemoteConfig RemoteConfig;
    public void Awake()
    {
        var panelObject = GetComponentInParent<Canvas>();
        Debug.Log(panelObject);
        var textObject = GetComponent<UnityEngine.UI.Text>();
        Debug.Log(textObject.text);

    }

    // Start is called before the first frame update
    public void Start()
    {
        Debug.Log("in Start - calling get remote config");
        RemoteConfig = RemoteConfigUtils.GetRemoteConfig(Version.v1);
        Debug.Log($"in Start - {RemoteConfig}");
        var textObject = GetComponent<UnityEngine.UI.Text>();
        textObject.text += "\n" + RemoteConfig;

        PlayerPrefs.SetInt("JobId", RemoteConfig.JobId);
        PlayerPrefs.SetString("HostIp", RemoteConfig.HostIp);

        if (RemoteConfig != null && RemoteConfig.AdditionalJsonConfig != null)
        {
            var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(RemoteConfig.AdditionalJsonConfig);
            UnityEngine.SceneManagement.SceneManager.LoadScene(mpConfig.SceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

    }

    // Update is called once per frame
    public void Update()
    {
        
    }
}

public class RemoteConfigUtils
{
    public static RemoteConfig GetRemoteConfig(Version version)
    {
        // There are three sources of information
        // 1. Command Line
        // 2. Web config
        // 3. Local file config
        // This needs to be ordered carefully as this also represents the order of priority, when one is found the next item on the list is not regarded

        string configData = " x ";

        // -m(?) | Start network in one of 3 modes: client, host, server
        bool isCommandLine = Environment.GetCommandLineArgs().Any(value => value == "-m");
        if (!isCommandLine)
        {
            // Try to get config from web resource
            configData = GetWebConfig();
            var remoteConfigList = new RemoteConfigList();
            JsonUtility.FromJsonOverwrite(configData, remoteConfigList);
            // var remoteConfigList = JsonUtility.FromJson<RemoteConfigList>(configData);
            // Debug.Log($"{remoteConfigList.JobQueueItems.Count}");
            foreach (var config in remoteConfigList.JobQueueItems)
            {
                Debug.Log(JsonUtility.ToJson(config));
                if (config.AdditionalJsonConfig != null)
                {
                    var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(config.AdditionalJsonConfig);
                    if (mpConfig != null && mpConfig.SceneName != null)
                    {
                        Debug.Log(config.AdditionalJsonConfig);
                        return config;
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
        var responseTask = client.GetAsync("http://localhost:5050/api/JobWithFile",
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
