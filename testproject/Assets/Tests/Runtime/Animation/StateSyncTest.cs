using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StateSyncTest : StateMachineBehaviour
{
    public static Dictionary<ulong, Dictionary<int, AnimatorStateInfo>> StatesEntered = new Dictionary<ulong, Dictionary<int, AnimatorStateInfo>>();
    public static bool IsVerboseDebug;
    public static void ResetTest()
    {
        StatesEntered.Clear();
        IsVerboseDebug = false;
    }

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    // This provides us with the AnimatorStateInfo when a SendAnimStateClientRpc is received by the client
    // (the server and the connected client are updated elsewhere)
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var networkObject = animator.GetComponent<NetworkObject>();
        if (networkObject == null || networkObject.NetworkManager == null || !networkObject.IsSpawned)
        {
            return;
        }

        var clientId = networkObject.NetworkManager.LocalClientId;
        if (!StatesEntered.ContainsKey(clientId))
        {
            StatesEntered.Add(clientId, new Dictionary<int, AnimatorStateInfo>());
        }

        if (!StatesEntered[clientId].ContainsKey(layerIndex))
        {
            StatesEntered[clientId].Add(layerIndex, stateInfo);
        }
        else
        {
            StatesEntered[clientId][layerIndex] = stateInfo;
        }

        if (IsVerboseDebug)
        {
            Debug.Log($"[{layerIndex}][STATE-ENTER][{clientId}] {networkObject.NetworkManager.name} entered state {stateInfo.normalizedTime}!");
        }
    }
}
