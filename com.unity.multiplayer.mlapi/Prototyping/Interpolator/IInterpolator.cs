using UnityEngine;

public interface IInterpolator<T>
{
    public void Update(float deltaTime);
    public void FixedUpdate(float fixedDeltaTime);
    public void AddMeasurement(T newMeasurement, int SentTick);
    public T GetInterpolatedValue();
    public void Teleport(T value);
}

public abstract class InterpolatorFactory<T> : ScriptableObject
{
    public abstract IInterpolator<T> CreateInterpolator();
}

public abstract class InterpolatorVector3Factory : InterpolatorFactory<Vector3>
{

}