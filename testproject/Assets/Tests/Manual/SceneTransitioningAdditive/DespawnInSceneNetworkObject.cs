using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class DespawnInSceneNetworkObject : NetworkBehaviour
    {
        private Coroutine m_ScanInputHandle;
        private MeshRenderer m_MeshRenderer;

        private void Start()
        {
            if (!IsSpawned)
            {
                m_MeshRenderer = GetComponent<MeshRenderer>();
                if (m_MeshRenderer != null)
                {
                    m_MeshRenderer.enabled = false;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"{name} spawned!");
            m_MeshRenderer = GetComponent<MeshRenderer>();
            if (m_MeshRenderer != null)
            {
                m_MeshRenderer.enabled = true;
            }
            if (!IsServer)
            {
                return;
            }
            if (m_ScanInputHandle == null)
            {
                m_ScanInputHandle = StartCoroutine(ScanInput());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (m_MeshRenderer != null)
            {
                m_MeshRenderer.enabled = false;
            }
            Debug.Log($"{name} despawned!");
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (m_ScanInputHandle != null)
            {
                StopCoroutine(m_ScanInputHandle);
            }
            m_ScanInputHandle = null;
            base.OnDestroy();
        }

        private IEnumerator ScanInput()
        {
            while (true)
            {
                try
                {
                    if (IsSpawned)
                    {
                        if (Input.GetKeyDown(KeyCode.Backspace))
                        {
                            Debug.Log($"{name} should despawn.");
                            NetworkObject.Despawn(false);
                        }
                    }
                    else if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
                    {
                        if (Input.GetKeyDown(KeyCode.Backspace))
                        {
                            Debug.Log($"{name} should spawn.");
                            NetworkObject.Spawn();
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
