using System;
using System.IO;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable BitReader that returns the Reader to the BitReaderPool when disposed
    /// </summary>
    public sealed class PooledBitReader : BitReader, IDisposable
    {
        internal bool isDisposed = false;

        internal PooledBitReader(NetworkingManager manager, Stream stream) : base(manager, stream)
        {
        }

        /// <summary>
        /// Returns the PooledBitReader into the static BitReaderPool
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                networkingManager.PooledBitReaders.PutBackInPool(this);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Disposing reader that thinks it is already disposed!");
            }
        }
    }
}
