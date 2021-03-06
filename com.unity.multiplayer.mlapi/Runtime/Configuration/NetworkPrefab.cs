using System;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Configuration
{
    /// <summary>
    /// A class that represents a NetworkPrefab
    /// </summary>
    [Serializable]
    public class NetworkPrefab
    {
        internal ulong Hash
        {
            get
            {
                if (ReferenceEquals(Prefab, null))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} is not assigned");
                    }

                    return 0;
                }

                var networkObject = Prefab.GetComponent<NetworkObject>();
                if (ReferenceEquals(networkObject, null))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {Prefab.name} does not have a {nameof(NetworkObject)}");
                    }

                    return 0;
                }

                return networkObject.PrefabHash;
            }
        }

        /// <summary>
        /// The gameobject of the prefab
        /// </summary>
        public GameObject Prefab;

        /// <summary>
        /// Whether or not this is a playerPrefab
        /// </summary>
        public bool PlayerPrefab;
    }
}