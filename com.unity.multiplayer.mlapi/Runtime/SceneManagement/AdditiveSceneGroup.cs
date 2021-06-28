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
    public class AdditiveSceneGroup : AssetDependency, IAdditiveSceneGroup
    {
        [SerializeField]
        private List<AdditiveSceneGroup> m_AdditiveSceneGroups;

        // Since Unity does not support observable collections there are two ways to approach this:
        // 1.) Make a duplicate list that adjusts itself during OnValidate
        // 2.) Make a customizable property editor that can handle the serialization process (which you will end up with two lists in the end anyway)
        // For this pass, I opted for solution #1
        [HideInInspector]
        [SerializeField]
        private List<AdditiveSceneGroup> m_KnownAdditiveSceneGroups = new List<AdditiveSceneGroup>();

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
            var additiveScenesDirty = m_KnownAdditiveSceneGroups.Count != m_AdditiveSceneGroups.Count;
            // Used to catch the scenario where a user accidentally assigns an AdditiveSceneGroup to itself
            AdditiveSceneGroup recursionTrap = null;
            // Always add all known dependencies during validation
            // We can apply the same dependencies since AddDependency checks to assure that the dependency doesn't already exist before adding it
            foreach (var entry in m_AdditiveSceneGroups)
            {
                if (entry != null)
                {
                    // Make sure the user has not added an AdditiveSceneGroup to itself
                    if (entry != this)
                    {
                        entry.AddDependency(this);
                    }
                    else
                    {
                        recursionTrap = entry;
                    }
                }
            }

            // Remove this entry if it exists
            if(recursionTrap != null)
            {
                var index = m_AdditiveSceneGroups.IndexOf(recursionTrap);
                m_AdditiveSceneGroups[index] = null;

                Debug.LogError($"AdditiveSceneGroup {recursionTrap.name} cannot be added to the Additive Scene Groups of {name} (itself).  Invalid assignment!");
                additiveScenesDirty = true;
            }

            // Once all dependencies have been added, then check to see if we lost a dependency
            // If so, then we remove that dependency
            foreach (var entry in m_KnownAdditiveSceneGroups)
            {
                if (entry != null)
                {
                    if (!m_AdditiveSceneGroups.Contains(entry))
                    {
                        entry.RemoveDependency(this);
                        entry.ValidateBuildSettingsScenes();
                        additiveScenesDirty = true;
                    }
                }
            }

            if (additiveScenesDirty)
            {
                // Next, keep m_KnownAdditiveSceneGroups in sync with m_AdditiveSceneGroups
                m_KnownAdditiveSceneGroups.Clear();
                m_KnownAdditiveSceneGroups.AddRange(m_AdditiveSceneGroups);
            }

            ValidateBuildSettingsScenes();
        }

        /// <summary>
        /// Validate that all scenes in the build list is in sync with the current relative AdditiveSceneGroup and its children
        /// </summary>
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
                    // so we only apply this to the current AdditiveSceneGroup's referenced AdditiveSceneEntries
                    SceneRegistration.AddOrRemoveSceneAsset(includedScene.Scene, shouldInclude && partOfRootBranch && includedScene.IncludeInBuild);
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
    public class AdditiveSceneEntry : SceneEntryBsase
    {
    }


    public interface IAdditiveSceneGroup
    {
        List<AdditiveSceneEntry> GetAdditiveScenes();
    }

}


