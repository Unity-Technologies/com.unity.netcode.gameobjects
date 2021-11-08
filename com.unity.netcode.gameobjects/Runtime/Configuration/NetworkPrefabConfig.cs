using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    internal class NetworkPrefabConfig
    {
        public GameObject GetNetworkPrefabOverride(GameObject gameObject)
        {
            var networkObject = gameObject.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                if (m_NetworkPrefabOverrideLinks.ContainsKey(networkObject.GlobalObjectIdHash))
                {
                    switch (m_NetworkPrefabOverrideLinks[networkObject.GlobalObjectIdHash].Override)
                    {
                        case NetworkPrefabOverride.Hash:
                        case NetworkPrefabOverride.Prefab:
                        {
                            return m_NetworkPrefabOverrideLinks[networkObject.GlobalObjectIdHash].OverridingTargetPrefab;
                        }
                    }
                }
            }
            return gameObject;
        }

        public void InitializeOverrides(NetworkConfig config)
        {
            // This is used to remove entries not needed or invalid
            var removeEmptyPrefabs = new List<int>();

            // Always clear our prefab override links before building
            m_NetworkPrefabOverrideLinks.Clear();

            // Build the NetworkPrefabOverrideLinks dictionary
            for (int i = 0; i < config.NetworkPrefabs.Count; i++)
            {
                var sourcePrefabGlobalObjectIdHash = (uint)0;
                var targetPrefabGlobalObjectIdHash = (uint)0;
                var networkObject = (NetworkObject)null;
                if (config.NetworkPrefabs[i] == null || (config.NetworkPrefabs[i].Prefab == null && config.NetworkPrefabs[i].Override == NetworkPrefabOverride.None))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogWarning(
                            $"{nameof(NetworkPrefab)} cannot be null ({nameof(NetworkPrefab)} at index: {i})");
                    }

                    removeEmptyPrefabs.Add(i);
                    continue;
                }
                else if (config.NetworkPrefabs[i].Override == NetworkPrefabOverride.None)
                {
                    var networkPrefab = config.NetworkPrefabs[i];
                    networkObject = networkPrefab.Prefab.GetComponent<NetworkObject>();
                    if (networkObject == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                        {
                            NetworkLog.LogWarning($"{PrefabDebugHelper(networkPrefab)} is missing " +
                                $"a {nameof(NetworkObject)} component (entry will be ignored).");
                        }
                        removeEmptyPrefabs.Add(i);
                        continue;
                    }

                    // Otherwise get the GlobalObjectIdHash value
                    sourcePrefabGlobalObjectIdHash = networkObject.GlobalObjectIdHash;
                }
                else // Validate Overrides
                {
                    // Validate source prefab override values first
                    switch (config.NetworkPrefabs[i].Override)
                    {
                        case NetworkPrefabOverride.Hash:
                        {
                            if (config.NetworkPrefabs[i].SourceHashToOverride == 0)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                {
                                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.SourceHashToOverride)} is zero " +
                                        "(entry will be ignored).");
                                }
                                removeEmptyPrefabs.Add(i);
                                continue;
                            }
                            sourcePrefabGlobalObjectIdHash = config.NetworkPrefabs[i].SourceHashToOverride;
                            break;
                        }
                        case NetworkPrefabOverride.Prefab:
                        {
                            if (config.NetworkPrefabs[i].SourcePrefabToOverride == null)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                {
                                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.SourcePrefabToOverride)} is null (entry will be ignored).");
                                }
                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry {config.NetworkPrefabs[i].SourceHashToOverride} will be removed and ignored.");
                                removeEmptyPrefabs.Add(i);
                                continue;
                            }
                            else
                            {
                                networkObject = config.NetworkPrefabs[i].SourcePrefabToOverride.GetComponent<NetworkObject>();
                                if (networkObject == null)
                                {
                                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                    {
                                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} ({config.NetworkPrefabs[i].SourcePrefabToOverride.name}) " +
                                            $"is missing a {nameof(NetworkObject)} component (entry will be ignored).");
                                    }
                                    Debug.LogWarning($"{nameof(NetworkPrefab)} override entry (\"{config.NetworkPrefabs[i].SourcePrefabToOverride.name}\") will be removed and ignored.");
                                    removeEmptyPrefabs.Add(i);
                                    continue;
                                }
                                sourcePrefabGlobalObjectIdHash = networkObject.GlobalObjectIdHash;
                            }
                            break;
                        }
                    }

                    // Validate target prefab override values next
                    if (config.NetworkPrefabs[i].OverridingTargetPrefab == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                        {
                            NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.OverridingTargetPrefab)} is null!");
                        }
                        removeEmptyPrefabs.Add(i);
                        switch (config.NetworkPrefabs[i].Override)
                        {
                            case NetworkPrefabOverride.Hash:
                            {
                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry {config.NetworkPrefabs[i].SourceHashToOverride} will be removed and ignored.");
                                break;
                            }
                            case NetworkPrefabOverride.Prefab:
                            {
                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry ({config.NetworkPrefabs[i].SourcePrefabToOverride.name}) will be removed and ignored.");
                                break;
                            }
                        }
                        continue;
                    }
                    else
                    {
                        targetPrefabGlobalObjectIdHash = config.NetworkPrefabs[i].OverridingTargetPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
                    }
                }

                // Assign the appropriate GlobalObjectIdHash to the appropriate NetworkPrefab
                if (!m_NetworkPrefabOverrideLinks.ContainsKey(sourcePrefabGlobalObjectIdHash))
                {
                    if (config.NetworkPrefabs[i].Override == NetworkPrefabOverride.None)
                    {
                        m_NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, config.NetworkPrefabs[i]);
                    }
                    else
                    {
                        if (!m_OverrideToSourceHash.ContainsKey(targetPrefabGlobalObjectIdHash))
                        {
                            switch (config.NetworkPrefabs[i].Override)
                            {
                                case NetworkPrefabOverride.Prefab:
                                {
                                    m_NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, config.NetworkPrefabs[i]);
                                    m_OverrideToSourceHash.Add(targetPrefabGlobalObjectIdHash, sourcePrefabGlobalObjectIdHash);
                                }
                                    break;
                                case NetworkPrefabOverride.Hash:
                                {
                                    m_NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, config.NetworkPrefabs[i]);
                                    m_OverrideToSourceHash.Add(targetPrefabGlobalObjectIdHash, sourcePrefabGlobalObjectIdHash);
                                }
                                    break;
                            }
                        }
                        else
                        {
                            // This can happen if a user tries to make several GlobalObjectIdHash values point to the same target
                            Debug.LogError($"{nameof(NetworkPrefab)} (\"{networkObject.name}\") has a duplicate {nameof(NetworkObject.GlobalObjectIdHash)} target entry value of: {targetPrefabGlobalObjectIdHash}! Removing entry from list!");
                            removeEmptyPrefabs.Add(i);
                        }
                    }
                }
                else
                {
                    // This should never happen, but in the case it somehow does log an error and remove the duplicate entry
                    Debug.LogError($"{nameof(NetworkPrefab)} ({networkObject.name}) has a duplicate {nameof(NetworkObject.GlobalObjectIdHash)} source entry value of: {sourcePrefabGlobalObjectIdHash}! Removing entry from list!");
                    removeEmptyPrefabs.Add(i);
                }
            }

            // Clear out anything that is invalid or not used (for invalid entries we already logged warnings to the user earlier)
            // Iterate backwards so indices don't shift as we remove
            for (int i = removeEmptyPrefabs.Count - 1; i >= 0; i--)
            {
                config.NetworkPrefabs.RemoveAt(removeEmptyPrefabs[i]);
            }

            removeEmptyPrefabs.Clear();
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

        public bool TryGetSourcePrefabHashForOverride(uint overrideHash, out uint originalHash)
        {
            if (m_OverrideToSourceHash.ContainsKey(overrideHash))
            {
                originalHash = m_OverrideToSourceHash[overrideHash];
                return true;
            }

            originalHash = 0;
            return false;
        }

        internal static string PrefabDebugHelper(NetworkPrefab networkPrefab)
        {
            return $"{nameof(NetworkPrefab)} \"{networkPrefab.Prefab.gameObject.name}\"";
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
