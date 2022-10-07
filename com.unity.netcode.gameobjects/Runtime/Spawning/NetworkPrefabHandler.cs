using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
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
        /// a Network Prefab associated <see cref="NetworkObject"/> is spawned on clients
        ///
        /// Note On Hosts: Use the <see cref="NetworkPrefabHandler.RegisterHostGlobalObjectIdHashValues(GameObject, List{GameObject})"/>
        /// method to register all targeted NetworkPrefab overrides manually since the host will be acting as both a server and client.
        ///
        /// Note on Pooling:  If you are using a NetworkObject pool, don't forget to make the NetworkObject active
        /// via the  <see cref="GameObject.SetActive(bool)"/> method.
        /// </summary>
        /// <param name="ownerClientId">the owner for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <param name="position">the initial/default position for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <param name="rotation">the initial/default rotation for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <returns></returns>
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Invoked on Client and Server
        /// Once an implementation is registered with the <see cref="NetworkPrefabHandler"/>, this method will be called when
        /// a Network Prefab associated <see cref="NetworkObject"/> is:
        ///
        /// Server Side: destroyed or despawned with the destroy parameter equal to true
        /// If <see cref="NetworkObject.Despawn(bool)"/> is invoked with the default destroy parameter (i.e. false) then this method will NOT be invoked!
        ///
        /// Client Side: destroyed when the client receives a destroy object message from the server or host.
        ///
        /// Note on Pooling: When this method is invoked, you do not need to destroy the NetworkObject as long as you want your pool to persist.
        /// The most common approach is to make the <see cref="NetworkObject"/> inactive by calling <see cref="GameObject.SetActive(bool)"/>.
        /// </summary>
        /// <param name="networkObject">The <see cref="NetworkObject"/> being destroyed</param>
        void Destroy(NetworkObject networkObject);
    }

    /// <summary>
    /// Primary handler to add or remove customized spawn and destroy handlers for a network prefab (i.e. a prefab with a NetworkObject component)
    /// Register custom prefab handlers by implementing the <see cref="INetworkPrefabInstanceHandler"/> interface.
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
        /// HOST ONLY!
        /// Since a host is unique and is considered both a client and a server, for each source NetworkPrefab you must manually
        /// register all potential <see cref="GameObject"/> target overrides that have the <see cref="NetworkObject"/> component.
        /// </summary>
        /// <param name="sourceNetworkPrefab">source NetworkPrefab to be overridden</param>
        /// <param name="networkPrefabOverrides">one or more NetworkPrefabs could be used to override the source NetworkPrefab</param>
        public void RegisterHostGlobalObjectIdHashValues(GameObject sourceNetworkPrefab, List<GameObject> networkPrefabOverrides)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    var sourceNetworkObject = sourceNetworkPrefab.GetComponent<NetworkObject>();
                    if (sourceNetworkPrefab != null)
                    {
                        var sourceGlobalObjectIdHash = sourceNetworkObject.GlobalObjectIdHash;
                        // Now we register all
                        foreach (var gameObject in networkPrefabOverrides)
                        {
                            if (gameObject.TryGetComponent<NetworkObject>(out var targetNetworkObject))
                            {
                                if (!m_PrefabInstanceToPrefabAsset.ContainsKey(targetNetworkObject.GlobalObjectIdHash))
                                {
                                    m_PrefabInstanceToPrefabAsset.Add(targetNetworkObject.GlobalObjectIdHash, sourceGlobalObjectIdHash);
                                }
                                else
                                {
                                    Debug.LogWarning($"{targetNetworkObject.name} appears to be a duplicate entry!");
                                }
                            }
                            else
                            {
                                throw new System.Exception($"{targetNetworkObject.name} does not have a {nameof(NetworkObject)} component!");
                            }
                        }
                    }
                    else
                    {
                        throw new System.Exception($"{sourceNetworkPrefab.name} does not have a {nameof(NetworkObject)} component!");
                    }
                }
                else
                {
                    throw new System.Exception($"You should only call {nameof(RegisterHostGlobalObjectIdHashValues)} as a Host!");
                }
            }
            else
            {
                throw new System.Exception($"You can only call {nameof(RegisterHostGlobalObjectIdHashValues)} once NetworkManager is listening!");
            }
        }

        /// <summary>
        /// Use the <see cref="GameObject"/> of the overridden network prefab asset to remove a registered class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface.
        /// </summary>
        /// <param name="networkPrefabAsset"><see cref="GameObject"/> of the network prefab asset that was being overridden</param>
        /// <returns>true (success) or false (failure)</returns>
        public bool RemoveHandler(GameObject networkPrefabAsset)
        {
            return RemoveHandler(networkPrefabAsset.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        /// <summary>
        /// Use the <see cref="NetworkObject"/> of the overridden network prefab asset to remove a registered class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface.
        /// </summary>
        /// <param name="networkObject"><see cref="NetworkObject"/> of the source NetworkPrefab that was being overridden</param>
        /// <returns>true (success) or false (failure)</returns>
        public bool RemoveHandler(NetworkObject networkObject)
        {
            return RemoveHandler(networkObject.GlobalObjectIdHash);
        }

        /// <summary>
        /// Use the <see cref="NetworkObject.GlobalObjectIdHash"/> of the overridden network prefab asset to remove a registered class that implements the <see cref="INetworkPrefabInstanceHandler"/> interface.
        /// </summary>
        /// <param name="globalObjectIdHash"><see cref="NetworkObject.GlobalObjectIdHash"/> of the source NetworkPrefab that was being overridden</param>
        /// <returns>true (success) or false (failure)</returns>
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
        /// Check to see if a <see cref="GameObject"/> with a <see cref="NetworkObject"/> is registered to an <see cref="INetworkPrefabInstanceHandler"/> implementation
        /// </summary>
        /// <param name="networkPrefab"></param>
        /// <returns>true or false</returns>
        internal bool ContainsHandler(GameObject networkPrefab)
        {
            return ContainsHandler(networkPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        /// <summary>
        /// Check to see if a <see cref="NetworkObject"/> is registered to an <see cref="INetworkPrefabInstanceHandler"/> implementation
        /// </summary>
        /// <param name="networkObject"></param>
        /// <returns>true or false</returns>
        internal bool ContainsHandler(NetworkObject networkObject)
        {
            return ContainsHandler(networkObject.GlobalObjectIdHash);
        }

        /// <summary>
        /// Check to see if a <see cref="NetworkObject.GlobalObjectIdHash"/> is registered to an <see cref="INetworkPrefabInstanceHandler"/> implementation
        /// </summary>
        /// <param name="networkPrefabHash"></param>
        /// <returns>true or false</returns>
        internal bool ContainsHandler(uint networkPrefabHash)
        {
            return m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabHash) || m_PrefabInstanceToPrefabAsset.ContainsKey(networkPrefabHash);
        }

        /// <summary>
        /// Returns the source NetworkPrefab's <see cref="NetworkObject.GlobalObjectIdHash"/>
        /// </summary>
        /// <param name="networkPrefabHash"></param>
        /// <returns></returns>
        internal uint GetSourceGlobalObjectIdHash(uint networkPrefabHash)
        {
            if (m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabHash))
            {
                return networkPrefabHash;
            }
            else if (m_PrefabInstanceToPrefabAsset.ContainsKey(networkPrefabHash))
            {
                return m_PrefabInstanceToPrefabAsset[networkPrefabHash];
            }

            return 0;
        }

        /// <summary>
        /// Will return back a <see cref="NetworkObject"/> generated via an <see cref="INetworkPrefabInstanceHandler"/> implementation
        /// Note: Invoked only on the client side and called within NetworkSpawnManager.CreateLocalNetworkObject
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
                var networkObjectInstance = m_PrefabAssetToPrefabHandler[networkPrefabAssetHash].Instantiate(ownerClientId, position, rotation);

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
        /// Will invoke the <see cref="INetworkPrefabInstanceHandler"/> implementation's Destroy method
        /// </summary>
        /// <param name="networkObjectInstance"></param>
        internal void HandleNetworkPrefabDestroy(NetworkObject networkObjectInstance)
        {
            var networkObjectInstanceHash = networkObjectInstance.GlobalObjectIdHash;

            // Do we have custom overrides registered?
            if (m_PrefabInstanceToPrefabAsset.ContainsKey(networkObjectInstanceHash))
            {
                var networkPrefabAssetHash = m_PrefabInstanceToPrefabAsset[networkObjectInstanceHash];
                if (m_PrefabAssetToPrefabHandler.ContainsKey(networkPrefabAssetHash))
                {
                    m_PrefabAssetToPrefabHandler[networkPrefabAssetHash].Destroy(networkObjectInstance);
                }
            }
            else // Otherwise the NetworkObject is the source NetworkPrefab
            if (m_PrefabAssetToPrefabHandler.ContainsKey(networkObjectInstanceHash))
            {
                m_PrefabAssetToPrefabHandler[networkObjectInstanceHash].Destroy(networkObjectInstance);
            }
        }
    }
}
