using UnityEngine;
using Unity.Netcode.Components;

namespace TestProject.ManualTests
{
    public class BandwidthMover : NetworkTransform
    {
        public Vector3 Direction;

        [Range(0.01f,40.0f)]
        public float MoveSpeed = 5.0f;

        [Range(0.01f,40.0f)]
        public float RotationSpeed = 2.0f;

        private Vector3 m_Rotation;
        private Vector3 m_RotateBy;

        private void Start()
        {
            Direction.Normalize();

            m_Rotation = transform.eulerAngles;
            m_RotateBy = Vector3.one * RotationSpeed;
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, transform.position + (Direction * MoveSpeed), Time.fixedDeltaTime);
            var targetRotation = m_Rotation + m_RotateBy;
            var maxDegreeDelta = Time.fixedDeltaTime * RotationSpeed;
            for (int i = 0; i < 3; i++)
            {
                m_Rotation[i] = Mathf.MoveTowardsAngle(m_Rotation[i], targetRotation[i], maxDegreeDelta);
            }

            transform.eulerAngles = m_Rotation;
        }

    }
}
