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

        public ulong OverridingSourceHash;
        public GameObject OverridingSourcePrefab;
        public GameObject OverridingTargetPrefab;

        internal ulong Hash
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
