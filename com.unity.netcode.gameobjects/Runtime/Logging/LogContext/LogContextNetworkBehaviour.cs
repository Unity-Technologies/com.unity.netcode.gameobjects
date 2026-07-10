namespace Unity.Netcode.Logging
{
    internal readonly struct LogContextNetworkBehaviour : ILogContext
    {
        private readonly NetworkBehaviour m_NetworkBehaviour;

        public LogContextNetworkBehaviour(NetworkBehaviour networkBehaviour)
        {
            m_NetworkBehaviour = networkBehaviour;
        }

        public void AppendTo(LogBuilder builder)
        {
            builder.AppendTag(m_NetworkBehaviour.gameObject.name);
            if (m_NetworkBehaviour.IsSpawned)
            {
                builder.AppendInfo(nameof(NetworkBehaviour.NetworkBehaviourId), m_NetworkBehaviour.NetworkBehaviourId);
            }
        }
    }
}
