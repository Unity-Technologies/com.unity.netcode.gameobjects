using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ObjectToNotDestroyBehaviour : NetworkBehaviour
    {
        private void OnEnable()
        {
            DontDestroyOnLoad(this);
        }

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
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_ContinueSendingPing = true;
                StartCoroutine(SendContinualPing());
            }
            base.OnNetworkSpawn();
        }

        private bool m_ContinueSendingPing;
        private uint m_PingCounter;
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                StopCoroutine(SendContinualPing());
                m_ContinueSendingPing = false;
            }

            base.OnNetworkDespawn();
        }

        private IEnumerator SendContinualPing()
        {
            while(m_ContinueSendingPing)
            {
                m_PingCounter++;
                PingUpdateClientRpc(m_PingCounter);
                yield return new WaitForSeconds(1);
            }
            yield return null;
        }
    }
}
