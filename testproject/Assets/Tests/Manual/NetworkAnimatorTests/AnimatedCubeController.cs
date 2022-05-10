using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Tests.Manual.NetworkAnimatorTests
{
    [RequireComponent(typeof(Animator))]
    public class AnimatedCubeController : NetworkBehaviour
    {
        private Animator m_Animator;
        private bool m_Rotate;
        private NetworkAnimator m_NetworkAnimator;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_NetworkAnimator = GetComponent<NetworkAnimator>();
            m_Rotate = m_Animator.GetBool("Rotate");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (m_NetworkAnimator.OwnerAuthoritative && IsOwner)
                {
                    ToggleRotateAnimation();
                }
                else if (!m_NetworkAnimator.OwnerAuthoritative)
                {
                    if (IsServer && IsOwner)
                    {
                        ToggleRotateAnimation();
                    }
                    else
                    {
                        ToggleRotateAnimationServerRpc();
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (m_NetworkAnimator.OwnerAuthoritative && IsOwner)
                {
                    PlayPulseAnimation();
                }
                else if (!m_NetworkAnimator.OwnerAuthoritative)
                {
                    if (IsServer && IsOwner)
                    {
                        PlayPulseAnimation();
                    }
                    else if (IsOwner)
                    {
                        PlayPulseAnimationServerRpc();
                    }
                }
            }
        }

        private void ToggleRotateAnimation()
        {
            m_Rotate = !m_Rotate;
            m_Animator.SetBool("Rotate", m_Rotate);
        }

        private void PlayPulseAnimation()
        {
            m_NetworkAnimator.SetTrigger("Pulse");
        }

        [ServerRpc]
        public void ToggleRotateAnimationServerRpc(ServerRpcParams serverRpcParams = default)
        {
            // Since animator parameters update automatically, we only have to handle the case where it is server authoritative
            // but we still only allow the owner of the object to dictate when it would like something updated (for this example/test)
            if (!m_NetworkAnimator.OwnerAuthoritative && serverRpcParams.Receive.SenderClientId == OwnerClientId)
            {
                ToggleRotateAnimation();
            }
        }

        [ServerRpc]
        public void PlayPulseAnimationServerRpc(ServerRpcParams serverRpcParams = default)
        {
            // Since animator parameters update automatically, we only have to handle the case where it is server authoritative
            // but we still only allow the owner of the object to dictate when it would like something updated (for this example/test)
            if (!m_NetworkAnimator.OwnerAuthoritative && serverRpcParams.Receive.SenderClientId == OwnerClientId)
            {
                PlayPulseAnimation();
            }
        }

        [ClientRpc]
        public void PlayPulseAnimationClientRpc(ClientRpcParams clientRpcParams = default)
        {
            PlayPulseAnimation();
        }
    }
}

