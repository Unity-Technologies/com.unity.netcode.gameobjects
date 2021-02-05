using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Raygun : MonoBehaviour
{
    public float range = 15;

    private GameObject m_CurrentTarget;

    private void Start()
    {
        StartCoroutine(Attack());
    }

    private void Update()
    {
        var forward = transform.forward * range;
        Debug.DrawRay(transform.position, forward, Color.yellow);
    }

    private IEnumerator Attack()
    {
        m_CurrentTarget = FindTarget();

        ShootTarget();

        yield return new WaitForSeconds(10);

        StartCoroutine(Attack());
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
        if (ReferenceEquals(m_CurrentTarget, null)) return;

        transform.LookAt(m_CurrentTarget.transform);
        var forward = transform.TransformDirection(Vector3.forward) * range;
        if (Physics.Raycast(transform.position, forward, out var hit, range))
        {
            hit.transform.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}