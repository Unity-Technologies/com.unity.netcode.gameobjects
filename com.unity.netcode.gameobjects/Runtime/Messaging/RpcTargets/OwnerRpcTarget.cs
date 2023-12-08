namespace Unity.Netcode
{
    internal class OwnerRpcTarget : BaseRpcTarget
    {
        private IIndividualRpcTarget m_UnderlyingTarget;
        private LocalSendRpcTarget m_LocalRpcTarget;
        private ServerRpcTarget m_ServerRpcTarget;

        public override void Dispose()
        {
            m_LocalRpcTarget.Dispose();
            if (m_UnderlyingTarget != null)
            {
                m_UnderlyingTarget.Target.Dispose();
                m_UnderlyingTarget = null;
            }
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
            if (behaviour.OwnerClientId == behaviour.NetworkManager.LocalClientId)
            {
                m_LocalRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
                return;
            }

            if (behaviour.OwnerClientId == NetworkManager.ServerClientId)
            {
                m_ServerRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
                return;
            }

            if (m_UnderlyingTarget == null)
            {
                if (behaviour.NetworkManager.IsServer)
                {
                    m_UnderlyingTarget = new DirectSendRpcTarget(m_NetworkManager);
                }
                else
                {
                    m_UnderlyingTarget = new ProxyRpcTarget(behaviour.OwnerClientId, m_NetworkManager);
                }
            }
            m_UnderlyingTarget.SetClientId(behaviour.OwnerClientId);
            m_UnderlyingTarget.Target.Send(behaviour, ref message, delivery, rpcParams);
        }

        internal OwnerRpcTarget(NetworkManager manager) : base(manager)
        {
            m_LocalRpcTarget = new LocalSendRpcTarget(manager);
            m_ServerRpcTarget = new ServerRpcTarget(manager);
        }
    }
}
