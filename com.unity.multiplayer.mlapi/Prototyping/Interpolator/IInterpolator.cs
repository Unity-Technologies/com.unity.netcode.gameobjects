using MLAPI.Timing;
using UnityEngine;

public interface IInterpolator<T>
{
    public void Update(float deltaTime);
    public void NetworkTickUpdate(float tickDeltaTime);
    public void AddMeasurement(T newMeasurement, NetworkTime sentTime);
    public T GetInterpolatedValue();
    public void Teleport(T value, NetworkTime sentTime);
}

public abstract class InterpolatorFactory<T> : ScriptableObject
{
    public const string BaseMenuName = "MLAPI/Interpolator/";
    public abstract IInterpolator<T> CreateInterpolator();
}
