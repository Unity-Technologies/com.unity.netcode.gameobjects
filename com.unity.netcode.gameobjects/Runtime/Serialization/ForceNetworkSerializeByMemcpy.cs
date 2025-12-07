using System;

namespace Unity.Netcode
{
    /// <summary>
    /// This is a wrapper that adds `INetworkSerializeByMemcpy` support to existing structs that the developer
    /// doesn't have the ability to modify (for example, external structs like `Guid`).
    /// </summary>
    /// <typeparam name="T">The type to be wrapped, must be unmanaged and implement IEquatable{T}</typeparam>
    public struct ForceNetworkSerializeByMemcpy<T> : INetworkSerializeByMemcpy, IEquatable<ForceNetworkSerializeByMemcpy<T>> where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// The wrapped value
        /// </summary>
        public T Value;

        /// <summary>
        /// The default constructor for <see cref="ForceNetworkSerializeByMemcpy{T}"/>
        /// </summary>
        /// <param name="value">sets the initial value of type `T`</param>
        public ForceNetworkSerializeByMemcpy(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Convert implicitly from the ForceNetworkSerializeByMemcpy wrapper to the underlying value
        /// </summary>
        /// <param name="container">The wrapper</param>
        /// <returns>The underlying value</returns>
        public static implicit operator T(ForceNetworkSerializeByMemcpy<T> container) => container.Value;

        /// <summary>
        /// Convert implicitly from a T value to a ForceNetworkSerializeByMemcpy wrapper
        /// </summary>
        /// <param name="underlyingValue">the value</param>
        /// <returns>a new wrapper</returns>
        public static implicit operator ForceNetworkSerializeByMemcpy<T>(T underlyingValue) => new ForceNetworkSerializeByMemcpy<T> { Value = underlyingValue };

        /// <summary>
        /// Check if wrapped values are equal
        /// </summary>
        /// <param name="other">Other wrapper</param>
        /// <returns>true if equal</returns>
        public bool Equals(ForceNetworkSerializeByMemcpy<T> other)
        {
            return Value.Equals(other.Value);
        }

        /// <summary>
        /// Check if this value is equal to a boxed object value
        /// </summary>
        /// <param name="obj">The boxed value to check against</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object obj)
        {
            return obj is ForceNetworkSerializeByMemcpy<T> other && Equals(other);
        }

        /// <summary>
        /// Obtains the wrapped value's hash code
        /// </summary>
        /// <returns>Wrapped value's hash code</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
