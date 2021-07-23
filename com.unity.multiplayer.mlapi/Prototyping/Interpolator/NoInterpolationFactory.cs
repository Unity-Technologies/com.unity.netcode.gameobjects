using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "NoInterpolation", menuName = "MLAPI/NoInterpolation", order = 1)]
    public class NoInterpolationFactory : InterpolatorVector3Factory
    {
        public override IInterpolator<Vector3> CreateInterpolator()
        {
            return new NoInterpolation();
        }
    }

    public class NoInterpolation : IInterpolator<Vector3>
    {
        public Vector3 m_Current;

        public void Update(float deltaTime)
        {
            // nothing
        }

        public void FixedUpdate(float fixedDeltaTime)
        {

        }

        public void AddMeasurement(Vector3 newMeasurement, int SentTick)
        {
            m_Current = newMeasurement;
        }

        public Vector3 GetInterpolatedValue()
        {
            return m_Current;
        }

        public void Teleport(Vector3 value)
        {
            m_Current = value;
        }
    }
}