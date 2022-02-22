using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.RuntimeTests;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTest
{
    public class NetworkAnimatorTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;
        private GameObject m_TestPrefab;

        private GameObject m_PlayerOnServer;
        private GameObject m_PlayerOnClient;

        private Animator m_PlayerOnServerAnimator;
        private Animator m_PlayerOnClientAnimator;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    // ideally, we would build up the AnimatorController entirely in code and not need an asset,
                    //  but after some attempts this doesn't seem readily doable.  Instead, we load a controller
                    var controller = Resources.Load("TestAnimatorController") as AnimatorController;
                    var animator = playerPrefab.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;

                    var theAnimator = playerPrefab.AddComponent<NetworkAnimator>();
                    theAnimator.Animator = animator;
                });

            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));
            m_PlayerOnServer = serverClientPlayerResult.Result.gameObject;
            m_PlayerOnServerAnimator = m_PlayerOnServer.GetComponent<Animator>();

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));
            m_PlayerOnClient = clientClientPlayerResult.Result.gameObject;
            m_PlayerOnClientAnimator = m_PlayerOnClient.GetComponent<Animator>();
        }

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
        public IEnumerator AnimationStateSyncTest()
        {
            // check that we have started in the default state
            Assert.True(m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));
            Assert.True(m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));

            // cause a change to the AlphaState state by setting AlphaParameter, which is
            //  the variable bound to the transition from default to AlphaState (see the TestAnimatorController asset)
            m_PlayerOnServerAnimator.SetBool("AlphaParameter", true);

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            yield return WaitForConditionOrTimeOut(() =>
                m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("AlphaState"));
            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Server failed to reach its animation state");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() =>
                m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("AlphaState"));
            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Client failed to sync its animation state from the server");
        }

        [UnityTest]
        public IEnumerator AnimationStateSyncTriggerTest()
        {
            // check that we have started in the default state
            Assert.True(m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));
            Assert.True(m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("DefaultState"));

            // cause a change to the AlphaState state by setting AlphaParameter, which is
            //  the variable bound to the transition from default to AlphaState (see the TestAnimatorController asset)
            m_PlayerOnServer.GetComponent<NetworkAnimator>().SetTrigger("TestTrigger");

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            yield return WaitForConditionOrTimeOut(() =>
                m_PlayerOnServerAnimator.GetCurrentAnimatorStateInfo(0).IsName("TriggeredState"));
            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Server failed to reach its animation state via trigger");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() =>
                m_PlayerOnClientAnimator.GetCurrentAnimatorStateInfo(0).IsName("TriggeredState"));
            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Client failed to sync its animation state from the server via trigger");
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

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            yield return WaitForConditionOrTimeOut(() =>
                HasClip(m_PlayerOnServerAnimator, "OverrideAlphaAnimation"));
            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Server failed to reach its overriden animation state");

            // ...and now the client should also have sync'd and arrived at the correct state
            yield return WaitForConditionOrTimeOut(() =>
                HasClip(m_PlayerOnServerAnimator, "OverrideAlphaAnimation"));
            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Client failed to reach its overriden animation state");
        }
    }
}
