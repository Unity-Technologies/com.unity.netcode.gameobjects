using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    // TODO: Can we make this public? Need to make NetworkPrefab public...
    internal class NetworkPrefabs : ScriptableObject
    {
        public bool IsDefault;

        [SerializeField]
        private List<NetworkPrefab> prefabs = new();

        internal IReadOnlyList<NetworkPrefab> Prefabs => prefabs;

        // Dedupe?
        public void Add(NetworkPrefab networkPrefab)
        {
            prefabs.Add(networkPrefab);
        }
    }
}
