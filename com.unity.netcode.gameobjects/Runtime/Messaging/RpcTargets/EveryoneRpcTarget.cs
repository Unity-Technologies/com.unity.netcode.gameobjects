namespace Unity.Netcode
{
    internal class EveryoneRpcTarget : BaseRpcTarget
    {
        private NotServerRpcTarget m_NotServerRpcTarget;
        private ServerRpcTarget m_ServerRpcTarget;

        public override void Dispose()
        {
            m_NotServerRpcTarget.Dispose();
            m_ServerRpcTarget.Dispose();
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
            m_NotServerRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
            m_ServerRpcTarget.Send(behaviour, ref message, delivery, rpcParams);
        }

        internal EveryoneRpcTarget(NetworkManager manager) : base(manager)
        {
            m_NotServerRpcTarget = new NotServerRpcTarget(manager);
            m_ServerRpcTarget = new ServerRpcTarget(manager);
        }
    }
}
