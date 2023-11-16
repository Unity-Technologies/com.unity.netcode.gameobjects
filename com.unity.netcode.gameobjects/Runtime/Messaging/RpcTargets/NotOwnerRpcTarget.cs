namespace Unity.Netcode
{
    internal class NotOwnerRpcTarget : BaseRpcTarget
    {
        private IGroupRpcTarget m_GroupSendTarget;
        private ServerRpcTarget m_ServerRpcTarget;
        private LocalSendRpcTarget m_LocalSendRpcTarget;

        public override void Dispose()
        {
            m_ServerRpcTarget.Dispose();
            m_LocalSendRpcTarget.Dispose();
            if (m_GroupSendTarget != null)
            {
                m_GroupSendTarget.Target.Dispose();
                m_GroupSendTarget = null;
            }
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
            if (m_GroupSendTarget == null)
            {
                if (behaviour.IsServer)
                {
                    m_GroupSendTarget = new RpcTargetGroup(m_NetworkManager);
                }
                else
                {
                    m_GroupSendTarget = new ProxyRpcTargetGroup(m_NetworkManager);
                }
            }
            m_GroupSendTarget.Clear();

            if (behaviour.IsServer)
            {
                foreach (var clientId in behaviour.NetworkObject.Observers)
                {
                    if (clientId == behaviour.OwnerClientId)
                    {
                        continue;
                    }
                    if (clientId == behaviour.NetworkManager.LocalClientId)
                    {
                        m_LocalSendRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
                        continue;
                    }

                    m_GroupSendTarget.Add(clientId);
                }
            }
            else
            {
                foreach (var clientId in m_NetworkManager.ConnectedClientsIds)
                {
                    if (clientId == behaviour.OwnerClientId)
                    {
                        continue;
                    }
                    if (clientId == behaviour.NetworkManager.LocalClientId)
                    {
                        m_LocalSendRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
                        continue;
                    }

                    m_GroupSendTarget.Add(clientId);
                }
            }

            m_GroupSendTarget.Target.Send(behaviour, ref message, delivery, rpcParams);
            if (behaviour.OwnerClientId != NetworkManager.ServerClientId)
            {
                m_ServerRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
            }
        }

        internal NotOwnerRpcTarget(NetworkManager manager) : base(manager)
        {
            m_ServerRpcTarget = new ServerRpcTarget(manager);
            m_LocalSendRpcTarget = new LocalSendRpcTarget(manager);
        }
    }
}
