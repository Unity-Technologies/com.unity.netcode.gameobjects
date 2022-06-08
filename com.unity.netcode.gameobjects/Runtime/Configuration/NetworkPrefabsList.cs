using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Netcode
{
    [CreateAssetMenu(fileName = "NetworkPrefabsList", menuName = "Netcode/Network Prefabs List")]
    public class NetworkPrefabsList : ScriptableObject
    {
        [SerializeField]
        internal bool IsDefault;

        [FormerlySerializedAs("Prefabs")]
        [SerializeField]
        public List<NetworkPrefab> List = new();
    }
}
