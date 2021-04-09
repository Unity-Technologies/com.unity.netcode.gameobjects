using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Spawning
{
    /// <summary>
    /// Interface for customizing asset spawn and destroy handlers
    /// NOTE: Custom spawn and destroy handlers are only invoked on clients
    /// </summary>
    public interface INetworkPrefabInstanceHandler
    {
        NetworkObject HandleNetworkPrefabSpawn(ulong ownerClientId, Vector3 position, Quaternion rotation);
        void HandleNetworkPrefabDestroy(NetworkObject networkObject);
    }

    /// <summary>
    /// Primary handler to add or remove customized spawn and destroy handlers for a network prefab (i.e. a prefab with a NetworkObject component)
    /// </summary>
    public class NetworkPrefabHandler
    {
        /// <summary>
        /// Links a network prefab asset to a class with the INetworkPrefabInstanceHandler interface
        /// </summary>
        private readonly Dictionary<uint, INetworkPrefabInstanceHandler> m_PrefabAssetToPrefabHandler = new Dictionary<uint, INetworkPrefabInstanceHandler>();

        /// <summary>
        /// Links the custom prefab instance's GlobalNetworkObjectId to the original prefab asset's GlobalNetworkObjectId.  (Needed for HandleNetworkPrefabDestroy)
        /// [PrefabInstance][PrefabAsset]
        /// </summary>
        private readonly Dictionary<uint, uint> m_PrefabInstanceToPrefabAsset = new Dictionary<uint, uint>();

        public bool AddHandler(GameObject networkPrefabAsset, INetworkPrefabInstanceHandler instanceHandler)
        {
            return AddHandler(networkPrefabAsset.GetComponent<NetworkObject>().GlobalObjectIdHash, instanceHandler);
        }

        public bool AddHandler(NetworkObject prefabAssetNetworkObject, INetworkPrefabInstanceHandler instanceHandler)
        {
            return AddHandler(prefabAssetNetworkObject.GlobalObjectIdHash, instanceHandler);
        }

        public bool AddHandler(uint networkPrefabHash, INetworkPrefabInstanceHandler instanceHandler)
        {
            if (!m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabHash))
            {
                m_PrefabAssetToPrefabHandler.Add(networkPrefabHash, instanceHandler);
                return true;
            }

            return false;
        }

        public bool RemoveHandler(GameObject networkPrefabAsset)
        {
            return RemoveHandler(networkPrefabAsset.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        public bool RemoveHandler(NetworkObject networkObject)
        {
            return RemoveHandler(networkObject.GlobalObjectIdHash);
        }

        public bool RemoveHandler(uint networkPrefabHash)
        {
            if (m_PrefabInstanceToPrefabAsset.ContainsValue(networkPrefabHash))
            {
                uint networkPrefabHashKey = 0;
                foreach (var kvp in m_PrefabInstanceToPrefabAsset)
                {
                    if (kvp.Value == networkPrefabHash)
                    {
                        networkPrefabHashKey = kvp.Key;
                        break;
                    }
                }
                m_PrefabInstanceToPrefabAsset.Remove(networkPrefabHashKey);
            }

            return m_PrefabAssetToPrefabHandler.Remove(networkPrefabHash);
        }

        internal bool ContainsHandler(GameObject networkPrefab)
        {
            return ContainsHandler(networkPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        internal bool ContainsHandler(NetworkObject networkObject)
        {
            return ContainsHandler(networkObject.GlobalObjectIdHash);
        }

        internal bool ContainsHandler(uint networkPrefabHash)
        {
            return m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabHash) || m_PrefabInstanceToPrefabAsset.ContainsKey(networkPrefabHash);
        }

        internal NetworkObject HandleNetworkPrefabSpawn(uint networkPrefabAssetHash, ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            if (m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabAssetHash))
            {
                var networkObjectInstance = m_PrefabAssetToPrefabHandler[networkPrefabAssetHash].HandleNetworkPrefabSpawn(ownerClientId, position, rotation);
                if (networkObjectInstance != null && !m_PrefabInstanceToPrefabAsset.ContainsKey(networkObjectInstance.GlobalObjectIdHash))
                {
                    m_PrefabInstanceToPrefabAsset.Add(networkObjectInstance.GlobalObjectIdHash, networkPrefabAssetHash);
                }

                return networkObjectInstance;
            }

            return null;
        }

        internal void HandleNetworkPrefabDestroy(NetworkObject networkObjectInstance)
        {
            var networkObjectInstanceHash = networkObjectInstance.GlobalObjectIdHash;
            if (m_PrefabInstanceToPrefabAsset.ContainsKey(networkObjectInstanceHash))
            {
                var networkPrefabAssetHash = m_PrefabInstanceToPrefabAsset[networkObjectInstanceHash];
                if (m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabAssetHash))
                {
                    m_PrefabAssetToPrefabHandler[networkPrefabAssetHash].HandleNetworkPrefabDestroy(networkObjectInstance);
                }
            }
        }
    }
}
