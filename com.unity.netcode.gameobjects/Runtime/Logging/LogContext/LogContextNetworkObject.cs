namespace Unity.Netcode.Logging
{
    internal readonly struct LogContextNetworkObject : ILogContext
    {
        private readonly NetworkObject m_NetworkObject;

        public LogContextNetworkObject(NetworkObject networkObject)
        {
            m_NetworkObject = networkObject;
        }

        public void AppendTo(LogBuilder builder)
        {
            builder.AppendTag(m_NetworkObject.name);
            if (m_NetworkObject.IsSpawned)
            {
                builder.AppendInfo(nameof(NetworkObject.NetworkObjectId), m_NetworkObject.NetworkObjectId);
            }
        }
    }
}
