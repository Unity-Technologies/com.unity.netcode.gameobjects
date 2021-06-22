using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneRegistrationEntry", menuName = "MLAPI/SceneManagement/SceneRegistrationEntry")]
    [Serializable]
    public class SceneRegistrationEntry : ScriptableObject, ISceneRegistrationEntry
    {

#if UNITY_EDITOR
        [SerializeField]
        private SceneAsset m_PrimaryScene;

        [SerializeField]
        private SceneRegistration m_SceneRegistrationParent;

        [Tooltip("When set to true, this will automatically register the primary scene with the build settings scenes in build list.  If false, then the scene has to be manually added or will not be included in the build.")]
        [SerializeField]
        private bool m_AutoIncludeInBuild = true;       //Default to true

        internal bool ShouldIncludeInBuildSettings()
        {
            if(m_SceneRegistrationParent != null && m_SceneRegistrationParent.IncludeInBuildSettings() && m_AutoIncludeInBuild)
            {
                return true;
            }
            return false;
        }

        private void OnValidate()
        {
            if (m_PrimaryScene != null)
            {
                m_PrimarySceneName = m_PrimaryScene.name;
            }
        }

        internal void AssignSceneRegistrationParent(SceneRegistration sceneRegistration)
        {
            m_SceneRegistrationParent = sceneRegistration;
        }

        internal void ValidateBuildSettingsScenes()
        {
            if(m_PrimaryScene != null)
            {
                SceneRegistration.AddOrRemoveSceneAsset(m_PrimaryScene, ShouldIncludeInBuildSettings());
            }

            if(m_AddtiveSceneGroup != null)
            {
                m_AddtiveSceneGroup.ValidateBuildSettingsScenes(this);
            }
        }
#endif

        [SerializeField]
        [HideInInspector]
        internal uint SceneIdentifier;

        [HideInInspector]
        [SerializeField]
        private string m_PrimarySceneName;

        [SerializeField]
        private AddtiveSceneGroup m_AddtiveSceneGroup;


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


