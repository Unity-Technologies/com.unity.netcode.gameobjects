using System;
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.RuntimeTests;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Unity.Netcode.RuntimeTest
{
    public class AnimatorTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;
        private GameObject m_TestPrefab;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    AnimatorController controller = Resources.Load("TestAnimatorController") as AnimatorController;
                    var animator = playerPrefab.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;

                    var theAnimator = playerPrefab.AddComponent<NetworkAnimator>();
                    theAnimator.Animator = animator;
                });
        }

        [UnityTest]
        public IEnumerator AnimationStateSyncTest()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));
            var serverPlayer = serverClientPlayerResult.Result.gameObject;
            var serverAnimator =  serverPlayer.GetComponent<Animator>();

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));
            var clientPlayer = clientClientPlayerResult.Result.gameObject;
            var clientAnimator = clientPlayer.GetComponent<Animator>();

            var serverAnimatorStateInfo = serverAnimator.GetCurrentAnimatorStateInfo(0);
            var clientAnimatorStateInfo = clientAnimator.GetCurrentAnimatorStateInfo(0);

            // we start in the default state
            Assert.True(serverAnimatorStateInfo.IsName("DefaultState"));
            Assert.True(clientAnimatorStateInfo.IsName("DefaultState"));

            serverAnimator.SetBool("AlphaParameter", true);

            // (ugh)
            yield return new WaitForSeconds(4.0f);

            // ...and now we should be in the AlphaState having triggered the AlphaParameter
            serverAnimatorStateInfo = serverAnimator.GetCurrentAnimatorStateInfo(0);
            clientAnimatorStateInfo = clientAnimator.GetCurrentAnimatorStateInfo(0);
            yield return WaitForConditionOrTimeOut(() => serverAnimatorStateInfo.IsName("AlphaState"));
            Assert.True(serverAnimatorStateInfo.IsName("AlphaState"));
            yield return WaitForConditionOrTimeOut(() => clientAnimatorStateInfo.IsName("AlphaState"));
            Assert.True(clientAnimatorStateInfo.IsName("AlphaState"));
        }
    }
}
