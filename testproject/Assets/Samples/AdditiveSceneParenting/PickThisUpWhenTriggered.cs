using UnityEngine;
using Unity.Netcode;

public class PickThisUpWhenTriggered : NetworkBehaviour
{

    private Collider m_LocalCollider;
    private GenericMover m_AIMoverScript;

    // Start is called before the first frame update
    private void Start()
    {
        m_LocalCollider = GetComponent<Collider>();
        m_AIMoverScript = GetComponent<GenericMover>();
    }
    private bool m_DelayEnableCollision;
    private float m_DelayCollision;

    public void DropObject()
    {
        transform.position = transform.parent.transform.position + Vector3.right + Vector3.forward;
        m_DelayEnableCollision = true;
        m_DelayCollision = Time.realtimeSinceStartup + 0.5f;
        m_AIMoverScript.SetHasParent(false);
        NetworkObject.transform.parent = null;
        m_LocalCollider.enabled = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Floor") || collision.collider.CompareTag("Boundary"))
        {
            return;
        }

        if (NetworkManager != null && NetworkManager.IsListening && IsServer)
        {
            if (m_DelayEnableCollision && m_DelayCollision > Time.realtimeSinceStartup)
            {
                return;
            }
            else if (m_DelayEnableCollision)
            {
                m_DelayEnableCollision = false;
            }

            if (transform.parent == null)
            {
                var networkObjectOther = collision.collider.gameObject.GetComponent<NetworkObject>();

                if (networkObjectOther != null && networkObjectOther.IsPlayerObject && networkObjectOther.IsOwner)
                {
                    if (NetworkObject.TrySetParent(collision.collider.gameObject))
                    {
                        m_LocalCollider.enabled = false;
                        m_AIMoverScript.SetHasParent(true);
                        transform.position = networkObjectOther.transform.position + Vector3.up;
                    }
                }
            }
        }
    }
}
