using System;
using System.Collections.Generic;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Static class containing PooledNetworkStreams
    /// </summary>
    public static class NetworkStreamPool
    {
        private static uint s_CreatedStreams = 0;
        private static readonly Queue<WeakReference> k_OverflowStreams = new Queue<WeakReference>();
        private static readonly Queue<PooledNetworkStream> k_Streams = new Queue<PooledNetworkStream>();

        private const uint k_MaxBitPoolStreams = 1024;
        private const uint k_MaxCreatedDelta = 768;


        /// <summary>
        /// Retrieves an expandable PooledNetworkStream from the pool
        /// </summary>
        /// <returns>An expandable PooledNetworkStream</returns>
        public static PooledNetworkStream GetStream()
        {
            if (k_Streams.Count == 0)
            {
                if (k_OverflowStreams.Count > 0)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogInfo($"Retrieving {nameof(PooledNetworkStream)} from overflow pool. Recent burst?");
                    }

                    object weakStream = null;
                    while (k_OverflowStreams.Count > 0 && ((weakStream = k_OverflowStreams.Dequeue().Target) == null)) ;

                    if (weakStream != null)
                    {
                        PooledNetworkStream strongStream = (PooledNetworkStream)weakStream;

                        strongStream.SetLength(0);
                        strongStream.Position = 0;

                        return strongStream;
                    }
                }

                if (s_CreatedStreams == k_MaxBitPoolStreams)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning($"{k_MaxBitPoolStreams} streams have been created. Did you forget to dispose?");
                }
                else if (s_CreatedStreams < k_MaxBitPoolStreams) s_CreatedStreams++;

                return new PooledNetworkStream();
            }

            PooledNetworkStream stream = k_Streams.Dequeue();

            stream.SetLength(0);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Puts a PooledNetworkStream back into the pool
        /// </summary>
        /// <param name="stream">The stream to put in the pool</param>
        public static void PutBackInPool(PooledNetworkStream stream)
        {
            if (k_Streams.Count > k_MaxCreatedDelta)
            {
                // The user just created lots of streams without returning them in between.
                // Streams are essentially byte array wrappers. This is valuable memory.
                // Thus we put this stream as a weak reference incase of another burst
                // But still leave it to GC
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"Putting {nameof(PooledNetworkStream)} into overflow pool. Did you forget to dispose?");
                }

                k_OverflowStreams.Enqueue(new WeakReference(stream));
            }
            else
            {
                k_Streams.Enqueue(stream);
            }
        }
    }
}