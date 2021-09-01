using UnityEngine;

namespace Unity.Netcode
{
    public interface IInterpolator<T>
    {
        public void Awake();
        public void OnNetworkSpawn();
        public void Start();
        public void OnEnable();
        public T Update(float deltaTime);
        public void FixedUpdate(float tickDeltaTime);
        public void AddMeasurement(T newMeasurement, NetworkTime sentTime);
        public T GetInterpolatedValue();
        public void OnDestroy();
        public bool UseFixedUpdate { get; set; }
    }
}
