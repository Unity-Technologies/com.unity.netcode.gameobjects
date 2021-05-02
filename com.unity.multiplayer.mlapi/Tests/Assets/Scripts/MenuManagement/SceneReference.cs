using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace MLAPI.TestAssets.MenuManagement
{   
    [CreateAssetMenu(fileName = "SceneReference", menuName = "MLAPI/MenuManagement/SceneReference")]
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

        public string GetFirstSceneName()
        {
            if(m_ReferencedScenes.Count > 0)
            {
                return m_ReferencedScenes[0];
            }
            return string.Empty;
        }


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
        string GetFirstSceneName();
        string GetDisplayName();
        List<string> GetReferencedScenes();
    }
}
