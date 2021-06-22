using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneRegistrationEntry", menuName = "MLAPI/SceneManagement/SceneRegistrationEntry")]
    [Serializable]
    public class SceneRegistrationEntry : ScriptableObject, ISceneRegistrationEntry
    {
        [HideInInspector]
        [SerializeField]
        private string m_PrimarySceneName;

        [SerializeField]
        private AddtiveSceneGroup m_AddtiveSceneGroup;

#if UNITY_EDITOR
        [SerializeField]
        private SceneAsset m_PrimaryScene;

        private void OnValidate()
        {
            if (m_PrimaryScene != null)
            {
                m_PrimarySceneName = m_PrimaryScene.name;
            }
        }
#endif
        public string GetPrimaryScene()
        {
            return m_PrimarySceneName;
        }

        public string GetAllScenesForHash()
        {
            var scenesHashBase = m_PrimarySceneName;
            if(m_AddtiveSceneGroup != null)
            {
                scenesHashBase += m_AddtiveSceneGroup.GetAllScenesForHash();
            }
            return scenesHashBase;
        }
    }

    public interface ISceneRegistrationEntry
    {
        string GetPrimaryScene();

        string GetAllScenesForHash();
    }

}


