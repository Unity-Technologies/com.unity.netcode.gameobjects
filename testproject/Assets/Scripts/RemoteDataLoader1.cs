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
    private Task m_TaskToReadConfig;
    private int m_UpdateCounter;

    public void Awake()
    {
        var panelObject = GetComponentInParent<Canvas>();
        Debug.Log(panelObject);
        var textObject = GetComponent<UnityEngine.UI.Text>();
        Debug.Log(textObject.text);

        Application.targetFrameRate = 5;
        QualitySettings.vSyncCount = 0;

    }

    // Start is called before the first frame update
    public void Start()
    {
        Debug.Log("in Start - calling get remote config");

        m_TaskToReadConfig = Task.Factory.StartNew(() => {
            m_RemoteConfig = RemoteConfigUtils.GetRemoteConfig(Version.v1);
            var textObject = GetComponent<UnityEngine.UI.Text>();
            textObject.text += "\n" + m_RemoteConfig;

            PlayerPrefs.SetInt("JobId", m_RemoteConfig.JobId);
            PlayerPrefs.SetString("HostIp", m_RemoteConfig.HostIp);
        });


    }

    // Update is called once per frame
    public void Update()
    {
        m_UpdateCounter++;
        Debug.Log($" in Update {m_UpdateCounter} - checking if remote config task is done");

        if (m_RemoteConfig != null && m_RemoteConfig.AdditionalJsonConfig != null)
        {
            Debug.Log("Checking SceneName");
            var mpConfig = JsonUtility.FromJson<MultiprocessConfig>(m_RemoteConfig.AdditionalJsonConfig);
            Debug.Log($"Checking SceneName - found {mpConfig.SceneName}, loading scene with LoadSceneMode.Single");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mpConfig.SceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
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
