using System;
using UnityEngine;

namespace Unity.Netcode
{
    public abstract class SimpleInterpolator<T> : IInterpolator<T>
    {
        private float m_CurrentTime;
        private T m_Start;
        private T m_End;
        private T m_Updated;

        private const float k_MaxLerpTime = 0.1f;

        public void Awake()
        {
            Update(0);
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

        public T Update(float deltaTime)
        {
            m_CurrentTime += deltaTime;
            m_Updated = Interpolate(m_Start, m_End, m_CurrentTime / k_MaxLerpTime);
            return GetInterpolatedValue();
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
        }

        public void AddMeasurement(T newMeasurement, NetworkTime sentTick)
        {
            m_End = newMeasurement;
            m_CurrentTime = 0;
            m_Start = m_Updated;
        }

        public T GetInterpolatedValue()
        {
            return m_Updated;
        }

        public void OnDestroy()
        {
        }

        public void ResetTo(T targetValue)
        {
            m_End = targetValue;
            m_Start = targetValue;
            m_Updated = targetValue;
            m_CurrentTime = 0;
        }

        protected abstract T Interpolate(T a, T b, float time);

        public bool UseFixedUpdate { get; set; }
    }
    public class SimpleInterpolatorFloat : SimpleInterpolator<float>
    {
        protected override float Interpolate(float a, float b, float time)
        {
            return Mathf.Lerp(a, b, time);
        }
    }

    public class SimpleInterpolatorQuaternion : SimpleInterpolator<Quaternion>
    {
        protected override Quaternion Interpolate(Quaternion a, Quaternion b, float time)
        {
            return Quaternion.Slerp(a, b, time);
        }
    }
}