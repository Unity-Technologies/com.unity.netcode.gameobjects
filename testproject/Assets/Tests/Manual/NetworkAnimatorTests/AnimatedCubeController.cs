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

        internal void ToggleRotateAnimation()
        {
            m_Rotate = !m_Rotate;
            m_Animator.SetBool("Rotate", m_Rotate);
        }

        internal void PlayPulseAnimation(bool useNetworkAnimator = true)
        {
            if (useNetworkAnimator)
            {
                m_NetworkAnimator.SetTrigger("Pulse");
            }
            else
            {
                m_Animator.SetBool("Pulse", true);
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

