using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Raygun : MonoBehaviour
{
  public float range = 15;

  private GameObject currentTarget;
  private Rigidbody rb;

  void Start()
  {
    StartCoroutine(Attack());
    rb = GetComponent<Rigidbody>();
  }
  void Update()
  {
    Vector3 forward = transform.forward * range;
    Debug.DrawRay(transform.position, forward, Color.yellow);
  }

  IEnumerator Attack()
  {
    currentTarget = FindTarget();

    ShootTarget();

    yield return new WaitForSeconds(10);

    StartCoroutine(Attack());
  }

  GameObject FindTarget()
  {
    GameObject[] targets = GameObject.FindGameObjectsWithTag("Target");

    List<GameObject> list = new List<GameObject>(targets);
    list.Remove(gameObject);

    if (list.Count == 0)
    {
      return null;
    }

    return list[Random.Range(0, list.Count)];
  }

  void ShootTarget()
  {
    if (currentTarget == null)
    {
      return;
    }

    transform.LookAt(currentTarget.transform);
    Vector3 forward = transform.TransformDirection(Vector3.forward) * range;
    RaycastHit hit;
    if (Physics.Raycast(transform.position, forward, out hit, range))
    {
      Renderer render = hit.transform.GetComponent<Renderer>();
      render.material.color = Color.red;
    }
  }
}
