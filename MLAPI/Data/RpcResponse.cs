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
        /// Gets the value from the operation.
        /// Note that this is an expensive operation, grab and cache
        /// </summary>
        public T Value => Result == null ? default(T) : (T) Result;
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
        /// Whether or not a result has been received
        /// </summary>
        public bool IsDone { get; internal set; }
        /// <summary>
        /// The clientId which the Request/Response was done wit
        /// </summary>
        public uint ClientId { get; internal set; }
        internal object Result { get; set; }
        internal Type Type { get; set; }
    }
}