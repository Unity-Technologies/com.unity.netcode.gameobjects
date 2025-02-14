using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnHelper : NetworkBehaviour
{
    public GameObject PlayerPrefab;
    private MoverScriptNoRigidbody m_MoverScriptNoRigidbody;
    private Vector3 m_InitialPrefabPosition = Vector3.zero;

    private void Awake()
    {
        m_MoverScriptNoRigidbody = PlayerPrefab.GetComponent<MoverScriptNoRigidbody>();
        m_MoverScriptNoRigidbody.ManualSpawn = true;
        m_InitialPrefabPosition = PlayerPrefab.transform.position;
    }

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return)) 
        { 
            if (NetworkManager.LocalClient.PlayerObject != null) 
            {
                return;
            }
            SpawnPlayerRpc();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (NetworkManager.LocalClient.PlayerObject == null)
            {
                return;
            }
            DespawnPlayerRpc();
        }

        // Replicates issue #3221
        // Simulates what happens if the GameObject is set inactive on the client side
        // prior to receiving despawn message.
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (NetworkManager.LocalClient.PlayerObject == null)
            {
                return;
            }
            if (!IsHost)
            {
                // This is just to preserve the camera for this example and has nothing to do with the issue.
                NetworkManager.LocalClient.PlayerObject.GetComponent<MoverScriptNoRigidbody>().OnNetworkDespawn();
                NetworkManager.LocalClient.PlayerObject.gameObject.SetActive(false);
            }
            DespawnPlayerRpc();
        }
    }

    private void SpawnPlayer(ulong playerId)
    {
        if (NetworkManager.SpawnManager.GetPlayerNetworkObject(playerId) == null)
        {
            var randomPosition = new Vector3(Random.Range(-m_MoverScriptNoRigidbody.SpawnRadius, m_MoverScriptNoRigidbody.SpawnRadius), 1.25f, Random.Range(0, m_MoverScriptNoRigidbody.SpawnRadius));
            NetworkObject.InstantiateAndSpawn(PlayerPrefab, NetworkManager, playerId, false, true, position: m_InitialPrefabPosition + randomPosition);
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerRpc(RpcParams rpcParams = default)
    {
        SpawnPlayer(rpcParams.Receive.SenderClientId);
    }

    private void DespawnPlayer(ulong playerId)
    {
        var playerObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(playerId);
        if (playerObject)
        {
            playerObject.Despawn();
        }
    }

    [Rpc(SendTo.Server)]
    private void DespawnPlayerRpc(RpcParams rpcParams = default)
    {
        DespawnPlayer(rpcParams.Receive.SenderClientId);
    }
}
