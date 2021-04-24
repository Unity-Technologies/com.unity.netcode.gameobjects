using System;
#if (!UNITY_EDITOR)
using System.IO;
#else
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MLAPI;

public class SceneMenuManager : NetworkBehaviour
{
    [HideInInspector]
    [SerializeField]
    private List<string> m_AvailableScenes;

#if UNITY_EDITOR
    [SerializeField]
    private List<IncludedSceneProperties> m_ScenesToLoad;
#endif

    public Dropdown SceneDropDownList;

    [Tooltip("Horizontal window resolution size")]
    public int HorizontalResolution = 1024;

    [Tooltip("Vertical window resolution size")]
    public int VerticalResolution = 768;

    private int m_CurrentLoadedScene;
    private int m_NextSceneToLoad;

    private Dictionary<int, int> m_DropDownToSceneIndex = new Dictionary<int, int>();
    private Dictionary<string, int> m_SceneIndexesByName = new Dictionary<string, int>();

    [HideInInspector]
    [SerializeField]
    private Dropdown.OptionDataList m_OptionsList = new Dropdown.OptionDataList();

#if (UNITY_EDITOR)
    private void OnValidate()
    {
        if (SceneDropDownList != null)
        {
            SceneDropDownList.ClearOptions();
            m_SceneIndexesByName.Clear();
            m_DropDownToSceneIndex.Clear();
            m_OptionsList.options.Clear();

            // The SceneManager.sceneCountInBuildSettings is only populated in actual builds, and as such
            //we have to use the EditorBuildSettingsScene to get the values when running from an editor instance
            var selectedSceneAssets = new Dictionary<string, IncludedSceneProperties>();

            foreach (var sceneEntry in m_ScenesToLoad)
            {
                if (!selectedSceneAssets.ContainsKey(sceneEntry.Scene.name))
                {
                    selectedSceneAssets.Add(sceneEntry.Scene.name, sceneEntry);
                }
            }

            //Now populate our drop down while also only allowing IncludedSceneProperties.SceneTypes.Selectable
            var sceneCounter = 0;
            foreach (var nextscene in EditorBuildSettings.scenes)
            {
                var sceneFilename = nextscene.path.Substring(nextscene.path.LastIndexOf('/') + 1);
                var splitSceneFileName = sceneFilename.Split('.');
                if (selectedSceneAssets.ContainsKey(splitSceneFileName[0]) && selectedSceneAssets[splitSceneFileName[0]].SceneType == IncludedSceneProperties.SceneTypes.Selectable)
                {
                    AddSceneToOptions(splitSceneFileName[0], sceneCounter);
                }
                sceneCounter++;
            }
        }
    }
#endif

    private void Awake()
    {
        Screen.SetResolution(HorizontalResolution, VerticalResolution, false);
    }

    private void AddSceneToOptions(string scenename, int index)
    {
        if (scenename != SceneManager.GetActiveScene().name && !m_SceneIndexesByName.ContainsKey(scenename))
        {
            var optionData = new Dropdown.OptionData();
            optionData.text = scenename;

            m_SceneIndexesByName.Add(scenename, index);
            m_DropDownToSceneIndex.Add(m_OptionsList.options.Count, index);
            m_OptionsList.options.Add(optionData);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

#if !UNITY_EDITOR
        if (SceneDropDownList && Application.isPlaying)
        {
            var validScenes = new List<string>();
            foreach(var entry in m_OptionsList.options)
            {
                validScenes.Add(entry.text);
            }
            m_SceneIndexesByName.Clear();
            m_OptionsList.options.Clear();
            m_DropDownToSceneIndex.Clear();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var sceneName = Path.GetFileNameWithoutExtension(path);
                if(validScenes.Contains(sceneName))
                {
                    AddSceneToOptions(sceneName, i);
                }                
            }            
        }
#endif
        SceneDropDownList.options = m_OptionsList.options;
    }


    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        m_CurrentLoadedScene = m_NextSceneToLoad;
        m_NextSceneToLoad = 0;
    }

    private void LoadNextScene()
    {
        if (m_NextSceneToLoad == 0 && m_CurrentLoadedScene != 0 || m_NextSceneToLoad != 0)
        {
            SceneManager.LoadSceneAsync(m_NextSceneToLoad, LoadSceneMode.Single);
        }
    }

    public void OnLoadSelectedScene()
    {
        if (m_DropDownToSceneIndex.ContainsKey(SceneDropDownList.value))
        {
            m_NextSceneToLoad = m_DropDownToSceneIndex[SceneDropDownList.value];

            LoadNextScene();
        }
    }
}

#if UNITY_EDITOR
[Serializable]
public class IncludedSceneProperties
{
    public enum SceneTypes
    {
        IncludeOnly,
        Selectable,
    }

    public SceneAsset Scene;
    public SceneTypes SceneType;
}
#endif
