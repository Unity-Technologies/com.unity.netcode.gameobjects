using System.Collections.Generic;
using MLAPI;
using MLAPI.Timing;
using UnityEngine;

namespace DefaultNamespace
{
    public interface IBufferedLinearInterpolatorSettings : IInterpolatorSettings
    {
        public float InterpolationTime { get; }
    }

    public abstract class BufferedLinearInterpolatorFactory<T> : InterpolatorFactory<T>, IBufferedLinearInterpolatorSettings
    {
        [SerializeField]
        public float InterpolationTime => 0.100f;
    }

    public class BufferedLinearInterpolatorSettings : IBufferedLinearInterpolatorSettings
    {
        public float InterpolationTime { get; set; } = 0.1f;
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

        public float InterpolationConfigTimeSec => m_Settings.InterpolationTime; // todo use with range

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
        private NetworkTime m_ValueCurrentTickConsumed;

        private List<BufferedItem<T>> m_Buffer = new List<BufferedItem<T>>();

        private readonly IBufferedLinearInterpolatorSettings m_Settings;

        public BufferedLinearInterpolator()
        {
            m_Settings = new BufferedLinearInterpolatorSettings {InterpolationTime = 0.2f};
        }

        public BufferedLinearInterpolator(IBufferedLinearInterpolatorSettings settings)
        {
            m_Settings = settings;
        }

        public BufferedLinearInterpolator(NetworkVariable<IInterpolatedState<T>> netvar)
        {
            netvar.OnValueChanged += OnStateChanged;
        }

        public void OnStateChanged(IInterpolatedState<T> old, IInterpolatedState<T> newState)
        {
            var sentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, newState.SentTick);
            AddMeasurement(newState.Value, sentTime);
        }

        private NetworkTime m_PreviousTickConsumed;

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
                    samPreviousTickConsumed = m_ValueCurrentTickConsumed;
                    m_ValueCurrentTickConsumed = bufferedValue.tickSent;
                    Debug.Log($"hellooooo {bufferedValue.tickSent}");
                    m_Buffer.RemoveAt(i);
                    nbConsumed++;

                    var pos = m_LerpEndValue is Vector3 value ? value : default;
                    Debug.DrawLine(pos, pos + Random.Range(0f, 1f) * Vector3.up + Random.Range(0f, 1f) * Vector3.left, Color.green, 10, false);
                }
            }

            Debug.Log($"Buffer size: {count}, nb consumed: {nbConsumed}");
            var pos2 = m_LerpEndValue is Vector3 value2 ? value2 : default;

            Debug.DrawLine(pos2, pos2 + Vector3.down * (m_ValueCurrentTickConsumed.Tick - m_PreviousTickConsumed.Tick), Color.cyan, 10, false);
            for (int i = 0; i < count; i++)
            {
                Debug.DrawLine(pos2 + Vector3.up * (i+1), pos2 + Vector3.up* (i+1) + Vector3.left, Color.white, 10, false);
            }
        }

        private float samLastServerTime;
        private float samLastTime;

        private float serverOffset;
        private float m_RenderTime => Time.time - serverOffset;

        public void Start()
        {
            serverOffset = (float) (Time.time - NetworkManager.Singleton.NetworkTickSystem.ServerTime.Time);
        }

        public void OnEnable()
        {
        }

        public void Awake()
        {
        }

        public void OnNetworkSpawn()
        {
        }

        public T Update(float deltaTime)
        {
            if (!NetworkManager.Singleton.IsConnectedClient) return default;

            TryConsumeFromBuffer();

            var timeB = m_ValueCurrentTickConsumed;//new NetworkTime(NetworkManager.Singleton.NetworkTickSystem.TickRate, m_ValueLastTick);
            // var timeA = timeB - timeB.FixedDeltaTime;//

            // Interpolation example to understand the math below
            // 4            6   6.5
            // |            |   |
            // A            B   Server
            var timeA = m_PreviousTickConsumed;
            double range = timeB.Time - timeA.Time;
            var renderTime = NetworkManager.Singleton.NetworkTickSystem.ServerTime.Time - range;
            // var renderTime = m_RenderTime;
            float t = (float)((renderTime - timeA.Time) / range);
            var diffServerTime = NetworkManager.Singleton.NetworkTickSystem.ServerTime.Time - samLastServerTime;
            samLastServerTime = (float) NetworkManager.Singleton.NetworkTickSystem.ServerTime.Time;
            var diffTime = Time.time - samLastTime;
            samLastTime = Time.time;
            Debug.Log($"diffServerTime {diffServerTime} diffTime {diffTime} deltaTime {deltaTime}");
            Debug.Log($"ttttttttttttt {t}");
            m_CurrentValue = Interpolate(m_LerpStartValue, m_LerpEndValue, t);
            // m_CurrentValue = m_LerpEndValue;


            // var timeB = m_ValueCurrentTickConsumed;//new NetworkTime(NetworkManager.Singleton.NetworkTickSystem.TickRate, m_ValueLastTick);
            // var timeA = timeB - timeB.FixedDeltaTime;//
            // double range = timeB.Time - timeA.Time;
            // float t = (float)((NetworkManager.Singleton.NetworkTickSystem.ServerTime.Time - timeA.FixedDeltaTime - timeA.Time) / range);
            // m_CurrentValue = Interpolate(m_LerpStartValue, m_LerpEndValue, t);



            var pos = m_CurrentValue is Vector3 value ? value : default;
            Debug.DrawLine(pos, pos + Vector3.up, Color.magenta, 10, false);
            return m_CurrentValue;
        }

        public void NetworkTickUpdate(float fixedDeltaTime)
        {
        }

        public void AddMeasurement(T newMeasurement, NetworkTime SentTick)
        {
            var debugPos = newMeasurement is Vector3 value ? value : default;
            Debug.DrawLine(debugPos, debugPos + Vector3.right + Vector3.up, Color.red, 10, false);

            Debug.Log($"Adding measurement {Time.time}");
            // todo put limit on size, we don't want lag spikes to create 100 entries and have a list that size in memory for ever
            m_Buffer.Add(new BufferedItem<T>() {item = newMeasurement, tickSent = SentTick});
            m_Buffer.Sort((item1, item2) => item2.tickSent.Tick.CompareTo(item1.tickSent.Tick));
        }

        public T GetInterpolatedValue()
        {
            return m_CurrentValue;
        }

        public void Reset(T value, NetworkTime SentTick)
        {
            m_CurrentValue = value;
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
        public BufferedLinearInterpolatorVector3(IBufferedLinearInterpolatorSettings settings) : base(settings) { }

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

        public BufferedLinearInterpolatorQuaternion(IBufferedLinearInterpolatorSettings settings) : base(settings) { }
    }
}