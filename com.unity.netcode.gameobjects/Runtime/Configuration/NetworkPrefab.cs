using System;
using UnityEngine;

namespace Unity.Netcode
{
    internal enum NetworkPrefabOverride
    {
        None,
        Prefab,
        Hash
    }

    /// <summary>
    /// Class that represents a NetworkPrefab
    /// </summary>
    [Serializable]
    internal class NetworkPrefab
    {
        /// <summary>
        /// The override setttings for this NetworkPrefab
        /// </summary>
        public NetworkPrefabOverride Override;

        /// <summary>
        /// Asset reference of the network prefab
        /// </summary>
        public GameObject Prefab;

        /// <summary>
        /// Used when prefab is selected for the source prefab to override value (i.e. direct reference, the prefab is within the same project)
        /// We keep a separate value as the user might want to have something different than the default Prefab for the SourcePrefabToOverride
        /// </summary>
        public GameObject SourcePrefabToOverride;

        /// <summary>
        /// Used when hash is selected for the source prefab to override value (i.e. a direct reference is not possible such as in a multi-project pattern)
        /// </summary>
        public uint SourceHashToOverride;

        /// <summary>
        /// The prefab to replace (override) the source prefab with
        /// </summary>
        public GameObject OverridingTargetPrefab;

        public uint SourcePrefabGlobalObjectIdHash
        {
            get
            {
                throw new NotImplementedException();
                // None = Prefab.NetworkObject.GlobalObjectIdHash;
                // Hash = SourceHashToOverride
                // Prefab = SourcePrefabToOverride.NetworkObject.GlobalObjectIdHash
            }
        }

        public uint TargetPrefabGlobalObjectIdHash
        {
            get
            {
                throw new NotImplementedException();
                // None = 0
                // Hash = OverridingTargetPrefab.NetworkObject.GlobalObjectIdHash
                // Prefab = OverridingTargetPrefab.NetworkObject.GlobalObjectIdHash
            }
        }

        public bool Validate(out uint sourcePrefabGlobalObjectIdHash, out uint targetPrefabGlobalObjectIdHash, int index = -1)
        {
            sourcePrefabGlobalObjectIdHash = 0;
            targetPrefabGlobalObjectIdHash = 0;
            NetworkObject networkObject;
            if (Override == NetworkPrefabOverride.None)
            {
                if (Prefab == null)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} cannot be null ({nameof(NetworkPrefab)} at index: {index})");
                    return false;
                }

                networkObject = Prefab.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogWarning($"{NetworkManager.PrefabDebugHelper(this)} is missing " +
                                              $"a {nameof(NetworkObject)} component (entry will be ignored).");
                    }
                    return false;
                }

                // Otherwise get the GlobalObjectIdHash value
                sourcePrefabGlobalObjectIdHash = networkObject.GlobalObjectIdHash;
            }
            else // Validate Overrides
            {
                // Validate source prefab override values first
                switch (Override)
                {
                    case NetworkPrefabOverride.Hash:
                        {
                            if (SourceHashToOverride == 0)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                {
                                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.SourceHashToOverride)} is zero " +
                                                          "(entry will be ignored).");
                                }
                                return false;
                            }
                            sourcePrefabGlobalObjectIdHash = SourceHashToOverride;
                            break;
                        }
                    case NetworkPrefabOverride.Prefab:
                        {
                            if (SourcePrefabToOverride == null)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                {
                                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.SourcePrefabToOverride)} is null (entry will be ignored).");
                                }

                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry {SourceHashToOverride} will be removed and ignored.");
                                return false;
                            }
                            else
                            {
                                networkObject = SourcePrefabToOverride.GetComponent<NetworkObject>();
                                if (networkObject == null)
                                {
                                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                    {
                                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} ({SourcePrefabToOverride.name}) " +
                                                              $"is missing a {nameof(NetworkObject)} component (entry will be ignored).");
                                    }

                                    Debug.LogWarning($"{nameof(NetworkPrefab)} override entry (\"{SourcePrefabToOverride.name}\") will be removed and ignored.");
                                    return false;
                                }

                                sourcePrefabGlobalObjectIdHash = networkObject.GlobalObjectIdHash;
                            }
                            break;
                        }
                }

                // Validate target prefab override values next
                if (OverridingTargetPrefab == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.OverridingTargetPrefab)} is null!");
                    }
                    switch (Override)
                    {
                        case NetworkPrefabOverride.Hash:
                            {
                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry {SourceHashToOverride} will be removed and ignored.");
                                break;
                            }
                        case NetworkPrefabOverride.Prefab:
                            {
                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry ({SourcePrefabToOverride.name}) will be removed and ignored.");
                                break;
                            }
                    }
                    return false;
                }
                else
                {
                    targetPrefabGlobalObjectIdHash = OverridingTargetPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
                }
            }
            return true;
        }
    }
}
