using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnChildAndParent : NetworkBehaviour
{
    public GameObject ChildObject;

    public override void OnNetworkSpawn()
    {
        // This is a network topology agnostic way of handling the authority spawning and parenting the 
        if (HasAuthority)
        {
            var playerSpawnHandler = NetworkManager.GetComponent<PlayerSpawnHandler>();
            if (playerSpawnHandler.SpawnPlayerOption != PlayerSpawnHandler.SpawnPlayerOptions.NetworkManagerPlayerPrefab)
            {
                return;
            }
            // The server can spawn and parent its own player prefab and child object of the player prefab
            if (NetworkManager.DistributedAuthorityMode || (IsOwnedByServer && IsHost))
            {
                // Client-Server: Host can spawn their child immediately
                SpawnChild();
            }
            else // Client-Server: Otherwise, the server defers spawning the player prefab's child
            if (!NetworkManager.DistributedAuthorityMode)
            {
                // With a Client-Server topology, the server has to defer spawning of any NetworkObjects
                // from a client's player prefab instance until after the client has synchronized (for now).
                NetworkManager.SceneManager.OnSynchronizeComplete += SceneManager_OnSynchronizeComplete;
            }
        }
    }

    /// <summary>
    /// Invoked only when using a Client-Server network topology.
    /// </summary>
    /// <param name="clientId"></param>
    private void SceneManager_OnSynchronizeComplete(ulong clientId)
    {
        NetworkManager.SceneManager.OnSynchronizeComplete -= SceneManager_OnSynchronizeComplete;
        SpawnChild();
    }

    private void SpawnChild()
    {
        // Do any position calculations here
        var rigidBody = GetComponent<Rigidbody>();
        var position = rigidBody.position + (transform.up * 2.5f);

        // Spawn the child as an object owned by the client
        var childInstance = NetworkObject.InstantiateAndSpawn(ChildObject, NetworkManager, OwnerClientId, position: position);

        // Parent the child under the player
        if (!childInstance.TrySetParent(NetworkObject))
        {
            Debug.LogError($"Failed to parent ChildObject under {name}!");
        }
    }
}