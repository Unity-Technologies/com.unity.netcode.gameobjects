using System;
using System.Collections;
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
        private bool m_IsServerAuthoritative = true;

        private void DetermineNetworkAnimatorComponentType()
        {
            m_NetworkAnimator = GetComponent<NetworkAnimator>();
            if (m_NetworkAnimator != null)
            {
                m_IsServerAuthoritative = m_NetworkAnimator.GetType() != typeof(OwnerNetworkAnimator);
            }
            else
            {
                throw new Exception($"{nameof(AnimatedCubeController)} requires that it is paired with either a {nameof(NetworkAnimator)} or {nameof(OwnerNetworkAnimator)}.  Neither of the two components were found!");
            }
        }

        public override void OnNetworkSpawn()
        {
            DetermineNetworkAnimatorComponentType();

            m_Animator = GetComponent<Animator>();

            m_Rotate = m_Animator.GetBool("Rotate");
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
                    m_NetworkAnimator.SetTrigger("Pulse");
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
            var waitForSeconds = new WaitForSeconds(0.016f);
            var counter = 1.0f;
            Debug.Log("Linearly increase test:");
            while (counter < 100)
            {
                m_Animator.SetFloat("TestFloat", counter);
                m_Animator.SetInteger("TestInt", (int)counter);
                counter++;
                yield return waitForSeconds;
            }
            Debug.Log("Random value assignment test:");
            counter = 0.0f;
            while (counter < 100)
            {
                m_Animator.SetFloat("TestFloat", UnityEngine.Random.Range(0.0f, 100.0f));
                m_Animator.SetInteger("TestInt", UnityEngine.Random.Range(0, 100));
                counter++;
                yield return waitForSeconds;
            }
            StopCoroutine(m_TestAnimatorRoutine);
            m_TestAnimatorRoutine = null;
        }

        private int m_TestIntValue;
        private float m_TestFloatValue;

        private void DisplayTestIntValueIfChanged()
        {
            var testIntValue = m_Animator.GetInteger("TestInt");
            if (m_TestIntValue != testIntValue)
            {
                m_TestIntValue = testIntValue;
                Debug.Log($"[{name}]TestInt value changed to = {m_TestIntValue}");
            }
            var testFloatValue = m_Animator.GetFloat("TestFloat");
            if (m_TestFloatValue != testFloatValue)
            {
                m_TestFloatValue = testFloatValue;
                Debug.Log($"[{name}]TestFloat value changed to = {m_TestIntValue}");
            }
        }

        private void LateUpdate()
        {

            if (!IsSpawned || !IsOwner)
            {
                if (!IsOwner && IsSpawned)
                {
                    DisplayTestIntValueIfChanged();
                    return;
                }

                return;
            }

            DisplayTestIntValueIfChanged();

            // Rotates the cube
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleRotateAnimation();
            }

            // Pulse animation (scale down and up slowly)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayPulseAnimation();
            }

            // Test changing Animator parameters over time
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestAnimator();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log($"[{name}] TestInt value = {m_TestIntValue}");
                Debug.Log($"[{name}] TestInt value = {m_TestIntValue}");
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (m_IsServerAuthoritative && !IsServer)
                {
                    CrossFadeToStateServerRpc();
                }
                else
                {
                    CrossFadeToState();
                }
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                TestChangingBoolOneFrame();
            }

            if (m_CheckBoolValue)
            {
                m_CheckBoolValue = false;
                var testBool = m_Animator.GetBool("TestBool");
                if (testBool != m_BoolValueToExpect)
                {
                    Debug.LogError($"TestBool Value was {testBool} and expected {m_BoolValueToExpect}");
                }
                else
                {
                    Debug.Log($"TestBool Value maintained its value {m_BoolValueToExpect} set in a coroutine after one frame.");
                }
            }
        }

        private Coroutine m_CrossFadeCoroutine;

        private void CrossFadeToState()
        {

            var currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
            var currentStateHash = currentState.shortNameHash;
            m_Animator.CrossFade("NonLoopingState", 0);

            m_CrossFadeCoroutine = StartCoroutine(CrossFadeCoroutine(currentStateHash));
        }

        [ServerRpc(RequireOwnership = false)]
        private void CrossFadeToStateServerRpc()
        {
            CrossFadeToState();
        }

        private IEnumerator CrossFadeCoroutine(int originatingState)
        {
            bool reachedEnd = false;
            yield return null;
            var nonLoopingHash = Animator.StringToHash("NonLoopingState");
            var currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
            var currentStateHash = currentState.shortNameHash;
            while(currentStateHash != nonLoopingHash)
            {
                currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
                currentStateHash = currentState.shortNameHash;
                yield return null;
            }
            Debug.Log("Fist non-looping state crossfade playing!");
            var animationClipInfo = m_Animator.GetCurrentAnimatorClipInfo(0);

            while (!reachedEnd)
            {
                currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
                reachedEnd = currentState.normalizedTime >= 1.0f;
                yield return null;
            }
            Debug.Log("First non-looping state finished! Transitioning back to originating state...");
            reachedEnd = false;
            currentStateHash = currentState.shortNameHash;
            m_Animator.CrossFade(originatingState, 0);
            while (currentStateHash != originatingState)
            {
                currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
                currentStateHash = currentState.shortNameHash;
                yield return null;
            }

            Debug.Log("Transitioned to originating state! Starting second cross fade transition...");
            m_Animator.CrossFade("NonLoopingState", 0);
            while (currentStateHash != nonLoopingHash)
            {
                currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
                currentStateHash = currentState.shortNameHash;
                yield return null;
            }
            Debug.Log("Started second nonlooping state!");
            reachedEnd = false;
            while (!reachedEnd)
            {
                currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
                reachedEnd = currentState.normalizedTime >= 1.0f;
                yield return null;
            }
            Debug.Log("Reached the end of the second state using crossfade! Transitioning back to the originating state.");
            m_Animator.CrossFade(originatingState, 0);
            m_CrossFadeCoroutine = null;
            yield break;
        }

        private bool m_BoolValueToExpect;
        private bool m_BoolValueThatWasTemporarilySet;
        private bool m_CheckBoolValue;
        private Coroutine m_TestChangingBoolOneFrameCoroutine;

        private void TestChangingBoolOneFrame()
        {
            if (m_TestChangingBoolOneFrameCoroutine == null)
            {
                m_CheckBoolValue = false;
                m_BoolValueToExpect = m_Animator.GetBool("TestBool");
                m_BoolValueThatWasTemporarilySet = !m_BoolValueToExpect;
                m_Animator.SetBool("TestBool", m_BoolValueThatWasTemporarilySet);
                m_TestChangingBoolOneFrameCoroutine = StartCoroutine(WaitOneFrameThenChangeBool(m_BoolValueToExpect));
            }
        }

        private IEnumerator WaitOneFrameThenChangeBool(bool originalValue)
        {
            yield return null;
            var testBool = m_Animator.GetBool("TestBool");
            if (testBool != m_BoolValueThatWasTemporarilySet)
            {
                Debug.LogError($"[Coroutine] TestBool was {testBool} but expected {m_BoolValueThatWasTemporarilySet}");
                m_TestChangingBoolOneFrameCoroutine = null;
                yield break;
            }
            else
            {
                Debug.Log($"[Coroutine] TestBool Value maintained its 1 frame value {testBool} before being reset in this coroutine.");
            }
            m_Animator.SetBool("TestBool", originalValue);
            m_CheckBoolValue = true;
            m_TestChangingBoolOneFrameCoroutine = null;
            yield break;
        }
    }
}

