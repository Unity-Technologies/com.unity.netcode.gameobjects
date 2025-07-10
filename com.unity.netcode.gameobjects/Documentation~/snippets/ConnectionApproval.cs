using Game.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.ConnectionApproval
{
    /// <summary>
    /// A class that walks through what a server would need to validate from a client when dynamically loading network
    /// prefabs. This is another simple use-case scenario, as this is just the implementation of the connection approval
    /// callback, which is an optional feature from Netcode for GameObjects. To enable it, make sure the "Connection
    /// Approval" toggle is enabled on the NetworkManager in your scene. Other use-cases don't allow for reconciliation
    /// after the server has loaded a prefab dynamically, whereas this one enables that functionality. To see it all in
    /// harmony, see <see cref="APIPlayground"/>, where all post-connection techniques are showcased in one scene.
    /// </summary>
    public sealed class ConnectionApproval : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;

        [SerializeField]
        AssetReferenceGameObject m_AssetReferenceGameObject;
        
        [SerializeField] InGameUI m_InGameUI;
        
        const int k_MaxConnectedClientCount = 4;
        
        const int k_MaxConnectPayload = 1024;
        
        void Start()
        {
            DynamicPrefabLoadingUtilities.Init(m_NetworkManager);

            // In the use-cases where connection approval is implemented, the server can begin to validate a user's
            // connection payload, and either approve or deny connection to the joining client.
            m_NetworkManager.NetworkConfig.ConnectionApproval = true;
            
            // Here, we keep ForceSamePrefabs disabled. This will allow us to dynamically add network prefabs to Netcode
            // for GameObject after establishing a connection. In this implementation of the connection approval
            // callback, the server validates the client's connection payload based on the hash of their dynamic prefabs
            // loaded, and either approves or denies connection to the joining client. If a client is denied connection,
            // the server provides a disconnection payload through NetworkManager's DisconnectReason, so that a
            // late-joining client can load dynamic prefabs locally and reattempt connection.
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            m_NetworkManager.ConnectionApprovalCallback += ConnectionApprovalCallback;

            // to force a simple connection approval on all joining clients, the server will load a dynamic prefab as
            // soon as the server is started
            // for more complex use-cases where the server must wait for all connected clients to load the same network
            // prefab, see the other use-cases inside this sample
            m_NetworkManager.OnServerStarted += LoadAPrefab;
        }

        async void LoadAPrefab()
        {
            var assetGuid = new AddressableGUID() { Value = m_AssetReferenceGameObject.AssetGUID }; 
            
            // server is starting to load a prefab, update UI
            m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, 
                assetGuid.GetHashCode(), 
                "Undefined", 
                InGameUI.LoadStatus.Loading);
            
            await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, 0);
            
            // server loaded a prefab, update UI with the loaded asset's name
            DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
            m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);
        }

        public override void OnDestroy()
        {
            m_NetworkManager.ConnectionApprovalCallback -= ConnectionApprovalCallback;
            m_NetworkManager.OnServerStarted -= LoadAPrefab;
            DynamicPrefabLoadingUtilities.UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }
    
        void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            Debug.Log($"Client {request.ClientNetworkId} is trying to connect ");
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            
            if (clientId == m_NetworkManager.LocalClientId)
            {
                //allow the host to connect
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
    
            if (DynamicPrefabLoadingUtilities.LoadedPrefabCount == 0)
            {
                //immediately approve the connection if we haven't loaded any prefabs yet
                Approve();
                return;
            }
    
            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
    
            int clientPrefabHash = connectionPayload.hashOfDynamicPrefabGUIDs;
            int serverPrefabHash = DynamicPrefabLoadingUtilities.HashOfDynamicPrefabGUIDs;
            
            //if the client has the same prefabs as the server - approve the connection
            if (clientPrefabHash == serverPrefabHash)
            {
                Approve();

                DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAllPrefabs(clientId);
                
                return;
            }
            
            // In order for clients to not just get disconnected with no feedback, the server needs to tell the client
            // why it disconnected it. This could happen after an auth check on a service or because of gameplay
            // reasons (server full, wrong build version, etc).
            // The server can do so via the DisconnectReason in the ConnectionApprovalResponse. The guids of the prefabs
            // the client will need to load will be sent, such that the client loads the needed prefabs, and reconnects.
            
            // A note: DisconnectReason will not be written to if the string is too large in size. This should be used
            // only to tell the client "why" it failed -- the client should instead use services like UGS to fetch the
            // relevant data it needs to fetch & download.

            DynamicPrefabLoadingUtilities.RefreshLoadedPrefabGuids();

            response.Reason = DynamicPrefabLoadingUtilities.GenerateDisconnectionPayload();
            
            ImmediateDeny();
            
            // A note: sending large strings through Netcode is not ideal -- you'd usually want to use REST services to
            // accomplish this instead. UGS services like Lobby can be a useful alternative. Another route may be to
            // set ConnectionApprovalResponse's Pending flag to true, and send a CustomMessage containing the array of 
            // GUIDs to a client, which the client would load and reattempt a reconnection.
    
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
    }
}
