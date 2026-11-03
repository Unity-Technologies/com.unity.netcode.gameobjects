#if COM_UNITY_MODULES_ANIMATION
using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Unity.Netcode.EditorTests
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

        [Test]
        public void RootConditionalExitTransitionResolvesRootEntry()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_TestControllerPath);
            controller.AddParameter("ExitRoot", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            var defaultState = rootStateMachine.AddState("Default State");
            var originatingState = rootStateMachine.AddState("State A");
            rootStateMachine.defaultState = defaultState;

            var exitTransition = originatingState.AddExitTransition();
            exitTransition.hasExitTime = false;
            exitTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitRoot");
            AssetDatabase.SaveAssets();

            var networkAnimator = CreateNetworkAnimator(controller, nameof(RootConditionalExitTransitionResolvesRootEntry));
            networkAnimator.InvokeOnValidate();

            Assert.That(networkAnimator.TransitionStateInfoList, Has.Count.EqualTo(1));
            Assert.That(networkAnimator.TransitionStateInfoList[0].OriginatingState, Is.EqualTo(originatingState.nameHash));
            Assert.That(networkAnimator.TransitionStateInfoList[0].DestinationState, Is.EqualTo(defaultState.nameHash));
        }

        [Test]
        public void ConditionalExitWithoutParentTransitionResolvesParentEntry()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_TestControllerPath);
            controller.AddParameter("ExitSubMachine", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            var rootDefaultState = rootStateMachine.AddState("Root Default State");
            rootStateMachine.defaultState = rootDefaultState;
            var outerStateMachine = rootStateMachine.AddStateMachine("Outer SM");
            var outerDefaultState = outerStateMachine.AddState("Outer Default State");
            outerStateMachine.defaultState = outerDefaultState;
            var innerStateMachine = outerStateMachine.AddStateMachine("Inner SM");
            var originatingState = innerStateMachine.AddState("State A");

            var exitTransition = originatingState.AddExitTransition();
            exitTransition.hasExitTime = false;
            exitTransition.AddCondition(AnimatorConditionMode.If, 0.0f, "ExitSubMachine");
            AssetDatabase.SaveAssets();

            var networkAnimator = CreateNetworkAnimator(controller, nameof(ConditionalExitWithoutParentTransitionResolvesParentEntry));
            networkAnimator.InvokeOnValidate();

            Assert.That(networkAnimator.TransitionStateInfoList, Has.Count.EqualTo(1));
            Assert.That(networkAnimator.TransitionStateInfoList[0].OriginatingState, Is.EqualTo(originatingState.nameHash));
            Assert.That(networkAnimator.TransitionStateInfoList[0].DestinationState, Is.EqualTo(outerDefaultState.nameHash));
        }

        [Test]
        public void ConditionalTransitionToStateMachineResolvesItsEntryStates()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_TestControllerPath);
            controller.AddParameter("EnterSubMachine", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            var originatingState = rootStateMachine.AddState("State A");
            var destinationStateMachine = rootStateMachine.AddStateMachine("Destination SM");
            var defaultDestinationState = destinationStateMachine.AddState("Default State");
            var entryDestinationState = destinationStateMachine.AddState("Entry State");
            destinationStateMachine.defaultState = defaultDestinationState;
            destinationStateMachine.AddEntryTransition(entryDestinationState);

            var transition = originatingState.AddTransition(destinationStateMachine);
            transition.hasExitTime = false;
            transition.AddCondition(AnimatorConditionMode.If, 0.0f, "EnterSubMachine");
            AssetDatabase.SaveAssets();

            var networkAnimator = CreateNetworkAnimator(controller, nameof(ConditionalTransitionToStateMachineResolvesItsEntryStates));
            networkAnimator.InvokeOnValidate();

            Assert.That(networkAnimator.TransitionStateInfoList, Has.Count.EqualTo(2));
            Assert.That(networkAnimator.TransitionStateInfoList.Exists(entry => entry.DestinationState == defaultDestinationState.nameHash), Is.True);
            Assert.That(networkAnimator.TransitionStateInfoList.Exists(entry => entry.DestinationState == entryDestinationState.nameHash), Is.True);
        }

        [Test]
        public void TransitionInfoLookupMatchesOriginatingState()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_TestControllerPath);
            controller.AddParameter("ToB", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            var stateA = rootStateMachine.AddState("State A");
            var stateC = rootStateMachine.AddState("State C");
            var stateB = rootStateMachine.AddState("State B");

            var transitionFromA = stateA.AddTransition(stateB);
            transitionFromA.duration = 0.1f;
            transitionFromA.AddCondition(AnimatorConditionMode.If, 0.0f, "ToB");
            var transitionFromC = stateC.AddTransition(stateB);
            transitionFromC.duration = 0.7f;
            transitionFromC.AddCondition(AnimatorConditionMode.If, 0.0f, "ToB");
            AssetDatabase.SaveAssets();

            var networkAnimator = CreateNetworkAnimator(controller, nameof(TransitionInfoLookupMatchesOriginatingState));
            networkAnimator.InvokeOnValidate();
            networkAnimator.OnAfterDeserialize();

            Assert.That(networkAnimator.TryGetTransitionInfo(0, stateB.nameHash, stateA.nameHash, out var infoFromA), Is.True);
            Assert.That(infoFromA.OriginatingState, Is.EqualTo(stateA.nameHash));
            Assert.That(infoFromA.TransitionDuration, Is.EqualTo(transitionFromA.duration));
            Assert.That(networkAnimator.TryGetTransitionInfo(0, stateB.nameHash, stateC.nameHash, out var infoFromC), Is.True);
            Assert.That(infoFromC.OriginatingState, Is.EqualTo(stateC.nameHash));
            Assert.That(infoFromC.TransitionDuration, Is.EqualTo(transitionFromC.duration));
            Assert.That(networkAnimator.TryGetTransitionInfo(0, stateB.nameHash, stateB.nameHash, out _), Is.False);
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
#endif
