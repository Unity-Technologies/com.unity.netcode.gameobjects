using System;
using MLAPI.Timing;
using UnityEngine;

namespace MLAPI
{
    [CreateAssetMenu(fileName = "PositionLinearInterpolator", menuName = BaseMenuName + "PositionLinearInterpolator", order = 1)]
    public class PositionLinearInterpolatorFactory : InterpolatorFactory<Vector3>
    {
        [SerializeField]
        public float MaxLerpTime = 0.2f;

        public override IInterpolator<Vector3> CreateInterpolator()
        {
            return new PositionLinearInterpolator(this);
        }
    }

    public class PositionLinearInterpolator : IInterpolator<Vector3>
    {
        public float m_CurrentTime;
        public Vector3 m_StartVector;
        public Vector3 m_EndVector;
        public Vector3 m_UpdatedVector;
        private readonly PositionLinearInterpolatorFactory m_Factory;

        public PositionLinearInterpolator(PositionLinearInterpolatorFactory factory)
        {
            m_Factory = factory;
        }

        public void Awake()
        {
        }

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
            m_UpdatedVector = Vector3.Lerp(m_StartVector, m_EndVector, m_CurrentTime / m_Factory.MaxLerpTime);
            return GetInterpolatedValue();
        }

        public void NetworkTickUpdate(float fixedDeltaTime)
        {
        }

        public void AddMeasurement(Vector3 newMeasurement, NetworkTime SentTick)
        {
            m_EndVector = newMeasurement;
            m_CurrentTime = 0;
            m_StartVector = m_UpdatedVector;
        }

        public Vector3 GetInterpolatedValue()
        {
            return m_UpdatedVector;
        }

        public void Reset(Vector3 value, NetworkTime SentTick)
        {
            m_UpdatedVector = value;
        }

        public void OnDestroy()
        {
            throw new NotImplementedException();
        }
    }
}