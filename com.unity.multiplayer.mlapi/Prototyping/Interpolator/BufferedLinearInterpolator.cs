using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    public abstract class BufferedLinearInterpolator<T> : IInterpolator<T> where T : struct
    {
        // public const float InterpolationConfigTimeSec = 0.100f; // todo expose global config, todo use in actual code

        private struct BufferedItem
        {
            public T item;
            public NetworkTime timeSent;
        }

        protected virtual double ServerTimeBeingHandledForBuffering => NetworkManager.Singleton.ServerTime.Time; // override this if you want configurable buffering, right now using ServerTick's own global buffering

        private T m_LerpStartValue;
        private T m_LerpEndValue;

        private T m_CurrentUpdatedValue;

        private NetworkTime m_CurrentTimeConsumed;
        private NetworkTime m_PreviousTimeConsumed;

        private readonly List<BufferedItem> m_Buffer = new List<BufferedItem>();
        private const int k_BufferSizeLimit = 100;

        public void Start()
        {
        }

        public void OnEnable()
        {
        }

        public void OnNetworkSpawn()
        {
        }

        private void TryConsumeFromBuffer()
        {
            int nbConsumed = 0;
            // sorted so older (smaller) time values are at the end.
            for (int i = m_Buffer.Count - 1; i >= 0; i--)
            {
                var bufferedValue = m_Buffer[i];
                if (bufferedValue.timeSent.Time <= ServerTimeBeingHandledForBuffering)
                {
                    if (nbConsumed == 0)
                    {
                        m_PreviousTimeConsumed = m_CurrentTimeConsumed;
                        m_LerpStartValue = m_LerpEndValue;
                    }

                    m_LerpEndValue = bufferedValue.item;
                    m_CurrentTimeConsumed = bufferedValue.timeSent;
                    m_Buffer.RemoveAt(i);
                    nbConsumed++;
                }
            }
        }

        public T Update(float deltaTime)
        {
            if (!NetworkManager.Singleton.IsConnectedClient) return default;

            TryConsumeFromBuffer();

            // Interpolation example to understand the math below
            // 4   4.5      6   6.5
            // |   |        |   |
            // A   render   B   Server

            var timeB = m_CurrentTimeConsumed;
            var timeA = m_PreviousTimeConsumed;
            double range = timeB.Time - timeA.Time;
            var renderTime = ServerTimeBeingHandledForBuffering - range;
            float t = (float)((renderTime - timeA.Time) / range);
            m_CurrentUpdatedValue = Interpolate(m_LerpStartValue, m_LerpEndValue, t);

            return m_CurrentUpdatedValue;
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
        }

        public void AddMeasurement(T newMeasurement, NetworkTime sentTime)
        {
            if (m_Buffer.Count > k_BufferSizeLimit)
            {
                Debug.LogWarning("Going over buffer size limit while adding new interpolation values, interpolation buffering isn't consuming fast enough, removing oldest value now.");
                m_Buffer.RemoveAt(m_Buffer.Count - 1);
            }

            m_Buffer.Add(new BufferedItem {item = newMeasurement, timeSent = sentTime});
            m_Buffer.Sort((item1, item2) => item2.timeSent.Time.CompareTo(item1.timeSent.Time));
        }

        public T GetInterpolatedValue()
        {
            return m_CurrentUpdatedValue;
        }

        public void Reset(T value, NetworkTime sentTime)
        {
            m_CurrentUpdatedValue = value;
            m_LerpEndValue = value;
            m_LerpStartValue = value;
        }

        public void OnDestroy()
        {
            throw new System.NotImplementedException();
        }

        public abstract T Interpolate(T start, T end, float time);
    }

    public class BufferedLinearInterpolatorVector3 : BufferedLinearInterpolator<Vector3>
    {
        public override Vector3 Interpolate(Vector3 start, Vector3 end, float time)
        {
            return Vector3.Lerp(start, end, time);
        }
    }

    public class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        public override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            return Quaternion.Slerp(start, end, time);
        }
    }

    public class BufferedLinearInterpolatorFloat : BufferedLinearInterpolator<float>
    {
        public override float Interpolate(float start, float end, float time)
        {
            return Mathf.Lerp(start, end, time);
        }
    }
}