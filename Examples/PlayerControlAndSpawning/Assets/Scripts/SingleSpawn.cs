using Unity.Netcode;
using UnityEngine;

public class SingleSpawn : NetworkBehaviour
{
    public GameObject ObjectToSpawn;

    protected override void OnNetworkPostSpawn()
    {
        if (IsHost) 
        {
            NetworkObject.InstantiateAndSpawn(ObjectToSpawn, NetworkManager);
        }
        base.OnNetworkPostSpawn();
    }
}
