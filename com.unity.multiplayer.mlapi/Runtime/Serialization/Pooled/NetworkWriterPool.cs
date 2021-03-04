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
        private static byte s_CreatedWriters = 0;
        private static Queue<PooledNetworkWriter> s_Writers = new Queue<PooledNetworkWriter>();

        /// <summary>
        /// Retrieves a PooledNetworkWriter
        /// </summary>
        /// <param name="stream">The stream the writer should write to</param>
        /// <returns>A PooledNetworkWriter</returns>
        public static PooledNetworkWriter GetWriter(Stream stream)
        {
            if (s_Writers.Count == 0)
            {
                if (s_CreatedWriters == 254)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("255 writers have been created. Did you forget to dispose?");
                }
                else if (s_CreatedWriters < 255) s_CreatedWriters++;

                return new PooledNetworkWriter(stream);
            }

            PooledNetworkWriter writer = s_Writers.Dequeue();
            writer.SetStream(stream);

            return writer;
        }

        /// <summary>
        /// Puts a PooledNetworkWriter back into the pool
        /// </summary>
        /// <param name="writer">The writer to put in the pool</param>
        public static void PutBackInPool(PooledNetworkWriter writer)
        {
            if (s_Writers.Count < 64) s_Writers.Enqueue(writer);
            else if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo($"{nameof(NetworkWriterPool)} already has 64 queued. Throwing to GC. Did you forget to dispose?");
            }
        }
    }
}