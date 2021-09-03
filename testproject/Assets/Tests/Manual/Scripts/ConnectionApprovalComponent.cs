using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace TestProject.ManualTests
{
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
                NetworkManager.ConnectionApprovalCallback += ConnectionApprovalCallback;

                if (m_ApprovalToken != string.Empty)
                {
                    NetworkManager.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(m_ApprovalToken);
                }
                else
                {
                    Debug.LogError($"You need to set the {nameof(m_ApprovalToken)} to a value first!");
                }

                NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
                NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
            }
        }

        private void NetworkManager_OnClientConnectedCallback(ulong obj)
        {
            if (m_ClientDisconnectButton)
            {
                m_ClientDisconnectButton.gameObject.SetActive(!IsServer);
            }

            AddNewMessage($"Client {obj} was connected.");
        }

        private void NetworkManager_OnClientDisconnectCallback(ulong obj)
        {

            AddNewMessage($"Client {obj} was disconnected!");

            if (!NetworkManager.IsListening && !NetworkManager.IsServer)
            {
                m_ConnectionModeButtons.Reset();
            }

            if (m_ClientDisconnectButton)
            {
                m_ClientDisconnectButton.gameObject.SetActive(false);
            }
        }

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

        public void OnDisconnectClient()
        {
            if ( NetworkManager != null && NetworkManager.IsListening && !NetworkManager.IsServer)
            {
                NetworkManager.Shutdown();
                m_ConnectionModeButtons.Reset();
                if (m_ClientDisconnectButton)
                {
                    m_ClientDisconnectButton.gameObject.SetActive(false);
                }
            }
        }

        private void ConnectionApprovalCallback(byte[] dataToken, ulong clientId, NetworkManager.ConnectionApprovedDelegate aprovalCallback)
        {
            string approvalToken = Encoding.ASCII.GetString(dataToken);
            var isTokenValid = approvalToken == m_ApprovalToken;
            if (m_SimulateFailure && m_SimulateFailure.isOn && IsServer && clientId != NetworkManager.LocalClientId)
            {
                isTokenValid = false;
            }

            if (m_GlobalObjectIdHashOverride != 0 && m_PlayerPrefabOverride && m_PlayerPrefabOverride.isOn)
            {
                aprovalCallback.Invoke(true, m_GlobalObjectIdHashOverride, isTokenValid, null, null);
            }
            else
            {
                aprovalCallback.Invoke(true, null, isTokenValid, null, null);
            }


            if (m_ConnectionMessageToDisplay)
            {
                if (isTokenValid)
                {
                    AddNewMessage($"Client id {clientId} is authorized!");
                }
                else
                {
                    AddNewMessage($"Client id {clientId} failed authorization!");
                }

                m_ConnectionMessageToDisplay.gameObject.SetActive(true);
                StartCoroutine(WaitToHideConnectionText());
            }
        }

        private void AddNewMessage(string msg)
        {
            m_Messages.Add(new MessageEntry() { Message = msg, TimeOut = Time.realtimeSinceStartup + 8.0f });
            if (!m_ConnectionMessageToDisplay.gameObject.activeInHierarchy)
            {
                StartCoroutine(WaitToHideConnectionText());
                if (m_ConnectionMessageToDisplay)
                {
                    m_ConnectionMessageToDisplay.gameObject.SetActive(true);
                }
            }
        }

        private IEnumerator WaitToHideConnectionText()
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
