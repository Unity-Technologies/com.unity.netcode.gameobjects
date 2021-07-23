using System;
using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "PositionLinearInterpolator", menuName = "MLAPI/PositionLinearInterpolator", order = 1)]
    public class PositionLinearInterpolatorFactory : InterpolatorVector3Factory
    {
        [SerializeField]
        private float m_MaxLerpTime = 0.2f;

        public override IInterpolator<Vector3> CreateInterpolator()
        {
            return new PositionLinearInterpolator(m_MaxLerpTime);
        }
    }

    public class PositionLinearInterpolator : IInterpolator<Vector3>
    {
        public float m_CurrentTime;
        public Vector3 m_StartVector;
        public Vector3 m_EndVector;
        public Vector3 m_UpdatedVector;

        private float m_MaxLerpTime;

        public PositionLinearInterpolator(float maxLerpTime)
        {
            m_MaxLerpTime = maxLerpTime;
        }

        public void Update(float deltaTime)
        {
            m_CurrentTime += deltaTime;
            m_UpdatedVector = Vector3.Lerp(m_StartVector, m_EndVector, m_CurrentTime / m_MaxLerpTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {

        }

        public void AddMeasurement(Vector3 newMeasurement, int SentTick)
        {
            m_EndVector = newMeasurement;
            m_CurrentTime = 0;
            m_StartVector = m_UpdatedVector;
        }

        public Vector3 GetInterpolatedValue()
        {
            return m_UpdatedVector;
        }

        public void Teleport(Vector3 value)
        {
            m_UpdatedVector = value;
        }
    }
}