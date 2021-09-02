using System.Diagnostics;

namespace Unity.Netcode
{
    /// <summary>
    /// Disposable NetworkBuffer that returns back to the NetworkBufferPool when disposed
    /// </summary>
    public sealed class PooledNetworkBuffer : NetworkBuffer
    {
        private bool m_IsDisposed = false;

        internal StackTrace instantiationSite;

        internal PooledNetworkBuffer()
        {
            instantiationSite = new StackTrace();
        }

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
        /// Called by Dispose in the parent class implementation.
        /// We cannot override Dispose because it's not declared virtual
        /// And if we override it via `public new void Dispose()` then it doesn't get called
        /// on anything with a static type other than PooledNetworkBuffer, which then results in a leak:
        ///
        /// Stream buffer = PooledNetworkBuffer.Get();
        /// buffer.Dispose();
        ///
        /// ^ Static type is Stream, this calls Stream::Dispose() instead of PooledNetworkBuffer::Dispose()
        /// </summary>
        public override void Close()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                NetworkBufferPool.PutBackInPool(this);
                instantiationSite = null;
            }
        }

        internal void Reset()
        {
            instantiationSite = new StackTrace();
            SetLength(0);
            Position = 0;
        }
    }
}
