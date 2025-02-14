using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Netcode
{
    /// <summary>
    /// A ScriptableObject for holding a network prefabs list, which can be
    /// shared between multiple NetworkManagers.
    ///
    /// When NetworkManagers hold references to this list, modifications to the
    /// list at runtime will be picked up by all NetworkManagers that reference it.
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkPrefabsList", menuName = "Netcode/Network Prefabs List")]
    public class NetworkPrefabsList : ScriptableObject
    {
        internal delegate void OnAddDelegate(NetworkPrefab prefab);
        internal OnAddDelegate OnAdd;

        internal delegate void OnRemoveDelegate(NetworkPrefab prefab);
        internal OnRemoveDelegate OnRemove;

        [SerializeField]
        internal bool IsDefault;

        [FormerlySerializedAs("Prefabs")]
        [SerializeField]
        internal List<NetworkPrefab> List = new List<NetworkPrefab>();

        /// <summary>
        /// Read-only view into the prefabs list, enabling iterating and examining the list.
        /// Actually modifying the list should be done using <see cref="Add"/>
        /// and <see cref="Remove"/>.
        /// </summary>
        public IReadOnlyList<NetworkPrefab> PrefabList => List;

        /// <summary>
        /// Adds a prefab to the prefab list. Performing this here will apply the operation to all
        /// <see cref="NetworkManager"/>s that reference this list.
        /// </summary>
        /// <param name="prefab">The NetworkPrefab to add to the shared list</param>
        public void Add(NetworkPrefab prefab)
        {
            List.Add(prefab);
            OnAdd?.Invoke(prefab);
        }

        /// <summary>
        /// Removes a prefab from the prefab list. Performing this here will apply the operation to all
        /// <see cref="NetworkManager"/>s that reference this list.
        /// </summary>
        /// <param name="prefab">The NetworkPrefab to remove from the shared list</param>
        public void Remove(NetworkPrefab prefab)
        {
            List.Remove(prefab);
            OnRemove?.Invoke(prefab);
        }

        /// <summary>
        /// Check if the given GameObject is present as a prefab within the list
        /// </summary>
        /// <param name="prefab">The prefab to check</param>
        /// <returns>Whether or not the prefab exists</returns>
        public bool Contains(GameObject prefab)
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].Prefab == prefab)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the given NetworkPrefab is present within the list
        /// </summary>
        /// <param name="prefab">The prefab to check</param>
        /// <returns>Whether or not the prefab exists</returns>
        public bool Contains(NetworkPrefab prefab)
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].Equals(prefab))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
