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
        /// Note On Hosts: Use the <see cref="NetworkPrefabHandler.RegisterHostGlobalObjectIdHashValues(GameObject, List{T})"/>
        /// method to register all targeted NetworkPrefab overrides manually since the host will be acting as both a server and client.
        ///
        /// Note on Pooling:  If you are using a NetworkObject pool, don't forget to make the NetworkObject active
        /// via the  <see cref="GameObject.SetActive(bool)"/> method.
        /// </summary>
        /// <remarks>
        /// If you need to pass custom data at instantiation time (e.g., selecting a variant, setting initialization parameters, or choosing a pre-instantiated object),
        /// implement <see cref="NetworkPrefabInstanceHandlerWithData{T}"/> instead.
        /// </remarks>
        /// <param name="ownerClientId">the owner for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <param name="position">the initial/default position for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <param name="rotation">the initial/default rotation for the <see cref="NetworkObject"/> to be instantiated</param>
        /// <returns>The instantiated NetworkObject instance. Returns null if instantiation fails.</returns>
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation);

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
        public void Destroy(NetworkObject networkObject);
    }
}
