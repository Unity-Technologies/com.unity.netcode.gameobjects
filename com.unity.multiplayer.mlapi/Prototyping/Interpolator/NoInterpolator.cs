namespace Unity.Netcode
{
    public class NoInterpolator<T> : IInterpolator<T>
    {
        private T m_Current;

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

        public T Update(float deltaTime)
        {
            // nothing
            return GetInterpolatedValue();
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
        }

        public void AddMeasurement(T newMeasurement, NetworkTime sentTick)
        {
            m_Current = newMeasurement;
        }

        public T GetInterpolatedValue()
        {
            return m_Current;
        }

        public void OnDestroy()
        {
        }
    }
}