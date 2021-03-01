using System;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable NetworkStream that returns the Stream to the NetworkStreamPool when disposed
    /// </summary>
    public sealed class PooledNetworkStream : NetworkStream, IDisposable
    {
        private bool isDisposed = false;

        internal PooledNetworkStream() { }

        /// <summary>
        /// Gets a PooledNetworkStream from the static NetworkStreamPool
        /// </summary>
        /// <returns>PooledNetworkStream</returns>
        public static PooledNetworkStream Get()
        {
            PooledNetworkStream stream = NetworkStreamPool.GetStream();
            stream.isDisposed = false;
            return stream;
        }

        /// <summary>
        /// Returns the PooledNetworkStream into the static NetworkStreamPool
        /// </summary>
        public new void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                NetworkStreamPool.PutBackInPool(this);
            }
        }
    }
}