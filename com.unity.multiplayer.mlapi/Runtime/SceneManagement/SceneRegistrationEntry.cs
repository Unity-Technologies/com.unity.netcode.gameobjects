using System;
using UnityEngine;
using MLAPI.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneRegistrationEntry", menuName = "MLAPI/SceneManagement/SceneRegistrationEntry")]
    [Serializable]
    public class SceneRegistrationEntry : AssetDependency, ISceneRegistrationEntry
    {
#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField]
        private SceneAsset m_PreviousPrimaryScene;

        [HideInInspector]
        [SerializeField]
        private AdditiveSceneGroup m_PreviousAddtiveSceneGroup;

        [SerializeField]
        private SceneAsset m_PrimaryScene;

        [Tooltip("When set to true, this will automatically register the primary scene with the build settings scenes in build list.  If false, then the scene has to be manually added or will not be included in the build.")]
        [SerializeField]
        private bool m_AutoIncludeInBuild = true;       //Default to true

        internal bool ShouldIncludeInBuildSettings()
        {
            var belongsToRoot = BelongsToRootAssetBranch();
            return (belongsToRoot && m_AutoIncludeInBuild);
        }

        protected override bool OnShouldAssetBeIncluded()
        {
            return m_AutoIncludeInBuild;
        }

        private void OnValidate()
        {
            ValidateBuildSettingsScenes();
        }

        /// <summary>
        /// For scene management related usage, currently this only needs to invoke the
        /// ValidateBuildSettingsScenes method.
        /// Note: more complex logic could be added here to provide additional clean up
        /// </summary>
        /// <param name="dependencyRemoved"></param>
        protected override void OnDependecyRemoved(IAssetDependency dependencyRemoved)
        {
            ValidateBuildSettingsScenes();
        }

        /// <summary>
        /// For scene management related usage, currently this only needs to invoke the
        /// ValidateBuildSettingsScenes method.
        /// Note: more complex logic could be added here to provide additional dependency
        /// registration related actions.
        /// </summary>
        /// <param name="dependencyAdded"></param>
        protected override void OnDependecyAdded(IAssetDependency dependencyAdded)
        {
            ValidateBuildSettingsScenes();
        }

        internal void ValidateBuildSettingsScenes()
        {
            if (m_PrimaryScene != m_PreviousPrimaryScene)
            {
                // If we had a different scene, then remove it from the build settings
                if (m_PreviousPrimaryScene != null)
                {
                    SceneRegistration.AddOrRemoveSceneAsset(m_PreviousPrimaryScene, false);
                }
            }

            // If the newly assigned scene is not null
            if (m_PrimaryScene != null)
            {
                // As long as we should include this scene registration entry, then add it to the build settings list of scenes to be included
                SceneRegistration.AddOrRemoveSceneAsset(m_PrimaryScene, ShouldIncludeInBuildSettings());
            }

            m_PreviousPrimaryScene = m_PrimaryScene;

            if (m_PrimaryScene != null)
            {
                m_PrimarySceneName = m_PrimaryScene.name;
            }

            if (m_AddtiveSceneGroup != null)
            {
                m_AddtiveSceneGroup.AddDependency(this);
                m_AddtiveSceneGroup.ValidateBuildSettingsScenes();
            }
            if (m_PreviousAddtiveSceneGroup != m_AddtiveSceneGroup)
            {
                if (m_PreviousAddtiveSceneGroup != null)
                {
                    m_PreviousAddtiveSceneGroup.RemoveDependency(this);
                    m_PreviousAddtiveSceneGroup.ValidateBuildSettingsScenes();
                }
                m_PreviousAddtiveSceneGroup = m_AddtiveSceneGroup;
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
        private AdditiveSceneGroup m_AddtiveSceneGroup;


        public string GetPrimaryScene()
        {
            return m_PrimarySceneName;
        }

        protected override void OnWriteHashSynchValues(NetworkWriter writer)
        {
            if (m_PrimarySceneName != null || m_PrimarySceneName != string.Empty)
            {
                writer.WriteString(m_PrimarySceneName);
            }

            if (m_AddtiveSceneGroup != null)
            {
                m_AddtiveSceneGroup.WriteHashSynchValues(writer);
            }
        }
    }

    public interface ISceneRegistrationEntry
    {
        string GetPrimaryScene();
    }

}


