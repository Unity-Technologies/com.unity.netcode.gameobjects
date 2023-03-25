using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TestProject.ManualTests
{
    /// <summary>
    /// This component demonstrates how to use the Netcode for GameObjects connection approval feature
    /// </summary>
    public class ConnectionApprovalComponent : NetworkBehaviour
    {
        [SerializeField]
        private string m_ApprovalToken;

        [SerializeField]
        private uint m_GlobalObjectIdHashOverride;

        [SerializeField]
        private Text m_ConnectionMessageToDisplay;

        [SerializeField]
        private Toggle m_SimulateFailure;

        [SerializeField]
        private Toggle m_PlayerPrefabOverride;

        [SerializeField]
        private Button m_ClientDisconnectButton;

        [SerializeField]
        private ConnectionModeScript m_ConnectionModeButtons;

        private class MessageEntry
        {
            public string Message;
            public float TimeOut;
        }

        private List<MessageEntry> m_Messages = new List<MessageEntry>();

        private void Start()
        {
            if (m_PlayerPrefabOverride)
            {
                m_PlayerPrefabOverride.gameObject.SetActive(false);
            }

            if (m_SimulateFailure)
            {
                m_SimulateFailure.gameObject.SetActive(false);
            }

            if (m_ConnectionMessageToDisplay)
            {
                m_ConnectionMessageToDisplay.gameObject.SetActive(false);
            }

            if (m_ClientDisconnectButton)
            {
                m_ClientDisconnectButton.gameObject.SetActive(false);
            }

            if (NetworkManager != null && NetworkManager.NetworkConfig.ConnectionApproval)
            {
                NetworkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;

                if (m_ApprovalToken != string.Empty)
                {
                    NetworkManager.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(m_ApprovalToken);
                }
                else
                {
                    Debug.LogError($"You need to set the {nameof(m_ApprovalToken)} to a value first!");
                }
            }

            NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        /// <summary>
        /// When a client connects we display a message and if we are not the server
        /// we display a disconnect button for ease of testing.
        /// </summary>
        private void NetworkManager_OnClientConnectedCallback(ulong clientId)
        {
            if (m_ClientDisconnectButton)
            {
                m_ClientDisconnectButton.gameObject.SetActive(!IsServer);
            }

            AddNewMessage($"Client {clientId} was connected.");
        }

        /// <summary>
        /// When a client is disconnected we display a message and if we
        /// are not listening and not the server we reset the UI Connection
        /// mode buttons
        /// </summary>
        private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
        {

            AddNewMessage($"Client {clientId} was disconnected!");

            if (!NetworkManager.IsListening && !NetworkManager.IsServer)
            {
                m_ConnectionModeButtons.Reset();
            }

            if (m_ClientDisconnectButton)
            {
                m_ClientDisconnectButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Just display certain check boxes only when we are in a network session
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (m_SimulateFailure)
            {
                m_SimulateFailure.gameObject.SetActive(IsServer);
            }

            if (m_PlayerPrefabOverride)
            {
                m_PlayerPrefabOverride.gameObject.SetActive(IsServer);
            }
        }

        /// <summary>
        /// Used for the client when the disconnect button is pressed
        /// </summary>
        public void OnDisconnectClient()
        {
            if (NetworkManager != null && NetworkManager.IsListening && !NetworkManager.IsServer)
            {
                NetworkManager.Shutdown();
                m_ConnectionModeButtons.Reset();
                if (m_ClientDisconnectButton)
                {
                    m_ClientDisconnectButton.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Invoked only on the server, this will handle the various connection approval combinations
        /// </summary>
        /// <param name="request">The connection approval request</param>
        /// <returns>ConnectionApprovalResult with the approval decision, with parameters</returns>
        private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            string approvalToken = Encoding.ASCII.GetString(request.Payload);
            var isTokenValid = approvalToken == m_ApprovalToken;
            if (m_SimulateFailure && m_SimulateFailure.isOn && IsServer && request.ClientNetworkId != NetworkManager.LocalClientId)
            {
                isTokenValid = false;
            }

            if (m_GlobalObjectIdHashOverride != 0 && m_PlayerPrefabOverride && m_PlayerPrefabOverride.isOn)
            {
                response.Approved = isTokenValid;
                response.PlayerPrefabHash = m_GlobalObjectIdHashOverride;
                response.Position = null;
                response.Rotation = null;
                response.CreatePlayerObject = true;
            }
            else
            {
                response.Approved = isTokenValid;
                response.PlayerPrefabHash = null;
                response.Position = null;
                response.Rotation = null;
                response.CreatePlayerObject = true;
            }

            if (m_ConnectionMessageToDisplay)
            {
                if (isTokenValid)
                {
                    AddNewMessage($"Client id {request.ClientNetworkId} is authorized!");
                }
                else
                {
                    AddNewMessage($"Client id {request.ClientNetworkId} failed authorization!");
                }

                m_ConnectionMessageToDisplay.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Adds a new message to be displayed and if our display coroutine is not running start it.
        /// </summary>
        /// <param name="msg">message to add to the list of messages to be displayed</param>
        private void AddNewMessage(string msg)
        {
            m_Messages.Add(new MessageEntry() { Message = msg, TimeOut = Time.realtimeSinceStartup + 8.0f });
            if (!m_ConnectionMessageToDisplay.gameObject.activeInHierarchy)
            {
                StartCoroutine(DisplayMessatesUntilEmpty());
                if (m_ConnectionMessageToDisplay)
                {
                    m_ConnectionMessageToDisplay.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Coroutine that displays messages until there are no more messages to be displayed.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DisplayMessatesUntilEmpty()
        {
            var messagesToRemove = new List<MessageEntry>();
            while (m_Messages.Count > 0)
            {
                m_ConnectionMessageToDisplay.text = string.Empty;
                foreach (var message in m_Messages)
                {
                    if (message.TimeOut > Time.realtimeSinceStartup)
                    {
                        m_ConnectionMessageToDisplay.text += message.Message + "\n";
                    }
                    else
                    {
                        messagesToRemove.Add(message);
                    }
                }
                yield return new WaitForSeconds(0.5f);
                foreach (var message in messagesToRemove)
                {
                    m_Messages.Remove(message);
                }
                messagesToRemove.Clear();
            }

            if (m_ConnectionMessageToDisplay)
            {
                m_ConnectionMessageToDisplay.gameObject.SetActive(false);
            }
            yield return null;
        }
    }
}
