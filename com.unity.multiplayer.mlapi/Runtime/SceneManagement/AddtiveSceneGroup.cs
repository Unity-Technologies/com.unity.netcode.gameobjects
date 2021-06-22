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

        private List<SceneRegistrationEntry> m_SceneRegistrationEntryParents;

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
        }
        internal void ValidateBuildSettingsScenes(SceneRegistrationEntry sceneRegistrationEntry)
        {
            if(!m_SceneRegistrationEntryParents.Contains(sceneRegistrationEntry))
            {
                m_SceneRegistrationEntryParents.Add(sceneRegistrationEntry);
            }

            var includeInBuildSettings = false;
            foreach(var sceneRegistrationEntryItem in m_SceneRegistrationEntryParents)
            {
                // We only need one SceneRegistrationEntry to respond with true in order to include additive scenes or groups
                if (sceneRegistrationEntryItem.ShouldIncludeInBuildSettings())
                {                    
                    includeInBuildSettings = true;
                    break;
                }
            }

            foreach (var includedScene in m_AdditiveScenes)
            {
                if (includedScene != null)
                {
                    // Only filter out the referenced AdditiveSceneEntries if we shouldn't include this specific AdditiveSceneGroup's reference scene assets
                    // Note: Other AdditiveSceneGroups could have other associated SceneRegistrationEntries that might qualify it to be added to the build settings
                    // so we only apply this to the current AddtiveSceneGroup's referenced AdditiveSceneEntries
                    SceneRegistration.AddOrRemoveSceneAsset(includedScene.Scene, includeInBuildSettings && includedScene.AutoIncludeInBuild);
                }
            }

            // Now validate the build settings includsion for any reference additive scene groups
            foreach(var additveSceneGroup in AdditiveSceneGroups)
            {
                additveSceneGroup.ValidateBuildSettingsScenes(sceneRegistrationEntry);
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


