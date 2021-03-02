using System;
using System.Collections.Generic;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledNetworkBuffers
    /// </summary>
    public static class NetworkBufferPool
    {
        private static uint createdStreams = 0;
        private static readonly Queue<WeakReference> overflowStreams = new Queue<WeakReference>();
        private static readonly Queue<PooledNetworkBuffer> streams = new Queue<PooledNetworkBuffer>();

        private const uint MaxBitPoolStreams = 1024;
        private const uint MaxCreatedDelta = 768;


        /// <summary>
        /// Retrieves an expandable PooledNetworkBuffer from the pool
        /// </summary>
        /// <returns>An expandable PooledNetworkBuffer</returns>
        public static PooledNetworkBuffer GetStream()
        {
            if (streams.Count == 0)
            {
                if (overflowStreams.Count > 0)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogInfo($"Retrieving {nameof(PooledNetworkBuffer)} from overflow pool. Recent burst?");
                    }

                    object weakStream = null;
                    while (overflowStreams.Count > 0 && ((weakStream = overflowStreams.Dequeue().Target) == null)) ;

                    if (weakStream != null)
                    {
                        PooledNetworkBuffer strongBuffer = (PooledNetworkBuffer)weakStream;

                        strongBuffer.SetLength(0);
                        strongBuffer.Position = 0;

                        return strongBuffer;
                    }
                }

                if (createdStreams == MaxBitPoolStreams)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning($"{MaxBitPoolStreams} streams have been created. Did you forget to dispose?");
                }
                else if (createdStreams < MaxBitPoolStreams) createdStreams++;

                return new PooledNetworkBuffer();
            }

            PooledNetworkBuffer buffer = streams.Dequeue();

            buffer.SetLength(0);
            buffer.Position = 0;

            return buffer;
        }

        /// <summary>
        /// Puts a PooledNetworkBuffer back into the pool
        /// </summary>
        /// <param name="buffer">The buffer to put in the pool</param>
        public static void PutBackInPool(PooledNetworkBuffer buffer)
        {
            if (streams.Count > MaxCreatedDelta)
            {
                // The user just created lots of streams without returning them in between.
                // Streams are essentially byte array wrappers. This is valuable memory.
                // Thus we put this stream as a weak reference incase of another burst
                // But still leave it to GC
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"Putting {nameof(PooledNetworkBuffer)} into overflow pool. Did you forget to dispose?");
                }

                overflowStreams.Enqueue(new WeakReference(buffer));
            }
            else
            {
                streams.Enqueue(buffer);
            }
        }
    }
}