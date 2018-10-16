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
        internal string name
        {
            get
            {
                if (prefab == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedPrefab is not assigned");
                    return string.Empty;
                }
                else if (prefab.GetComponent<NetworkedObject>() == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The NetworkedPrefab " + prefab.name + " does not have a NetworkedObject");
                    return prefab.name;
                }
                else return prefab.GetComponent<NetworkedObject>().NetworkedPrefabName;
            }
        }

        internal ulong hash
        {
            get
            {
                if (prefab == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedPrefab is not assigned");
                    return 0;
                }
                else if (prefab.GetComponent<NetworkedObject>() == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The NetworkedPrefab " + prefab.name + " does not have a NetworkedObject");
                    return 0;
                }
                else return prefab.GetComponent<NetworkedObject>().NetworkedPrefabHash;
            }
        }
        /// <summary>
        /// The gameobject of the prefab
        /// </summary>
        public GameObject prefab;
        /// <summary>
        /// Wheter or not this is a playerPrefab
        /// </summary>
        public bool playerPrefab;
    }
}
