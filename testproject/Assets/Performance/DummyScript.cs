using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyScript : MonoBehaviour
{
    private void Update()
    {
        transform.position += Vector3.up * Time.deltaTime;
    }
}
