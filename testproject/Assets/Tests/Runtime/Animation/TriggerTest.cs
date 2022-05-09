using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TriggerTest : StateMachineBehaviour
{
    public static List<ulong> ClientsThatTriggered = new List<ulong>();

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var clientId = animator.GetComponent<NetworkObject>().NetworkManager.LocalClientId;
        if (!ClientsThatTriggered.Contains(clientId))
        {
            ClientsThatTriggered.Add(clientId);
        }
        else
        {
            Debug.LogWarning($"Client-{clientId} already triggered!");
        }
    }

    // NSS: Leaving this here for potential future tests

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
