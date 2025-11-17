using Unity.Collections;
namespace Unity.Netcode
{
    internal class ServerRpcTarget : BaseRpcTarget
    {
        protected BaseRpcTarget m_UnderlyingTarget;
        protected ProxyRpcTarget m_ProxyRpcTarget;

        public override void Dispose()
        {
            if (m_UnderlyingTarget != null)
            {
                m_UnderlyingTarget.Dispose();
                m_UnderlyingTarget = null;
            }

            if (m_ProxyRpcTarget != null)
            {
                m_ProxyRpcTarget.Dispose();
                m_ProxyRpcTarget = null;
            }
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
            // For distributed authority the "server" is considered the authority of the object
            if (behaviour.NetworkManager.DistributedAuthorityMode && behaviour.NetworkManager.CMBServiceConnection)
            {
                // If the local instance is the owner, then invoke the message locally on this behaviour
                if (behaviour.IsOwner)
                {
                    var context = new NetworkContext
                    {
                        SenderId = m_NetworkManager.LocalClientId,
                        Timestamp = m_NetworkManager.RealTimeProvider.RealTimeSinceStartup,
                        SystemOwner = m_NetworkManager,
                        // header information isn't valid since it's not a real message.
                        // RpcMessage doesn't access this stuff so it's just left empty.
                        Header = new NetworkMessageHeader(),
                        SerializedHeaderSize = 0,
                        MessageSize = 0
                    };
                    using var tempBuffer = new FastBufferReader(message.WriteBuffer, Allocator.None);
                    message.ReadBuffer = tempBuffer;
                    message.Handle(ref context);
#if MULTIPLAYER_TOOLS && (DEVELOPMENT_BUILD || UNITY_EDITOR || UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE)
                    // Local invocation sends to self
                    behaviour.TrackRpcMetricsSend(m_NetworkManager.LocalClientId, ref message, tempBuffer.Length);
#endif
                }
                else // Otherwise, send a proxied message to the owner of the object
                {
                    if (m_ProxyRpcTarget == null)
                    {
                        m_ProxyRpcTarget = new ProxyRpcTarget(behaviour.OwnerClientId, m_NetworkManager);
                    }
                    else
                    {
                        m_ProxyRpcTarget.SetClientId(behaviour.OwnerClientId);
                    }
                    m_ProxyRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
                }
                return;
            }

            if (m_UnderlyingTarget == null)
            {
                if (behaviour.NetworkManager.IsServer)
                {
                    m_UnderlyingTarget = new LocalSendRpcTarget(m_NetworkManager);
                }
                else
                {
                    m_UnderlyingTarget = new DirectSendRpcTarget(m_NetworkManager) { ClientId = NetworkManager.ServerClientId };
                }
            }
            m_UnderlyingTarget.Send(behaviour, ref message, delivery, rpcParams);
        }

        internal ServerRpcTarget(NetworkManager manager) : base(manager)
        {
        }
    }
}
