using System;
using System.IO;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable BitReader that returns the Reader to the BitReaderPool when disposed
    /// </summary>
    public sealed class PooledBitReader : BitReader, IDisposable
    {
        private bool isDisposed = false;
        
        internal PooledBitReader(Stream stream) : base(stream)
        {
        }

        /// <summary>
        /// Gets a PooledBitReader from the static BitReaderPool
        /// </summary>
        /// <returns>PooledBitReader</returns>
        public static PooledBitReader Get(Stream stream)
        {
            PooledBitReader reader = BitReaderPool.GetReader(stream);
            reader.isDisposed = false;
            return reader;
        }

        /// <summary>
        /// Returns the PooledBitReader into the static BitReaderPool
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                BitReaderPool.PutBackInPool(this);
            }
        }
    }
}