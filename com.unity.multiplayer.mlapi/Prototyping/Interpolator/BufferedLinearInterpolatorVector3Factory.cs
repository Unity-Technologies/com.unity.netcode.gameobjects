using System.Collections.Generic;
using MLAPI;
using MLAPI.Timing;
using UnityEngine;

namespace DefaultNamespace
{
    public abstract class BufferedLinearInterpolatorFactory<T> : InterpolatorFactory<T>
    {
        [SerializeField]
        public float InterpolationTime = 0.100f;
    }

    [CreateAssetMenu(fileName = "BufferedLinearInterpolatorVector3", menuName = BaseMenuName + "BufferedLinearInterpolatorVector3", order = 1)]
    public class BufferedLinearInterpolatorVector3Factory : BufferedLinearInterpolatorFactory<Vector3>
    {
        public override IInterpolator<Vector3> CreateInterpolator()
        {
            return new BufferedLinearInterpolatorVector3(this);
        }
    }

    [CreateAssetMenu(fileName = "BufferedLinearInterpolatorQuaternion", menuName = BaseMenuName + "BufferedLinearInterpolatorQuaternion", order = 1)]
    public class BufferedLinearInterpolatorQuaternionFactory : BufferedLinearInterpolatorFactory<Quaternion>
    {
        public override IInterpolator<Quaternion> CreateInterpolator()
        {
            return new BufferedLinearInterpolatorQuaternion(this);
        }
    }

    public abstract class BufferedLinearInterpolator<T> : IInterpolator<T> where T : struct
    {
        public int AdditionalBufferAmountTick = 3; // todo config, todo expose global config, todo use in actual code
        // public const float InterpolationConfigTimeSec = 0.100f; // todo remove const for config

        public float InterpolationConfigTimeSec => m_Factory.InterpolationTime; // todo use with range

        struct BufferedItem<T>
        {
            public T item;
            public NetworkTime tickSent;
        }

        // private double ServerTick => NetworkManager.Singleton.ServerTime.Tick;
        protected virtual double ServerTickBeingHandledForBuffering => NetworkManager.Singleton.ServerTime.Tick; // override this if you want configurable buffering, right now using ServerTick's own global buffering

        T m_LerpStartValue;
        T m_LerpEndValue;
        private T m_CurrentValue;
        private NetworkTime m_ValueLastTick;

        private List<BufferedItem<T>> m_Buffer = new List<BufferedItem<T>>();

        private readonly BufferedLinearInterpolatorFactory<T> m_Factory;

        public BufferedLinearInterpolator(BufferedLinearInterpolatorFactory<T> factory)
        {
            m_Factory = factory;
        }

        private NetworkTime samPreviousToLast;

        private void TryConsumeFromBuffer()
        {
            var count = m_Buffer.Count;
            int nbConsumed = 0;
            for (int i = m_Buffer.Count - 1; i >= 0; i--)
            {
                var bufferedValue = m_Buffer[i];
                if (bufferedValue.tickSent.Tick <= ServerTickBeingHandledForBuffering)
                {
                    m_LerpStartValue = m_LerpEndValue;
                    m_LerpEndValue = bufferedValue.item;
                    samPreviousToLast = m_ValueLastTick;
                    m_ValueLastTick = bufferedValue.tickSent;
                    Debug.Log($"hellooooo {bufferedValue.tickSent}");
                    m_Buffer.RemoveAt(i);
                    nbConsumed++;

                    var pos = m_LerpEndValue is Vector3 value ? value : default;
                    Debug.DrawLine(pos, pos + Random.Range(0f, 1f) * Vector3.up + Random.Range(0f, 1f) * Vector3.left, Color.green, 10, false);
                }
            }

            Debug.Log($"Buffer size: {count}, nb consumed: {nbConsumed}");
            var pos2 = m_LerpEndValue is Vector3 value2 ? value2 : default;

            Debug.DrawLine(pos2, pos2 + Vector3.down * (m_ValueLastTick.Tick - samPreviousToLast.Tick), Color.cyan, 10, false);
            for (int i = 0; i < count; i++)
            {
                Debug.DrawLine(pos2 + Vector3.up * (i+1), pos2 + Vector3.up* (i+1) + Vector3.left, Color.white, 10, false);
            }
        }

        public void Update(float deltaTime)
        {
            TryConsumeFromBuffer();

            var timeB = m_ValueLastTick;//new NetworkTime(NetworkManager.Singleton.NetworkTickSystem.TickRate, m_ValueLastTick);
            var timeA = timeB - timeB.FixedDeltaTime;//
            double range = timeB.Time - timeA.Time;
            float t = (float)((NetworkManager.Singleton.NetworkTickSystem.ServerTime.Time - timeA.FixedDeltaTime - timeA.Time) / range);
            Debug.Log($"ttttttttttttt {t}");
            // m_CurrentValue = Interpolate(m_LerpStartValue, m_LerpEndValue, t);
            m_CurrentValue = m_LerpEndValue;

            var pos = m_CurrentValue is Vector3 value ? value : default;
            Debug.DrawLine(pos, pos + Vector3.up, Color.magenta, 10, false);
        }

        public void NetworkTickUpdate(float fixedDeltaTime)
        {

        }


        public void AddMeasurement(T newMeasurement, NetworkTime SentTick)
        {
            var debugPos = newMeasurement is Vector3 value ? value : default;
            Debug.DrawLine(debugPos, debugPos + Vector3.right + Vector3.up, Color.red, 10, false);

            Debug.Log($"Adding measurement {Time.time}");
            m_Buffer.Add(new BufferedItem<T>() {item = newMeasurement, tickSent = SentTick});
            m_Buffer.Sort((item1, item2) => item2.tickSent.Tick.CompareTo(item1.tickSent.Tick));
        }

        public T GetInterpolatedValue()
        {
            return m_CurrentValue;
        }

        public void Teleport(T value, NetworkTime SentTick)
        {
            m_CurrentValue = value;
            m_LerpEndValue = value;
            m_LerpStartValue = value;
        }

        public abstract T Interpolate(T start, T end, float time);
    }

    public class BufferedLinearInterpolatorVector3 : BufferedLinearInterpolator<Vector3>
    {
        public BufferedLinearInterpolatorVector3(BufferedLinearInterpolatorVector3Factory bufferedLinearInterpolatorVector3Factory) : base(bufferedLinearInterpolatorVector3Factory) { }

        public override Vector3 Interpolate(Vector3 start, Vector3 end, float time)
        {
            return Vector3.LerpUnclamped(start, end, time);
        }
    }

    public class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        public override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            return Quaternion.Slerp(start, end, time);
        }

        public BufferedLinearInterpolatorQuaternion(BufferedLinearInterpolatorFactory<Quaternion> factory) : base(factory) { }
    }
}