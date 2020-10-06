using System;
using System.IO;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable BitWriter that returns the Writer to the BitWriterPool when disposed
    /// </summary>
    public sealed class PooledBitWriter : BitWriter, IDisposable
    {
        private bool isDisposed = false;

        internal PooledBitWriter(Stream stream) : base(stream)
        {
        }

        /// <summary>
        /// Gets a PooledBitWriter from the static BitWriterPool
        /// </summary>
        /// <returns>PooledBitWriter</returns>
        public static PooledBitWriter Get(Stream stream)
        {
            PooledBitWriter writer = BitWriterPool.GetWriter(stream);
            writer.isDisposed = false;
            return writer;
        }

        /// <summary>
        /// Returns the PooledBitWriter into the static BitWriterPool
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                BitWriterPool.PutBackInPool(this);
            }
        }
    }
}
