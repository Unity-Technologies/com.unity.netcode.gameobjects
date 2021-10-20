using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = nameof(SceneReference), menuName = "Netcode/" + nameof(SceneReference))]
[Serializable]
public class SceneReference : ScriptableObject, ISceneReference
{
#if UNITY_EDITOR
    public SceneAsset SceneToReference;
    [SerializeField]
    private List<SceneAsset> m_IncludedScenes;
#endif
    [SerializeField]
    private string m_DisplayName;

    [HideInInspector]
    [SerializeField]
    private List<string> m_ReferencedScenes;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (m_ReferencedScenes == null)
        {
            m_ReferencedScenes = new List<string>();
        }
        else
        {
            m_ReferencedScenes.Clear();
        }

        if (SceneToReference != null)
        {
            m_ReferencedScenes.Add(SceneToReference.name);
        }

        foreach (var includedScene in m_IncludedScenes)
        {
            if (includedScene != null)
            {
                m_ReferencedScenes.Add(includedScene.name);
            }
        }
    }
#endif

    public string GetDisplayName()
    {
        return m_DisplayName;
    }

    public List<string> GetReferencedScenes()
    {
        return m_ReferencedScenes;
    }
}


public interface ISceneReference
{
    string GetDisplayName();
    List<string> GetReferencedScenes();
}

