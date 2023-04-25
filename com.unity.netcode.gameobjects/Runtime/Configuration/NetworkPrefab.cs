using System;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// The method of NetworkPrefab override used to identify the source prefab
    /// </summary>
    public enum NetworkPrefabOverride
    {
        /// <summary>
        /// No oeverride is present
        /// </summary>
        None,

        /// <summary>
        /// Override the prefab when the given SourcePrefabToOverride is requested
        /// </summary>
        Prefab,

        /// <summary>
        /// Override the prefab when the given SourceHashToOverride is requested
        /// Used in situations where the server assets do not exist in client builds
        /// </summary>
        Hash
    }

    /// <summary>
    /// Class that represents a NetworkPrefab
    /// </summary>
    [Serializable]
    public class NetworkPrefab
    {
        /// <summary>
        /// The override settings for this NetworkPrefab
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

        public bool Equals(NetworkPrefab other)
        {
            return Override == other.Override &&
                   Prefab == other.Prefab &&
                   SourcePrefabToOverride == other.SourcePrefabToOverride &&
                   SourceHashToOverride == other.SourceHashToOverride &&
                   OverridingTargetPrefab == other.OverridingTargetPrefab;
        }

        public uint SourcePrefabGlobalObjectIdHash
        {
            get
            {
                switch (Override)
                {
                    case NetworkPrefabOverride.None:
                        {
                            if (Prefab != null && Prefab.TryGetComponent(out NetworkObject networkObject))
                            {
                                return networkObject.GlobalObjectIdHash;
                            }

                            throw new InvalidOperationException($"Prefab field is not set or is not a {nameof(NetworkObject)}");
                        }
                    case NetworkPrefabOverride.Prefab:
                        {
                            if (SourcePrefabToOverride != null && SourcePrefabToOverride.TryGetComponent(out NetworkObject networkObject))
                            {
                                return networkObject.GlobalObjectIdHash;
                            }

                            throw new InvalidOperationException($"Source Prefab field is not set or is not a {nameof(NetworkObject)}");
                        }
                    case NetworkPrefabOverride.Hash:
                        return SourceHashToOverride;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public uint TargetPrefabGlobalObjectIdHash
        {
            get
            {
                switch (Override)
                {
                    case NetworkPrefabOverride.None:
                        return 0;
                    case NetworkPrefabOverride.Prefab:
                    case NetworkPrefabOverride.Hash:
                        {
                            if (OverridingTargetPrefab != null && OverridingTargetPrefab.TryGetComponent(out NetworkObject networkObject))
                            {
                                return networkObject.GlobalObjectIdHash;
                            }

                            throw new InvalidOperationException($"Target Prefab field is not set or is not a {nameof(NetworkObject)}");
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool Validate(int index = -1)
        {
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
                        NetworkLog.LogWarning($"{NetworkPrefabHandler.PrefabDebugHelper(this)} is missing a {nameof(NetworkObject)} component (entry will be ignored).");
                    }

                    return false;
                }

                return true;
            }

            // Validate source prefab override values first
            switch (Override)
            {
                case NetworkPrefabOverride.Hash:
                    {
                        if (SourceHashToOverride == 0)
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                            {
                                NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(SourceHashToOverride)} is zero (entry will be ignored).");
                            }

                            return false;
                        }

                        break;
                    }
                case NetworkPrefabOverride.Prefab:
                    {
                        if (SourcePrefabToOverride == null)
                        {
                            // This is a leftover side-effect from NetworkManager's OnValidate. It's a usability
                            // adjustment to automatically set the "Prefab" field as the source prefab when a user
                            // swaps from the default Inspector to the override one.
                            if (Prefab != null)
                            {
                                SourcePrefabToOverride = Prefab;
                            }
                            else if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                            {
                                NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(SourcePrefabToOverride)} is null (entry will be ignored).");
                                return false;
                            }
                        }

                        if (!SourcePrefabToOverride.TryGetComponent(out networkObject))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                            {
                                NetworkLog.LogWarning($"{nameof(NetworkPrefab)} ({SourcePrefabToOverride.name}) is missing a {nameof(NetworkObject)} component (entry will be ignored).");
                            }

                            return false;
                        }

                        break;
                    }
            }

            // Validate target prefab override values next
            if (OverridingTargetPrefab == null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(OverridingTargetPrefab)} is null!");
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

            return true;
        }

        public override string ToString()
        {
            return $"{{SourceHash: {SourceHashToOverride}, TargetHash: {TargetPrefabGlobalObjectIdHash}}}";
        }
    }
}
