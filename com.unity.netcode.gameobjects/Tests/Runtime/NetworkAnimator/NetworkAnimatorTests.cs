#if COM_UNITY_MODULES_ANIMATION
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkAnimatorTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_PlayerOnServer;
        private GameObject m_PlayerOnClient;

        private Animator m_PlayerOnServerAnimator;
        private Animator m_PlayerOnClientAnimator;

        public NetworkAnimatorTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnCreatePlayerPrefab()
        {
            // ideally, we would build up the AnimatorController entirely in code and not need an asset,
            //  but after some attempts this doesn't seem readily doable.  Instead, we load a controller
            var controller = Resources.Load("TestAnimatorController") as RuntimeAnimatorController;
            var animator = m_PlayerPrefab.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            var networkAnimator = m_PlayerPrefab.AddComponent<NetworkAnimator>();
            networkAnimator.Animator = animator;
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_PlayerOnServer = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][m_ClientNetworkManagers[0].LocalClientId].gameObject;
            m_PlayerOnServerAnimator = m_PlayerOnServerAnimator = m_PlayerOnServer.GetComponent<Animator>();

            m_PlayerOnClient = m_PlayerNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][m_ClientNetworkManagers[0].LocalClientId].gameObject;
            m_PlayerOnClientAnimator = m_PlayerOnClient.GetComponent<Animator>();

            return base.OnServerAndClientsConnected();
        }

        // helper function to scan an animator and verify a given clip is present
        private bool HasClip(Animator animator, string clipName)
        {
            var clips = new List<AnimatorClipInfo>();
            animator.GetCurrentAnimatorClipInfo(0, clips);
            foreach (var clip in clips)
            {
                if (clip.clip.name == clipName)
                {
                    return true;
                }
            }
            return false;
        }

        [UnityTest]
        public IEnumerator AnimationTriggerReset([Values(true, false)] bool asHash)
        {
            // We have "UnboundTrigger" purposely not bound to any animations so we can test resetting.
            //  If we used a trigger that was bound to a transition, then the trigger would reset as soon as the
            //  transition happens.  This way it will stay stuck on
            string triggerString = "UnboundTrigger";
            int triggerHash = Animator.StringToHash(triggerString);

            // Verify trigger is off
            Assert.True(m_PlayerOnServerAnimator.GetBool(triggerString) == false);
            Assert.True(m_PlayerOnClientAnimator.GetBool(triggerString) == false);

            // trigger.
            if (asHash)
            {
                m_PlayerOnServer.GetComponent<NetworkAnimator>().SetTrigger(triggerHash);
            }
            else
            {
                m_PlayerOnServer.GetComponent<NetworkAnimator>().SetTrigger(triggerString);
            }

            // verify trigger is set for client and server
            yield return WaitForConditionOrTimeOut(() => asHash ? m_PlayerOnServerAnimator.GetBool(triggerHash) : m_PlayerOnServerAnimator.GetBool(triggerString));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out on server trigger set check");

            yield return WaitForConditionOrTimeOut(() => asHash ? m_PlayerOnClientAnimator.GetBool(triggerHash) : m_PlayerOnClientAnimator.GetBool(triggerString));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out on client trigger set check");

            // reset the trigger
            if (asHash)
            {
                m_PlayerOnServer.GetComponent<NetworkAnimator>().ResetTrigger(triggerHash);
            }
            else
            {
                m_PlayerOnServer.GetComponent<NetworkAnimator>().ResetTrigger(triggerString);
            }

            // verify trigger is reset for client and server
            yield return WaitForConditionOrTimeOut(() => asHash ? m_PlayerOnServerAnimator.GetBool(triggerHash) == false : m_PlayerOnServerAnimator.GetBool(triggerString) == false);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out on server reset check");

            yield return WaitForConditionOrTimeOut(() => asHash ? m_PlayerOnClientAnimator.GetBool(triggerHash) == false : m_PlayerOnClientAnimator.GetBool(triggerString) == false);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out on client reset check");
        }


        [UnityTest]
        public IEnumerator AnimationStateSyncTest()
        {
            // check that we have started in the default state
            Assert.True(m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));
            Assert.True(m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));

            // cause a change to the AlphaState state by setting AlphaParameter, which is
            //  the variable bound to the transition from default to AlphaState (see the TestAnimatorController asset)
            m_PlayerOnServerAnimator.SetBool("AlphaParameter", true);

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            yield return WaitForConditionOrTimeOut(() => m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("AlphaState"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Server failed to reach its animation state");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() => m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("AlphaState"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Client failed to sync its animation state from the server");
        }

        [UnityTest]
        public IEnumerator AnimationLayerStateSyncTest()
        {
            int layer = 1;
            // check that we have started in the default state
            Assert.True(m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(layer).IsName("DefaultStateLayer2"));
            Assert.True(m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(layer).IsName("DefaultStateLayer2"));

            // cause a change to the AlphaState state by setting AlphaParameter, which is
            //  the variable bound to the transition from default to AlphaState (see the TestAnimatorController asset)
            m_PlayerOnServerAnimator.SetBool("Layer2AlphaParameter", true);

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            yield return WaitForConditionOrTimeOut(() => m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(layer).IsName("Layer2AlphaState"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Server failed to reach its animation state");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() => m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(layer).IsName("Layer2AlphaState"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Client failed to sync its animation state from the server");
        }

        [UnityTest]
        public IEnumerator AnimationLayerWeightTest()
        {
            int layer = 1;
            float targetWeight = 0.333f;

            // check that we have started in the default state
            Assert.True(Mathf.Approximately(m_PlayerOnServerAnimator.GetLayerWeight(layer), 1f));
            Assert.True(Mathf.Approximately(m_PlayerOnClientAnimator.GetLayerWeight(layer), 1f));

            m_PlayerOnServerAnimator.SetLayerWeight(layer, targetWeight);

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            yield return WaitForConditionOrTimeOut(() =>
                Mathf.Approximately(m_PlayerOnServerAnimator.GetLayerWeight(layer), targetWeight)
            );
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Server failed to reach its animation state");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() =>
                Mathf.Approximately(m_PlayerOnClientAnimator.GetLayerWeight(layer), targetWeight)
            );
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Server failed to reach its animation state");
        }


        [UnityTest]
        public IEnumerator AnimationStateSyncTriggerTest([Values(true, false)] bool asHash)
        {
            string triggerString = "TestTrigger";
            int triggerHash = Animator.StringToHash(triggerString);

            // check that we have started in the default state
            Assert.True(m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));
            Assert.True(m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));

            // cause a change to the AlphaState state by setting TestTrigger
            //  note, we have a special test for triggers because activating triggers via the
            //  NetworkAnimator is special; for other parameters you set them on the Animator and NetworkAnimator
            //  listens.  But because triggers are super short and transitory, we require users to call
            //  NetworkAnimator.SetTrigger so we don't miss it
            if (asHash)
            {
                m_PlayerOnServer.GetComponent<NetworkAnimator>().SetTrigger(triggerHash);
            }
            else
            {
                m_PlayerOnServer.GetComponent<NetworkAnimator>().SetTrigger(triggerString);
            }

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            yield return WaitForConditionOrTimeOut(() => m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("TriggeredState"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Server failed to reach its animation state via trigger");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() => m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("TriggeredState"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Client failed to sync its animation state from the server via trigger");
        }

        [UnityTest]
        public IEnumerator AnimationStateSyncTestWithOverride()
        {
            // set up the animation override controller
            var overrideController = Resources.Load("TestAnimatorOverrideController") as AnimatorOverrideController;
            m_PlayerOnServer.GetComponent<Animator>().runtimeAnimatorController = overrideController;
            m_PlayerOnClient.GetComponent<Animator>().runtimeAnimatorController = overrideController;

            // in our default state, we should see the OverrideDefaultAnimation clip
            Assert.True(HasClip(m_PlayerOnServerAnimator, "OverrideDefaultAnimation"));
            Assert.True(HasClip(m_PlayerOnClientAnimator, "OverrideDefaultAnimation"));

            // cause a change to the AlphaState state by setting AlphaParameter, which is
            //  the variable bound to the transition from default to AlphaState (see the TestAnimatorController asset)
            m_PlayerOnServerAnimator.SetBool("AlphaParameter", true);

            // ...and now we should be in the AlphaState having set the AlphaParameter
            yield return WaitForConditionOrTimeOut(() => HasClip(m_PlayerOnServerAnimator, "OverrideAlphaAnimation"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Server failed to reach its overriden animation state");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() => HasClip(m_PlayerOnServerAnimator, "OverrideAlphaAnimation"));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Client failed to reach its overriden animation state");
        }
    }
}
#endif // COM_UNITY_MODULES_ANIMATION
