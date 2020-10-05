using System;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable BitStream that returns the Stream to the BitStreamPool when disposed
    /// </summary>
    public sealed class PooledBitStream : BitStream, IDisposable
    {
        private bool isDisposed = false;

        internal PooledBitStream()
        {
        }

        /// <summary>
        /// Gets a PooledBitStream from the static BitStreamPool
        /// </summary>
        /// <returns>PooledBitStream</returns>
        public static PooledBitStream Get()
        {
            PooledBitStream stream = BitStreamPool.GetStream();
            stream.isDisposed = false;
            return stream;
        }

        /// <summary>
        /// Returns the PooledBitStream into the static BitStreamPool
        /// </summary>
        public new void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                BitStreamPool.PutBackInPool(this);
            }
        }
    }
}
