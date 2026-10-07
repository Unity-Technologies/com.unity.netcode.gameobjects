using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Component should be added to the NetworkManager GameObject
/// This controls whether the NetworkManager player pefab will be used or
/// if it will handle spawning the player upon the player being connected.
/// </summary>
public class PlayerSpawnHandler : MonoBehaviour
{
    public enum SpawnPlayerOptions
    {
        /// Use <see cref="SpawnPlayerOptions.NetworkManagerPlayerPrefab"/> to let NetworkManager automatically spawn 
        /// the player <see cref="PlayerSpawnChildAndParent"/> to see how to spawn a child
        NetworkManagerPlayerPrefab,
        // Use either of these to let this component handle spawning the player
        ClientConnectedCallback, 
        ClientConnectionEvent,
    }

    public SpawnPlayerOptions SpawnPlayerOption;
    public GameObject PlayerPrefab;
    public GameObject PlayerChildPrefab;
    private NetworkManager m_NetworkManager;

    private void Start()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        if (SpawnPlayerOption != SpawnPlayerOptions.NetworkManagerPlayerPrefab)
        {
            m_NetworkManager.OnServerStarted += OnServerStarted;
            m_NetworkManager.NetworkConfig.PlayerPrefab = null;
        }
        else
        {
            m_NetworkManager.NetworkConfig.PlayerPrefab = PlayerPrefab;
        }
    }

    private void OnServerStarted()
    {
        m_NetworkManager.OnServerStarted -= OnServerStarted;
        m_NetworkManager.OnServerStopped += OnServerStopped;
        if (SpawnPlayerOption == SpawnPlayerOptions.ClientConnectedCallback)
        {
            m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        }
        else
        {
            m_NetworkManager.OnConnectionEvent += OnConnectionEvent;
        }

        // Host spawns when started
        if (m_NetworkManager.IsHost)
        {
            SpawnPlayer(m_NetworkManager.LocalClientId);
        }
    }

    private void OnServerStopped(bool wasHost)
    {
        m_NetworkManager.OnServerStopped -= OnServerStopped;
        m_NetworkManager.OnServerStarted += OnServerStarted;
        if (SpawnPlayerOption == SpawnPlayerOptions.ClientConnectedCallback)
        {
            m_NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
        }
        else
        {
            m_NetworkManager.OnConnectionEvent -= OnConnectionEvent;
        }
    }

    private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        if (eventData.EventType == ConnectionEvent.ClientConnected)
        {
            SpawnPlayer(eventData.ClientId);
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!PlayerPrefab)
        {
            Debug.LogWarning("[No Player Prefab Defined] Player prefab not spawned!");
            return;
        }
        // Handle player initial position and rotation here
        var position = Vector3.up * 2f;
        var rotation = Quaternion.identity;
        var playerNetworkObject = NetworkObject.InstantiateAndSpawn(PlayerPrefab, m_NetworkManager, clientId, isPlayerObject: true, position: position, rotation: rotation);
        SpawnChild(playerNetworkObject);
    }

    private void SpawnChild(NetworkObject playerNetworkObject)
    {
        if (!PlayerChildPrefab)
        {
            Debug.LogWarning("[No Player Child Prefab Defined] Player child prefab not spawned!");
            return;
        }
        // Do any position calculations here
        var rigidBody = playerNetworkObject.GetComponent<Rigidbody>();
        var position = rigidBody.position + (transform.up * 2.5f);

        // Spawn the child as an object owned by the client
        var childInstance = NetworkObject.InstantiateAndSpawn(PlayerChildPrefab, m_NetworkManager, playerNetworkObject.OwnerClientId, position: position);

        // Parent the child under the player
        if (!childInstance.TrySetParent(playerNetworkObject))
        {
            Debug.LogError($"Failed to parent ChildObject under {name}!");
        }
    }
}

