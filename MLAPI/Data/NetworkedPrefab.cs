using System;
using UnityEngine;

namespace MLAPI.Data
{
    /// <summary>
    /// A class that represents a NetworkedPrefab
    /// </summary>
    [Serializable]
    public class NetworkedPrefab
    {
        /// <summary>
        /// The name of the networked prefab
        /// </summary>
        public string name;
        /// <summary>
        /// The gameobject of the prefab
        /// </summary>
        public GameObject prefab;
    }
}
