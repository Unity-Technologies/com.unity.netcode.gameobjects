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
        private List<AdditiveSceneEntry> m_AdditiveScenes;

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
                    if (includedScene.Scene != null)
                    {
                        m_AdditiveSceneNames.Add(includedScene.Scene.name);
                    }
                }
            }

            ValidateBuildSettingsScenes();
        }
        internal void ValidateBuildSettingsScenes()
        {
            foreach (var includedScene in m_AdditiveScenes)
            {
                if (includedScene != null)
                {
                    SceneRegistration.AddOrRemoveSceneAsset(includedScene.Scene, includedScene.AutoIncludeInBuild);
                }
            }

            foreach(var additveSceneGroup in AdditiveSceneGroups)
            {
                additveSceneGroup.ValidateBuildSettingsScenes();
            }
        }

#endif

        [Tooltip("When set to true, this will automatically register all of the additive scenes with the build settings scenes in build list.  If false, then the scene(s) have to be manually added or will not be included in the build.")]
        [SerializeField]
        private bool m_AutoIncludeInBuild = true;       //Default to true


        protected virtual List<string> OnGetAdditiveScenes()
        {
            return m_AdditiveSceneNames;
        }

        public List<string> GetAdditiveScenes()
        {
            return OnGetAdditiveScenes();
        }

        public string GetAllScenesForHash()
        {
            var scenesHashBase = string.Empty;
            foreach (var sceneEntry in OnGetAdditiveScenes())
            {
                scenesHashBase += sceneEntry;
            }

            foreach (var additiveSceneGroup in AdditiveSceneGroups)
            {
                scenesHashBase += additiveSceneGroup.GetAllScenesForHash();
            }

            return scenesHashBase;
        }
    }

    [Serializable]
    public class AdditiveSceneEntry
    {
        public SceneAsset Scene;

        [Tooltip("When set to true, this will automatically register all of the additive scenes with the build settings scenes in build list.  If false, then the scene(s) have to be manually added or will not be included in the build.")]
        public bool AutoIncludeInBuild = true;       //Default to true
    }


    public interface IAdditiveSceneGroup
    {
        List<string> GetAdditiveScenes();

        string GetAllScenesForHash();
    }

}


