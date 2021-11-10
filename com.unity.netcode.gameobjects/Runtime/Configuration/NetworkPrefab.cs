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
    internal class NetworkPrefab : ISerializationCallbackReceiver
    {
        /// <summary>
        /// The override setttings for this NetworkPrefab
        /// </summary>
        public NetworkPrefabOverride Override;

        /// <summary>
        /// The source (original) prefab used when <see cref="Override"/> is <see cref="NetworkPrefabOverride.None"/>
        /// or <see cref="NetworkPrefabOverride.Prefab"/>.
        /// </summary>
        public GameObject Prefab;

        [Obsolete("Use Prefab instead.")]
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
        /// Validates overrides configuration has proper values (set when required, and points to valid NetworkObjects)
        /// </summary>
        /// <returns>True if the hash and prefab settings are appropriate per the Override value.</returns>
        public bool Validate(bool logFailures = false)
        {
            // Validate "source" settings
            switch (Override)
            {
                case NetworkPrefabOverride.None:
                case NetworkPrefabOverride.Prefab:
                    if (!ValidatePrefab(logFailures))
                    {
                        return false;
                    }
                    break;
                case NetworkPrefabOverride.Hash:
                    if (!ValidateHashOverrideSource(logFailures))
                    {
                        return false;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Validate "target" settings
            if (Override != NetworkPrefabOverride.None)
            {
                if (OverridingTargetPrefab == null)
                {
                    if (logFailures)
                    {
                        NetworkLog.LogWarning($"{nameof(OverridingTargetPrefab)} is null.");
                    }

                    return false;
                }

                if (OverridingTargetPrefab.GetComponent<NetworkObject>() == null)
                {
                    if (logFailures && NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogWarning($"{OverridingTargetPrefab.name}) " +
                            $"is missing a {nameof(NetworkObject)} component.");
                    }

                    return false;
                }
            }

            return true;
        }

        private bool ValidateHashOverrideSource(bool logFailures)
        {
            if (SourceHashToOverride == 0)
            {
                if (logFailures && NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogWarning($"{nameof(SourceHashToOverride)} is zero.");
                }

                return false;
            }

            return true;
        }

        private bool ValidatePrefab(bool logFailures)
        {
            if (Prefab == null)
            {
                if (logFailures)
                {
                    Debug.LogWarning($"{nameof(Prefab)} is null.");
                }

                return false;
            }

            if (Prefab.GetComponent<NetworkObject>() == null)
            {
                if (logFailures && NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogWarning($"{Prefab.name}) " +
                        $"is missing a {nameof(NetworkObject)} component.");
                }

                return false;
            }

            if (Prefab.GetComponentsInChildren<NetworkObject>().Length > 1)
            {
                if (logFailures && NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogWarning($"{Prefab.name} has a child {nameof(NetworkObject)}. " +
                        $"Nested {nameof(NetworkObject)}s are not supported.");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Identifies the proper source prefab GlobalObjectIdHash if one is set.
        /// </summary>
        /// <returns>The GlobalObjectIdHash used to identify this NetworkPrefab configuration.</returns>
        /// <remarks>This method does not perform full validation of the NetworkPrefab configuration.</remarks>
        /// <exception cref="InvalidOperationException">The source prefab or prefab hash is not a valid NetworkObject GlobalObjectIdHash.</exception>
        public uint GetSourcePrefabHash()
        {
            if (Override == NetworkPrefabOverride.None)
            {
                if (Prefab != null)
                {
                    var no = Prefab.GetComponent<NetworkObject>();
                    if (no != null)
                    {
                        return no.GlobalObjectIdHash;
                    }
                }

                throw new InvalidOperationException("Prefab must be set to valid NetworkObject when Override is None");
            }

            if (Override == NetworkPrefabOverride.Prefab)
            {
                if (Prefab != null)
                {
                    var no = Prefab.GetComponent<NetworkObject>();
                    if (no != null)
                    {
                        return no.GlobalObjectIdHash;
                    }
                }

                throw new InvalidOperationException("SourcePrefab must be set to valid NetworkObject when Override is Prefab");
            }

            if (Override == NetworkPrefabOverride.Hash)
            {
                if (SourceHashToOverride == 0)
                {
                    throw new InvalidOperationException("SourcePrefab must be set to valid NetworkObject when Override is Prefab");
                }

                return SourceHashToOverride;
            }

            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Returns the appropriate prefab to instantiate for this NetworkPrefab if one is set.
        /// </summary>
        /// <returns>The prefab which should be instantiated for this NetworkPrefab. May return null.</returns>
        /// <remarks>This method does not verify the validity of the NetworkPrefab configuration.</remarks>
        public GameObject GetTargetPrefab()
        {
            switch (Override)
            {
                case NetworkPrefabOverride.None:
                    return Prefab;
                case NetworkPrefabOverride.Prefab:
                case NetworkPrefabOverride.Hash:
                    return OverridingTargetPrefab;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnBeforeSerialize()
        {
        }

        // We can't use "FormerlySerializedAs" because we're trying to conditionally map 2 old fields to 1 field.
        // If the object was configured to use prefab overrides, assume that the prefab override is the intended field
        // to save and overwrite the previously hidden Prefab field with this value.
        public void OnAfterDeserialize()
        {
#pragma warning disable 618
            if (Override == NetworkPrefabOverride.Prefab && SourcePrefabToOverride != null)
            {
                Prefab = SourcePrefabToOverride;
            }
#pragma warning restore 618
        }
    }
}
