using System;
using System.Linq;

namespace Unity.Netcode
{
    /// <summary>
    /// A context used for building an internal command.
    /// </summary>
    internal struct InternalCommandContext : IDisposable
    {
        public PooledNetworkWriter NetworkWriter;

        private ulong[] m_ClientIds;
        private NetworkUpdateStage m_UpdateStage;
        private MessageQueueContainer m_Owner;

        public InternalCommandContext(PooledNetworkWriter writer, ulong[] clientIds, NetworkUpdateStage updateStage, MessageQueueContainer owner)
        {
            NetworkWriter = writer;
            m_ClientIds = clientIds;
            m_UpdateStage = updateStage;
            m_Owner = owner;
        }

        public void Dispose()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            if (m_Owner.NetworkManager.IsHost)
            {
                var containsServerClientId = m_ClientIds.Contains(m_Owner.NetworkManager.ServerClientId);
                if (containsServerClientId && m_ClientIds.Length == 1)
                {
                    m_Owner.EndAddQueueItemToFrame(NetworkWriter, MessageQueueHistoryFrame.QueueFrameType.Inbound, m_UpdateStage);
                    return;
                }
            }

            m_Owner.EndAddQueueItemToFrame(NetworkWriter, MessageQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
        }
    }
}
