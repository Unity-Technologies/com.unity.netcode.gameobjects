using System;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Data
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
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedPrefab is not assigned");
                    return 0;
                }
                else if (Prefab.GetComponent<NetworkedObject>() == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The NetworkedPrefab " + Prefab.name + " does not have a NetworkedObject");
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
        /// Wheter or not this is a playerPrefab
        /// </summary>
        public bool PlayerPrefab;
    }
}
