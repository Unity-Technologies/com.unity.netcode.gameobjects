using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementRandomizer : MonoBehaviour
{
    public Vector3 targetLocation;
    public float speed = 1;
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Vector3 temp = transform.position;
        temp.y = 0.5f;
        transform.position = temp;
        targetLocation = GetRandomLocation();
    }

    void FixedUpdate()
    {
        float distance = Vector3.Distance(transform.position, targetLocation);

        if (distance > 0.5f)
        {
            Vector3 stepPosition = Vector3.MoveTowards(transform.position, targetLocation, speed);
            rb.MovePosition(stepPosition);
        }
        else
        {
            targetLocation = GetRandomLocation();
        }
    }

    private Vector3 GetRandomLocation()
    {
        return new Vector3(Random.Range(-15.0f, 15.0f), transform.position.y, Random.Range(-15.0f, 15.0f));
    }
}
