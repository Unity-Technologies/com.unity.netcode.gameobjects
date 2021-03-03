using System;
using System.IO;

namespace MLAPI.Serialization.Pooled
{
    /// <summary>
    /// Disposable NetworkWriter that returns the Writer to the NetworkWriterPool when disposed
    /// </summary>
    public sealed class PooledNetworkWriter : NetworkWriter, IDisposable
    {
        private NetworkSerializer m_Serializer;
        public NetworkSerializer Serializer => m_Serializer ?? (m_Serializer = new NetworkSerializer(this));

        private bool isDisposed = false;

        internal PooledNetworkWriter(Stream stream) : base(stream) { }

        /// <summary>
        /// Gets a PooledNetworkWriter from the static NetworkWriterPool
        /// </summary>
        /// <returns>PooledNetworkWriter</returns>
        public static PooledNetworkWriter Get(Stream stream)
        {
            PooledNetworkWriter writer = NetworkWriterPool.GetWriter(stream);
            writer.isDisposed = false;
            return writer;
        }

        /// <summary>
        /// Returns the PooledNetworkWriter into the static NetworkWriterPool
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                NetworkWriterPool.PutBackInPool(this);
            }
            else
            {
                UnityEngine.Debug.LogError("Writer is being disposed but thinks it is already disposed");
            }
        }
    }
}