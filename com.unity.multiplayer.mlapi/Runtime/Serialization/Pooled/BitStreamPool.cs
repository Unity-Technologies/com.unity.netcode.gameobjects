using System;
using System.Collections.Generic;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledBitStreams
    /// </summary>
    public static class BitStreamPool
    {
        private static uint createdStreams = 0;
        private static readonly Queue<WeakReference> overflowStreams = new Queue<WeakReference>();
        private static readonly Queue<PooledBitStream> streams = new Queue<PooledBitStream>();

        private const uint MaxBitPoolStreams = 1024;
        private const uint MaxCreatedDelta = 768;

        /// <summary>
        /// Retrieves an expandable PooledBitStream from the pool
        /// </summary>
        /// <returns>An expandable PooledBitStream</returns>
        public static PooledBitStream GetStream()
        {
            if (streams.Count == 0)
            {
                if (overflowStreams.Count > 0)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogInfo("Retrieving PooledBitStream from overflow pool. Recent burst?");

                    object weakStream = null;
                    while (overflowStreams.Count > 0 && ((weakStream = overflowStreams.Dequeue().Target) == null)) ;

                    if (weakStream != null)
                    {
                        PooledBitStream strongStream = (PooledBitStream)weakStream;

                        strongStream.SetLength(0);
                        strongStream.Position = 0;

                        return strongStream;
                    }
                }
                
                if (createdStreams == MaxBitPoolStreams)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning(MaxBitPoolStreams.ToString() + " streams have been created. Did you forget to dispose?");
                }
                else if (createdStreams < MaxBitPoolStreams) createdStreams++;

                return new PooledBitStream();
            }

            PooledBitStream stream = streams.Dequeue();

            stream.SetLength(0);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Puts a PooledBitStream back into the pool
        /// </summary>
        /// <param name="stream">The stream to put in the pool</param>
        public static void PutBackInPool(PooledBitStream stream)
        {
            if (streams.Count > MaxCreatedDelta)
            {
                // The user just created lots of streams without returning them in between.
                // Streams are essentially byte array wrappers. This is valuable memory.
                // Thus we put this stream as a weak reference incase of another burst
                // But still leave it to GC
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogInfo("Putting PooledBitStream into overflow pool. Did you forget to dispose?");
                overflowStreams.Enqueue(new WeakReference(stream));
            }
            else
            {
                streams.Enqueue(stream);
            }
        }
    }
}
