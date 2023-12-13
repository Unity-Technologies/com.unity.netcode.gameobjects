using System.Collections.Generic;

namespace Unity.Netcode
{
    internal class RpcTargetGroup : BaseRpcTarget, IGroupRpcTarget
    {
        public BaseRpcTarget Target => this;

        internal List<BaseRpcTarget> Targets = new List<BaseRpcTarget>();

        private LocalSendRpcTarget m_LocalSendRpcTarget;
        private HashSet<ulong> m_Ids = new HashSet<ulong>();
        private Stack<DirectSendRpcTarget> m_TargetCache = new Stack<DirectSendRpcTarget>();

        public override void Dispose()
        {
            CheckLockBeforeDispose();
            foreach (var target in Targets)
            {
                target.Dispose();
            }
            foreach (var target in m_TargetCache)
            {
                target.Dispose();
            }
            m_LocalSendRpcTarget.Dispose();
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
            foreach (var target in Targets)
            {
                target.Send(behaviour, ref message, delivery, rpcParams);
            }
        }

        public void Add(ulong clientId)
        {
            if (!m_Ids.Contains(clientId))
            {
                m_Ids.Add(clientId);
                if (clientId == m_NetworkManager.LocalClientId)
                {
                    Targets.Add(m_LocalSendRpcTarget);
                }
                else
                {
                    if (m_TargetCache.Count == 0)
                    {
                        Targets.Add(new DirectSendRpcTarget(m_NetworkManager) { ClientId = clientId });
                    }
                    else
                    {
                        var target = m_TargetCache.Pop();
                        target.ClientId = clientId;
                        Targets.Add(target);
                    }
                }
            }
        }

        public void Clear()
        {
            m_Ids.Clear();
            foreach (var target in Targets)
            {
                if (target is DirectSendRpcTarget directSendRpcTarget)
                {
                    m_TargetCache.Push(directSendRpcTarget);
                }
            }
            Targets.Clear();
        }

        internal RpcTargetGroup(NetworkManager manager) : base(manager)
        {
            m_LocalSendRpcTarget = new LocalSendRpcTarget(manager);
        }
    }
}
