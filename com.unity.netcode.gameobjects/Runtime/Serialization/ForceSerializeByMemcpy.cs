using System;

namespace Unity.Netcode
{
    public struct ForceSerializeByMemcpy<T> : ISerializeByMemcpy, IEquatable<ForceSerializeByMemcpy<T>> where T : unmanaged, IEquatable<T>
    {
        public T Value;

        public ForceSerializeByMemcpy(T value)
        {
            Value = value;
        }

        public static implicit operator T(ForceSerializeByMemcpy<T> container) => container.Value;
        public static implicit operator ForceSerializeByMemcpy<T>(T underlyingValue) => new ForceSerializeByMemcpy<T> { Value = underlyingValue };

        public bool Equals(ForceSerializeByMemcpy<T> other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is ForceSerializeByMemcpy<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
