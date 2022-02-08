using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

namespace TestProject.ManualTests
{
    public class ListSerializerInt : INetworkSerializable
    {
        public List<int> Values { get; set; }

        public ListSerializerInt()
        {
            Values = new List<int>();
        }

        public ListSerializerInt(List<int> values)
        {
            Values = values;
        }

        private void Read(FastBufferReader reader)
        {
            reader.ReadValue(out int[] ts);
            Values = ts.ToList();
        }

        private void Write(FastBufferWriter writer)
        {
            writer.WriteValue(Values.ToArray());
        }

        public void NetworkSerialize<T1>(BufferSerializer<T1> serializer) where T1 : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                Write(serializer.GetFastBufferWriter());
            }
            else if (serializer.IsReader)
            {
                Read(serializer.GetFastBufferReader());
            }
        }
    }

    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class RandomMovement : NetworkBehaviour, IPlayerMovement
    {
        private Vector3 m_Direction;
        private Rigidbody m_Rigidbody;

        [ServerRpc(RequireOwnership = false)]
        internal void SendOrdersToServerRpc(ListSerializerInt orders, ServerRpcParams serverRpcParams = default)
        {
            Debug.Log("Received Server RPC");
        }


        public override void OnNetworkSpawn()
        {
            Debug.Log($"IsServer is {IsServer}");

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
            if (IsLocalPlayer)
            {
                if (Input.GetKeyDown("space"))
                {
                    Debug.Log("Pressed SPACE");

                    ListSerializerInt foo = new ListSerializerInt();
                    foo.Values.Add(42);
                    foo.Values.Add(56);

                    SendOrdersToServerRpc(foo);
                }
            }

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

        private static void ChangeDirectionClientRpcInHandler(NetworkBehaviour target, FastBufferReader reader)
        {
            NetworkManager networkManager = target.NetworkManager;
            if (networkManager != null && networkManager.IsListening)
            {
                reader.ReadValueSafe(out Vector3 value);
                ((RandomMovement)target).ChangeDirectionClientRpc(value);
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
