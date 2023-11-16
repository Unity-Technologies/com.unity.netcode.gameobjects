namespace Unity.Netcode
{
    internal class ClientsAndHostRpcTarget : BaseRpcTarget
    {
        private BaseRpcTarget m_UnderlyingTarget;

        public override void Dispose()
        {
            m_UnderlyingTarget = null;
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
            if (m_UnderlyingTarget == null)
            {
                if (behaviour.NetworkManager.ConnectionManager.ConnectedClientIds.Contains(NetworkManager.ServerClientId))
                {
                    m_UnderlyingTarget = behaviour.RpcTarget.Everyone;
                }
                else
                {
                    m_UnderlyingTarget = behaviour.RpcTarget.NotServer;
                }
            }

            m_UnderlyingTarget.Send(behaviour, ref message, delivery, rpcParams);
        }

        internal ClientsAndHostRpcTarget(NetworkManager manager) : base(manager)
        {
        }
    }
}
