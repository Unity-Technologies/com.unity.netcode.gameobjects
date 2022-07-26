using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class StateSyncTest : StateMachineBehaviour
{
    public static Dictionary<ulong, List<AnimatorStateInfo>> StatesEntered = new Dictionary<ulong, List<AnimatorStateInfo>>();
    public static bool IsVerboseDebug;
    public static void ResetTest()
    {
        StatesEntered.Clear();
        IsVerboseDebug = false;
    }

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    // This provides us with the exact AnimatorStateInfo applied when a SendAnimStateClientRpc is received
    // and applied.
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var networkObject = animator.GetComponent<NetworkObject>();
        if (networkObject == null || networkObject.NetworkManager == null)
        {
            return;
        }

        var clientId = networkObject.NetworkManager.LocalClientId;
        if (!StatesEntered.ContainsKey(clientId))
        {
            StatesEntered.Add(clientId, new List<AnimatorStateInfo>());
        }

        if (IsVerboseDebug)
        {
            Debug.Log($"[{layerIndex}][STATE-ENTER][{clientId}] {networkObject.NetworkManager.name} state entered!");
        }
    }
}
