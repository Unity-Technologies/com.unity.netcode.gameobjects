using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class Raygun : NetworkBehaviour
{
    public float m_Range = 10;

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
        var forward = transform.forward * m_Range;

        m_LineRenderer.positionCount = 2;
        m_LineRenderer.SetPosition(0, transform.position);
        m_LineRenderer.SetPosition(1, transform.position + forward);

        if (IsLocalPlayer && Input.GetKeyDown(KeyCode.P))
        {
            m_CurrentTarget = FindTarget();
            transform.position = m_CurrentTarget.transform.position + forward;
        }
    }

    private GameObject FindTarget()
    {
        var targetsObjs = GameObject.FindGameObjectsWithTag("Target");
        var list = new List<GameObject>(targetsObjs);
        list.Remove(gameObject);

        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }
}