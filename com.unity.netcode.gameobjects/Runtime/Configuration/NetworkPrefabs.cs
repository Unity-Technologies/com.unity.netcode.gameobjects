using System;
using System.Collections.Generic;
using Unity.Netcode.Logging;
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
        /// or <see cref="NetworkObject.InstantiateAndSpawn(NetworkManager, ulong, bool, bool, bool, Vector3, Quaternion)"/>
        /// </summary>
        [NonSerialized]
        public Dictionary<uint, uint> OverrideToNetworkPrefab = new Dictionary<uint, uint>();

        /// <summary>
        /// Gets the read-only list of all registered network prefabs
        /// </summary>
        public IReadOnlyList<NetworkPrefab> Prefabs => m_Prefabs;

        [NonSerialized]
        private List<NetworkPrefab> m_Prefabs = new List<NetworkPrefab>();

        [NonSerialized]
        private List<NetworkPrefab> m_RuntimeAddedPrefabs = new List<NetworkPrefab>();

        private ContextualLogger m_Log;

        private bool m_Initialized;


        private void AddTriggeredByNetworkPrefabList(NetworkPrefab networkPrefab)
        {
            // We don't have to re-validate the prefab as the PrefabList will have validated before invoking this
            if (AddPrefabRegistrationPreValidated(networkPrefab))
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
        /// Finalizer that ensures proper cleanup of network prefab resources
        /// </summary>
        ~NetworkPrefabs()
        {
            Shutdown();
        }

        /// <summary>
        /// Deregister from add and remove events
        /// Clear the list
        /// </summary>
        internal void Shutdown()
        {
            m_Initialized = false;
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
        /// <param name="warnInvalid">When true, logs warnings about invalid prefabs that are removed during initialization</param>
        public void Initialize(bool warnInvalid = true)
        {
            Initialize(m_Log ?? new ContextualLogger(), warnInvalid);
        }

        internal void Initialize(ContextualLogger log, bool warnInvalid = true)
        {
            m_Log = log;
            m_Prefabs.Clear();
            NetworkPrefabsLists.RemoveAll(x => x == null);

            NetworkPrefabOverrideLinks.Clear();
            OverrideToNetworkPrefab.Clear();

            m_Prefabs = new List<NetworkPrefab>();

            List<NetworkPrefab> removeList = null;
            if (warnInvalid)
            {
                removeList = new List<NetworkPrefab>();
            }

            foreach (var list in NetworkPrefabsLists)
            {
                if (list == null)
                {
                    continue;
                }
                // Validate will remove any invalid items from the list
                list.BuildLogger();

                list.OnAdd += AddTriggeredByNetworkPrefabList;
                list.OnRemove += RemoveTriggeredByNetworkPrefabList;

                foreach (var networkPrefab in list.List)
                {
                    if (networkPrefab == null)
                    {
                        continue;
                    }

                    if (networkPrefab.Validate(list.Log) && AddPrefabRegistrationPreValidated(networkPrefab))
                    {
                        m_Prefabs.Add(networkPrefab);
                    }
                    else
                    {
                        removeList?.Add(networkPrefab);
                    }
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
                log.Warning(new Context(LogLevel.Error, "Removing invalid prefabs from Network Prefab registration"));
            }

            m_Initialized = true;
        }

        /// <summary>
        /// Add a new NetworkPrefab instance to the list
        /// </summary>
        /// <param name="networkPrefab">The NetworkPrefab to add</param>
        /// <returns>True if the prefab was successfully added, false if it was invalid or already registered</returns>
        /// <remarks>
        /// The framework does not synchronize this list between clients. Any runtime changes must be handled manually.
        ///
        /// Any modifications made here are not persisted. Permanent configuration changes should be done
        /// through the <see cref="NetworkPrefabsList"/> scriptable object property.
        /// </remarks>
        public bool Add(NetworkPrefab networkPrefab)
        {
            if (!m_Initialized)
            {
                m_RuntimeAddedPrefabs.Add(networkPrefab);
                return true;
            }

            if (AddPrefabRegistration(networkPrefab))
            {
                m_Prefabs.Add(networkPrefab);
                m_RuntimeAddedPrefabs.Add(networkPrefab);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove a NetworkPrefab instance from the list
        /// </summary>
        /// <param name="prefab">The NetworkPrefab to remove</param>
        /// <remarks>
        /// The framework does not synchronize this list between clients. Any runtime changes must be handled manually.
        ///
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
        /// Remove a NetworkPrefab instance with matching <see cref="NetworkPrefab.Prefab"/> from the list
        /// </summary>
        /// <param name="prefab">The GameObject to match against for removal</param>
        /// <remarks>
        /// The framework does not synchronize this list between clients. Any runtime changes must be handled manually.
        ///
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
        /// Check if the given GameObject is present as a prefab within the list
        /// </summary>
        /// <param name="prefab">The prefab to check</param>
        /// <returns>Whether or not the prefab exists</returns>
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
        /// Check if the given NetworkPrefab is present within the list
        /// </summary>
        /// <param name="prefab">The prefab to check</param>
        /// <returns>Whether or not the prefab exists</returns>
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
        /// Configures <see cref="NetworkPrefabOverrideLinks"/> for the given <see cref="NetworkPrefab"/>
        /// </summary>
        private bool AddPrefabRegistration(NetworkPrefab networkPrefab)
        {
            if (networkPrefab == null)
            {
                return false;
            }
            // Safeguard validation check since this method is called from outside of NetworkConfig and we can't control what's passed in.
            if (!networkPrefab.Validate(m_Log))
            {
                return false;
            }
            return AddPrefabRegistrationPreValidated(networkPrefab);
        }

        private bool AddPrefabRegistrationPreValidated(NetworkPrefab networkPrefab)
        {
            uint source = networkPrefab.SourcePrefabGlobalObjectIdHash;
            uint target = networkPrefab.TargetPrefabGlobalObjectIdHash;

            // Make sure the prefab isn't already registered.
            if (NetworkPrefabOverrideLinks.TryGetValue(source, out var otherPrefab))
            {
                // This should never happen, but in the case it somehow does log an error and remove the duplicate entry
                m_Log.Error(new Context(LogLevel.Error, $"{nameof(NetworkPrefab)} has a matching {nameof(NetworkObject.GlobalObjectIdHash)} with another object. This should not happen!").AddInfo(nameof(NetworkObject.GlobalObjectIdHash), source).AddInfo("Duplicated Object", otherPrefab.Prefab.name).AddObject(networkPrefab.Prefab));
                return false;
            }

            switch (networkPrefab.Override)
            {
                case NetworkPrefabOverride.None:
                    {
                        NetworkPrefabOverrideLinks.Add(source, networkPrefab);
                        break;
                    }
                case NetworkPrefabOverride.Prefab:
                case NetworkPrefabOverride.Hash:
                    {
                        NetworkPrefabOverrideLinks.Add(source, networkPrefab);
                        OverrideToNetworkPrefab.TryAdd(target, source);
                        break;
                    }
            }

            return true;
        }
    }
}
