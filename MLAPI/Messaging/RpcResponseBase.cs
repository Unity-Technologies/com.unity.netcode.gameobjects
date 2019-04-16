using System;

namespace MLAPI.Messaging
{
    /// <summary>
    /// Abstract base class for RpcResponse
    /// </summary>
    public abstract class RpcResponseBase
    {
        /// <summary>
        /// Unique ID for the Rpc Request and Response pair
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
        public ulong ClientId { get; internal set; }
        /// <summary>
        /// The amount of time to wait for the operation to complete
        /// </summary>
        public float Timeout { get; set; } = 10f;
        internal abstract object Result { set; }
        internal Type Type { get; set; }
    }
}