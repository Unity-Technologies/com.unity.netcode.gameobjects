using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = nameof(MenuReference), menuName = "Netcode/" + nameof(MenuReference))]
public class MenuReference : ScriptableObject, ISceneReference
{
#if UNITY_EDITOR
    public SceneAsset MenuScene;
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

        if (MenuScene != null)
        {
            m_ReferencedScenes.Add(MenuScene.name);
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

