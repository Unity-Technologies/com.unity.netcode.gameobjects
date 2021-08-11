using System;
using UnityEngine;

namespace Unity.Netcode
{
    public class SimpleInterpolatorVector3 : IInterpolator<Vector3>
    {
        private float m_CurrentTime;
        private Vector3 m_StartVector;
        private Vector3 m_EndVector;
        private Vector3 m_UpdatedVector;

        private const float k_MaxLerpTime = 0.1f;

        public void OnNetworkSpawn()
        {
        }

        public void Start()
        {
        }

        public void OnEnable()
        {
        }

        public Vector3 Update(float deltaTime)
        {
            m_CurrentTime += deltaTime;
            m_UpdatedVector = Vector3.Lerp(m_StartVector, m_EndVector, m_CurrentTime / k_MaxLerpTime);
            return GetInterpolatedValue();
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
        }

        public void AddMeasurement(Vector3 newMeasurement, NetworkTime sentTick)
        {
            m_EndVector = newMeasurement;
            m_CurrentTime = 0;
            m_StartVector = m_UpdatedVector;
        }

        public Vector3 GetInterpolatedValue()
        {
            return m_UpdatedVector;
        }

        public void Reset(Vector3 value, NetworkTime sentTick)
        {
            m_UpdatedVector = value;
        }

        public void OnDestroy()
        {
            throw new NotImplementedException();
        }
    }
}