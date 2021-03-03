using System;
using System.IO;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable NetworkReader that returns the Reader to the NetworkReaderPool when disposed
    /// </summary>
    public sealed class PooledNetworkReader : NetworkReader, IDisposable
    {
        private NetworkSerializer m_Serializer;
        public NetworkSerializer Serializer => m_Serializer ?? (m_Serializer = new NetworkSerializer(this));

        private bool isDisposed = false;

        internal PooledNetworkReader(Stream stream) : base(stream) { }

        /// <summary>
        /// Gets a PooledNetworkReader from the static NetworkReaderPool
        /// </summary>
        /// <returns>PooledNetworkReader</returns>
        public static PooledNetworkReader Get(Stream stream)
        {
            PooledNetworkReader reader = NetworkReaderPool.GetReader(stream);
            reader.isDisposed = false;
            return reader;
        }

        /// <summary>
        /// Returns the PooledNetworkReader into the static NetworkReaderPool
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                NetworkReaderPool.PutBackInPool(this);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Disposing reader that thinks it is already disposed!");
            }
        }
    }
}