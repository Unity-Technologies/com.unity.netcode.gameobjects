using UnityEngine;
using Random = UnityEngine.Random;

public class RandomMovement : MonoBehaviour, IPlayerMovement
{    
    private Vector3 m_Direction;
    private Rigidbody m_Rigidbody;

    public void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        ChangeDirection(true, true);
    }

    public void Move(int speed)
    {
        m_Rigidbody.MovePosition(transform.position + m_Direction * (speed * Time.fixedDeltaTime));
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor") || collision.gameObject.CompareTag("GenericObject"))
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

        m_Direction.x = Mathf.Cos(ang);
        m_Direction.y = 0.0f;
        ang = Random.Range(0, 2 * Mathf.PI);
        m_Direction.z = Mathf.Sin(ang);
        m_Direction.Normalize();
    }
}
