using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class SpawnHybridPrefab : NetworkBehaviour, INetworkUpdateSystem
{
    public GameObject HybridPrefabToSpawn;
    public KeyCode KeyToSpawn = KeyCode.Space;
    public KeyCode KeyToDespawnAll = KeyCode.Delete;

    [Range(1.0f, 10.0f)]
    public float SpawnRadius = 5.0f;

    private List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Update);
        }
        base.OnNetworkPostSpawn();
    }

    public override void OnNetworkPreDespawn()
    {
        if (IsServer)
        {
            NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
        }
        base.OnNetworkPreDespawn();
    }

    public void NetworkUpdate(NetworkUpdateStage updateStage)
    {
        if (Input.GetKeyDown(KeyToSpawn))
        {
            var spawnpoint = GetRandomVector3(-SpawnRadius, SpawnRadius);
            spawnpoint.y = transform.position.y;
            var instance = NetworkObject.InstantiateAndSpawn(HybridPrefabToSpawn, NetworkManager, position: spawnpoint, rotation: transform.rotation);
            var rigidBody = instance.GetComponent<Rigidbody>();
            rigidBody.position = spawnpoint;
            rigidBody.rotation = transform.rotation;

            m_SpawnedObjects.Add(instance);
        }
        else if (Input.GetKeyDown(KeyToDespawnAll))
        {
            foreach (var networkObject in m_SpawnedObjects)
            {
                networkObject.Despawn();
            }
            m_SpawnedObjects.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Vector3 GetRandomVector3(float min, float max)
    {
        return new Vector3(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
    }
}
