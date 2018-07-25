using System;
using System.Collections.Generic;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    /// <summary>
    /// Static class containing PooledBitStreams
    /// </summary>
    public static class BitStreamPool
    {
        private static readonly Queue<PooledBitStream> streams = new Queue<PooledBitStream>();

        /// <summary>
        /// Retrieves an expandable PooledBitStream from the pool
        /// </summary>
        /// <returns>An expandable PooledBitStream</returns>
        public static PooledBitStream GetStream()
        {
            if (streams.Count == 0) return new PooledBitStream();

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
            streams.Enqueue(stream);
        }
    }

    /// <summary>
    /// Disposable BitStream that returns the Stream to the BitStreamPool when disposed
    /// </summary>
    public sealed class PooledBitStream : BitStream, IDisposable
    {
        /// <summary>
        /// Gets a PooledBitStream from the static BitStreamPool
        /// </summary>
        /// <returns></returns>
        public static PooledBitStream Get()
        {
            return BitStreamPool.GetStream();
        }

        /// <summary>
        /// Returns the PooledBitStream into the static BitStreamPool
        /// </summary>
        public new void Dispose()
        {
            BitStreamPool.PutBackInPool(this);
        }
    }
}
