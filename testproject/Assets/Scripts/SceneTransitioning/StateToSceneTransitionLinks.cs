using System;
#if (!UNITY_EDITOR)
using System.IO;
#endif
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class StateToSceneTransitionLinks
{
    private static Dictionary<int,int> DropDownToSceneIndex = new Dictionary<int, int>();
    private static Dictionary<string,int> SceneIndexesByName = new Dictionary<String, int>();
    private static Dictionary<int,GlobalGameState.GameStates> SceneIndexToState = new Dictionary<int,GlobalGameState.GameStates>();
    private static Dictionary<GlobalGameState.GameStates, int> StateToSceneIndex = new Dictionary<GlobalGameState.GameStates, int>();
    private static Dropdown.OptionDataList SceneOptionsList = new Dropdown.OptionDataList();
    private static Dropdown.OptionDataList GameStateOptionsList = new Dropdown.OptionDataList();

    [SerializeField]
    private UnityEngine.Object m_SceneToLoad;

    [SerializeField]
    private GlobalGameState.GameStates m_StateToLoadScene;

    public UnityEngine.Object sceneToLoad { get { return m_SceneToLoad; } }
    public GlobalGameState.GameStates stateToLoadScene { get { return m_StateToLoadScene; } }


    public static void Initialize()
    {
        SceneOptionsList.options.Clear();
        DropDownToSceneIndex.Clear();
        SceneIndexesByName.Clear();
        StateToSceneIndex.Clear();

#if (UNITY_EDITOR)
        int sceneCounter = 0;
        foreach (UnityEditor.EditorBuildSettingsScene nextscene in UnityEditor.EditorBuildSettings.scenes)
        {
            if (nextscene.enabled)
            {
                string SceneFilename = nextscene.path.Substring(nextscene.path.LastIndexOf('/')+1);
                string[] SplitSceneFileName = SceneFilename.Split('.');
                AddSceneToOptions(SplitSceneFileName[0], sceneCounter);
            }
            sceneCounter++;
        }
#else
        for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = Path.GetFileNameWithoutExtension(path);
            AddSceneToOptions(sceneName, i);
        }
#endif
        foreach(string stateName in Enum.GetNames(typeof(GlobalGameState.GameStates)))
        {
            GameStateOptionsList.options.Add(new Dropdown.OptionData(stateName));
        }
    }



    static private void AddSceneToOptions(String scenename,int index)
    {
        if(scenename != SceneManager.GetActiveScene().name && !SceneIndexesByName.ContainsKey(scenename))
        {
            Dropdown.OptionData optionData = new Dropdown.OptionData();
            optionData.text = scenename;

            SceneIndexesByName.Add(scenename, index);
            DropDownToSceneIndex.Add( SceneOptionsList.options.Count, index);
            SceneOptionsList.options.Add(optionData);
        }

    }
}

[Serializable]
public class StateToSceneTransitionList
{
    [SerializeField]
    private List<StateToSceneTransitionLinks> m_Options;

    /// <summary>
    /// The list of options for the dropdown list.
    /// </summary>
    public List<StateToSceneTransitionLinks> options { get { return m_Options; } set { m_Options = value; } }

    /// <summary>
    /// GetSceneLinkedToState
    /// Returns the scene associated with the state
    /// </summary>
    /// <param name="gameState"></param>
    /// <returns></returns>
    public String GetSceneNameLinkedToState(GlobalGameState.GameStates gameState)
    {
        var results = m_Options.Where(entry => entry.stateToLoadScene == gameState);
        if(results != null)
        {
            return results.First().sceneToLoad.name;
        }
        return String.Empty;

    }

    public StateToSceneTransitionList()
    {
        options = new List<StateToSceneTransitionLinks>();
    }
}
