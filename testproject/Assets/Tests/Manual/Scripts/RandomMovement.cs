using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    class Foo
    {
        public int I;
    }

    static class FastBufferWriterExtensions
    {
        public static void WriteValueSafe(this ref FastBufferWriter writer, in Foo value)
        {
            writer.WriteValueSafe(value.I);
        }
        
        
        public static void ReadValueSafe(this ref FastBufferReader reader, out Foo value)
        {
            value = new Foo();
            reader.ReadValueSafe(out value.I);
        }

    }
    
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
                if (NetworkObject.IsOwner)
                {
                    ChangeDirection(true, true);
                }
            }
        }

        /// <summary>
        /// Notify the server of any client side change in direction or speed
        /// </summary>
        /// <param name="moveTowards"></param>
        [ServerRpc(RequireOwnership = false)]
        private void MovePlayerServerRpc(Vector3 moveTowards)
        {
            m_MoveTowardsPosition = moveTowards;
        }

        private Vector3 m_MoveTowardsPosition;

        public void Move(int speed)
        {
            // Server sets this locally
            if (IsServer && IsOwner)
            {
                m_MoveTowardsPosition = (m_Direction * speed);
            }
            else if (!IsServer && IsOwner)
            {
                // Client must sent Rpc
                MovePlayerServerRpc(m_Direction * speed * 1.05f);
            }
            else if (IsServer && !IsOwner)
            {
                m_MoveTowardsPosition = Vector3.Lerp(m_MoveTowardsPosition, Vector3.zero, 0.01f);
            }
        }


        // We just apply our current direction with magnitude to our current position during fixed update
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

        /// <summary>
        /// Handles server notification to client that we need to change direction
        /// </summary>
        /// <param name="direction"></param>
        [ClientRpc]
        private void ChangeDirectionClientRpc(Vector3 direction)
        {
            m_Direction = direction;
        }
        private void ChangeDirectionClientRpcManual(Vector3 direction)
        {
            //IL_00d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d2: Unknown result type (might be due to invalid IL or missing references)
            NetworkManager networkManager = base.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                FastBufferWriter writer = new FastBufferWriter(1300, (Allocator)2, 1300);
                try
                {
                    writer.WriteValueSafe<Vector3>(in direction);
                    ClientRpcParams sendParams = default(ClientRpcParams);
                    SendClientRpc(ref writer, 4099941514u, sendParams, RpcDelivery.Reliable);
                }
                finally
                {
                    writer.Dispose();
                }
            }
            if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
            {
                m_Direction = direction;
            }
        }

        [ClientRpc]
        private void ChangeDirectionClientRpc(Foo testFoo)
        {
        }
        private void ChangeDirectionClientRpcManual(NetworkBehaviour testFoo)
        {
            //IL_00d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d2: Unknown result type (might be due to invalid IL or missing references)
            NetworkManager networkManager = base.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                FastBufferWriter writer = new FastBufferWriter(1300, (Allocator)2, 1300);
                try
                {
                    writer.WriteValueSafe(testFoo);
                    ClientRpcParams sendParams = default(ClientRpcParams);
                    SendClientRpc(ref writer, 4099941514u, sendParams, RpcDelivery.Reliable);
                }
                finally
                {
                    writer.Dispose();
                }
            }
            if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
            {
            }
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

                // If we are not the owner then we need to notify the client that their direction
                // must change
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
