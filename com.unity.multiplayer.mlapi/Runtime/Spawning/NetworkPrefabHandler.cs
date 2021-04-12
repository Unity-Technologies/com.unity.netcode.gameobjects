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

        /// <summary>
        /// Use a GameObject to add a INetworkPrefabInstanceHandler derived class
        /// </summary>
        /// <param name="networkPrefabAsset"></param>
        /// <param name="instanceHandler"></param>
        /// <returns></returns>
        public bool AddHandler(GameObject networkPrefabAsset, INetworkPrefabInstanceHandler instanceHandler)
        {
            return AddHandler(networkPrefabAsset.GetComponent<NetworkObject>().GlobalObjectIdHash, instanceHandler);
        }

        /// <summary>
        /// Use a NetworkObject to add a INetworkPrefabInstanceHandler derived class
        /// </summary>
        /// <param name="prefabAssetNetworkObject"></param>
        /// <param name="instanceHandler"></param>
        /// <returns></returns>
        public bool AddHandler(NetworkObject prefabAssetNetworkObject, INetworkPrefabInstanceHandler instanceHandler)
        {
            return AddHandler(prefabAssetNetworkObject.GlobalObjectIdHash, instanceHandler);
        }

        /// <summary>
        /// Use a networkPrefabHash(GlobalObjectIdHash) to add a INetworkPrefabInstanceHandler derived class
        /// </summary>
        /// <param name="networkPrefabHash"></param>
        /// <param name="instanceHandler"></param>
        /// <returns></returns>
        public bool AddHandler(uint networkPrefabHash, INetworkPrefabInstanceHandler instanceHandler)
        {
            if (!m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabHash))
            {
                m_PrefabAssetToPrefabHandler.Add(networkPrefabHash, instanceHandler);
                return true;
            }

            return false;
        }

        /// <summary>
        ///  Use the GameObject of the network prefab asset to remove a INetworkPrefabInstanceHandler derived class
        /// </summary>
        /// <param name="networkPrefabAsset"></param>
        /// <returns>true or false</returns>
        public bool RemoveHandler(GameObject networkPrefabAsset)
        {
            return RemoveHandler(networkPrefabAsset.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        /// <summary>
        ///  Use the NetworkObject of the network prefab asset to remove a INetworkPrefabInstanceHandler derived class
        /// </summary>
        /// <param name="networkObject"></param>
        /// <returns>true or false</returns>
        public bool RemoveHandler(NetworkObject networkObject)
        {
            return RemoveHandler(networkObject.GlobalObjectIdHash);
        }

        /// <summary>
        ///  Use the networkPrefabHash(GlobalObjectIdHash) of the network prefab asset to remove a INetworkPrefabInstanceHandler derived class
        /// </summary>
        /// <param name="networkPrefabHash"></param>
        /// <returns>true or false</returns>
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

        /// <summary>
        /// Check to see if a GameObject with a NetworkObject component has an INetworkPrefabInstanceHandler derived class associated with it
        /// </summary>
        /// <param name="networkPrefab"></param>
        /// <returns>true or false</returns>
        internal bool ContainsHandler(GameObject networkPrefab)
        {
            return ContainsHandler(networkPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        /// <summary>
        /// Check to see if a NetworkObject component has an INetworkPrefabInstanceHandler derived class associated with it
        /// </summary>
        /// <param name="networkObject"></param>
        /// <returns>true or false</returns>
        internal bool ContainsHandler(NetworkObject networkObject)
        {
            return ContainsHandler(networkObject.GlobalObjectIdHash);
        }

        /// <summary>
        /// Check to see if a networkPrefabHash(GlobalObjectIdHash) component has an INetworkPrefabInstanceHandler derived class associated with it
        /// </summary>
        /// <param name="networkPrefabHash"></param>
        /// <returns>true or false</returns>
        internal bool ContainsHandler(uint networkPrefabHash)
        {
            return m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabHash) || m_PrefabInstanceToPrefabAsset.ContainsKey(networkPrefabHash);
        }

        /// <summary>
        /// Will return back a NetworkObject generated via the INetworkPrefabInstanceHandler derived class associated with the networkPrefabAssetHash
        /// Invoked only on the client side and called within NetworkSpawnManager.CreateLocalNetworkObject
        /// </summary>
        /// <param name="networkPrefabAssetHash">typically the "server-side" asset's prefab hash</param>
        /// <param name="ownerClientId"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        internal NetworkObject HandleNetworkPrefabSpawn(uint networkPrefabAssetHash, ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            if (m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabAssetHash))
            {
                var networkObjectInstance = m_PrefabAssetToPrefabHandler[networkPrefabAssetHash].HandleNetworkPrefabSpawn(ownerClientId, position, rotation);

                //Now we must make sure this alternate PrefabAsset spawned in place of the prefab asset with the networkPrefabAssetHash (GlobalObjectIdHash)
                //is registered and linked to the networkPrefabAssetHash so during the HandleNetworkPrefabDestroy process we can identify the alternate prefab asset.
                if (networkObjectInstance != null && !m_PrefabInstanceToPrefabAsset.ContainsKey(networkObjectInstance.GlobalObjectIdHash))
                {
                    m_PrefabInstanceToPrefabAsset.Add(networkObjectInstance.GlobalObjectIdHash, networkPrefabAssetHash);
                }

                return networkObjectInstance;
            }

            return null;
        }

        /// <summary>
        /// Will invoke the NetworkPrefabInstanceHandler derived class HandleNetworkPrefabDestroy method
        /// Note: On the client, when a custom NetworkObject is instantiated in place of the original PrefabAsset
        /// the newly generated NetworkObject will
        /// </summary>
        /// <param name="networkObjectInstance"></param>
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
