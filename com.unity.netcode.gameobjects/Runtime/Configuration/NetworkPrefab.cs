using System;
using System.Collections.Generic;
using Unity.Netcode.Logging;
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

        /// <summary>
        /// Compares this NetworkPrefab with another to determine equality
        /// </summary>
        /// <param name="other">The NetworkPrefab to compare against</param>
        /// <returns>True if all fields match between the two NetworkPrefabs, false otherwise</returns>
        public bool Equals(NetworkPrefab other)
        {
            return Override == other.Override &&
                   Prefab == other.Prefab &&
                   SourcePrefabToOverride == other.SourcePrefabToOverride &&
                   SourceHashToOverride == other.SourceHashToOverride &&
                   OverridingTargetPrefab == other.OverridingTargetPrefab;
        }

        /// <summary>
        /// Gets the GlobalObjectIdHash of the source prefab based on the current override settings
        /// </summary>
        /// <value>The hash value identifying the source prefab</value>
        /// <exception cref="InvalidOperationException">Thrown when required prefab references are missing or invalid</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when Override has an invalid value</exception>
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

        /// <summary>
        /// Gets the GlobalObjectIdHash of the target prefab when using prefab overrides
        /// </summary>
        /// <value>The hash value identifying the target prefab, or 0 if no override is set</value>
        /// <exception cref="InvalidOperationException">Thrown when required prefab references are missing or invalid</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when Override has an invalid value</exception>
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

        /// <summary>
        /// Validates the NetworkPrefab configuration to ensure all required fields are properly set
        /// </summary>
        /// <param name="index">Optional index used for error reporting when validating lists of prefabs</param>
        /// <returns>True if the NetworkPrefab is valid and ready for use, false otherwise</returns>
        public bool Validate(int index = -1)
        {
            var log = new ContextualLogger();
            return Validate(log, index);
        }

        internal bool Validate(ContextualLogger log, int index = -1)
        {
            using var logContext = log.AddDisposableInfo("Invalid prefab", Prefab?.name);

            NetworkObject networkObject;
            if (Override == NetworkPrefabOverride.None)
            {
                if (Prefab == null)
                {
                    log.Warning(new Context(LogLevel.Error, $"{nameof(NetworkPrefab)} cannot be null").AddInfo($"{nameof(NetworkPrefab)} at index", index));
                    return false;
                }

                networkObject = Prefab.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    log.Warning(new Context(LogLevel.Error, $"Prefab is missing a {nameof(NetworkObject)} component!").AddObject(Prefab));
                    return false;
                }

                {
                    var childNetworkObjects = new List<NetworkObject>();
                    Prefab.GetComponentsInChildren(true, childNetworkObjects);
                    if (childNetworkObjects.Count > 1) // total count = 1 root NetworkObject + n child NetworkObjects
                    {
                        log.Warning(new Context(LogLevel.Error, $"Prefab has child {nameof(NetworkObject)}(s) but they will not be spawned across the network (unsupported {nameof(NetworkPrefab)} setup)").AddObject(Prefab));
                    }
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
                            log.Warning(new Context(LogLevel.Error, $"{nameof(NetworkPrefab)} {nameof(SourceHashToOverride)} is zero!"));
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
                            else
                            {
                                log.Warning(new Context(LogLevel.Error, $"{nameof(NetworkPrefab)} {nameof(SourcePrefabToOverride)} is null!"));
                                return false;
                            }
                        }

                        if (!SourcePrefabToOverride.TryGetComponent(out networkObject))
                        {
                            log.Warning(new Context(LogLevel.Error, $"{nameof(NetworkPrefab)} is missing a {nameof(NetworkObject)} component!").AddObject(SourcePrefabToOverride));
                            return false;
                        }

                        break;
                    }
            }

            // Validate target prefab override values next
            if (OverridingTargetPrefab == null)
            {
                // Safe to create context early as this code is not in any hot path
                var ctx = new Context(LogLevel.Error, $"{nameof(OverridingTargetPrefab)} is null! {nameof(NetworkPrefab)} entry will be removed and ignored.");
                switch (Override)
                {
                    case NetworkPrefabOverride.Hash:
                        {
                            log.Warning(ctx.AddInfo(nameof(SourceHashToOverride), SourceHashToOverride));
                            break;
                        }
                    case NetworkPrefabOverride.Prefab:
                        {
                            log.Warning(ctx.AddInfo(nameof(SourcePrefabToOverride), SourcePrefabToOverride.name));
                            break;
                        }
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a string representation of this NetworkPrefab's source and target hash values
        /// </summary>
        /// <returns>A string containing the source and target hash values</returns>
        public override string ToString()
        {
            return $"{{SourceHash: {SourceHashToOverride}, TargetHash: {TargetPrefabGlobalObjectIdHash}}}";
        }
    }
}
