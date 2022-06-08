using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unity.Netcode
{
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
        public NetworkPrefabsList NetworkPrefabsList;

        /// <summary>
        /// This dictionary provides a quick way to check and see if a NetworkPrefab has a NetworkPrefab override.
        /// Generated at runtime and OnValidate
        /// </summary>
        [NonSerialized]
        public Dictionary<uint, NetworkPrefab> NetworkPrefabOverrideLinks = new();

        [NonSerialized]
        public Dictionary<uint, uint> OverrideToNetworkPrefab = new();

        public IReadOnlyList<NetworkPrefab> Prefabs => m_Prefabs;

        [NonSerialized]
        private List<NetworkPrefab> m_Prefabs = new();

        /// <summary>
        /// Processes the <see cref="NetworkPrefabsList"/> if one is present for use during runtime execution,
        /// else processes <see cref="Prefabs"/>.
        /// </summary>
        public void Initialize(bool warnInvalid = true)
        {
            if (NetworkPrefabsList != null && m_Prefabs.Count > 0)
            {
                NetworkLog.LogWarning("Runtime Network Prefabs was not empty at initialization time. Network " +
                    "Prefab registrations made before initialization will be replaced by NetworkPrefabsList.");
                m_Prefabs.Clear();
            }

            NetworkPrefabOverrideLinks.Clear();
            OverrideToNetworkPrefab.Clear();

            List<NetworkPrefab> prefabs = NetworkPrefabsList != null && NetworkPrefabsList.List != null ? NetworkPrefabsList.List : m_Prefabs;
            m_Prefabs = new List<NetworkPrefab>();

            List<NetworkPrefab> removeList = null;
            if (warnInvalid)
            {
                removeList = new List<NetworkPrefab>();
            }

            if (prefabs != null)
            {
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
            }

            // Clear out anything that is invalid or not used
            if (removeList?.Count > 0)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    StringBuilder sb = new StringBuilder("Removing invalid prefabs from Network Prefab registration: ");
                    sb.AppendJoin(", ", removeList);
                    NetworkLog.LogWarning(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Add a new NetworkPrefab instance to the list
        /// </summary>
        /// <remarks>
        /// The framework does not synchronize this list between clients. Any runtime changes must be handled manually.
        ///
        /// Any modifications made here are not persisted. Permanent configuration changes should be done
        /// through the <see cref="NetworkPrefabsList"/> scriptable object property.
        /// </remarks>
        public bool Add(NetworkPrefab networkPrefab)
        {
            if (AddPrefabRegistration(networkPrefab))
            {
                m_Prefabs.Add(networkPrefab);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove a NetworkPrefab instance from the list
        /// </summary>
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
            OverrideToNetworkPrefab.Remove(prefab.TargetPrefabGlobalObjectIdHash);
            NetworkPrefabOverrideLinks.Remove(prefab.SourcePrefabGlobalObjectIdHash);
        }

        /// <summary>
        /// Remove a NetworkPrefab instance with matching <see cref="NetworkPrefab.Prefab"/> from the list
        /// </summary>
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
        }

        /// <summary>
        /// Configures <see cref="NetworkPrefabOverrideLinks"/> and <see cref="OverrideToNetworkPrefab"/> for the given <see cref="NetworkPrefab"/>
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

            // Make sure we don't have several overrides targeting the same prefab. Apparently we don't support that... shame.
            if (OverrideToNetworkPrefab.ContainsKey(target))
            {
                var networkObject = networkPrefab.Prefab.GetComponent<NetworkObject>();

                // This can happen if a user tries to make several GlobalObjectIdHash values point to the same target
                Debug.LogError($"{nameof(NetworkPrefab)} (\"{networkObject.name}\") has a duplicate {nameof(NetworkObject.GlobalObjectIdHash)} target entry value of: {target}!");
                return false;
            }

            switch (networkPrefab.Override)
            {
                case NetworkPrefabOverride.Prefab:
                {
                    NetworkPrefabOverrideLinks.Add(source, networkPrefab);
                    OverrideToNetworkPrefab.Add(target, source);
                }
                    break;
                case NetworkPrefabOverride.Hash:
                {
                    NetworkPrefabOverrideLinks.Add(source, networkPrefab);
                    OverrideToNetworkPrefab.Add(target, source);
                }
                    break;
            }

            return true;
        }
    }
}
