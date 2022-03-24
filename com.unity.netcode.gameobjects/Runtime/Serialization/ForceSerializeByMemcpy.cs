namespace Unity.Netcode
{
    public struct ForceSerializeByMemcpy<T>: ISerializeByMemcpy where T : unmanaged
    {
        public T Value;

        public ForceSerializeByMemcpy(T value)
        {
            Value = value;
        }

        public static implicit operator T(ForceSerializeByMemcpy<T> container) => container.Value;
        public static implicit operator ForceSerializeByMemcpy<T>(T underlyingValue) => new ForceSerializeByMemcpy<T> { Value = underlyingValue };
    }
}
