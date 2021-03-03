using System;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable NetworkBuffer that returns back to the NetworkBufferPool when disposed
    /// </summary>
    public sealed class PooledNetworkBuffer : NetworkBuffer, IDisposable
    {
        private bool isDisposed = false;

        internal PooledNetworkBuffer() { }

        /// <summary>
        /// Gets a PooledNetworkBuffer from the static NetworkBufferPool
        /// </summary>
        /// <returns>PooledNetworkBuffer</returns>
        public static PooledNetworkBuffer Get()
        {
            var buffer = NetworkBufferPool.GetBuffer();
            buffer.isDisposed = false;
            return buffer;
        }

        /// <summary>
        /// Returns the PooledNetworkBuffer into the static NetworkBufferPool
        /// </summary>
        public new void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                NetworkBufferPool.PutBackInPool(this);
            }
        }
    }
}