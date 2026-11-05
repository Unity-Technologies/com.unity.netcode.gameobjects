#if COM_UNITY_MODULES_ANIMATION && UNITY_EDITOR
using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Verifies a client that joins while the authority is mid-transition from a state other than
    /// the controller's default state still cross-fades into the transition's destination state.
    /// </summary>
    internal class NetworkAnimatorLateJoinTransitionTests : NetcodeIntegrationTest
    {
        private const string k_ControllerPath = "Assets/NetworkAnimatorLateJoinTransitionTest.controller";
        private const string k_TriggerName = "ToB";

        protected override int NumberOfClients => 1;

        private AnimatorController m_Controller;
        private AnimatorState m_DefaultState;
        private AnimatorState m_SourceState;
        private AnimatorState m_DestinationState;
        private GameObject m_Prefab;

        protected override void OnServerAndClientsCreated()
        {
            m_Controller = AnimatorController.CreateAnimatorControllerAtPath(k_ControllerPath);
            m_Controller.AddParameter(k_TriggerName, AnimatorControllerParameterType.Trigger);

            var rootStateMachine = m_Controller.layers[0].stateMachine;
            m_DefaultState = rootStateMachine.AddState("State A");
            m_SourceState = rootStateMachine.AddState("State C");
            m_DestinationState = rootStateMachine.AddState("State B");
            rootStateMachine.defaultState = m_DefaultState;

            // The source clip is much longer than the default one, so a cross-fade normalized to the wrong state blends far faster
            m_DefaultState.motion = CreateClip("Short", 1.0f);
            m_SourceState.motion = CreateClip("Long", 100.0f);
            m_DestinationState.motion = CreateClip("Destination", 1.0f);

            // Both states transition into the same destination, and the transitions are long enough to still be running when the client joins
            foreach (var state in new[] { m_DefaultState, m_SourceState })
            {
                var transition = state.AddTransition(m_DestinationState);
                transition.hasExitTime = false;
                transition.hasFixedDuration = false;
                transition.duration = 0.5f;
                transition.AddCondition(AnimatorConditionMode.If, 0.0f, k_TriggerName);
            }
            AssetDatabase.SaveAssets();

            m_Prefab = CreateNetworkObjectPrefab("Animated");
            var animator = m_Prefab.AddComponent<Animator>();
            animator.runtimeAnimatorController = m_Controller;
            var networkAnimator = m_Prefab.AddComponent<TestNetworkAnimator>();
            networkAnimator.Animator = animator;
            networkAnimator.AnimatorParameterEntries = new NetworkAnimator.AnimatorParametersListContainer();
            networkAnimator.InvokeOnValidate();
            networkAnimator.OnAfterDeserialize();

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            AssetDatabase.DeleteAsset(k_ControllerPath);
            yield return base.OnTearDown();
        }

        [UnityTest]
        public IEnumerator LateJoinFromNonDefaultStateCrossFadesToDestination()
        {
            var authority = GetAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_Prefab, authority).GetComponent<NetworkObject>();
            var authorityAnimator = authorityInstance.GetComponent<Animator>();

            authorityAnimator.Play(m_SourceState.nameHash, 0);
            yield return WaitForConditionOrTimeOut(_ => authorityAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == m_SourceState.nameHash);
            AssertOnTimeout($"Authority did not enter {m_SourceState.name}!");

            authorityAnimator.SetTrigger(k_TriggerName);
            yield return WaitForConditionOrTimeOut(_ => authorityAnimator.IsInTransition(0) &&
                authorityAnimator.GetNextAnimatorStateInfo(0).shortNameHash == m_DestinationState.nameHash);
            AssertOnTimeout($"Authority did not start transitioning from {m_SourceState.name} to {m_DestinationState.name}!");

            yield return CreateAndStartNewClient();
            var lateJoinClient = m_ClientNetworkManagers[m_ClientNetworkManagers.Length - 1];

            yield return WaitForConditionOrTimeOut(_ => lateJoinClient.SpawnManager.SpawnedObjects.ContainsKey(authorityInstance.NetworkObjectId));
            AssertOnTimeout($"Late-joining client did not spawn {authorityInstance.name}!");
            var clientAnimator = lateJoinClient.SpawnManager.SpawnedObjects[authorityInstance.NetworkObjectId].GetComponent<Animator>();

            yield return WaitForConditionOrTimeOut(errorLog => ReachingDestination(clientAnimator, errorLog));
            AssertOnTimeout($"Late-joining client did not cross-fade to {m_DestinationState.name}!");
            Assert.That(clientAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash, Is.EqualTo(m_SourceState.nameHash), $"Late-joining client did not cross-fade from {m_SourceState.name}!");
        }

        private AnimationClip CreateClip(string name, float length)
        {
            var clip = new AnimationClip { name = name };
            clip.SetCurve(string.Empty, typeof(Transform), "localScale.x", AnimationCurve.Linear(0.0f, 1.0f, length, 1.0f));
            AssetDatabase.AddObjectToAsset(clip, m_Controller);
            return clip;
        }

        private bool ReachingDestination(Animator animator, StringBuilder errorLog)
        {
            var current = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            if (current == m_DestinationState.nameHash || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == m_DestinationState.nameHash))
            {
                return true;
            }
            errorLog.Append($"Client current state hash {current}, in transition: {animator.IsInTransition(0)}");
            return false;
        }

        private class TestNetworkAnimator : NetworkAnimator
        {
            public void InvokeOnValidate()
            {
                base.OnValidate();
            }
        }
    }
}
#endif
