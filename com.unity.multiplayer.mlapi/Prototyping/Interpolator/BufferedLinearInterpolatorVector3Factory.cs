using System.Collections.Generic;
using MLAPI;
using MLAPI.Timing;
using UnityEngine;

namespace DefaultNamespace
{
    public class BufferedLinearInterpolatorVector3Factory : InterpolatorVector3Factory
    {
        public override IInterpolator<Vector3> CreateInterpolator()
        {
            return new BufferedLinearInterpolatorVector3();
        }
    }

    public abstract class BufferedLinearInterpolator<T> : IInterpolator<T> where T : struct
    {
        public int BufferAmountTick = 3;
        public const float InterpolationConfigTimeSec = 0.100f; // todo remove const for config

        struct BufferedItem<T>
        {
            public T item;
            public int tickSent;
        }

        // private double ServerTime => NetworkManager.Singleton.ServerTime.Time;
        private double ServerTick => NetworkManager.Singleton.ServerTime.Tick;
        private double ServerTickBeingHandledForBuffering => ServerTick;// - BufferAmountTick;

        T m_LerpStartValue;
        T m_LerpEndValue;

        private T m_CurrentValue;

        private List<BufferedItem<T>> m_Buffer = new List<BufferedItem<T>>();

        private int samPreviousToLast;
        private void TryConsumeFromBuffer()
        {
            var count = m_Buffer.Count;
            int nbConsumed = 0;
            for (int i = m_Buffer.Count - 1; i >= 0; i--)
            {
                var bufferedValue = m_Buffer[i];
                if (bufferedValue.tickSent <= ServerTickBeingHandledForBuffering)
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


                    float maxTickAllowedInBuffer = NetworkManager.Singleton.NetworkTickSystem.TickRate;

                    // we're not getting values regularly from the network. This means interpolation needs to adapt
                    m_InterpolationTimeSec = InterpolationConfigTimeSec;

                    // if (ServerTickBeingHandledForBuffering - bufferedValue.tickSent < maxTickAllowedInBuffer)
                    // {
                    //     // Consume one value at a time, but make sure we don't have an ever growing buffer. We only use one item if we're under our max amount
                    //     break;
                    // }

                    // Debug.LogWarning("Consuming more than one value at a time from buffer"); // todo remove?
                }
            }

            Debug.Log($"Buffer size: {count}, nb consumed: {nbConsumed}");
            var pos2 = m_LerpEndValue is Vector3 value2 ? value2 : default;

            Debug.DrawLine(pos2, pos2 + Vector3.down * (m_ValueLastTick - samPreviousToLast), Color.cyan, 10, false);
            for (int i = 0; i < count; i++)
            {
                Debug.DrawLine(pos2 + Vector3.up * (i+1), pos2 + Vector3.up* (i+1) + Vector3.left, Color.white, 10, false);
            }
        }

        public void Update(float deltaTime)
        {
            TryConsumeFromBuffer();

            var timeB = new NetworkTime(NetworkManager.Singleton.NetworkTickSystem.TickRate, m_ValueLastTick);
            var timeA = timeB - timeB.FixedDeltaTime;//
            double range = timeB.Time - timeA.Time;
            float t = (float)((NetworkManager.Singleton.NetworkTickSystem.ServerTime.Time - timeA.FixedDeltaTime - timeA.Time) / range);
            Debug.Log($"ttttttttttttt {t}");
            // m_CurrentValue = Interpolate(m_LerpStartValue, m_LerpEndValue, t);
            m_CurrentValue = m_LerpEndValue;

            var pos = m_CurrentValue is Vector3 value ? value : default;
            Debug.DrawLine(pos, pos + Vector3.up, Color.magenta, 10, false);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {

        }


        // private int timeInitializedDebug = 20; // number so we wait a few frames to grab the offset. this is debug
        // private double clientServerOffsetDebug;
        private float m_InterpolationTimeSec;
        private int m_ValueLastTick;

        public void AddMeasurement(T newMeasurement, int SentTick)
        {
            var debugPos = newMeasurement is Vector3 value ? value : default;
            Debug.DrawLine(debugPos, debugPos + Vector3.right + Vector3.up, Color.red, 10, false);

            Debug.Log($"Adding measurement {Time.time}");
            m_Buffer.Add(new BufferedItem<T>() {item = newMeasurement, tickSent = SentTick});
            m_Buffer.Sort((item1, item2) => item2.tickSent.CompareTo(item1.tickSent));
        }

        public T GetInterpolatedValue()
        {
            return m_CurrentValue;
        }

        public void Teleport(T value)
        {
            m_CurrentValue = value;
            m_LerpEndValue = value;
            m_LerpStartValue = value;
        }

        public abstract T Interpolate(T start, T end, float time);
    }

    public class BufferedLinearInterpolatorVector3 : BufferedLinearInterpolator<Vector3>
    {
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
    }
}