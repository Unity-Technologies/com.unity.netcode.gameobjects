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

        [Range(0.001f, 1.0f)]
        public float MinScale = 1.0f;

        [Range(1.0f, 4.0f)]
        public float MaxScale = 1.0f;

        private Vector3 m_Rotation;
        private Vector3 m_RotateBy;
        private Vector3 m_TargetScale;

        private float m_NextRotationDeltaUpdate;

        private void Start()
        {
            Direction.Normalize();

            m_Rotation = transform.eulerAngles;
            GenerateNewRotationDeltaBase();

        }

        public delegate void NotifySerializedSizeHandler(int size);

        public NotifySerializedSizeHandler NotifySerializedSize;

        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            NotifySerializedSize?.Invoke(networkTransformState.LastSerializedSize);
            base.OnAuthorityPushTransformState(ref networkTransformState);
        }

        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            NotifySerializedSize?.Invoke(newState.LastSerializedSize);
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
        }

        public override void OnNetworkSpawn()
        {
            if (CanCommitToTransform)
            {
                GenerateNewTargetScale();
            }
            else
            {
                var cameraFollower = FindObjectOfType<CameraFollower>();
                var position = transform.position;
                cameraFollower.UpdateOffset(ref position);
            }
            base.OnNetworkSpawn();
        }

        private void GenerateNewRotationDeltaBase()
        {
            m_RotateBy = new Vector3(Random.Range(-RotationSpeed, RotationSpeed), Random.Range(-RotationSpeed, RotationSpeed), Random.Range(-RotationSpeed, RotationSpeed));
            m_NextRotationDeltaUpdate = Time.realtimeSinceStartup + Random.Range(1.0f, 5.0f);
        }

        private void GenerateNewTargetScale()
        {
            m_TargetScale = new Vector3(Random.Range(MinScale, MaxScale), Random.Range(MinScale, MaxScale), Random.Range(MinScale, MaxScale));
        }

        private void GenerateNewScaleIfReached()
        {
            var delta = m_TargetScale - transform.localScale;
            if (delta.sqrMagnitude < 0.15f)
            {
                GenerateNewTargetScale();
            }
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !CanCommitToTransform)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, transform.position + (Direction * MoveSpeed), Time.fixedDeltaTime);

            if (m_NextRotationDeltaUpdate < Time.realtimeSinceStartup)
            {
                GenerateNewRotationDeltaBase();
            }
            var targetRotation = m_Rotation + m_RotateBy;

            for (int i = 0; i < 3; i++)
            {
                m_Rotation[i] = Mathf.MoveTowardsAngle(m_Rotation[i], targetRotation[i], RotationSpeed);
            }
            transform.eulerAngles = m_Rotation;
            GenerateNewScaleIfReached();
            transform.localScale = Vector3.Lerp(transform.localScale, m_TargetScale, Time.fixedDeltaTime);
        }

    }
}
