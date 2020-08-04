using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledBitWriters
    /// </summary>
    public static class BitWriterPool
    {
        private static byte createdWriters = 0;
        private static readonly Queue<PooledBitWriter> writers = new Queue<PooledBitWriter>();

        /// <summary>
        /// Retrieves a PooledBitWriter
        /// </summary>
        /// <param name="stream">The stream the writer should write to</param>
        /// <returns>A PooledBitWriter</returns>
        public static PooledBitWriter GetWriter(Stream stream)
        {
            if (writers.Count == 0)
            {
                if (createdWriters == 254)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("255 writers have been created. Did you forget to dispose?");
                }
                else if (createdWriters < 255) createdWriters++;

                return new PooledBitWriter(stream);
            }

            PooledBitWriter writer = writers.Dequeue();
            writer.SetStream(stream);

            return writer;
        }

        /// <summary>
        /// Puts a PooledBitWriter back into the pool
        /// </summary>
        /// <param name="writer">The writer to put in the pool</param>
        public static void PutBackInPool(PooledBitWriter writer)
        {
            if (writers.Count < 64) writers.Enqueue(writer);
            else if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogInfo("BitWriterPool already has 64 queued. Throwing to GC. Did you forget to dispose?");
        }
    }
}
