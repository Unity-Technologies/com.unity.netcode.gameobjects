using UnityEngine;

public class MovementRandomizer : MonoBehaviour
{
    public Vector3 targetLocation;
    public float speed = 1;

    private Rigidbody m_Rigidbody;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        var temp = transform.position;
        temp.y = 0.5f;
        transform.position = temp;
        targetLocation = GetRandomLocation();
    }

    private void FixedUpdate()
    {
        float distance = Vector3.Distance(transform.position, targetLocation);
        if (distance > 0.5f)
        {
            var stepPosition = Vector3.MoveTowards(transform.position, targetLocation, speed * Time.fixedDeltaTime);
            m_Rigidbody.MovePosition(stepPosition);
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