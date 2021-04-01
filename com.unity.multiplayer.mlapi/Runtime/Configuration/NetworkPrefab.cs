using System;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Configuration
{
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

        /// <summary>
        /// Whether or not this is a player prefab
        /// </summary>
        public bool IsPlayer;

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
