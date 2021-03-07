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

        private NetworkWriterPool m_Parent;

        public NetworkManager NetworkManager => m_Parent.NetworkManager;

        internal PooledNetworkWriter(NetworkWriterPool parent, Stream stream) : base(stream) { m_Parent = parent; }

        internal void Undispose()
        {
            m_IsDisposed = false;
        }

        /// <summary>
        /// Returns the PooledNetworkWriter into the static NetworkWriterPool
        /// </summary>
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                m_Parent.PutBackInPool(this);
            }
            else
            {
                Debug.LogError("Writer is being disposed but thinks it is already disposed");
            }
        }
    }
}
