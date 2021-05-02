using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR    
using UnityEditor;
#endif

namespace MLAPI.TestAssets.MenuManagement
{

    [CreateAssetMenu(fileName = "MenuReference", menuName = "MLAPI/MenuManagement/MenuReference")]
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

        public string GetFirstSceneName()
        {
            if (m_ReferencedScenes.Count > 0)
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
}
