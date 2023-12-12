using System;

namespace Unity.Netcode
{
    public abstract class BaseRpcTarget : IDisposable
    {
        protected NetworkManager m_NetworkManager;
        private bool m_Locked;

        internal void Lock()
        {
            m_Locked = true;
        }

        internal void Unlock()
        {
            m_Locked = false;
        }

        internal BaseRpcTarget(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }

        protected void CheckLockBeforeDispose()
        {
            if (m_Locked)
            {
                throw new Exception($"RPC targets obtained through {nameof(RpcTargetUse)}.{RpcTargetUse.Temp} may not be disposed.");
            }
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
