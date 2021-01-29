using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledBitReaders
    /// </summary>
    public class BitReaderPool
    {
        private byte createdReaders = 0;
        private readonly Queue<PooledBitReader> readers = new Queue<PooledBitReader>();
        private NetworkingManager networkingManager;

        internal BitReaderPool(NetworkingManager manager )
        {
            networkingManager = manager;
        }

        /// <summary>
        /// Retrieves a PooledBitReader
        /// </summary>
        /// <param name="stream">The stream the reader should read from</param>
        /// <returns>A PooledBitReader</returns>
        public PooledBitReader GetReader(Stream stream)
        {
            if (readers.Count == 0)
            {
                if (createdReaders == 254)
                {
                    if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("255 readers have been created. Did you forget to dispose?");
                }
                else if (createdReaders < 255) createdReaders++;

                return new PooledBitReader(networkingManager, stream);
            }

            PooledBitReader reader = readers.Dequeue();
            reader.isDisposed = false;
            reader.SetStream(stream);

            return reader;
        }

        /// <summary>
        /// Puts a PooledBitReader back into the pool
        /// </summary>
        /// <param name="reader">The reader to put in the pool</param>
        public void PutBackInPool(PooledBitReader reader)
        {
            if (readers.Count < 64) readers.Enqueue(reader);
            else if (NetworkingManager.LogLevel <= LogLevel.Developer) NetworkLog.LogInfo("BitReaderPool already has 64 queued. Throwing to GC. Did you forget to dispose?");
        }
    }
}
