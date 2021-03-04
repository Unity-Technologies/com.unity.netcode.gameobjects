using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledNetworkReaders
    /// </summary>
    public static class NetworkReaderPool
    {
        private static byte s_CreatedReaders = 0;
        private static Queue<PooledNetworkReader> s_Readers = new Queue<PooledNetworkReader>();

        /// <summary>
        /// Retrieves a PooledNetworkReader
        /// </summary>
        /// <param name="stream">The stream the reader should read from</param>
        /// <returns>A PooledNetworkReader</returns>
        public static PooledNetworkReader GetReader(Stream stream)
        {
            if (s_Readers.Count == 0)
            {
                if (s_CreatedReaders == 254)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("255 readers have been created. Did you forget to dispose?");
                }
                else if (s_CreatedReaders < 255) s_CreatedReaders++;

                return new PooledNetworkReader(stream);
            }

            PooledNetworkReader reader = s_Readers.Dequeue();
            reader.SetStream(stream);

            return reader;
        }

        /// <summary>
        /// Puts a PooledNetworkReader back into the pool
        /// </summary>
        /// <param name="reader">The reader to put in the pool</param>
        public static void PutBackInPool(PooledNetworkReader reader)
        {
            if (s_Readers.Count < 64) s_Readers.Enqueue(reader);
            else if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo($"{nameof(NetworkReaderPool)} already has 64 queued. Throwing to GC. Did you forget to dispose?");
            }
        }
    }
}