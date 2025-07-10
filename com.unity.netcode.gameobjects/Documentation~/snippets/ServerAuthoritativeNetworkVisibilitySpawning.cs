using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace Game.ServerAuthoritativeNetworkVisibilitySpawning
{
    /// <summary>
    /// A dynamic prefab loading use-case where the server instructs all clients to load a single network prefab via a
    /// ClientRpc, will spawn said prefab as soon as it is loaded on the server, and will mark it as network-visible
    /// only to clients that have already loaded that same prefab. As soon as a client loads the prefab locally, it
    /// sends an acknowledgement ServerRpc, and the server will mark that spawned NetworkObject as network-visible to
    /// that client.
    /// </summary>
    /// <remarks>
    /// An important implementation detail to note about this technique: the server will not wait until all clients have
    /// loaded a dynamic prefab before spawning the corresponding NetworkObject. Thus, this means that a NetworkObject
    /// will become network-visible for a connected client as soon as it has loaded it as well -- a client is not
    /// blocked by the loading operation of another client (which may be loading the asset slower or may have failed to
    /// load it at all). A consequence of this asynchronous loading technique is that clients may experience differing
    /// world versions momentarily. Therefore, we don't recommend using this technique for spawning game-changing
    /// gameplay elements (like a boss fight for example) assuming you'd want all clients to interact with the spawned
    /// NetworkObject as soon as it is spawned on the server.
    /// </remarks>
    public sealed class ServerAuthoritativeNetworkVisibilitySpawning : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;
        
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabReferences;
        
        [SerializeField] InGameUI m_InGameUI;
        
        const int k_MaxConnectedClientCount = 4;
        
        const int k_MaxConnectPayload = 1024;
        
        //A storage where we keep association between prefab (hash of it's GUID) and the spawned network objects that use it
        Dictionary<int, HashSet<NetworkObject>> m_PrefabHashToNetworkObjectId = new Dictionary<int, HashSet<NetworkObject>>();

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
            m_InGameUI.SpawnUsingVisibilityButtonPressed += OnClickedTrySpawnInvisible;
        }
        
        public override void OnDestroy()
        {
            m_NetworkManager.ConnectionApprovalCallback -= ConnectionApprovalCallback;
            if (m_InGameUI)
            {
                m_InGameUI.SpawnUsingVisibilityButtonPressed -= OnClickedTrySpawnInvisible;
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
        void OnClickedTrySpawnInvisible()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }
            
            TrySpawnInvisible();
        }

        async void TrySpawnInvisible()
        {
            var randomPrefab = m_DynamicPrefabReferences[Random.Range(0, m_DynamicPrefabReferences.Count)];
            await SpawnImmediatelyAndHideUntilPrefabIsLoadedOnClient(randomPrefab.AssetGUID, Random.insideUnitCircle * 5, Quaternion.identity);
        }
        
        /// <summary>
        /// This call spawns an addressable prefab by it's guid. It does not ensure that all the clients have loaded the
        /// prefab before spawning it. All spawned objects are network-invisible to clients that don't have the prefab
        /// loaded. The server tells the clients that lack the preloaded prefab to load it and acknowledge that they've
        /// loaded it, and then the server makes the object network-visible to that client.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        async Task<NetworkObject> SpawnImmediatelyAndHideUntilPrefabIsLoadedOnClient(string guid, Vector3 position, Quaternion rotation)
        {
            if (IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };
                
                return await Spawn(assetGuid);
            }

            return null;

            async Task<NetworkObject> Spawn(AddressableGUID assetGuid)
            {
                // server is starting to load a prefab, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);
                
                var prefab = await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, 
                    m_InGameUI.ArtificialDelayMilliseconds);
                
                // server loaded a prefab, update UI with the loaded asset's name
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);
                
                var obj = Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
                
                if (m_PrefabHashToNetworkObjectId.TryGetValue(assetGuid.GetHashCode(), out var networkObjectIds))
                {
                    networkObjectIds.Add(obj);
                }
                else
                {
                    m_PrefabHashToNetworkObjectId.Add(assetGuid.GetHashCode(), new HashSet<NetworkObject>() {obj});
                }

                // This gets called on spawn and makes sure clients currently syncing and receiving spawns have the
                // appropriate network visibility settings automatically. This can happen on late join, on spawn, on
                // scene switch, etc.
                obj.CheckObjectVisibility = (clientId) => 
                {
                    if (clientId == NetworkManager.ServerClientId)
                    {
                        // object is loaded on the server, no need to validate for visibility
                        return true;
                    }
                    
                    //if the client has already loaded the prefab - we can make the object network-visible to them
                    if (DynamicPrefabLoadingUtilities.HasClientLoadedPrefab(clientId, assetGuid.GetHashCode()))
                    {
                        return true;
                    }
                    
                    // client is loading a prefab, update UI
                    m_InGameUI.ClientLoadedPrefabStatusChanged(clientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);
                    
                    //otherwise the clients need to load the prefab, and after they ack - the ShowHiddenObjectsToClient 
                    LoadAddressableClientRpc(assetGuid, new ClientRpcParams(){Send = new ClientRpcSendParams(){TargetClientIds = new ulong[]{clientId}}});
                    return false;
                };
                
                obj.Spawn();

                return obj;
            }
        }
        
        void ShowHiddenObjectsToClient(int prefabHash, ulong clientId)
        {
            if (m_PrefabHashToNetworkObjectId.TryGetValue(prefabHash, out var networkObjects))
            {
                foreach (var obj in networkObjects)
                {
                    if (!obj.IsNetworkVisibleTo(clientId))
                    {
                        obj.NetworkShow(clientId);
                    }
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
            DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAPrefab(prefabHash, 
                rpcParams.Receive.SenderClientId);
           
            //the server has all the objects network-visible, no need to do anything
            if (rpcParams.Receive.SenderClientId != m_NetworkManager.LocalClientId)
            {
                // Note: there's a potential security risk here if this technique is tied with gameplay that uses
                // a NetworkObject's Show() and Hide() methods. For example, a malicious player could invoke a similar
                // ServerRpc with the guids of enemy players, and it would make those enemies visible (network side
                // and/or visually) to that player, giving them a potential advantage.
                ShowHiddenObjectsToClient(prefabHash, rpcParams.Receive.SenderClientId);
            }
            
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
