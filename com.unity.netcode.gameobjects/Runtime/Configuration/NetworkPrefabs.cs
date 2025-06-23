using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// A class that represents the runtime aspect of network prefabs.
    /// This class contains processed prefabs from the NetworkPrefabsList, as
    /// well as additional modifications (additions and removals) made at runtime.
    /// </summary>
    [Serializable]
    public class NetworkPrefabs
    {
        /// <summary>
        /// Edit-time scripted object containing a list of NetworkPrefabs.
        /// </summary>
        /// <remarks>
        /// This field can be null if no prefabs are pre-configured.
        /// Runtime usages of <see cref="NetworkPrefabs"/> should not depend on this edit-time field for execution.
        /// </remarks>
        [SerializeField]
        public List<NetworkPrefabsList> NetworkPrefabsLists = new List<NetworkPrefabsList>();

        /// <summary>
        /// This dictionary provides a quick way to check and see if a NetworkPrefab has a NetworkPrefab override.
        /// Generated at runtime and OnValidate
        /// </summary>
        [NonSerialized]
        public Dictionary<uint, NetworkPrefab> NetworkPrefabOverrideLinks = new Dictionary<uint, NetworkPrefab>();

        /// <summary>
        /// This is used for the legacy way of spawning NetworkPrefabs with an override when manually instantiating and spawning.
        /// To handle multiple source NetworkPrefab overrides that all point to the same target NetworkPrefab use
        /// <see cref="NetworkSpawnManager.InstantiateAndSpawn(NetworkObject, ulong, bool, bool, bool, Vector3, Quaternion)"/>
        /// or <see cref="NetworkObject.InstantiateAndSpawn(NetworkManager, ulong, bool, bool, bool, Vector3, Quaternion)"/>.
        /// </summary>
        [NonSerialized]
        public Dictionary<uint, uint> OverrideToNetworkPrefab = new Dictionary<uint, uint>();

        /// <summary>
        /// Gets the read-only list of all registered network prefabs.
        /// </summary>
        public IReadOnlyList<NetworkPrefab> Prefabs => m_Prefabs;

        [NonSerialized]
        private List<NetworkPrefab> m_Prefabs = new List<NetworkPrefab>();

        [NonSerialized]
        private List<NetworkPrefab> m_RuntimeAddedPrefabs = new List<NetworkPrefab>();

        private void AddTriggeredByNetworkPrefabList(NetworkPrefab networkPrefab)
        {
            if (AddPrefabRegistration(networkPrefab))
            {
                // Don't add this to m_RuntimeAddedPrefabs
                // This prefab is now in the PrefabList, so if we shutdown and initialize again, we'll pick it up from there.
                m_Prefabs.Add(networkPrefab);
            }
        }

        private void RemoveTriggeredByNetworkPrefabList(NetworkPrefab networkPrefab)
        {
            m_Prefabs.Remove(networkPrefab);
        }

        /// <summary>
        /// Destructor that cleans up network prefab resources.
        /// </summary>
        ~NetworkPrefabs()
        {
            Shutdown();
        }

        /// <summary>
        /// Deregister from add and remove events and clear the events.
        /// </summary>
        internal void Shutdown()
        {
            foreach (var list in NetworkPrefabsLists)
            {
                list.OnAdd -= AddTriggeredByNetworkPrefabList;
                list.OnRemove -= RemoveTriggeredByNetworkPrefabList;
            }
        }

        /// <summary>
        /// Processes the <see cref="NetworkPrefabsList"/> if one is present for use during runtime execution,
        /// else processes <see cref="Prefabs"/>.
        /// </summary>
        /// <param name="warnInvalid">When true, logs warnings about invalid prefabs that are removed during initialization.</param>
        public void Initialize(bool warnInvalid = true)
        {
            m_Prefabs.Clear();
            foreach (var list in NetworkPrefabsLists)
            {
                list.OnAdd += AddTriggeredByNetworkPrefabList;
                list.OnRemove += RemoveTriggeredByNetworkPrefabList;
            }

            NetworkPrefabOverrideLinks.Clear();
            OverrideToNetworkPrefab.Clear();

            var prefabs = new List<NetworkPrefab>();

            if (NetworkPrefabsLists.Count != 0)
            {
                foreach (var list in NetworkPrefabsLists)
                {
                    foreach (var networkPrefab in list.PrefabList)
                    {
                        prefabs.Add(networkPrefab);
                    }
                }
            }

            m_Prefabs = new List<NetworkPrefab>();

            List<NetworkPrefab> removeList = null;
            if (warnInvalid)
            {
                removeList = new List<NetworkPrefab>();
            }

            foreach (var networkPrefab in prefabs)
            {
                if (AddPrefabRegistration(networkPrefab))
                {
                    m_Prefabs.Add(networkPrefab);
                }
                else
                {
                    removeList?.Add(networkPrefab);
                }
            }

            foreach (var networkPrefab in m_RuntimeAddedPrefabs)
            {
                if (AddPrefabRegistration(networkPrefab))
                {
                    m_Prefabs.Add(networkPrefab);
                }
                else
                {
                    removeList?.Add(networkPrefab);
                }
            }

            // Clear out anything that is invalid or not used
            if (removeList?.Count > 0)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    var sb = new StringBuilder("Removing invalid prefabs from Network Prefab registration: ");
                    sb.Append(string.Join(", ", removeList));
                    NetworkLog.LogWarning(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Add a new NetworkPrefab instance to the list.
        /// </summary>
        /// <param name="networkPrefab">The <see cref="NetworkPrefab"/> to add.</param>
        /// <returns>True if the prefab was successfully added, false if it was invalid or already registered</returns>
        /// <remarks>
        /// The framework does not synchronize this list between clients. Any runtime changes must be handled manually.<br />
        /// Any modifications made here are not persisted. Permanent configuration changes should be done
        /// through the <see cref="NetworkPrefabsList"/> scriptable object property.
        /// </remarks>
        public bool Add(NetworkPrefab networkPrefab)
        {
            if (AddPrefabRegistration(networkPrefab))
            {
                m_Prefabs.Add(networkPrefab);
                m_RuntimeAddedPrefabs.Add(networkPrefab);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove a NetworkPrefab instance from the list.
        /// </summary>
        /// <param name="prefab">The <see cref="NetworkPrefab"/> to remove.</param>
        /// <remarks>
        /// The framework does not synchronize this list between clients. Any runtime changes must be handled manually.<br />
        /// Any modifications made here are not persisted. Permanent configuration changes should be done
        /// through the <see cref="NetworkPrefabsList"/> scriptable object property.
        /// </remarks>
        public void Remove(NetworkPrefab prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            m_Prefabs.Remove(prefab);
            m_RuntimeAddedPrefabs.Remove(prefab);
            OverrideToNetworkPrefab.Remove(prefab.TargetPrefabGlobalObjectIdHash);
            NetworkPrefabOverrideLinks.Remove(prefab.SourcePrefabGlobalObjectIdHash);
        }

        /// <summary>
        /// Remove a NetworkPrefab instance with matching <see cref="NetworkPrefab.Prefab"/> from the list.
        /// </summary>
        /// <param name="prefab">The <see cref="GameObject"/> to match against for removal.</param>
        /// <remarks>
        /// The framework does not synchronize this list between clients. Any runtime changes must be handled manually.<br />
        /// Any modifications made here are not persisted. Permanent configuration changes should be done
        /// through the <see cref="NetworkPrefabsList"/> scriptable object property.
        /// </remarks>
        public void Remove(GameObject prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            for (int i = 0; i < m_Prefabs.Count; i++)
            {
                if (m_Prefabs[i].Prefab == prefab)
                {
                    Remove(m_Prefabs[i]);
                    return;
                }
            }

            for (int i = 0; i < m_RuntimeAddedPrefabs.Count; i++)
            {
                if (m_RuntimeAddedPrefabs[i].Prefab == prefab)
                {
                    Remove(m_RuntimeAddedPrefabs[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Check if the given GameObject is present as a prefab within the list.
        /// </summary>
        /// <param name="prefab">The prefab to check.</param>
        /// <returns>True if the prefab exists or false if it does not.</returns>
        public bool Contains(GameObject prefab)
        {
            for (int i = 0; i < m_Prefabs.Count; i++)
            {
                // Check both values as Prefab and be different than SourcePrefabToOverride
                if (m_Prefabs[i].Prefab == prefab || m_Prefabs[i].SourcePrefabToOverride == prefab)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the given NetworkPrefab is present within the list.
        /// </summary>
        /// <param name="prefab">The prefab to check.</param>
        /// <returns>True if the prefab exists or false if it does not.</returns>
        public bool Contains(NetworkPrefab prefab)
        {
            for (int i = 0; i < m_Prefabs.Count; i++)
            {
                if (m_Prefabs[i].Equals(prefab))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Configures <see cref="NetworkPrefabOverrideLinks"/> for the given <see cref="NetworkPrefab"/>.
        /// </summary>
        private bool AddPrefabRegistration(NetworkPrefab networkPrefab)
        {
            if (networkPrefab == null)
            {
                return false;
            }
            // Safeguard validation check since this method is called from outside of NetworkConfig and we can't control what's passed in.
            if (!networkPrefab.Validate())
            {
                return false;
            }

            uint source = networkPrefab.SourcePrefabGlobalObjectIdHash;
            uint target = networkPrefab.TargetPrefabGlobalObjectIdHash;

            // Make sure the prefab isn't already registered.
            if (NetworkPrefabOverrideLinks.ContainsKey(source))
            {
                var networkObject = networkPrefab.Prefab.GetComponent<NetworkObject>();

                // This should never happen, but in the case it somehow does log an error and remove the duplicate entry
                Debug.LogError($"{nameof(NetworkPrefab)} ({networkObject.name}) has a duplicate {nameof(NetworkObject.GlobalObjectIdHash)} source entry value of: {source}!");
                return false;
            }

            // If we don't have an override configured, registration is simple!
            if (networkPrefab.Override == NetworkPrefabOverride.None)
            {
                NetworkPrefabOverrideLinks.Add(source, networkPrefab);
                return true;
            }

            switch (networkPrefab.Override)
            {
                case NetworkPrefabOverride.Prefab:
                case NetworkPrefabOverride.Hash:
                    {
                        NetworkPrefabOverrideLinks.Add(source, networkPrefab);
                        if (!OverrideToNetworkPrefab.ContainsKey(target))
                        {
                            OverrideToNetworkPrefab.Add(target, source);
                        }
                    }
                    break;
            }

            return true;
        }
    }
}
