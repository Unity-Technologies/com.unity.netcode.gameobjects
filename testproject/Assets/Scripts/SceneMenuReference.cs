using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CreateAssetMenu(fileName = "SceneMenuReference", menuName = "MLAPI/SceneMenuReference", order = 1)]
#endif
public class SceneMenuReference : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField]
    private SceneAsset m_Scene;
#endif
    [HideInInspector]
    [SerializeField]
    internal string SceneName;

    [SerializeField]
    internal string DisplayName;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if(m_Scene != null)
        {
            SceneName = m_Scene.name;
        }
    }
#endif
}
