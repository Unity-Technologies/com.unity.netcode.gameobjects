using MLAPI.Serialization;
using MLAPI.Timing;
using UnityEngine;

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
    public void Reset(T value, NetworkTime sentTime);
    public void OnDestroy();
}

public abstract class InterpolatorFactory<T> : ScriptableObject
{
    public const string BaseMenuName = "MLAPI/Interpolator/";
    public abstract IInterpolator<T> CreateInterpolator();
}

public interface IInterpolatorSettings
{
}


