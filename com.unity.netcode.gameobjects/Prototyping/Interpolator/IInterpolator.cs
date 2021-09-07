namespace Unity.Netcode
{
    public interface IInterpolator<T>
    {
        void Awake();
        void OnNetworkSpawn();
        void Start();
        void OnEnable();
        T Update(float deltaTime);
        void FixedUpdate(float tickDeltaTime);
        void AddMeasurement(T newMeasurement, NetworkTime sentTime);
        T GetInterpolatedValue();
        void OnDestroy();
        void ResetTo(T targetValue);
        bool UseFixedUpdate { get; set; }
    }
}
