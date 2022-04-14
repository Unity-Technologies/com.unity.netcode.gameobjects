using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ClientDisconnectHandler : NetworkBehaviour
    {
        [SerializeField]
        private GameObject m_DisconnectClientButton;
        [SerializeField]
        private float m_ReconnectAttempts = 5;

        [SerializeField]
        private bool m_AutoFlushScenes = true;

        [Range(0.5f,20.0f)]
        [SerializeField]
        private float m_DelayBetweenAttempts = 5;
        private WaitForSeconds m_RetryAttemptDelay;
        private bool m_IsReconnecting;
        private float m_ConnectionAttempts;
        private Coroutine m_CurrentCoroutine;
        private ulong m_LastKnownClientId;

        private void Start()
        {
            if (m_DisconnectClientButton != null)
            {
                m_DisconnectClientButton.gameObject.SetActive(false);
            }

            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }

        private void OnClientStarted()
        {
            if (!IsServer)
            {
                NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
                m_LastKnownClientId = NetworkManager.LocalClientId;
                m_RetryAttemptDelay = new WaitForSeconds(m_DelayBetweenAttempts);

                if (m_NetworkSceneTableState.Count > 0)
                {
                    if (!m_AutoFlushScenes)
                    {
                        NetworkManager.SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;
                    }
                    NetworkManager.SceneManager.SetNetworkSceneTableState(m_NetworkSceneTableState, m_AutoFlushScenes);
                }
            }
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            // Clients only need to track loaded scenes in order to reconnect without having to reload scenes
            if (IsServer)
            {
                return;
            }

            if(sceneEvent.ClientId == NetworkManager.LocalClientId )
            {
                switch(sceneEvent.SceneEventType)
                {
                    case SceneEventType.LoadComplete:
                        {
                            if (!m_ScenesLoaded.ContainsKey(sceneEvent.SceneName))
                            {
                                m_ScenesLoaded.Add(sceneEvent.SceneName, new List<Scene>());
                            }
                            m_ScenesLoaded[sceneEvent.SceneName].Add(sceneEvent.Scene);
                            break;
                        }
                    case SceneEventType.UnloadComplete:
                        {
                            RemoveScene(sceneEvent.SceneName);
                            break;
                        }
                    case SceneEventType.SynchronizeComplete:
                        {
                            foreach (var sceneEntry in s_ScenesAlreadyLoaded)
                            {
                                foreach (var scene in sceneEntry.Value)
                                {
                                    if (gameObject.scene.handle != scene.handle)
                                    {
                                        SceneManager.UnloadSceneAsync(scene);
                                    }
                                }
                            }
                            break;
                        }
                }
            }
        }

        private Dictionary<string, int> m_ScenesValidated = new Dictionary<string, int>();

        private bool RemoveScene(string sceneName)
        {
            if (s_ScenesAlreadyLoaded.ContainsKey(sceneName))
            {
                if (s_ScenesAlreadyLoaded[sceneName].First().name == sceneName)
                {
                    s_ScenesAlreadyLoaded[sceneName].Remove(s_ScenesAlreadyLoaded[sceneName].First());
                    if (s_ScenesAlreadyLoaded[sceneName].Count == 0)
                    {
                        s_ScenesAlreadyLoaded.Remove(sceneName);
                    }
                    return true;
                }
            }
            return false;
        }


        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                m_LastKnownClientId = NetworkManager.LocalClientId;
                NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            }
            else
            {
                if (m_DisconnectClientButton != null)
                {
                    m_DisconnectClientButton.gameObject.SetActive(true);
                }
            }
            base.OnNetworkSpawn();
        }

        private bool VerifySceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            var shouldLoad = !RemoveScene(sceneName);
            Debug.Log($"Verify {sceneName} is going to be loaded? ({shouldLoad})");
            return shouldLoad;
        }

        private Dictionary<string, List<Scene>> m_ScenesLoaded = new Dictionary<string, List<Scene>>();
        private static Dictionary<string, List<Scene>> s_ScenesAlreadyLoaded = new Dictionary<string, List<Scene>>();

        public void OnDisconnectClients()
        {
            if (!IsServer)
            {
                return;
            }
            var connectedClients = NetworkManager.ConnectedClientsIds.ToList();
            foreach(var clientId in connectedClients)
            {
                if(clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }
                NetworkManager.DisconnectClient(clientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
            }
            base.OnNetworkDespawn();
        }
        private Dictionary<int, Scene> m_NetworkSceneTableState = new Dictionary<int, Scene>();

        private void NetworkManager_OnClientDisconnectCallback(ulong obj)
        {
            if (!m_IsReconnecting)
            {
                m_NetworkSceneTableState = new Dictionary<int, Scene>(NetworkManager.SceneManager.GetNetworkSceneTableState());
                m_ScenesLoaded.Clear();
                m_IsReconnecting = true;
                m_ConnectionAttempts = 0;
                NetworkManager.Singleton.OnClientStarted += OnClientStarted;
                NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
                m_CurrentCoroutine = StartCoroutine(ReconnectClient());
            }
        }

        private void NetworkManager_OnClientConnectedCallback(ulong obj)
        {
            if(m_IsReconnecting)
            {
                m_IsReconnecting = false;
                StopCoroutine(m_CurrentCoroutine);
                m_LastKnownClientId = NetworkManager.LocalClientId;
                NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
            }
        }

        private IEnumerator ReconnectClient()
        {
            while(m_ConnectionAttempts < m_ReconnectAttempts)
            {
                yield return m_RetryAttemptDelay;
                NetworkManager.StartClient();
                m_ConnectionAttempts++;
            }
            m_IsReconnecting = false;
            if (!NetworkManager.IsConnectedClient)
            {
                Debug.LogError($"Failed to reconnect client ({m_LastKnownClientId})");
            }
        }
    }
}
