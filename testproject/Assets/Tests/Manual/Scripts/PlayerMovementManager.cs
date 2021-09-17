using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Used to simulate a player moving around
    /// </summary>
    public class PlayerMovementManager : NetworkBehaviour
    {
        public int MoveSpeed = 10;

        private RandomMovement m_RandomMovement;

        // Start is called before the first frame update
        private void Start()
        {
            m_RandomMovement = GetComponent<RandomMovement>();
        }

        private void Update()
        {
            if (NetworkObject != null)
            {
                if (IsOwner && Input.GetKeyDown(KeyCode.Space))
                {
                    if (m_RandomMovement)
                    {
                        m_RandomMovement.enabled = !m_RandomMovement.enabled;
                    }
                }

                if (NetworkObject != null && NetworkObject.NetworkManager != null && NetworkObject.NetworkManager.IsListening)
                {
                    if (m_RandomMovement.enabled)
                    {
                        m_RandomMovement.Move(MoveSpeed);
                    }
                }
            }
        }
    }
}
