using System;

namespace MLAPI
{
    /// <summary>
    /// The RpcResponse class exposed by the API. Represents a network Request/Response operation with a result
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    public class RpcResponse<T> : RpcResponseBase
    {
        /// <summary>
        /// Gets the return value of the operation
        /// </summary>
        public T Value { get; private set; }

        internal override object Result
        {
            set => Value = (T) value;
        }
    }

    /// <summary>
    /// Abstract base class for RpcResponse
    /// </summary>
    public abstract class RpcResponseBase
    {
        /// <summary>
        /// Unique ID for the Rpc Request & Response pair
        /// </summary>
        public ulong Id { get; internal set; }
        /// <summary>
        /// Whether or not the operation is done. This does not mean it was successful. Check IsSuccessful for that
        /// This will be true both when the operation was successful and when a timeout occured
        /// </summary>
        public bool IsDone { get; internal set; }
        /// <summary>
        /// Whether or not a valid result was received
        /// </summary>
        public bool IsSuccessful { get; set; }
        /// <summary>
        /// The clientId which the Request/Response was done wit
        /// </summary>
        public uint ClientId { get; internal set; }
        /// <summary>
        /// The amount of time to wait for the operation to complete
        /// </summary>
        public float Timeout { get; set; } = 10f;
        internal abstract object Result { set; }
        internal Type Type { get; set; }
    }
}