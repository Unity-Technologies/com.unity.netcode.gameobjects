using Unity.Netcode;
using UnityEngine;

public class BallSpawner : NetworkBehaviour, INetworkUpdateSystem
{
    public GameObject BallPrefab;

    protected override void OnNetworkPostSpawn()
    {
        if (HasAuthority)
        {
            if (NetworkManager.DistributedAuthorityMode || (!NetworkManager.DistributedAuthorityMode && IsServer))
            {
                NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.PreLateUpdate);
            }
        }
        base.OnNetworkPostSpawn();
    }

    public override void OnNetworkDespawn()
    {
        NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.PreLateUpdate);
        base.OnNetworkDespawn();
    }

    public void NetworkUpdate(NetworkUpdateStage updateStage)
    {
        if (!IsSpawned || (NetworkManager && NetworkManager.ShutdownInProgress) || !Application.isFocused)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && BallPrefab)
        {
            var spawnPoint = transform.position + transform.forward * 2.5f;
            var instance = NetworkObject.InstantiateAndSpawn(BallPrefab, NetworkManager, OwnerClientId, position: spawnPoint);
            var rigidBody = instance.GetComponent<Rigidbody>();
            rigidBody.AddForce(transform.forward * 10.0f, ForceMode.Impulse);
        }
    }
}
