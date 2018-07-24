using System;
using System.Collections.Generic;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    public static class BitStreamPool
    {
        private static readonly Queue<PooledBitStream> streams = new Queue<PooledBitStream>();

        public static PooledBitStream GetStream()
        {
            if (streams.Count == 0) return new PooledBitStream();

            PooledBitStream stream = streams.Dequeue();
            stream.SetLength(0);
            stream.Position = 0;

            return stream;
        }

        public static void PutBackInPool(PooledBitStream stream)
        {
            streams.Enqueue(stream);
        }
    }

    public sealed class PooledBitStream : BitStream, IDisposable
    {
        public static PooledBitStream Get()
        {
            return BitStreamPool.GetStream();
        }

        public new void Dispose()
        {
            BitStreamPool.PutBackInPool(this);
        }
    }
}
