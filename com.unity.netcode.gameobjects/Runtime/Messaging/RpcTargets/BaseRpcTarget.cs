namespace Unity.Netcode
{
    public abstract class BaseRpcTarget
    {
        protected NetworkManager m_NetworkManager;

        internal BaseRpcTarget(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }

        public abstract void Dispose();

        internal abstract void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams);

        private protected void SendMessageToClient(NetworkBehaviour behaviour, ulong clientId, ref RpcMessage message, NetworkDelivery delivery)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var size =
#endif
                behaviour.NetworkManager.MessageManager.SendMessage(ref message, delivery, clientId);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (NetworkBehaviour.__rpc_name_table[behaviour.GetType()].TryGetValue(message.Metadata.NetworkRpcMethodId, out var rpcMethodName))
            {
                behaviour.NetworkManager.NetworkMetrics.TrackRpcSent(
                    clientId,
                    behaviour.NetworkObject,
                    rpcMethodName,
                    behaviour.__getTypeName(),
                    size);
            }
#endif
        }
    }
}
