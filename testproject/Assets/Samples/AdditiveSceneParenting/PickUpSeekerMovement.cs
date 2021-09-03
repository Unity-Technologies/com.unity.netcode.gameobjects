using UnityEngine;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class PickUpSeekerMovement : RandomMovement
    {
        private PickThisUpWhenTriggered m_PickThisUpWhenTriggered;

        private bool m_HasPickedUpTarget;
        private float m_LastSearchForPickup;

        private void Update()
        {
            if (IsServer && IsOwner)
            {
                if (!m_HasPickedUpTarget && m_LastSearchForPickup < Time.realtimeSinceStartup)
                {
                    if (m_PickThisUpWhenTriggered == null)
                    {
                        var foundTargets = FindObjectsOfType<PickThisUpWhenTriggered>();
                        if (foundTargets != null && foundTargets.Length > 0)
                        {
                            m_PickThisUpWhenTriggered = foundTargets[0];
                            m_Direction = Vector3.Normalize(m_PickThisUpWhenTriggered.gameObject.transform.position - transform.position);
                            m_LastSearchForPickup = Time.realtimeSinceStartup + 0.5f;
                        }
                        else
                        {
                            m_LastSearchForPickup = Time.realtimeSinceStartup + 1.0f;
                        }
                    }
                    else
                    {

                        m_Direction = Vector3.Normalize(m_PickThisUpWhenTriggered.gameObject.transform.position - transform.position);
                    }
                }
                else if (m_HasPickedUpTarget)
                {
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        m_PickThisUpWhenTriggered.DropObject();
                        m_Direction = Vector3.Normalize(transform.position - m_PickThisUpWhenTriggered.gameObject.transform.position);
                        m_PickThisUpWhenTriggered = null;
                        m_LastSearchForPickup = Time.realtimeSinceStartup + 5.0f;
                        m_HasPickedUpTarget = false;
                    }
                }
            }
        }

        private void OnTransformChildrenChanged()
        {
            var childObject = transform.GetComponentInChildren<PickThisUpWhenTriggered>();
            bool foundTargetChild = childObject != null;
            if (!m_HasPickedUpTarget && foundTargetChild)
            {
                m_HasPickedUpTarget = true;
                ChangeDirection(true, false);
                m_PickThisUpWhenTriggered = childObject;
            }
        }

        protected override void ChangeDirection(bool moveRight, bool moveDown)
        {
            if(m_PickThisUpWhenTriggered == null || m_HasPickedUpTarget)
            {
                base.ChangeDirection(moveRight, moveDown);
            }
            else if (m_PickThisUpWhenTriggered != null &&  !m_HasPickedUpTarget)
            {
                m_Direction = Vector3.Normalize(m_PickThisUpWhenTriggered.gameObject.transform.position - transform.position);
            }
        }


    }
}
