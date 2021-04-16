using System;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Configuration
{
    public enum NetworkPrefabOverride
    {
        Unset,
        Prefab,
        Hash
    }

    /// <summary>
    /// Class that represents a NetworkPrefab
    /// </summary>
    [Serializable]
    public class NetworkPrefab
    {
        /// <summary>
        /// Asset reference of the network prefab
        /// </summary>
        public GameObject Prefab;

        public NetworkPrefabOverride Override;

        /// <summary>
        /// The original "source" prefab
        /// </summary>
        public GameObject OverridingSourcePrefab;

        /// <summary>
        /// The original "source" prefab's hash
        /// This is used typically in multi-project patterns where a separate project contains the
        /// source prefab and the GlobalObjectIdHash was copied and pasted into this field.
        /// </summary>
        public uint OverridingSourceHash;

        /// <summary>
        /// The prefab to replace the OverridingSourcePrefab with
        /// </summary>
        public GameObject OverridingTargetPrefab;

        /// <summary>
        /// Whether or not this is a player prefab
        /// </summary>
        public bool IsPlayer;

        internal uint Hash
        {
            get
            {
                if (Prefab == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} does not have a prefab assigned");
                    }

                    return 0;
                }

                var networkObject = Prefab.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {Prefab.name} does not have a {nameof(NetworkObject)} component");
                    }

                    return 0;
                }

                return networkObject.GlobalObjectIdHash;
            }
        }
    }
}
