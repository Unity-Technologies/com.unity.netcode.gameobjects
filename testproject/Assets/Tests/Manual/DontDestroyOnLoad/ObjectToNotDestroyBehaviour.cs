using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    /// <summary>
    /// This will move itself into the DontDestroyOnLoadScene when instantiated
    /// </summary>
    public class ObjectToNotDestroyBehaviour : NetworkBehaviour
    {
        public static bool VerboseDebug;

        private bool m_ContinueSendingPing;
        private uint m_PingCounter;


        public uint CurrentPing
        {
            get
            {
                return m_PingCounter;
            }
        }

        private void Log(string msg)
        {
            if (VerboseDebug)
            {
                Debug.Log(msg);
            }
        }

        // Migrate into DDOL during pre-spawn
        protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            DontDestroyOnLoad(this);
            base.OnNetworkPreSpawn(ref networkManager);
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
                Log($"Sent ping number ({pingNumber}).");
            }
            else if (IsClient)
            {
                Log($"Receiving ping number ({pingNumber}) from server");
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
            while (m_ContinueSendingPing)
            {
                m_PingCounter++;
                PingUpdateClientRpc(m_PingCounter);
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }
    }
}
