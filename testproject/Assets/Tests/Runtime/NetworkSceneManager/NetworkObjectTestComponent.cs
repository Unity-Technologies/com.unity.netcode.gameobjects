using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Used with Integration tests to track how many instances of the component
    /// have been spawned with additional debug log information for spawn and despawn
    /// events.
    /// </summary>
    public class NetworkObjectTestComponent : NetworkBehaviour
    {
        public static bool DisableOnDespawn;
        public static bool DisableOnSpawn;
        public static NetworkObject ServerNetworkObjectInstance;
        public static List<NetworkObjectTestComponent> SpawnedInstances = new List<NetworkObjectTestComponent>();
        public static List<NetworkObjectTestComponent> DespawnedInstances = new List<NetworkObjectTestComponent>();

        public static void Reset()
        {
            DisableOnDespawn = false;
            DisableOnSpawn = false;
            ServerNetworkObjectInstance = null;
            SpawnedInstances.Clear();
            DespawnedInstances.Clear();
        }

        private Action<NetworkObject, int, bool, bool, bool> m_ActionClientConnected;
        private int m_NumberOfTimesInvoked;
        public void ConfigureClientConnected(NetworkManager networkManager, Action<NetworkObject, int, bool, bool, bool> clientConnected)
        {
            networkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
            m_ActionClientConnected = clientConnected;
        }

        private void NetworkManager_OnClientConnectedCallback(ulong obj)
        {
            m_NumberOfTimesInvoked++;
            m_ActionClientConnected?.Invoke(NetworkObject, m_NumberOfTimesInvoked, IsHost, IsClient, IsServer);
        }

        // When disabling on spawning we only want this to happen on the initial spawn.
        // This is used to track this so the server only does it once upon spawning.
        public bool ObjectWasDisabledUponSpawn;
        public override void OnNetworkSpawn()
        {
            SpawnedInstances.Add(this);
            if (DisableOnDespawn)
            {
                if (DespawnedInstances.Contains(this))
                {
                    DespawnedInstances.Remove(this);
                }
            }

            if (IsServer)
            {
                ServerNetworkObjectInstance = NetworkObject;
                if (DisableOnSpawn && !ObjectWasDisabledUponSpawn)
                {
                    NetworkObject.Despawn(false);
                    ObjectWasDisabledUponSpawn = true;
                }
            }
            base.OnNetworkSpawn();
        }

        public static Action<NetworkObject> OnInSceneObjectDespawned;

        public override void OnNetworkDespawn()
        {
            OnInSceneObjectDespawned?.Invoke(NetworkObject);
            m_HasNotifiedSpawned = false;
            Debug.Log($"{NetworkManager.name} de-spawned {gameObject.name}.");
            SpawnedInstances.Remove(this);
            if (DisableOnDespawn)
            {
                DespawnedInstances.Add(this);
                gameObject.SetActive(false);
            }
            base.OnNetworkDespawn();
        }

        private bool m_HasNotifiedSpawned;
        private void Update()
        {
            // We do this so the ObjectNameIdentifier has a chance to label it properly
            if (IsSpawned && !m_HasNotifiedSpawned)
            {
                Debug.Log($"{NetworkManager.name} spawned {gameObject.name} with scene origin handle {gameObject.scene.handle}.");
                m_HasNotifiedSpawned = true;
            }
        }
    }
}
