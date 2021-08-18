using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class RandomMovement : NetworkBehaviour, IPlayerMovement
    {
        private Vector3 m_Direction;
        private Rigidbody m_Rigidbody;


        public override void OnNetworkSpawn()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (NetworkObject != null && m_Rigidbody != null)
            {
                //m_Rigidbody.isKinematic = !NetworkObject.NetworkManager.IsServer;
                if (NetworkObject.IsOwner)
                {
                    ChangeDirection(true, true);
                }
            }
        }

        private void MovePlayer(Vector3 moveTowards)
        {
            m_MoveTowardsPosition = moveTowards;
        }

        [ServerRpc(RequireOwnership = false)]
        private void MovePlayerServerRpc(Vector3 moveTowards)
        {
            m_MoveTowardsPosition = moveTowards;
        }

        private Vector3 m_MoveTowardsPosition;

        public void Move(int speed)
        {
            var nextMoveToPositioin = (m_Direction * speed);
            if (IsServer && IsOwner)
            {
                MovePlayer(nextMoveToPositioin);
            }
            else if (!IsServer && IsOwner)
            {
                MovePlayerServerRpc(nextMoveToPositioin);
            }
        }

        private void FixedUpdate()
        {
            if (IsServer && NetworkObject && NetworkObject.NetworkManager && NetworkObject.NetworkManager.IsListening)
            {
                if (m_Rigidbody == null)
                {
                    m_Rigidbody = GetComponent<Rigidbody>();
                }
                if (m_Rigidbody != null)
                {
                    m_Rigidbody.MovePosition(transform.position + (m_MoveTowardsPosition * Time.fixedDeltaTime));
                }
            }
        }

        [ClientRpc]
        private void ChangeDirectionClientRpc(Vector3 direction)
        {
            m_Direction = direction;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (IsServer)
            {
                if (collision.gameObject.CompareTag("Floor") || collision.gameObject.CompareTag("GenericObject"))
                {
                    return;
                }
                Vector3 collisionPoint = collision.collider.ClosestPoint(transform.position);
                bool moveRight = collisionPoint.x < transform.position.x;
                bool moveDown = collisionPoint.z > transform.position.z;

                ChangeDirection(moveRight, moveDown);

                if (!IsOwner)
                {
                    m_MoveTowardsPosition = m_Direction * m_MoveTowardsPosition.magnitude;
                    ChangeDirectionClientRpc(m_Direction);
                }
            }
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
}
