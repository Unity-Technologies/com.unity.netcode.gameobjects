using MLAPI.MonoBehaviours.Core;
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
        internal string name
        {
            get
            {
                return prefab.GetComponent<NetworkedObject>().NetworkedPrefabName;
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
