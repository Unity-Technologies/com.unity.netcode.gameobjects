using System;
using System.IO;
using UnityEngine;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable NetworkWriter that returns the Writer to the NetworkWriterPool when disposed
    /// </summary>
    public sealed class PooledNetworkWriter : NetworkWriter, IDisposable
    {
        private NetworkSerializer m_Serializer;
        public NetworkSerializer Serializer => m_Serializer ?? (m_Serializer = new NetworkSerializer(this));

        private bool m_IsDisposed = false;

        internal PooledNetworkWriter(Stream stream) : base(stream) { }

        /// <summary>
        /// Gets a PooledNetworkWriter from the static NetworkWriterPool
        /// </summary>
        /// <returns>PooledNetworkWriter</returns>
        public static PooledNetworkWriter Get(Stream stream)
        {
            var writer = NetworkWriterPool.GetWriter(stream);
            writer.m_IsDisposed = false;
            return writer;
        }

        /// <summary>
        /// Returns the PooledNetworkWriter into the static NetworkWriterPool
        /// </summary>
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                NetworkWriterPool.PutBackInPool(this);
            }
            else
            {
                Debug.LogError("Writer is being disposed but thinks it is already disposed");
            }
        }
    }
}