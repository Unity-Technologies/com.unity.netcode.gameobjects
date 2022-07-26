namespace Unity.Netcode
{
    // Todo: Would be lovely to get this one nicely formatted with all the data it sends in the struct
    // like most of the other messages when we have some more time and can come back and refactor this.
    internal struct SceneEventMessage : INetworkMessage
    {
        public SceneEventData EventData;

        private FastBufferReader m_ReceivedData;

        public void Serialize(FastBufferWriter writer)
        {
            EventData.Serialize(writer);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            m_ReceivedData = reader;
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).SceneManager.HandleSceneEvent(context.SenderId, m_ReceivedData);
        }
    }
}
