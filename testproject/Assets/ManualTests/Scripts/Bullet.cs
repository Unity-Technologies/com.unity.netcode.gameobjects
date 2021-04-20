using System.Collections;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField]
    private bool m_MoveRandomly = true;
    Rigidbody bulletRigid;
    BoxCollider bulletCollider;
    public NetworkVariable<int> m_Id;

    private void Awake()
    {
        m_Id = new NetworkVariable<int>();
    }

    private void Start()
    {
        bulletRigid = GetComponent<Rigidbody>();

        bulletCollider = GetComponent<BoxCollider>();
    }

    Vector3 Direction;
    float Velocity;
    public void SetDirectionAndVelocity(Vector3 direction, float velocity)
    {
        Direction = direction;
        Direction.Normalize();
        Direction.y = 0;
        Velocity = velocity;
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            bulletRigid.MovePosition(transform.position + Direction * (Velocity * Time.fixedDeltaTime));

            if (m_MoveRandomly && Random.Range(0.0f, 1.0f) < 0.01f)
            {
                var dir = Random.insideUnitCircle;
                Direction.x = dir.x;
                Direction.z = dir.y;
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

            if(NetworkObject && !NetworkObject.DestroyWithScene)
            {
                //NetworkObject.DestroyWithScene = true;
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
            //if(collision.gameObject.CompareTag("Player"))
            //{

            //    float currentVelocity = bulletRigid.velocity.magnitude;
            //    Vector3 collisionPointA = collision.collider.ClosestPoint(transform.position);
            //    Vector3 collisionPointB = bulletCollider.ClosestPoint(collision.gameObject.transform.position);
            //    Vector3 awayFrom = collisionPointA - collisionPointB;
            //    awayFrom.Normalize();
            //    Direction = awayFrom;

            //}
            //else
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

    public void SetId(int id)
    {
        m_Id.Value = id;
    }

}
