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
        private static uint s_CreatedBuffers = 0;
        private static Queue<WeakReference> s_OverflowBuffers = new Queue<WeakReference>();
        private static Queue<PooledNetworkBuffer> s_Buffers = new Queue<PooledNetworkBuffer>();

        private const uint k_MaxBitPoolBuffers = 1024;
        private const uint k_MaxCreatedDelta = 768;


        /// <summary>
        /// Retrieves an expandable PooledNetworkBuffer from the pool
        /// </summary>
        /// <returns>An expandable PooledNetworkBuffer</returns>
        public static PooledNetworkBuffer GetBuffer()
        {
            if (s_Buffers.Count == 0)
            {
                if (s_OverflowBuffers.Count > 0)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogInfo($"Retrieving {nameof(PooledNetworkBuffer)} from overflow pool. Recent burst?");
                    }

                    object weakBuffer = null;
                    while (s_OverflowBuffers.Count > 0 && ((weakBuffer = s_OverflowBuffers.Dequeue().Target) == null)) ;

                    if (weakBuffer != null)
                    {
                        PooledNetworkBuffer strongBuffer = (PooledNetworkBuffer)weakBuffer;

                        strongBuffer.SetLength(0);
                        strongBuffer.Position = 0;

                        return strongBuffer;
                    }
                }

                if (s_CreatedBuffers == k_MaxBitPoolBuffers)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning($"{k_MaxBitPoolBuffers} buffers have been created. Did you forget to dispose?");
                }
                else if (s_CreatedBuffers < k_MaxBitPoolBuffers) s_CreatedBuffers++;

                return new PooledNetworkBuffer();
            }

            PooledNetworkBuffer buffer = s_Buffers.Dequeue();
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
            if (s_Buffers.Count > k_MaxCreatedDelta)
            {
                // The user just created lots of buffers without returning them in between.
                // Buffers are essentially byte array wrappers. This is valuable memory.
                // Thus we put this buffer as a weak reference incase of another burst
                // But still leave it to GC
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"Putting {nameof(PooledNetworkBuffer)} into overflow pool. Did you forget to dispose?");
                }

                s_OverflowBuffers.Enqueue(new WeakReference(buffer));
            }
            else
            {
                s_Buffers.Enqueue(buffer);
            }
        }
    }
}