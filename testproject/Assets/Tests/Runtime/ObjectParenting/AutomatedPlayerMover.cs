using UnityEngine;
using Unity.Netcode.Components;

namespace TestProject.RuntimeTests
{
    public class AutomatedPlayerMover : NetworkTransform
    {
        public static bool StopMovement;
        private float m_Speed = 15.0f;
        private float m_RotSpeed = 15.0f;

        private GameObject m_Destination;
        private Vector3 m_TargetPosition;



        /// <summary>
        /// Make this PlayerMovement-NetworkTransform component
        /// Owner Authoritative
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

        private void UpdateDestination()
        {
            if (Navigationpoints.Instance != null)
            {
                var targetNavPointIndex = Random.Range(0, Navigationpoints.Instance.NavPoints.Count - 1);
                m_Destination = Navigationpoints.Instance.NavPoints[targetNavPointIndex];

                m_TargetPosition = m_Destination.transform.position;
                m_TargetPosition.y = transform.position.y;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                UpdateDestination();
                var temp = transform.position;
                temp.y = 0.5f;
                transform.position = temp;
            }
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        private void LateUpdate()
        {
            if (!IsSpawned || !IsOwner || m_Destination == null || StopMovement)
            {
                return;
            }
            m_TargetPosition.y = transform.position.y;
            var distance = Vector3.Distance(transform.position, m_TargetPosition);
            if (distance < 0.25f)
            {
                var currentDestination = m_Destination;
                while (m_Destination == currentDestination)
                {
                    UpdateDestination();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsOwner || m_Destination == null || StopMovement)
            {
                return;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, m_Destination.transform.position, m_Speed * Time.fixedDeltaTime);
                var normalizedDirection = (m_TargetPosition - transform.position).normalized;
                if (normalizedDirection.magnitude != 0.0f)
                {
                    var lookRotation = Quaternion.LookRotation(normalizedDirection, transform.up).eulerAngles;
                    var currentEuler = transform.eulerAngles;
                    currentEuler.y = Mathf.LerpAngle(currentEuler.y, lookRotation.y, Time.deltaTime * m_RotSpeed);
                    transform.eulerAngles = currentEuler;
                }
            }
        }
    }
}
