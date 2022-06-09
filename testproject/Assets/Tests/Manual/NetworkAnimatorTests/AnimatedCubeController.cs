using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Tests.Manual.NetworkAnimatorTests
{
    [RequireComponent(typeof(Animator))]
    public class AnimatedCubeController : NetworkBehaviour
    {
        public int TestIterations = 20;
        private Animator m_Animator;
        private bool m_Rotate;
        private NetworkAnimator m_NetworkAnimator;
        private bool m_IsServerAuthoritative = true;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_NetworkAnimator = GetComponent<NetworkAnimator>();
            if (m_NetworkAnimator == null)
            {
                m_NetworkAnimator = GetComponent<OwnerNetworkAnimator>();
                if (m_NetworkAnimator != null)
                {
                    m_IsServerAuthoritative = false;
                }
                else
                {
                    throw new System.Exception($"{nameof(AnimatedCubeController)} requires that it is paired with either a {nameof(NetworkAnimator)} or {nameof(OwnerNetworkAnimator)}.  Neither of the two components were found!");
                }
            }
            m_Rotate = m_Animator.GetBool("Rotate");
        }

        public override void OnNetworkSpawn()
        {
            if (HasAuthority())
            {
                enabled = false;
            }
        }

        private bool HasAuthority()
        {
            if (IsOwnerAuthority() || IsServerAuthority())
            {
                return true;
            }
            return false;
        }

        private bool IsServerAuthority()
        {
            if (IsServer && m_IsServerAuthoritative)
            {
                return true;
            }
            return false;
        }

        private bool IsOwnerAuthority()
        {
            if (IsOwner && !m_IsServerAuthoritative)
            {
                return true;
            }
            return false;
        }


        [ServerRpc(RequireOwnership = false)]
        private void ToggleRotateAnimationServerRpc(bool rotate)
        {
            m_Rotate = rotate;
            m_Animator.SetBool("Rotate", m_Rotate);
        }

        internal void ToggleRotateAnimation()
        {
            m_Rotate = !m_Rotate;
            if (m_IsServerAuthoritative)
            {
                if (!IsServer && IsOwner)
                {
                    ToggleRotateAnimationServerRpc(m_Rotate);
                }
                else if (IsServer && IsOwner)
                {
                    m_Animator.SetBool("Rotate", m_Rotate);
                }
            }
            else if (IsOwner)
            {
                m_Animator.SetBool("Rotate", m_Rotate);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlayPulseAnimationServerRpc(bool rotate)
        {
            m_NetworkAnimator.SetTrigger("Pulse");
        }

        internal void PlayPulseAnimation()
        {
            if (m_IsServerAuthoritative)
            {
                if (!IsServer && IsOwner)
                {
                    PlayPulseAnimationServerRpc(m_Rotate);
                }
                else if (IsServer && IsOwner)
                {
                    m_NetworkAnimator.SetTrigger("Pulse");
                }
            }
            else if (IsOwner)
            {
                m_NetworkAnimator.SetTrigger("Pulse");
            }
        }


        private Coroutine m_TestAnimatorRoutine;

        internal void TestAnimator(bool useNetworkAnimator = true)
        {
            if (IsServer)
            {
                if (m_TestAnimatorRoutine == null)
                {
                    m_TestAnimatorRoutine = StartCoroutine(TestAnimatorRoutine());
                }
            }
        }


        private IEnumerator TestAnimatorRoutine()
        {
            var interations = 0;
            while (interations < TestIterations)
            {
                var counter = 1.0f;
                m_NetworkAnimator.SetTrigger("TestTrigger");
                while (counter < 100)
                {
                    m_Animator.SetFloat("TestFloat", counter);
                    m_Animator.SetInteger("TestInt", (int)counter);
                    counter++;
                    yield return null;
                }
                interations++;
            }
            yield return null;
        }
    }
}

