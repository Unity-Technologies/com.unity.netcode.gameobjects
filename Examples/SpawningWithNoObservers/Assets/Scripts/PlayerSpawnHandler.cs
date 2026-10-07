using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Component should be added to the NetworkManager GameObject
/// This controls whether the NetworkManager player pefab will be used or
/// if it will handle spawning the player upon the player being connected.
/// </summary>
public class PlayerSpawnHandler : MonoBehaviour, INetworkUpdateSystem
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
    private NetworkManager m_NetworkManager;

    private Dictionary<ulong, NetworkObject> m_SpawndNetworkObjects = new Dictionary<ulong, NetworkObject>();

    private HashSet<NetworkObject> m_UnspawnedInstances = new HashSet<NetworkObject>();

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

        if (SpawnPlayerOption != SpawnPlayerOptions.NetworkManagerPlayerPrefab)
        {
            NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Update);
        }

        // Host spawns when started
        if (m_NetworkManager.IsHost)
        {
            SpawnPrefab(m_NetworkManager.LocalClientId);
        }

        // Register after spawning the host player
        m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private List<KeyCode> m_KeyCodes = new List<KeyCode>()
    {
        KeyCode.Alpha0,
        KeyCode.Alpha1, KeyCode.Alpha2,
        KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8,
        KeyCode.Alpha9
    };

    private bool IsVisibleToClient(ulong clientId, NetworkObject networkObject)
    {
        var observers = networkObject.GetObservers();
        do
        {
            if (observers.Current == clientId)
            {
                return true;
            }
        }
        while (observers.MoveNext());
        return false;
    }

    public void NetworkUpdate(NetworkUpdateStage updateStage)
    {
        var clientIndex = 0;
        foreach (KeyCode keyCode in m_KeyCodes)
        {
            if (Input.GetKeyDown(keyCode) && m_NetworkManager.ConnectedClientsIds.Count > clientIndex)
            {
                var clientId = m_NetworkManager.ConnectedClientsIds[clientIndex];

                var playerNetworkObject = m_NetworkManager.SpawnManager.GetClientOwnedObjects(clientId).First();
                if (!playerNetworkObject)
                {
                    continue;
                }
                foreach (var clientToShowOrHide in m_NetworkManager.ConnectedClientsIds)
                {
                    if (clientId == clientToShowOrHide && clientId == 0)
                    {
                        continue;
                    }
                    if (IsVisibleToClient(clientToShowOrHide, playerNetworkObject))
                    {
                        playerNetworkObject.GetComponent<PlayerMotion>().enabled = false;
                        playerNetworkObject.NetworkHide(clientToShowOrHide);
                    }
                    else
                    {
                        playerNetworkObject.NetworkShow(clientToShowOrHide);
                        playerNetworkObject.GetComponent<PlayerMotion>().enabled = true;
                    }
                }
            }
            clientIndex++;
        }
    }

    private void OnServerStopped(bool wasHost)
    {
        m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
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
        if (!m_NetworkManager.IsServer || eventData.ClientId == NetworkManager.ServerClientId)
        {
            return;
        }
        if (eventData.EventType == ConnectionEvent.ClientConnected)
        {
            SpawnPrefab(eventData.ClientId);
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (!m_NetworkManager.IsServer || clientId == NetworkManager.ServerClientId)
        {
            return;
        }
        SpawnPrefab(clientId);
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (!m_NetworkManager.ShutdownInProgress && m_SpawndNetworkObjects.ContainsKey(clientId))
        {
            var networkObject = m_SpawndNetworkObjects[clientId];
            m_UnspawnedInstances.Add(networkObject);
            if (networkObject.IsSpawned)
            {
                networkObject.Despawn(false);
            }
            m_SpawndNetworkObjects.Remove(clientId);
            networkObject.gameObject.SetActive(false);
        }
    }

    private void SpawnPrefab(ulong clientId)
    {
        if (!PlayerPrefab)
        {
            Debug.LogWarning("[No Player Prefab Defined] Player prefab not spawned!");
            return;
        }
        // Handle player initial position and rotation here
        var position = Vector3.up * 2f;
        var rotation = Quaternion.identity;
        var playerNetworkObject = (NetworkObject)null;
        if (m_UnspawnedInstances.Count > 0)
        {
            playerNetworkObject = m_UnspawnedInstances.First();
            m_UnspawnedInstances.Remove(playerNetworkObject);
            playerNetworkObject.gameObject.SetActive(true);
            playerNetworkObject.GetComponent<PlayerMotion>().enabled = true;
        }
        else
        {
            playerNetworkObject = Instantiate(PlayerPrefab, position, rotation).GetComponent<NetworkObject>();
        }

        playerNetworkObject.SpawnWithOwnership(clientId);
        m_SpawndNetworkObjects.Add(clientId, playerNetworkObject);
    }
}

