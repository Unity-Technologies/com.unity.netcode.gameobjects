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

        private NetworkReaderPool m_Parent;

        private bool m_IsDisposed = false;

        internal PooledNetworkReader(NetworkReaderPool parent, Stream stream) : base(parent.NetworkManager, stream) { m_Parent = parent; }

        internal void Undispose() { m_IsDisposed = false;  }

        /// <summary>
        /// Returns the PooledNetworkReader into the static NetworkReaderPool
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
                Debug.LogWarning("Disposing reader that thinks it is already disposed!");
            }
        }
    }
}
