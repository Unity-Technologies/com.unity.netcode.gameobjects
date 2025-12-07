using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerCheerInput : NetworkBehaviour
{
    private NetworkAnimator m_NetworkAnimator;

    public enum AnimationTriggers
    {
        Idle,
        Cheer,
    }

    private void Awake()
    {
        m_NetworkAnimator = GetComponent<NetworkAnimator>();
    }

    /// <summary>
    /// Sets a trigger for the one of two possible triggers
    /// </summary>
    /// <param name="trigger">the trigger to set</param>
    public void SetTrigger(AnimationTriggers trigger)
    {
        // Since triggers can be set to a perminant on state by setting the same trigger more than once before the trigger has
        // completed its trigger transition, we need to reset the opposite trigger from the one we want to set to assure we
        // don't immediately transition back to the opposite trigger.
        // https://docs.unity3d.com/ScriptReference/Animator.SetTrigger.html
        var oherTriggerValue = 1 + (int)trigger;
        var otherTrigger = (AnimationTriggers)(oherTriggerValue % System.Enum.GetValues(typeof(AnimationTriggers)).Length);
        m_NetworkAnimator.ResetTrigger(otherTrigger.ToString());
        m_NetworkAnimator.SetTrigger(trigger.ToString());
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetTrigger(AnimationTriggers.Idle);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetTrigger(AnimationTriggers.Cheer);
            }
        }
    }
}
