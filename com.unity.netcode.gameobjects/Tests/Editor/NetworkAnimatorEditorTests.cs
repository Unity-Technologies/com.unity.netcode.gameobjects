using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Unity.Netcode.GameObjects.EditorTests
{
    internal class NetworkAnimatorEditorTests
    {
        private const string k_TestControllerPath = "Assets/NetworkAnimatorConditionalExitTest.controller";

        private GameObject m_GameObject;

        [TearDown]
        public void TearDown()
        {
            if (m_GameObject != null)
            {
                Object.DestroyImmediate(m_GameObject);
            }

            AssetDatabase.DeleteAsset(k_TestControllerPath);
        }

        [Test]
        public void ConditionalExitTransitionResolvesParentDestination()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_TestControllerPath);
            controller.AddParameter("ExitSubMachine", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            var destinationState = rootStateMachine.AddState("State B");
            var subStateMachine = rootStateMachine.AddStateMachine("Sub SM");
            var originatingState = subStateMachine.AddState("State A");
            subStateMachine.defaultState = originatingState;

            rootStateMachine.AddEntryTransition(subStateMachine);

            var exitTransition = originatingState.AddExitTransition();
            exitTransition.hasExitTime = false;
            exitTransition.duration = 0.25f;
            exitTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitSubMachine");

            var parentTransition = rootStateMachine.AddStateMachineTransition(subStateMachine, destinationState);
            parentTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitSubMachine");
            AssetDatabase.SaveAssets();

            var networkAnimator = CreateNetworkAnimator(controller, nameof(ConditionalExitTransitionResolvesParentDestination));
            networkAnimator.InvokeOnValidate();

            Assert.That(networkAnimator.TransitionStateInfoList, Has.Count.EqualTo(1));
            var transitionInfo = networkAnimator.TransitionStateInfoList[0];
            Assert.That(transitionInfo.Layer, Is.Zero);
            Assert.That(transitionInfo.OriginatingState, Is.EqualTo(originatingState.nameHash));
            Assert.That(transitionInfo.DestinationState, Is.EqualTo(destinationState.nameHash));
            Assert.That(transitionInfo.TransitionDuration, Is.EqualTo(exitTransition.duration));
            Assert.That(transitionInfo.TriggerNameHash, Is.EqualTo(Animator.StringToHash("ExitSubMachine")));
        }

        [Test]
        public void NestedConditionalExitTransitionResolvesAncestorDestination()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_TestControllerPath);
            controller.AddParameter("ExitNested", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            var destinationState = rootStateMachine.AddState("State B");
            var outerStateMachine = rootStateMachine.AddStateMachine("Outer SM");
            var innerStateMachine = outerStateMachine.AddStateMachine("Inner SM");
            var originatingState = innerStateMachine.AddState("State A");
            innerStateMachine.defaultState = originatingState;

            rootStateMachine.AddEntryTransition(outerStateMachine);
            outerStateMachine.AddEntryTransition(innerStateMachine);

            var exitTransition = originatingState.AddExitTransition();
            exitTransition.hasExitTime = false;
            exitTransition.duration = 0.5f;
            exitTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitNested");

            var exitInnerTransition = outerStateMachine.AddStateMachineExitTransition(innerStateMachine);
            exitInnerTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitNested");
            var exitOuterTransition = rootStateMachine.AddStateMachineTransition(outerStateMachine, destinationState);
            exitOuterTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitNested");
            AssetDatabase.SaveAssets();

            var networkAnimator = CreateNetworkAnimator(controller, nameof(NestedConditionalExitTransitionResolvesAncestorDestination));
            networkAnimator.InvokeOnValidate();

            Assert.That(networkAnimator.TransitionStateInfoList, Has.Count.EqualTo(1));
            var transitionInfo = networkAnimator.TransitionStateInfoList[0];
            Assert.That(transitionInfo.OriginatingState, Is.EqualTo(originatingState.nameHash));
            Assert.That(transitionInfo.DestinationState, Is.EqualTo(destinationState.nameHash));
            Assert.That(transitionInfo.TransitionDuration, Is.EqualTo(exitTransition.duration));
        }

        [Test]
        public void ConditionalExitTransitionResolvesDestinationStateMachineStates()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_TestControllerPath);
            controller.AddParameter("ExitSubMachine", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            var sourceStateMachine = rootStateMachine.AddStateMachine("Source SM");
            var originatingState = sourceStateMachine.AddState("State A");
            sourceStateMachine.defaultState = originatingState;
            rootStateMachine.AddEntryTransition(sourceStateMachine);

            var destinationStateMachine = rootStateMachine.AddStateMachine("Destination SM");
            var defaultDestinationState = destinationStateMachine.AddState("Default State");
            var entryDestinationState = destinationStateMachine.AddState("Entry State");
            destinationStateMachine.defaultState = defaultDestinationState;
            destinationStateMachine.AddEntryTransition(entryDestinationState);

            var exitTransition = originatingState.AddExitTransition();
            exitTransition.hasExitTime = false;
            exitTransition.duration = 0.25f;
            exitTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitSubMachine");

            var parentTransition = rootStateMachine.AddStateMachineTransition(sourceStateMachine, destinationStateMachine);
            parentTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitSubMachine");
            AssetDatabase.SaveAssets();

            var networkAnimator = CreateNetworkAnimator(controller, nameof(ConditionalExitTransitionResolvesDestinationStateMachineStates));
            networkAnimator.InvokeOnValidate();

            Assert.That(networkAnimator.TransitionStateInfoList, Has.Count.EqualTo(2));
            Assert.That(networkAnimator.TransitionStateInfoList.Exists(entry => entry.DestinationState == defaultDestinationState.nameHash), Is.True);
            Assert.That(networkAnimator.TransitionStateInfoList.Exists(entry => entry.DestinationState == entryDestinationState.nameHash), Is.True);
        }

        private TestNetworkAnimator CreateNetworkAnimator(AnimatorController controller, string name)
        {
            m_GameObject = new GameObject(name);
            m_GameObject.AddComponent<NetworkObject>();
            var animator = m_GameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            var networkAnimator = m_GameObject.AddComponent<TestNetworkAnimator>();
            networkAnimator.Animator = animator;
            networkAnimator.AnimatorParameterEntries = new NetworkAnimator.AnimatorParametersListContainer();
            return networkAnimator;
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
