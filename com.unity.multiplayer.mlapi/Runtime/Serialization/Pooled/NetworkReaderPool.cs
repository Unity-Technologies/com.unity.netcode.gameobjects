using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// class containing PooledNetworkReaders
    /// </summary>
    public class NetworkReaderPool
    {
        private byte m_CreatedReaders = 0;
        private Queue<PooledNetworkReader> m_Readers = new Queue<PooledNetworkReader>();
        public NetworkManager NetworkManager { get; private set; }

        internal NetworkReaderPool(NetworkManager manager) { NetworkManager = manager;  }

        /// <summary>
        /// Retrieves a PooledNetworkReader
        /// </summary>
        /// <param name="stream">The stream the reader should read from</param>
        /// <returns>A PooledNetworkReader</returns>
        public PooledNetworkReader GetReader(Stream stream)
        {
            if (m_Readers.Count == 0)
            {
                if (m_CreatedReaders == 254)
                {
                    if (NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkManager.NetworkLog.LogWarning("255 readers have been created. Did you forget to dispose?");
                }
                else if (m_CreatedReaders < 255) m_CreatedReaders++;

                return new PooledNetworkReader(this, stream);
            }

            PooledNetworkReader reader = m_Readers.Dequeue();

            reader.Undispose();
            reader.SetStream(stream);

            return reader;
        }

        /// <summary>
        /// Puts a PooledNetworkReader back into the pool
        /// </summary>
        /// <param name="reader">The reader to put in the pool</param>
        public void PutBackInPool(PooledNetworkReader reader)
        {
            if (m_Readers.Count < 64) m_Readers.Enqueue(reader);
            else if (NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkManager.NetworkLog.LogInfo($"{nameof(NetworkReaderPool)} already has 64 queued. Throwing to GC. Did you forget to dispose?");
            }
        }
    }
}
