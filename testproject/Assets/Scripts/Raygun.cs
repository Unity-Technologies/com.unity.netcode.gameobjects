using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Raygun : NetworkBehaviour
{
    public float Range = 10;

    private GameObject m_CurrentTarget;
    private LineRenderer m_LineRenderer;

    private void Awake()
    {
        m_LineRenderer = GetComponent<LineRenderer>();
        m_LineRenderer.useWorldSpace = true;
        m_LineRenderer.alignment = LineAlignment.View;
        m_LineRenderer.widthMultiplier = 0.1f;
    }

    private void FixedUpdate()
    {
        var forward = transform.forward * Range;

        m_LineRenderer.positionCount = 2;
        m_LineRenderer.SetPosition(0, transform.position);
        m_LineRenderer.SetPosition(1, transform.position + forward);

        if (IsLocalPlayer && Input.GetKeyDown(KeyCode.P))
        {
            m_CurrentTarget = FindTarget();
            if (m_CurrentTarget != null)
            {
                transform.position = m_CurrentTarget.transform.position + forward;
            }
        }
    }

    private GameObject FindTarget()
    {
        var targetsObjs = GameObject.FindGameObjectsWithTag("Target");
        var targetList = new List<GameObject>(targetsObjs);
        targetList.Remove(gameObject); // remove self

        return targetList.Count == 0 ? null : targetList[Random.Range(0, targetList.Count)];
    }
}
