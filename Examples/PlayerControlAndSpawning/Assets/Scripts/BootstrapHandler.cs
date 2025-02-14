using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapHandler : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private List<RuntimeSceneEntry> m_ScenesToLoad;

    [Serializable]
    public class RuntimeSceneEntry
    {
        public string SceneToLoad;
        public string DisplayName;
    }

#if UNITY_EDITOR
    [Serializable]
    public class SceneEntry
    {
        public SceneAsset SceneAsset;
        public string DisplayName;
    }

    public List<SceneEntry> SceneEntries;
    private void OnValidate()
    {
        m_ScenesToLoad = new List<RuntimeSceneEntry>();
        foreach (var sceneEntry in SceneEntries)
        {
            if (!sceneEntry.SceneAsset)
            {
                continue;
            }
            var displayName = sceneEntry.DisplayName == null || sceneEntry.DisplayName == string.Empty ? sceneEntry.SceneAsset.name : sceneEntry.DisplayName;
            m_ScenesToLoad.Add(new RuntimeSceneEntry()
            {
                DisplayName = displayName,
                SceneToLoad = sceneEntry.SceneAsset.name,
            });
        }
    }
#endif

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 800));

        foreach (var sceneEntry in m_ScenesToLoad)
        {
            if (GUILayout.Button(sceneEntry.DisplayName))
            {
                SceneManager.LoadScene(sceneEntry.SceneToLoad);
            }
        }
        GUILayout.EndArea();
    }
}
