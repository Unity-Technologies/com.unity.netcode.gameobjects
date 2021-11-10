using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    internal class NetworkPrefabConfig
    {
        public void InitializeOverrides(NetworkConfig config, bool removeBadEntries)
        {
            // This is used to remove entries not needed or invalid
            var removeEmptyPrefabs = new List<int>();

            // Always clear our prefab override links before building
            m_NetworkPrefabOverrideLinks.Clear();

            // Build the NetworkPrefabOverrideLinks dictionary
            for (int i = 0; i < config.NetworkPrefabs.Count; i++)
            {
                if (config.NetworkPrefabs[i] == null || !config.NetworkPrefabs[i].Validate(logFailures: true))
                {
                    removeEmptyPrefabs.Add(i);
                    continue;
                }

                var sourcePrefabGlobalObjectIdHash = config.NetworkPrefabs[i].GetSourcePrefabHash();
                var targetPrefab = config.NetworkPrefabs[i].GetTargetPrefab();
                var targetPrefabGlobalObjectIdHash = targetPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;

                // Check for duplicate "source" prefabs. We only support 1:1 mappings.
                if (m_NetworkPrefabOverrideLinks.ContainsKey(sourcePrefabGlobalObjectIdHash))
                {
                    Debug.LogError($"{nameof(NetworkPrefab)} with {nameof(NetworkObject.GlobalObjectIdHash)} of {sourcePrefabGlobalObjectIdHash} registered multiple times. Ignoring repeat entry.");
                    removeEmptyPrefabs.Add(i);
                    continue;
                }

                // TODO: The original code here split, only registering reverse lookups for configured overrides.
                // Preserving that same flow here, but it feels like it could introduce an edge case where a prefab
                // registered once without an override and a second time as the target of an override may have unexpected
                // behavior on reverse lookups.
                if (config.NetworkPrefabs[i].Override == NetworkPrefabOverride.None)
                {
                    m_NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, config.NetworkPrefabs[i]);
                }
                else
                {
                    // Check for duplicate "target" prefabs. We only support 1:1 mappings.
                    if (m_OverrideToSourceHash.ContainsKey(targetPrefabGlobalObjectIdHash))
                    {
                        Debug.LogError($"{targetPrefab.name} cannot be the target of more than 1 prefab override.");
                        removeEmptyPrefabs.Add(i);
                    }
                    else
                    {
                        m_NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, config.NetworkPrefabs[i]);
                        m_OverrideToSourceHash.Add(targetPrefabGlobalObjectIdHash, sourcePrefabGlobalObjectIdHash);
                    }
                }
            }

            if (removeBadEntries)
            {
                // Clear out anything that is invalid or not used (for invalid entries we already logged warnings to the user earlier)
                // Iterate backwards so indices don't shift as we remove
                for (int i = removeEmptyPrefabs.Count - 1; i >= 0; i--)
                {
                    config.NetworkPrefabs.RemoveAt(removeEmptyPrefabs[i]);
                }
            }
        }

        /// <summary>
        /// Looks for the appropriate prefab for a given hash based on override configs.
        /// </summary>
        /// <param name="gameObject">The GameObject for which to search for an override.</param>
        /// <returns>The appropriate GameObject to use for a given source GameObject.</returns>
        /// <exception cref="InvalidOperationException">The passed GameObject is not a NetworkObject, or is not registered with the system.</exception>
        public GameObject GetPrefab(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            var networkObject = gameObject.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                throw new InvalidOperationException($"No NetworkObject found on {gameObject.name}");
            }

            if (!TryGetPrefab(networkObject.GlobalObjectIdHash, out GameObject target))
            {
                throw new InvalidOperationException($"No such NetworkObject registered. Consider using {nameof(TryGetPrefab)}.");
            }

            return target;
        }

        /// <summary>
        /// Looks for the appropriate prefab for a given hash based on override configs.
        /// </summary>
        /// <param name="sourceHash">The GlobalObjectIdHash to search for.</param>
        /// <param name="prefab">Out param will be set to the appropriate GameObject reference if one is found, else null.</param>
        /// <returns>True if a configuration was found for the given hash.</returns>
        public bool TryGetPrefab(uint sourceHash, out GameObject prefab)
        {
            if (m_NetworkPrefabOverrideLinks.TryGetValue(sourceHash, out NetworkPrefab prefabConfig))
            {
                switch (prefabConfig.Override)
                {
                    default:
                    case NetworkPrefabOverride.None:
                        prefab = prefabConfig.Prefab;
                        break;
                    case NetworkPrefabOverride.Hash:
                    case NetworkPrefabOverride.Prefab:
                        prefab = prefabConfig.OverridingTargetPrefab;
                        break;
                }

                return true;
            }

            prefab = null;
            return false;
        }

        /// <summary>
        /// Search for the original prefab hash for which the supplied override was applied.
        /// </summary>
        /// <param name="overrideHash">The global hash id for a networked prefab override.</param>
        /// <param name="originalHash">The original prefab hash if an override configuration was found, else 0.</param>
        /// <returns>True if an override configuration with the provided hash was found.</returns>
        public bool TryGetSourcePrefabHash(uint overrideHash, out uint originalHash)
        {
            if (m_OverrideToSourceHash.ContainsKey(overrideHash))
            {
                originalHash = m_OverrideToSourceHash[overrideHash];
                return true;
            }

            originalHash = 0;
            return false;
        }

        internal IEnumerable<uint> GetRegisteredPrefabHashCodes()
        {
            return m_NetworkPrefabOverrideLinks.Keys;
        }

        /// <summary>
        /// This dictionary provides a quick way to check and see if a NetworkPrefab has a NetworkPrefab override.
        /// Generated at runtime and OnValidate
        /// </summary>
        private Dictionary<uint, NetworkPrefab> m_NetworkPrefabOverrideLinks = new();

        private Dictionary<uint, uint> m_OverrideToSourceHash = new();
    }
}
