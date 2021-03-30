using System;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable NetworkBuffer that returns back to the NetworkBufferPool when disposed
    /// </summary>
    public sealed class PooledNetworkBuffer : NetworkBuffer, IDisposable
    {
        private bool m_IsDisposed = false;

        internal PooledNetworkBuffer() { }

        /// <summary>
        /// Gets a PooledNetworkBuffer from the static NetworkBufferPool
        /// </summary>
        /// <returns>PooledNetworkBuffer</returns>
        public static PooledNetworkBuffer Get()
        {
            var buffer = NetworkBufferPool.GetBuffer();
            buffer.m_IsDisposed = false;
            return buffer;
        }

        /// <summary>
        /// Returns the PooledNetworkBuffer into the static NetworkBufferPool
        /// </summary>
        public new void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                NetworkBufferPool.PutBackInPool(this);
            }
        }
    }
}
