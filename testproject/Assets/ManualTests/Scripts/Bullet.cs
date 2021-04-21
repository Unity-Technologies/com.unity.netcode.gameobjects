using MLAPI;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField]
    [Tooltip("This will make the spawned objects move around randomly.  !Caution! You can generate a lot of objects this way!")]
    private bool m_MoveRandomly = true;

    private Rigidbody m_BulletRigid;
    private Vector3 m_Direction;
    private float m_Velocity;

    private void Start()
    {
        m_BulletRigid = GetComponent<Rigidbody>();
    }


    public void SetDirectionAndVelocity(Vector3 direction, float velocity)
    {
        m_Direction = direction;
        m_Direction.Normalize();
        m_Direction.y = 0;
        m_Velocity = velocity;
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            m_BulletRigid.MovePosition(transform.position + m_Direction * (m_Velocity * Time.fixedDeltaTime));

            if (m_MoveRandomly && Random.Range(0.0f, 1.0f) < 0.01f)
            {
                var dir = Random.insideUnitCircle;
                m_Direction.x = dir.x;
                m_Direction.z = dir.y;
            }
        }
        else
        {
            if(NetworkObject != null && !NetworkObject.isActiveAndEnabled )
            {
                Debug.LogWarning("Bullet id " + NetworkObject.NetworkObjectId.ToString() + " is not active and enabled but game object is still active!");
            }

            if(NetworkObject != null && !NetworkObject.IsSpawned )
            {
                Debug.LogWarning("Bullet id " + NetworkObject.NetworkObjectId.ToString() + " is not spawned but still active and enabled");
            }
        }
    }

    private void OnDestroy()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsOwner)
        {
            if (other.CompareTag("Bullet") || other.CompareTag("Floor"))
            {
                return;
            }
            else
            {
                NetworkObject.Despawn();
                NetworkObject.gameObject.SetActive(false);
            }
        }
    }
}
