using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledBitReaders
    /// </summary>
    public static class BitReaderPool
    {
        private static byte createdReaders = 0;
        private static readonly Queue<PooledBitReader> readers = new Queue<PooledBitReader>();

        /// <summary>
        /// Retrieves a PooledBitReader
        /// </summary>
        /// <param name="stream">The stream the reader should read from</param>
        /// <returns>A PooledBitReader</returns>
        public static PooledBitReader GetReader(Stream stream)
        {
            if (readers.Count == 0)
            {
                if (createdReaders == 254)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("255 readers have been created. Did you forget to dispose?");
                }
                else if (createdReaders < 255) createdReaders++;

                return new PooledBitReader(stream);
            }

            PooledBitReader reader = readers.Dequeue();
            reader.SetStream(stream);

            return reader;
        }

        /// <summary>
        /// Puts a PooledBitReader back into the pool
        /// </summary>
        /// <param name="reader">The reader to put in the pool</param>
        public static void PutBackInPool(PooledBitReader reader)
        {
            if (readers.Count < 64) readers.Enqueue(reader);
            else if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogInfo("BitReaderPool already has 64 queued. Throwing to GC. Did you forget to dispose?");
        }
    }
}
