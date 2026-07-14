using System.Collections.Generic;
using Unity.Netcode.Logging;
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

        // Need own logger as is a UnityEngine.Object
        // we want the logs to point to this Object in the editor
        internal ContextualLogger Log;

        /// <summary>
        /// Read-only view into the prefabs list, enabling iterating and examining the list.
        /// Actually modifying the list should be done using <see cref="Add"/>
        /// and <see cref="Remove"/>.
        /// </summary>
        public IReadOnlyList<NetworkPrefab> PrefabList => List;

        internal void BuildLogger()
        {
            if (Log == null)
            {
                Log = new ContextualLogger(this);
                Log.AddInfo(nameof(NetworkPrefabsList), name);
            }
        }

        private void Awake() => BuildLogger();

        /// <summary>
        /// Adds a prefab to the prefab list. Performing this here will apply the operation to all
        /// <see cref="NetworkManager"/>s that reference this list.
        /// </summary>
        /// <param name="prefab">The NetworkPrefab to add to the shared list</param>
        public void Add(NetworkPrefab prefab)
        {
            if (prefab == null || !prefab.Validate(Log))
            {
                Log.Error(new Context(LogLevel.Normal, $"Failed to register {nameof(NetworkPrefab)}"));
                return;
            }

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
            if (!List.Remove(prefab))
            {
                Log.Warning(new Context(LogLevel.Normal, $"Failed to remove {nameof(NetworkPrefab)}"));
            }
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

        /// <summary>
        /// Validates all the prefabs in the list and removes them from the list if not valid
        /// </summary>
        internal void Validate(bool doRemove = true)
        {
            BuildLogger();

            for (int i = 0; i < List.Count; i++)
            {
                var prefab = List[i];

                // Blank entry - This is ok
                if (prefab == null)
                {
                    continue;
                }

                // Pass in local logger so any logs will highlight this list in the editor in case of an error
                prefab.Validate(Log, i);
            }
        }
    }
}
