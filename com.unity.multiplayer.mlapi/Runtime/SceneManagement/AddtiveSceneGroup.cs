using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "AdditiveSceneGroup", menuName = "MLAPI/SceneManagement/AdditiveSceneGroup")]
    [Serializable]
    public class AddtiveSceneGroup : AssetDependency, IAdditiveSceneGroup
    {
        [SerializeField]
        private List<AddtiveSceneGroup> m_AdditiveSceneGroups;

        // Since Unity does not support observable collections there are two ways to approach this:
        // 1.) Make a duplicate list that adjusts itself during OnValidate
        // 2.) Make a customizable property editor that can handle the serialization process (which you will end up with two lists in the end anyway)
        // For this pass, I opted for solution #1
        [HideInInspector]
        [SerializeField]
        private List<AddtiveSceneGroup> m_KnownAdditiveSceneGroups = new List<AddtiveSceneGroup>();

        [SerializeField]
        private List<AdditiveSceneEntry> m_AdditiveScenes;

#if UNITY_EDITOR

        /// <summary>
        ///
        /// </summary>
        /// <param name="dependencyRemoved"></param>
        protected override void OnDependecyRemoved(IAssetDependency dependencyRemoved)
        {
            ValidateBuildSettingsScenes();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dependencyAdded"></param>
        protected override void OnDependecyAdded(IAssetDependency dependencyAdded)
        {
            ValidateBuildSettingsScenes();
        }

        private void OnValidate()
        {
            //if(BuildPipeline.isBuildingPlayer)
            //{
            //    return;
            //}

            foreach (var entry in m_AdditiveSceneGroups)
            {
                if (entry != null)
                {
                    entry.AddDependency(this);
                }
            }

            foreach (var entry in m_KnownAdditiveSceneGroups)
            {
                if (entry != null)
                {
                    if (!m_AdditiveSceneGroups.Contains(entry))
                    {
                        entry.RemoveDependency(this);
                    }
                }
            }

            m_KnownAdditiveSceneGroups.Clear();
            m_KnownAdditiveSceneGroups.AddRange(m_AdditiveSceneGroups);
            ValidateBuildSettingsScenes();
        }


        internal void ValidateBuildSettingsScenes()
        {
            var shouldInclude = ShouldAssetBeIncluded();
            var partOfRootBranch = BelongsToRootAssetBranch();

            foreach (var includedScene in m_AdditiveScenes)
            {
                if (includedScene != null && includedScene.Scene != null)
                {
                    // Only filter out the referenced AdditiveSceneEntries if we shouldn't include this specific AdditiveSceneGroup's reference scene assets
                    // Note: Other AdditiveSceneGroups could have other associated SceneRegistrationEntries that might qualify it to be added to the build settings
                    // so we only apply this to the current AddtiveSceneGroup's referenced AdditiveSceneEntries
                    SceneRegistration.AddOrRemoveSceneAsset(includedScene.Scene, shouldInclude && partOfRootBranch && includedScene.AutoIncludeInBuild);
                }
            }

            // Now validate the build settings inclusion for any reference additive scene groups
            foreach(var additveSceneGroup in m_AdditiveSceneGroups)
            {
                if (additveSceneGroup != null)
                {
                    additveSceneGroup.ValidateBuildSettingsScenes();
                }
            }
        }

#endif

        protected virtual List<AdditiveSceneEntry> OnGetAdditiveScenes()
        {
            return m_AdditiveScenes;
        }

        public List<AdditiveSceneEntry> GetAdditiveScenes()
        {
            return OnGetAdditiveScenes();
        }

        protected override void OnWriteHashSynchValues(NetworkWriter writer)
        {
            foreach (var sceneEntry in OnGetAdditiveScenes())
            {
                if (sceneEntry != null)
                {
                    writer.WriteString(sceneEntry.SceneEntryName);
                }
            }

            foreach (var additiveSceneGroup in m_AdditiveSceneGroups)
            {
                if (additiveSceneGroup != null)
                {
                    additiveSceneGroup.WriteHashSynchValues(writer);
                }
            }
        }
    }

    /// <summary>
    /// A container class to hold the editor specific assets and
    /// the scene name that it is pointing to for runtime
    /// </summary>
    [Serializable]
    public class AdditiveSceneEntry: ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        public SceneAsset Scene;

        [Tooltip("When set to true, this will automatically register all of the additive scenes with the build settings scenes in build list.  If false, then the scene(s) have to be manually added or will not be included in the build.")]
        public bool AutoIncludeInBuild;
#endif
        [HideInInspector]
        public string SceneEntryName;

        public void OnAfterDeserialize()
        {

        }
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (Scene != null && SceneEntryName != Scene.name)
            {
                SceneEntryName = Scene.name;
            }
#endif
        }
    }


    public interface IAdditiveSceneGroup
    {
        List<AdditiveSceneEntry> GetAdditiveScenes();
    }

}


