using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TriggerTest : StateMachineBehaviour
{
    public static List<ulong> ClientsThatTriggered = new List<ulong>();
    public static bool IsVerboseDebug;
    public static void ResetTest()
    {
        ClientsThatTriggered.Clear();
        ClientsThatResetTrigger.Clear();
        s_EnteredCount = 0;
    }

    internal static int Iteration = 0;
    private static int s_EnteredCount = 0;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var networkObject = animator.GetComponent<NetworkObject>();
        if (networkObject == null || networkObject.NetworkManager == null)
        {
            return;
        }
        var clientId = networkObject.NetworkManager.LocalClientId;
        if (IsVerboseDebug)
        {
            Debug.Log($"[{Iteration}][{s_EnteredCount}][STATE-ENTER][{clientId}] {networkObject.NetworkManager.name} state entered!");
            s_EnteredCount++;
        }
        if (!ClientsThatTriggered.Contains(clientId))
        {
            ClientsThatTriggered.Add(clientId);
        }
        else if (IsVerboseDebug)
        {
            Debug.LogWarning($"[{Iteration}][{s_EnteredCount}][STATE-EXISTS][{clientId}] {networkObject.NetworkManager.name} already entered!");
        }
    }

    // NSS: Leaving this here for potential future tests

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    var networkObject = animator.GetComponent<NetworkObject>();
    //    var clientId = networkObject.NetworkManager.LocalClientId;
    //    if (!ClientsThatTriggered.Contains(clientId))
    //    {
    //        if (IsVerboseDebug)
    //        {
    //            Debug.Log($"[{Iteration}][{s_EnteredCount}][STATE-UPDATE][{clientId}] {networkObject.NetworkManager.name} state entered!");
    //            s_EnteredCount++;
    //        }

    //        ClientsThatTriggered.Add(clientId);
    //    }

    //    //var networkObject = animator.GetComponent<NetworkObject>();
    //    //var clientId = networkObject.NetworkManager.LocalClientId;
    //    //if (!ClientsThatTriggered.Contains(clientId))
    //    //{
    //    //    Debug.Log($"{networkObject.NetworkManager} was added in update but not OnStateEnter!");
    //    //    ClientsThatTriggered.Add(clientId);
    //    //}
    //}


    public static List<ulong> ClientsThatResetTrigger = new List<ulong>();
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var networkObject = animator.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            var clientId = networkObject.NetworkManager.LocalClientId;
            Debug.Log($"Client-{clientId} state exited (trigger reset)!");
            if (ClientsThatTriggered.Contains(clientId) && networkObject.OwnerClientId == clientId)
            {
                ClientsThatTriggered.Remove(clientId);
            }


            if (!ClientsThatResetTrigger.Contains(clientId) && networkObject.OwnerClientId == clientId)
            {
                ClientsThatResetTrigger.Add(clientId);
            }
        }
    }

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
