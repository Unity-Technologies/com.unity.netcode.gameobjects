namespace Unity.Netcode
{
    internal class ServerRpcTarget : BaseRpcTarget
    {
        private BaseRpcTarget m_UnderlyingTarget;

        public override void Dispose()
        {
            if (m_UnderlyingTarget != null)
            {
                m_UnderlyingTarget.Dispose();
                m_UnderlyingTarget = null;
            }
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
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
