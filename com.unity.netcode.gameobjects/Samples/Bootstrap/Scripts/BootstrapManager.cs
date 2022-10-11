using UnityEngine;
using UnityEngine.UI;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_OfflinePanel;

        [SerializeField]
        private GameObject m_OnlinePanel;

        [SerializeField]
        private GameObject m_RandomTeleportButton;

        [SerializeField]
        private Text m_ModeLabel;

        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            OnGameStarted();
        }

        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
            OnGameStarted();
        }

        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
            OnGameStarted();
        }

        public void Shutdown()
        {
            NetworkManager.Singleton.Shutdown();
            m_OfflinePanel.SetActive(true);
            m_OnlinePanel.SetActive(false);
        }

        private void OnGameStarted()
        {
            m_OfflinePanel.SetActive(false);
            m_OnlinePanel.SetActive(true);
            var networkManager = NetworkManager.Singleton;
            m_ModeLabel.text = $"Mode: {(networkManager.IsHost ? "Host" : networkManager.IsClient ? "Client" : "Server")}";
            m_RandomTeleportButton.SetActive(networkManager.IsClient);
        }

        public void RandomTeleport()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager.IsClient && networkManager.LocalClient != null)
            {
                // Get `BootstrapPlayer` component from the player's `PlayerObject`
                if (networkManager.LocalClient.PlayerObject.TryGetComponent(out BootstrapPlayer bootstrapPlayer))
                {
                    // Invoke a `ServerRpc` from client-side to teleport player to a random position on the server-side
                    bootstrapPlayer.RandomTeleportServerRpc();
                }
            }
        }
    }
}
