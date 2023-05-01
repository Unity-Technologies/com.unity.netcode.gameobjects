using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Used for manually testing spawning and despawning in-scene
    /// placed NetworkObjects
    /// </summary>
    public class DespawnInSceneNetworkObject : NetworkBehaviour
    {
        [Tooltip("When set, the server will despawn the NetworkObject upon its first spawn.")]
        public bool StartDespawned;

        private Coroutine m_ScanInputHandle;

        // Used to prevent the server from despawning
        // the in-scene placed NetworkObject after the
        // first spawn (only if StartDespawned is true)
        private bool m_ServerDespawnedOnFirstSpawn;

        private NetworkManager m_CachedNetworkManager;

        public override void OnNetworkSpawn()
        {
            Debug.Log($"{name} spawned!");

            if (!IsServer)
            {
                return;
            }

            m_CachedNetworkManager = NetworkManager;

            if (m_ScanInputHandle == null)
            {
                // Using the NetworkManager to create the coroutine so it is not deactivated
                // when the GameObject this NetworkBehaviour is attached to is disabled.
                m_ScanInputHandle = NetworkManager.StartCoroutine(ScanInput(NetworkObject));
            }

            // m_ServerDespawnedOnFirstSpawn prevents the server from always
            // despawning on the server-side after the first spawn.
            if (StartDespawned && !m_ServerDespawnedOnFirstSpawn)
            {
                m_ServerDespawnedOnFirstSpawn = true;
                NetworkObject.Despawn(false);
            }
        }

        public override void OnNetworkDespawn()
        {
            // It is OK to disable in-scene placed NetworkObjects upon
            // despawning.  When re-spawned the client-side will re-activate
            // the GameObject, while the server-side must set the GameObject
            // active itself.
            gameObject.SetActive(false);

            Debug.Log($"{name} despawned!");
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (m_ScanInputHandle != null && m_CachedNetworkManager != null)
            {
                m_CachedNetworkManager.StopCoroutine(m_ScanInputHandle);
            }
            m_ScanInputHandle = null;
            base.OnDestroy();
        }

        private IEnumerator ScanInput(NetworkObject networkObject)
        {
            while (true)
            {
                try
                {
                    if (networkObject.IsSpawned)
                    {
                        if (Input.GetKeyDown(KeyCode.Backspace))
                        {
                            Debug.Log($"{name} should despawn.");
                            networkObject.Despawn(false);
                        }
                    }
                    else if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
                    {
                        if (Input.GetKeyDown(KeyCode.Backspace))
                        {
                            Debug.Log($"{name} should spawn.");
                            networkObject.gameObject.SetActive(true);
                            networkObject.Spawn();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                yield return null;
            }
        }
    }
}
