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

        [Range(0.5f, 20.0f)]
        [SerializeField]
        private float m_DelayBetweenAttempts = 5;
        private WaitForSeconds m_RetryAttemptDelay;
        private bool m_IsReconnecting;
        private float m_ConnectionAttempts;
        private Coroutine m_CurrentCoroutine;
        private ulong m_LastKnownClientId;

        private void Start()
        {
            m_RetryAttemptDelay = new WaitForSeconds(m_DelayBetweenAttempts);
            if (m_DisconnectClientButton != null && !IsServer)
            {
                m_DisconnectClientButton.gameObject.SetActive(false);
            }
            // Keep track of the scene we belong to (see notes in OnClientStarted)
            m_CurrentScene = gameObject.scene;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }

        private Scene m_CurrentScene;

        /// <summary>
        /// WHen the NetworkManager is started as a client, we check to see if we
        /// have a previously saved off NetworSceneTableState and if so set it
        /// </summary>
        private void OnClientStarted()
        {
            // Clients can be reconnecting to a server that has already switched the scene
            // (i.e. LoadSceneMode.Single) prior to the client reconnecting.  When this
            // happens, we need to make sure the scene is valid and still loaded. If it is
            // not loaded (most likely state between the two), then this is a "ghost object
            // callback" where the component really is no longer valid (i.e. its scene is
            // unloading/unloaded) so just exit and touch/access nothing.
            if (!IsServer && m_CurrentScene.IsValid() && m_CurrentScene.isLoaded)
            {
                m_ConnectionAttempts = 0;
                m_LastKnownClientId = NetworkManager.LocalClientId;
                if (m_NetworkSceneTableState.Count > 0)
                {
                    NetworkManager.SceneManager.SetNetworkSceneTableState(m_NetworkSceneTableState, true);
                }
            }
        }

        public override void OnDestroy()
        {
            // We use NetworkManager.Singleton on purpose here
            if (!IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.SceneManager.VerifySceneBeforeLoading = null;
                NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
                NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
                NetworkManager.OnClientStarted -= OnClientStarted;
                if (m_CurrentCoroutine != null)
                {
                    StopCoroutine(m_CurrentCoroutine);
                }

            }
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                m_ConnectionAttempts = 0;
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


        /// <summary>
        /// Disconnect all clients
        /// </summary>
        public void OnDisconnectClients()
        {
            if (!IsServer)
            {
                return;
            }
            var connectedClients = NetworkManager.ConnectedClientsIds.ToList();
            foreach (var clientId in connectedClients)
            {
                if (clientId == NetworkManager.LocalClientId)
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

        /// <summary>
        /// When the client disconnects we get the current NetworkSceneTableState and start a co-routine to attempt to re-connect
        /// </summary>
        private void NetworkManager_OnClientDisconnectCallback(ulong obj)
        {
            if (!m_IsReconnecting)
            {

                m_NetworkSceneTableState = new Dictionary<int, Scene>(NetworkManager.SceneManager.GetNetworkSceneTableState());
                m_IsReconnecting = true;
                m_ConnectionAttempts = 0;
                NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
                m_CurrentCoroutine = StartCoroutine(ReconnectClient());
            }
        }

        /// <summary>
        /// Once we connect we stop the reconnection co-routine
        /// </summary>
        private void NetworkManager_OnClientConnectedCallback(ulong obj)
        {
            if (m_IsReconnecting)
            {
                m_IsReconnecting = false;
                m_ConnectionAttempts = 0;
                StopCoroutine(m_CurrentCoroutine);
                m_LastKnownClientId = NetworkManager.LocalClientId;
                NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
            }
        }


        private IEnumerator ReconnectClient()
        {
            while (m_ConnectionAttempts < m_ReconnectAttempts)
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
