using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledNetworkWriters
    /// </summary>
    public static class NetworkWriterPool
    {
        private static byte createdWriters = 0;
        private static readonly Queue<PooledNetworkWriter> writers = new Queue<PooledNetworkWriter>();

        /// <summary>
        /// Retrieves a PooledNetworkWriter
        /// </summary>
        /// <param name="stream">The stream the writer should write to</param>
        /// <returns>A PooledNetworkWriter</returns>
        public static PooledNetworkWriter GetWriter(Stream stream)
        {
            if (writers.Count == 0)
            {
                if (createdWriters == 254)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("255 writers have been created. Did you forget to dispose?");
                }
                else if (createdWriters < 255) createdWriters++;

                return new PooledNetworkWriter(stream);
            }

            PooledNetworkWriter writer = writers.Dequeue();
            writer.SetStream(stream);

            return writer;
        }

        /// <summary>
        /// Puts a PooledNetworkWriter back into the pool
        /// </summary>
        /// <param name="writer">The writer to put in the pool</param>
        public static void PutBackInPool(PooledNetworkWriter writer)
        {
            if (writers.Count < 64) writers.Enqueue(writer);
            else if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo($"{nameof(NetworkWriterPool)} already has 64 queued. Throwing to GC. Did you forget to dispose?");
            }
        }
    }
}