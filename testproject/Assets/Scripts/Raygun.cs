using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEditor;

public class Raygun : NetworkedBehaviour
{
    public float range = 15;

    private GameObject m_CurrentTarget;
    private LineRenderer lineRenderer;

    private void Start()
    {
    }

    void Awake()
    {
        UnityEngine.Color c1 = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 0.1f);
        UnityEngine.Color c2 = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 0.1f);

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.widthMultiplier = 0.1f;
//        lineRenderer.colorGradient = c1;
//        lineRenderer.endColor = c2;
    }

    private void Update()
    {
        var forward = transform.forward * range;

        lineRenderer.SetVertexCount(2);
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + transform.forward * 10.0f);

        if (IsLocalPlayer && Input.GetKeyDown(KeyCode.P))
        {
            m_CurrentTarget = FindTarget();
            transform.position = m_CurrentTarget.transform.position + m_CurrentTarget.transform.forward * 10.0f;
        }

    }

    private GameObject FindTarget()
    {
        var targetsObjs = GameObject.FindGameObjectsWithTag("Target");
        var list = new List<GameObject>(targetsObjs);
        list.Remove(gameObject);

        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }

    private void ShootTarget()
    {
        // you can't do that here
        // transform.LookAt should only change the localPlayer
        // this breaks the position synchronization when done both sides
/*        if (ReferenceEquals(m_CurrentTarget, null)) return;

        transform.LookAt(m_CurrentTarget.transform);
        var forward = transform.TransformDirection(Vector3.forward) * range;
        if (Physics.Raycast(transform.position, forward, out var hit, range))
        {
            hit.transform.GetComponent<Renderer>().material.color = Color.red;
        }
        */
    }
}
