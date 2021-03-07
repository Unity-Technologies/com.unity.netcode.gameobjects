using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// class containing PooledNetworkWriters
    /// </summary>
    public class NetworkWriterPool
    {
        private byte m_CreatedWriters = 0;
        private Queue<PooledNetworkWriter> m_Writers = new Queue<PooledNetworkWriter>();
        public NetworkManager NetworkManager { get; private set; }

        internal NetworkWriterPool(NetworkManager manager) { NetworkManager = manager; }

        /// <summary>
        /// Retrieves a PooledNetworkWriter
        /// </summary>
        /// <param name="stream">The stream the writer should write to</param>
        /// <returns>A PooledNetworkWriter</returns>
        public PooledNetworkWriter GetWriter(Stream stream)
        {
            if (m_Writers.Count == 0)
            {
                if (m_CreatedWriters == 254)
                {
                    if (NetworkManager.LogLevel <= LogLevel.Normal) NetworkManager.NetworkLog.LogWarning("255 writers have been created. Did you forget to dispose?");
                }
                else if (m_CreatedWriters < 255) m_CreatedWriters++;

                return new PooledNetworkWriter(this, stream);
            }

            PooledNetworkWriter writer = m_Writers.Dequeue();
            writer.Undispose();
            writer.SetStream(stream);

            return writer;
        }

        /// <summary>
        /// Puts a PooledNetworkWriter back into the pool
        /// </summary>
        /// <param name="writer">The writer to put in the pool</param>
        public void PutBackInPool(PooledNetworkWriter writer)
        {
            if (m_Writers.Count < 64) m_Writers.Enqueue(writer);
            else if (NetworkManager.LogLevel <= LogLevel.Developer)
            {
                NetworkManager.NetworkLog.LogInfo($"{nameof(NetworkWriterPool)} already has 64 queued. Throwing to GC. Did you forget to dispose?");
            }
        }
    }
}
