using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.RuntimeTests
{
    public class NetworkObjectTestComponent : NetworkBehaviour
    {
        public static NetworkObject ServerNetworkObjectInstance;
        public static List<NetworkObjectTestComponent> SpawnedInstances = new List<NetworkObjectTestComponent>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerNetworkObjectInstance = NetworkObject;
            }
            SpawnedInstances.Add(this);
            base.OnNetworkSpawn();
        }

        public static Action<NetworkObject> OnInSceneObjectDespawned;

        public override void OnNetworkDespawn()
        {
            OnInSceneObjectDespawned?.Invoke(NetworkObject);
            m_HasNotifiedSpawned = false;
            Debug.Log($"{NetworkManager.name} de-spawned {gameObject.name}.");
            SpawnedInstances.Remove(this);
            base.OnNetworkDespawn();
        }

        private bool m_HasNotifiedSpawned;
        private void Update()
        {
            // We do this so the ObjectNameIdentifier has a chance to label it properly
            if (IsSpawned && !m_HasNotifiedSpawned)
            {
                Debug.Log($"{NetworkManager.name} spawned {gameObject.name}.");
                m_HasNotifiedSpawned = true;
            }
        }
    }
}
