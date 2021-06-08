using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "AdditiveSceneGroup", menuName = "MLAPI/SceneManagement/AdditiveSceneGroup")]
    [Serializable]
    public class AddtiveSceneGroup : ScriptableObject, IAdditiveSceneGroup
    {
        public List<AddtiveSceneGroup> AdditiveSceneGroups;

        [HideInInspector]
        [SerializeField]
        private List<string> m_AdditiveSceneNames;

#if UNITY_EDITOR
        [SerializeField]
        private List<SceneAsset> m_AdditiveScenes;
        private void OnValidate()
        {
            if (m_AdditiveSceneNames == null)
            {
                m_AdditiveSceneNames = new List<string>();
            }
            else
            {
                m_AdditiveSceneNames.Clear();
            }

            foreach (var includedScene in m_AdditiveScenes)
            {
                if (includedScene != null)
                {
                    m_AdditiveSceneNames.Add(includedScene.name);
                }
            }

        }
#endif

        protected virtual List<string> OnGetAdditiveScenes()
        {
            return m_AdditiveSceneNames;
        }

        public List<string> GetAdditiveScenes()
        {
            return OnGetAdditiveScenes();
        }
    }


    public interface IAdditiveSceneGroup
    {
        List<string> GetAdditiveScenes();
    }

}


