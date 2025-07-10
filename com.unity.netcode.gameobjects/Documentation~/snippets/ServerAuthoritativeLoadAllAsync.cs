using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.ServerAuthoritativeLoadAllAsync
{
    /// <summary>
    /// A simple use-case where the server notifies all clients to preload a collection of network prefabs. The server
    /// will not invoke a spawn in this use-case, and will incrementally load each dynamic prefab, one prefab at a time.
    /// </summary>
    /// <remarks>
    /// A gameplay scenario where this technique would be useful: clients and host are connected, the host arrives at a
    /// point in the game where they know some prefabs will be needed soon, and so the server instructs all clients to
    /// preemptively load those prefabs. Some time later in the same session, the server needs to perform a spawn, and
    /// can do so without making sure all clients have loaded said dynamic prefab, since it already did so preemptively.
    /// This allows for simple spawn management.
    /// </remarks>
    public sealed class ServerAuthoritativeLoadAllAsync : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;
        
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabReferences;

        [SerializeField]
        InGameUI m_InGameUI;
        
        const int k_MaxConnectedClientCount = 4;

        const int k_MaxConnectPayload = 1024;
        
        void Start()
        {
            DynamicPrefabLoadingUtilities.Init(m_NetworkManager);
            
            // In the use-cases where connection approval is implemented, the server can begin to validate a user's
            // connection payload, and either approve or deny connection to the joining client.
            // Note: we will define a very simplistic connection approval below, which will effectively deny all
            // late-joining clients unless the server has not loaded any dynamic prefabs. You could choose to not define
            // a connection approval callback, but late-joining clients will have mismatching NetworkConfigs (and  
            // potentially different world versions if the server has spawned a dynamic prefab).
            m_NetworkManager.NetworkConfig.ConnectionApproval = true;
            
            // Here, we keep ForceSamePrefabs disabled. This will allow us to dynamically add network prefabs to Netcode
            // for GameObject after establishing a connection.
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            
            // This is a simplistic use-case of a connection approval callback. To see how a connection approval should
            // be used to validate a user's connection payload, see the connection approval use-case, or the
            // APIPlayground, where all post-connection techniques are used in harmony.
            m_NetworkManager.ConnectionApprovalCallback += ConnectionApprovalCallback;
            
            // hooking up UI callbacks
            m_InGameUI.LoadAllAsyncButtonPressed += OnClickedPreload;
        }

        public override void OnDestroy()
        {
            m_NetworkManager.ConnectionApprovalCallback -= ConnectionApprovalCallback;
            if (m_InGameUI)
            {
                m_InGameUI.LoadAllAsyncButtonPressed -= OnClickedPreload;
            }
            DynamicPrefabLoadingUtilities.UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }
        
        void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            Debug.Log("Client is trying to connect " + request.ClientNetworkId);
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            
            if (clientId == m_NetworkManager.LocalClientId)
            {
                // allow the host to connect
                Approve();
                return;
            }
            
            // A sample-specific denial on clients after k_MaxConnectedClientCount clients have been connected
            if (m_NetworkManager.ConnectedClientsList.Count >= k_MaxConnectedClientCount)
            {
                ImmediateDeny();
                return;
            }
            
            if (connectionData.Length > k_MaxConnectPayload)
            {
                // If connectionData is too big, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                ImmediateDeny();
                return;
            }
            
            // simple approval if the server has not loaded any dynamic prefabs yet
            if (DynamicPrefabLoadingUtilities.LoadedPrefabCount == 0)
            {
                Approve();
            }
            else
            {
                ImmediateDeny();
            }
            
            void Approve()
            {
                Debug.Log($"Client {clientId} approved");
                response.Approved = true;
                response.CreatePlayerObject = false; //we're not going to spawn a player object for this sample
            }
            
            void ImmediateDeny()
            {
                Debug.Log($"Client {clientId} denied connection");
                response.Approved = false;
                response.CreatePlayerObject = false;
            }
        }
        
        // invoked by UI
        public void OnClickedPreload()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }
            
            PreloadPrefabs();
        }

        async void PreloadPrefabs()
        {
            var tasks = new List<Task>();
            foreach (var p in m_DynamicPrefabReferences)
            {
                tasks.Add(PreloadDynamicPrefabOnServerAndStartLoadingOnAllClients(p.AssetGUID));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// This call preloads the dynamic prefab on the server and sends a client rpc to all the clients to do the same.
        /// </summary>
        /// <param name="guid"></param>
        async Task PreloadDynamicPrefabOnServerAndStartLoadingOnAllClients(string guid)
        {
            if (m_NetworkManager.IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };
                
                if (DynamicPrefabLoadingUtilities.IsPrefabLoadedOnAllClients(assetGuid))
                {
                    Debug.Log("Prefab is already loaded by all peers");
                    return;
                }
                
                // update UI for each client that is requested to load a certain prefab
                foreach (var client in m_NetworkManager.ConnectedClients.Keys)
                {
                    m_InGameUI.ClientLoadedPrefabStatusChanged(client, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);
                }
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);
                
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_InGameUI.ArtificialDelayMilliseconds);
                
                // server loaded a prefab, update UI with the loaded asset's name
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                
                // every client loaded dynamic prefab, their respective ClientUIs in case they loaded first
                foreach (var client in m_NetworkManager.ConnectedClients.Keys)
                {
                    m_InGameUI.ClientLoadedPrefabStatusChanged(client,
                        assetGuid.GetHashCode(), 
                        loadedGameObject.Result.name, 
                        InGameUI.LoadStatus.Loading);
                }
            }
        }
        
        [ClientRpc]
        void LoadAddressableClientRpc(AddressableGUID guid, ClientRpcParams rpcParams = default)
        {
            if (!IsHost)
            {
                Load(guid);
            }

            async void Load(AddressableGUID assetGuid)
            {
                // loading prefab as a client, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(m_NetworkManager.LocalClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);
                
                Debug.Log("Loading dynamic prefab on the client...");
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_InGameUI.ArtificialDelayMilliseconds);
                Debug.Log("Client loaded dynamic prefab");
                
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(m_NetworkManager.LocalClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);
                
                AcknowledgeSuccessfulPrefabLoadServerRpc(assetGuid.GetHashCode());
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(int prefabHash, ServerRpcParams rpcParams = default)
        {
            Debug.Log($"Client acknowledged successful prefab load with hash: {prefabHash}");
            DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAPrefab(prefabHash, rpcParams.Receive.SenderClientId);
            
            // a quick way to grab a matching prefab reference's name via its prefabHash
            var loadedPrefabName = "Undefined";
            foreach (var prefabReference in m_DynamicPrefabReferences)
            {
                var prefabReferenceGuid = new AddressableGUID() { Value = prefabReference.AssetGUID };
                if (prefabReferenceGuid.GetHashCode() == prefabHash)
                {
                    // found the matching prefab reference
                    if (DynamicPrefabLoadingUtilities.LoadedDynamicPrefabResourceHandles.TryGetValue(
                            prefabReferenceGuid, 
                            out var loadedGameObject))
                    {
                        // if it is loaded on the server, update the name on the ClientUI
                        loadedPrefabName = loadedGameObject.Result.name;
                    }
                    break;
                }
            }
            
            // client has successfully loaded a prefab, update UI
            m_InGameUI.ClientLoadedPrefabStatusChanged(rpcParams.Receive.SenderClientId, prefabHash, loadedPrefabName, InGameUI.LoadStatus.Loaded);
        }
    }
}
