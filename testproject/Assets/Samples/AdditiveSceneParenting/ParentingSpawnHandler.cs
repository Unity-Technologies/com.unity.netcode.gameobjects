using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ParentingSpawnHandler : NetworkBehaviour
{
    public GameObject PrefabToSpawnAndParent;

    [SerializeField]
    private float m_MoverVelocity;

    [Tooltip("When true, the MoverPickupObject will be spawned and moved into the scene this ParentingSpawnHandler is located within.")]
    [SerializeField]
    private bool m_SpawnLocalScene;

    private void OnDestroy()
    {
        // In the event we want to test unloading the additive scene but we are
        // targeting the additive scene (scene this is located in), then we want
        // to remove our reference from anything we spawned that is still around
        foreach (var spawnedThing in m_ThingsSpawned)
        {
            var pickupObject = spawnedThing.GetComponent<MoverPickupObject>();
            if (pickupObject != null)
            {
                pickupObject.SetParentSpawnHandler(null);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && PrefabToSpawnAndParent != null)
        {
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            // Always spawn a thing to be picked up for the host
            if (IsHost)
            {
                SpawnThingToPickup(0);
            }
        }
        base.OnNetworkSpawn();
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        var pickupLogic = (MoverPickupObject)null;
        foreach (var spawnedThing in m_ThingsSpawned)
        {
            pickupLogic = spawnedThing.GetComponent<MoverPickupObject>();

            if (pickupLogic.CanBePickedUp(clientId) || (pickupLogic.GetHunter() == clientId && pickupLogic.IsHunterHoldingObject()))
            {
                break;
            }
            pickupLogic = null;
        }

        if (pickupLogic != null)
        {
            pickupLogic.DropObject();
            pickupLogic.GetComponent<NetworkObject>().Despawn(true);
            m_ThingsSpawned.Remove(pickupLogic.gameObject);
            Destroy(pickupLogic.gameObject);
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        if (clientId != NetworkManager.ServerClientId)
        {
            SpawnThingToPickup(clientId);
        }
    }

    private List<GameObject> m_ThingsSpawned = new List<GameObject>();

    private void SpawnThingToPickup(ulong clientId)
    {
        if (IsServer)
        {
            foreach( var thing in m_ThingsSpawned)
            {
                var pickupObject = GetComponent<MoverPickupObject>();
                if (pickupObject != null && !pickupObject.IsHunterHoldingObject())
                {
                    pickupObject.MoveObjectBackToOriginalSceneClientRpc();
                }
            }

            var numbertoSpawn = NetworkManager.ConnectedClientsIds.Length - m_ThingsSpawned.Count;
            for (int i = 0; i < numbertoSpawn; i++)
            {
                var objectToParent = Instantiate(PrefabToSpawnAndParent);
                objectToParent.transform.position = transform.position;
                objectToParent.transform.rotation = transform.rotation;
                objectToParent.transform.localScale = transform.localScale;
                var networkObject = objectToParent.GetComponent<NetworkObject>();
                networkObject.Spawn(false);
                float ang = Random.Range(0.0f, 2 * Mathf.PI);
                networkObject.GetComponent<MoverPickupObject>().SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), m_MoverVelocity);
                m_ThingsSpawned.Add(objectToParent);
                if (m_SpawnLocalScene)
                {
                    networkObject.GetComponent<MoverPickupObject>().SetParentSpawnHandler(this);
                }
            }
        }
    }

    public Scene GetMyTargetScene(MoverPickupObject moverPickupObject )
    {
        if (m_SpawnLocalScene)
        {
            return gameObject.scene;
        }
        return moverPickupObject.gameObject.scene;
    }
}



