using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Spawning
{
    /// <summary>
    /// Interface for customizing, overriding, spawning, and destroying Network Prefabs
    /// Used by <see cref="NetworkPrefabHandler"/>
    /// </summary>
    public interface INetworkPrefabInstanceHandler
    {
        /// <summary>
        /// Client Side Only
        /// Once an implementation is registered with the <see cref="NetworkPrefabHandler"/>, this method will be called every time
        /// a Network Prefab associated <see cref="NetworkObject"/> is spawned on clients (excluding host since it is the server).
        ///
        /// Note on Pooling:  If you are using a NetworkObject pool, don't forget to make the NetworkObject active
        /// via the  <see cref="GameObject.SetActive(bool)"/> method.
        /// </summary>
        /// <param name="ownerClientId">the owner for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <param name="position">the initial/default position for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <param name="rotation">the initial/default rotation for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <returns></returns>
        NetworkObject ClientInstantiateOverride(ulong ownerClientId, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Client and Server
        /// Once an implementation is registered with the <see cref="NetworkPrefabHandler"/>, this method will be called every time
        /// a Network Prefab associated <see cref="NetworkObject"/> is destroyed.
        /// If <see cref="NetworkObject.Despawn(bool)"/> is invoked with the default destroy parameter (i.e. false) then this method will not be invoked.
        ///
        /// Note on Pooling: When invoked, you do not need to despawn or destroy the NetworkObject as long as you want your pool to persist.
        /// The most common approach is to make the <see cref="NetworkObject"/> inactive by calling <see cref="GameObject.SetActive(bool)"/>.
        /// </summary>
        /// <param name="networkObject">The <see cref="NetworkObject"/> being destroyed</param>
        void DestroyOverride(NetworkObject networkObject);
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
        /// Use a <see cref="GameObject"/> to register a class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface with the <see cref="NetworkPrefabHandler"/>
        /// </summary>
        /// <param name="networkPrefabAsset">the <see cref="GameObject"/> of the network prefab asset to be overridden</param>
        /// <param name="instanceHandler">class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface to be registered</param>
        /// <returns>true (registered) false (failed to register)</returns>
        public bool AddHandler(GameObject networkPrefabAsset, INetworkPrefabInstanceHandler instanceHandler)
        {
            return AddHandler(networkPrefabAsset.GetComponent<NetworkObject>().GlobalObjectIdHash, instanceHandler);
        }

        /// <summary>
        /// Use a  <see cref="NetworkObject"/> to register a class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface with the <see cref="NetworkPrefabHandler"/>
        /// </summary>
        /// <param name="prefabAssetNetworkObject"> the <see cref="NetworkObject"/> of the network prefab asset to be overridden</param>
        /// <param name="instanceHandler">the class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface to be registered</param>
        /// <returns></returns>
        public bool AddHandler(NetworkObject prefabAssetNetworkObject, INetworkPrefabInstanceHandler instanceHandler)
        {
            return AddHandler(prefabAssetNetworkObject.GlobalObjectIdHash, instanceHandler);
        }

        /// <summary>
        /// Use a <see cref="NetworkObject.GlobalObjectIdHash"/> to register a class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface with the <see cref="NetworkPrefabHandler"/>
        /// </summary>
        /// <param name="globalObjectIdHash"> the <see cref="NetworkObject.GlobalObjectIdHash"/> value of the network prefab asset being overridden</param>
        /// <param name="instanceHandler">a class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface</param>
        /// <returns></returns>
        public bool AddHandler(uint globalObjectIdHash, INetworkPrefabInstanceHandler instanceHandler)
        {
            if (!m_PrefabAssetToPrefabHandler.ContainsKey(globalObjectIdHash))
            {
                m_PrefabAssetToPrefabHandler.Add(globalObjectIdHash, instanceHandler);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Use the <see cref="GameObject"/> of the overridden network prefab asset to remove a registered class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface.
        /// </summary>
        /// <param name="networkPrefabAsset"><see cref="GameObject"/> of the network prefab asset that was being overridden</param>
        /// <returns>true or false</returns>
        public bool RemoveHandler(GameObject networkPrefabAsset)
        {
            return RemoveHandler(networkPrefabAsset.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        /// <summary>
        /// Use the <see cref="NetworkObject"/> of the overridden network prefab asset to remove a registered class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface.
        /// </summary>
        /// <param name="networkObject"></param>
        /// <returns>true or false</returns>
        public bool RemoveHandler(NetworkObject networkObject)
        {
            return RemoveHandler(networkObject.GlobalObjectIdHash);
        }

        /// <summary>
        /// Use the <see cref="NetworkObject.GlobalObjectIdHash"/> of the overridden network prefab asset to remove a registered class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface.
        /// </summary>
        /// <param name="networkPrefabHash"></param>
        /// <returns>true or false</returns>
        public bool RemoveHandler(uint globalObjectIdHash)
        {
            if (m_PrefabInstanceToPrefabAsset.ContainsValue(globalObjectIdHash))
            {
                uint networkPrefabHashKey = 0;
                foreach (var kvp in m_PrefabInstanceToPrefabAsset)
                {
                    if (kvp.Value == globalObjectIdHash)
                    {
                        networkPrefabHashKey = kvp.Key;
                        break;
                    }
                }
                m_PrefabInstanceToPrefabAsset.Remove(networkPrefabHashKey);
            }

            return m_PrefabAssetToPrefabHandler.Remove(globalObjectIdHash);
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
                var networkObjectInstance = m_PrefabAssetToPrefabHandler[networkPrefabAssetHash].ClientInstantiateOverride(ownerClientId, position, rotation);

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
        /// Note [both the client and server]: A NetworkObject that was instantiated via a INetworkPrefabInstanceHandler implementation
        /// will have this called in place of being destroyed.
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
                    m_PrefabAssetToPrefabHandler[networkPrefabAssetHash].DestroyOverride(networkObjectInstance);
                }
            }
        }
    }
}
