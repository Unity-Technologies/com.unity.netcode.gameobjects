using MLAPI;
using UnityEngine;

namespace TestProject.ManualTests
{
    /// <summary>
    /// A general object that can be used for testing purposes
    /// </summary>
    public class GenericNetworkObjectBehaviour : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("This will make the spawned objects move around randomly.  !Caution! You can generate a lot of objects this way!")]
        private bool m_MoveRandomly = true;

        private Rigidbody m_RigidBody;
        private Vector3 m_Direction;
        private float m_Velocity;

        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Sets the object's direction and velocity
        /// </summary>
        /// <param name="direction">vector3 direction</param>
        /// <param name="velocity">float velocity</param>
        public void SetDirectionAndVelocity(Vector3 direction, float velocity)
        {
            m_Direction = direction;
            m_Direction.Normalize();
            m_Direction.y = 0;
            m_Velocity = velocity;
        }

        /// <summary>
        /// Handles moving the object based on its current direction and velocity
        /// </summary>
        private void FixedUpdate()
        {
            if (NetworkManager != null && NetworkManager.IsListening)
            {
                if (IsOwner)
                {
                    m_RigidBody.MovePosition(transform.position + m_Direction * (m_Velocity * Time.fixedDeltaTime));

                    if (m_MoveRandomly && Random.Range(0.0f, 1.0f) < 0.01f)
                    {
                        var dir = Random.insideUnitCircle;
                        m_Direction.x = dir.x;
                        m_Direction.z = dir.y;
                    }
                }
                else
                {
                    if (NetworkObject != null && !NetworkObject.isActiveAndEnabled)
                    {
                        Debug.LogWarning($"{nameof(GenericNetworkObjectBehaviour)} id {NetworkObject.NetworkObjectId} is not active and enabled but game object is still active!");
                    }

                    if (NetworkObject != null && !NetworkObject.IsSpawned)
                    {
                        Debug.LogWarning($"{nameof(GenericNetworkObjectBehaviour)} id {NetworkObject.NetworkObjectId} is not spawned but still active and enabled");
                    }
                }
            }
        }


        private void OnTriggerEnter(Collider other)
        {
            if (IsOwner)
            {
                if (other.CompareTag("GenericObject") || other.CompareTag("Floor"))
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
}
