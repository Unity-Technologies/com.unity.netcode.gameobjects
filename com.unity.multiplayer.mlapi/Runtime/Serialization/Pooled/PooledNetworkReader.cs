using System;
using System.IO;
using UnityEngine;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable NetworkReader that returns the Reader to the NetworkReaderPool when disposed
    /// </summary>
    public sealed class PooledNetworkReader : NetworkReader, IDisposable
    {
        private NetworkSerializer m_Serializer;
        public NetworkSerializer Serializer => m_Serializer ?? (m_Serializer = new NetworkSerializer(this));

        private bool m_IsDisposed = false;

        internal PooledNetworkReader(Stream stream) : base(stream) { }

        /// <summary>
        /// Gets a PooledNetworkReader from the static NetworkReaderPool
        /// </summary>
        /// <returns>PooledNetworkReader</returns>
        public static PooledNetworkReader Get(Stream stream)
        {
            var reader = NetworkReaderPool.GetReader(stream);
            reader.m_IsDisposed = false;
            return reader;
        }

        /// <summary>
        /// Returns the PooledNetworkReader into the static NetworkReaderPool
        /// </summary>
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                NetworkReaderPool.PutBackInPool(this);
            }
            else
            {
                Debug.LogWarning("Disposing reader that thinks it is already disposed!");
            }
        }
    }
}