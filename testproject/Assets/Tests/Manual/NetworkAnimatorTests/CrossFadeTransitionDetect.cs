using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This StateMachineBehaviour is used to detect an <see cref="Animator.CrossFade"/> initiated transition
/// for integration test purposes.
/// </summary>
public class CrossFadeTransitionDetect : StateMachineBehaviour
{
    public static Dictionary<ulong, Dictionary<int, AnimatorStateInfo>> StatesEntered = new Dictionary<ulong, Dictionary<int, AnimatorStateInfo>>();
    public static bool IsVerboseDebug;

    public static string CurrentTargetStateName { get; private set; }
    public static int CurrentTargetStateHash { get; private set; }

    public static List<ulong> ClientIds = new List<ulong>();

    public static void ResetTest()
    {
        ClientIds.Clear();
        StatesEntered.Clear();
        IsVerboseDebug = false;
    }

    private void Log(string logMessage)
    {
        if (!IsVerboseDebug)
        {
            return;
        }
        Debug.Log($"[CrossFadeDetect] {logMessage}");
    }

    public static bool AllClientsTransitioned()
    {
        foreach (var clientId in ClientIds)
        {
            if (!StatesEntered.ContainsKey(clientId))
            {
                return false;
            }

            if (!StatesEntered[clientId].ContainsKey(CurrentTargetStateHash))
            {
                return false;
            }
        }
        return true;
    }

    public static void SetTargetAnimationState(string animationStateName)
    {
        CurrentTargetStateName = animationStateName;
        CurrentTargetStateHash = Animator.StringToHash(animationStateName);
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.shortNameHash != CurrentTargetStateHash)
        {
            Log($"[Ignoring State][Layer-{layerIndex}] Incoming: ({stateInfo.fullPathHash}) | Targeting: ({CurrentTargetStateHash})");
            return;
        }

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

        if (!StatesEntered[clientId].ContainsKey(stateInfo.shortNameHash))
        {
            StatesEntered[clientId].Add(stateInfo.shortNameHash, stateInfo);
        }
        else
        {
            StatesEntered[clientId][stateInfo.shortNameHash] = stateInfo;
        }

        Log($"[{layerIndex}][STATE-ENTER][{clientId}] {networkObject.NetworkManager.name} entered state {stateInfo.shortNameHash}!");
    }
}
