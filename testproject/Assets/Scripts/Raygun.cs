using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEditor;

public class Raygun : NetworkedBehaviour
{
    public float range = 10;

    private GameObject m_CurrentTarget;
    private LineRenderer lineRenderer;

    private void Start()
    {
    }

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.widthMultiplier = 0.1f;
    }

    private void FixedUpdate()
    {
        var forward = transform.forward * range;

        lineRenderer.SetVertexCount(2);
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + forward);

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
