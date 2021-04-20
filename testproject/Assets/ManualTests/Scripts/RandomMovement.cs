using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using UnityEditor.Experimental;

public class RandomMovement : MonoBehaviour, IPlayerMovement
{


    [SerializeField]
    private Vector3 direction;
    private Rigidbody rb;



    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        ChangeDirection(true, true);
    }

    public void Move(int speed)
    {
        rb.MovePosition(transform.position + direction * (speed * Time.fixedDeltaTime));
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor") || collision.gameObject.CompareTag("Bullet"))
        {
            return;
        }
        Vector3 collisionPoint = collision.collider.ClosestPoint(transform.position);
        bool moveRight = collisionPoint.x < transform.position.x;
        bool moveDown = collisionPoint.z > transform.position.z;
        ChangeDirection(moveRight, moveDown);
    }

    private void ChangeDirection(bool moveRight, bool moveDown)
    {
        float ang = Random.Range(0, 2 * Mathf.PI);

        direction.x = Mathf.Cos(ang);
        direction.y = 0.0f;
        ang = Random.Range(0, 2 * Mathf.PI);
        direction.z = Mathf.Sin(ang);
        direction.Normalize();
    }
}
