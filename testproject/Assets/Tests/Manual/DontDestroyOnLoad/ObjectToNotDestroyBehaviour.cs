using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    /// <summary>
    /// This will move itself into the DontDestroyOnLoadScene when instantiated
    /// </summary>
    public class ObjectToNotDestroyBehaviour : NetworkBehaviour
    {
        private bool m_ContinueSendingPing;
        private uint m_PingCounter;

        public uint CurrentPing
        {
            get
            {
                return m_PingCounter;
            }
        }

        /// <summary>
        /// When enabled, we move ourself to the DontDestroyOnLoad scene
        /// </summary>
        private void OnEnable()
        {
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// This is to visually verify this NetworkObject was synchronized and is working
        /// (i.e. receiving RPCs )
        /// </summary>
        /// <param name="pingNumber"></param>
        [ClientRpc]
        private void PingUpdateClientRpc(uint pingNumber)
        {
            if (IsHost)
            {
                Debug.Log($"Sent ping number ({pingNumber}).");
            }
            else if (IsClient)
            {
                Debug.Log($"Receiving ping number ({pingNumber}) from server");
                m_PingCounter = pingNumber;
            }
        }

        /// <summary>
        /// For the server it starts the coroutine to generate a RPC ping
        /// every second
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_ContinueSendingPing = true;
                StartCoroutine(SendContinualPing());
            }
            base.OnNetworkSpawn();
        }

        /// <summary>
        /// Server will stop the coroutine when we are despawning
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                StopCoroutine(SendContinualPing());
                m_ContinueSendingPing = false;
            }

            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Coroutine to send the ping message every second
        /// </summary>
        /// <returns></returns>
        private IEnumerator SendContinualPing()
        {
            while (m_ContinueSendingPing && NetworkManager.IsListening)
            {
                m_PingCounter++;
                PingUpdateClientRpc(m_PingCounter);
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }
    }
}
