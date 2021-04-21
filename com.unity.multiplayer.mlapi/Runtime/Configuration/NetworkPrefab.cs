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

                var prefabGameObject = Prefab;
                
                switch(Override)
                {
                    case NetworkPrefabOverride.Prefab:
                        {
                            prefabGameObject = SourcePrefabToOverride;
                            break;
                        }
                    case NetworkPrefabOverride.Hash:
                        {
                            return SourceHashToOverride;
                        }
                }

                var networkObject = prefabGameObject.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {prefabGameObject.name} does not have a {nameof(NetworkObject)} component");
                    }

                    return 0;
                }

                return networkObject.GlobalObjectIdHash;
            }
        }
    }
}
