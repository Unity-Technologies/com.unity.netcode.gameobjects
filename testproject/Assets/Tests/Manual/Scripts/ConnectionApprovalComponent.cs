using System.Collections;
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

        private void NetworkManager_OnClientDisconnectCallback(ulong obj)
        {
            Debug.Log($"Client {obj} connected!");
        }

        private void ConnectionApprovalCallback(byte[] arg1, ulong arg2, NetworkManager.ConnectionApprovedDelegate arg3)
        {
            string approvalToken = Encoding.ASCII.GetString(arg1);
            var isTokenValid = approvalToken == m_ApprovalToken;
            if (m_SimulateFailure && m_SimulateFailure.isOn && IsServer && arg2 != NetworkManager.LocalClientId)
            {
                isTokenValid = false;
            }

            if (isTokenValid)
            {
                if (m_GlobalObjectIdHashOverride != 0 && m_PlayerPrefabOverride && m_PlayerPrefabOverride.isOn)
                {
                    arg3.Invoke(true, m_GlobalObjectIdHashOverride, true, null, null);
                }
                else
                {
                    arg3.Invoke(true, null, true, null, null);
                }
            }
            else
            {
                NetworkManager.DisconnectClient(arg2);
                Debug.LogWarning($"User id {arg2} was disconnected due to failed connection approval!");
            }

            if (m_ConnectionMessageToDisplay && arg2 != NetworkManager.LocalClientId)
            {
                if (isTokenValid)
                {
                    m_ConnectionMessageToDisplay.text = $"Client id {arg2} is authorized!";
                }
                else
                {
                    m_ConnectionMessageToDisplay.text = $"Client id {arg2} failed authorization!";
                }

                m_ConnectionMessageToDisplay.gameObject.SetActive(true);
                StartCoroutine(WaitToHideConnectionText());
            }
        }

        private IEnumerator WaitToHideConnectionText()
        {
            yield return new WaitForSeconds(5);
            if (m_ConnectionMessageToDisplay)
            {
                m_ConnectionMessageToDisplay.gameObject.SetActive(false);
            }
            yield return null;
        }
    }
}
