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

public class MainMenuManager : NetworkBehaviour
{
    [HideInInspector]
    [SerializeField]
    private List<string> m_AvailableScenes;

#if UNITY_EDITOR
    [SerializeField]
    private List<SceneAsset> m_ScenesToLoad;
#endif

    public Dropdown SceneDropDownList;

    [Tooltip("Horizontal window resolution size")]
    public int HorizontalResolution = 1024;

    [Tooltip("Vertical window resolution size")]
    public int VerticalResolution = 768;

    private int m_CurrentLoadedScene;
    private int m_NextSceneToLoad;

    private Dictionary<int,int> m_DropDownToSceneIndex = new Dictionary<int, int>();
    private Dictionary<string,int> m_SceneIndexesByName = new Dictionary<string, int>();
    private Dropdown.OptionDataList m_OptionsList = new Dropdown.OptionDataList();

    private void AddSceneToOptions(string scenename,int index)
    {
        if(scenename != SceneManager.GetActiveScene().name && !m_SceneIndexesByName.ContainsKey(scenename))
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
        Screen.SetResolution(HorizontalResolution, VerticalResolution, false);

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

        if(SceneDropDownList != null)
        {
            SceneDropDownList.ClearOptions();

            // The SceneManager.sceneCountInBuildSettings is only populated in actual builds, and as such
            //we have to use the EditorBuildSettingsScene to get the values when running from an editor instance
#if (UNITY_EDITOR)

            var selectedSceneAssets = new Dictionary<string, SceneAsset>();
            var currentSceneAssets = new Dictionary<string, EditorBuildSettingsScene>();

            var scenesToBeAdded = new List<EditorBuildSettingsScene>();

            foreach (var sceneEntry in m_ScenesToLoad)
            {
                if(!selectedSceneAssets.ContainsKey(sceneEntry.name))
                {
                    selectedSceneAssets.Add(sceneEntry.name, sceneEntry);
                }
            }
            foreach (var nextscene in EditorBuildSettings.scenes)
            {
                var sceneFilename = nextscene.path.Substring(nextscene.path.LastIndexOf('/') + 1);
                var splitSceneFileName = sceneFilename.Split('.');
                if(!currentSceneAssets.ContainsKey(splitSceneFileName[0]))
                {
                    currentSceneAssets.Add(splitSceneFileName[0], nextscene);
                }
            }

            //Make sure we add all scenes we want to load into the build settings.
            foreach(var sceneToLoad in selectedSceneAssets)
            {
                if (!currentSceneAssets.ContainsKey(sceneToLoad.Key))
                {
                    var sceneToadd = SceneManager.GetSceneByName(sceneToLoad.Value.name);                    
                    var newEditorBuildSettingsScene = new EditorBuildSettingsScene(sceneToadd.path,true);
                    scenesToBeAdded.Add(newEditorBuildSettingsScene);
                }
                else
                {
                    //Scene is already added, make sure it is enabled
                    if(!currentSceneAssets[sceneToLoad.Key].enabled)
                    {
                        currentSceneAssets[sceneToLoad.Key].enabled = true;
                    }
                }
            }

            //Disable any scenes we don't really need (don't remove just disable)
            foreach (var nextscene in currentSceneAssets)
            {
                //Scene is not part of our selection, disable it
                if(!selectedSceneAssets.ContainsKey(nextscene.Key))
                {
                    nextscene.Value.enabled = false;
                }
            }

            var finalBuildSettingsScenes = new List<EditorBuildSettingsScene>(currentSceneAssets.Values);
            finalBuildSettingsScenes.InsertRange(0, scenesToBeAdded);

            //Apply the updated scenes to the build settings.
            EditorBuildSettings.scenes = finalBuildSettingsScenes.ToArray();

            //Now populate our drop down
            var sceneCounter = 0;
            foreach (var nextscene in EditorBuildSettings.scenes)
            {
                if (nextscene.enabled)
                {
                    var sceneFilename = nextscene.path.Substring(nextscene.path.LastIndexOf('/') + 1);
                    var splitSceneFileName = sceneFilename.Split('.');
                    AddSceneToOptions(splitSceneFileName[0], sceneCounter);
                    sceneCounter++;
                }

            }
#else
            for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var sceneName = Path.GetFileNameWithoutExtension(path);
                AddSceneToOptions(sceneName, i);
            }
#endif
                SceneDropDownList.options = m_OptionsList.options;
        }
    }


    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        m_CurrentLoadedScene = m_NextSceneToLoad;
        m_NextSceneToLoad = 0;
    }

    private void LoadNextScene()
    {
        if(m_NextSceneToLoad == 0 && m_CurrentLoadedScene != 0 || m_NextSceneToLoad != 0)
        {
            SceneManager.LoadSceneAsync(m_NextSceneToLoad, LoadSceneMode.Single);
        }
    }

    public void OnLoadSelectedScene()
    {
        if(m_DropDownToSceneIndex.ContainsKey(SceneDropDownList.value))
        {
            m_NextSceneToLoad = m_DropDownToSceneIndex[SceneDropDownList.value];

            LoadNextScene();
        }
    }
}
