namespace MLAPI.Messaging
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
}