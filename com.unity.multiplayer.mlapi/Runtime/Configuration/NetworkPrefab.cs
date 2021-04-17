using System;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Configuration
{
    public enum NetworkPrefabOverride
    {
        None,
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
        /// Used when prefab is selected for the source prefab to override value (i.e. direct reference, the prefab is within the same project)
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
