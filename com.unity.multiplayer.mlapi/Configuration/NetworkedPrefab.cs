using System;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Configuration
{
    /// <summary>
    /// A class that represents a NetworkedPrefab
    /// </summary>
    [Serializable]
    public class NetworkedPrefab
    {
        internal ulong Hash
        {
            get
            {
                if (Prefab == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedPrefab is not assigned");
                    return 0;
                }
                else if (Prefab.GetComponent<NetworkedObject>() == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("The NetworkedPrefab " + Prefab.name + " does not have a NetworkedObject");
                    return 0;
                }
                else return Prefab.GetComponent<NetworkedObject>().PrefabHash;
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
