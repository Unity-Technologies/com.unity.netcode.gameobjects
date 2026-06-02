namespace Unity.Netcode
{
    // Todo: Would be lovely to get this one nicely formatted with all the data it sends in the struct
    // like most of the other messages when we have some more time and can come back and refactor this.
    internal struct SceneEventMessage : INetworkMessage
    {
        public int Version => 0;

        public SceneEventData EventData;

        private const string k_Name = "SceneEventMessage";

        private FastBufferReader m_ReceivedData;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            EventData.Serialize(writer);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
#if UNIFIED_NETCODE
            // Defer this message if the OnGhostSpawned trigger is still being processed. This is because the scene event message can be sent
            // as part of the ghost spawning process and we want to make sure that all ghost spawning related messages are processed before we
            // process this one. This is to avoid any potential issues with the order of message processing and to ensure that all ghost
            // related messages are processed before we process this one.
            if (networkManager.DeferredMessageManager.HasAnyOfTrigger(IDeferredNetworkMessageManager.TriggerType
                    .OnGhostSpawned))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnOtherTriggerFinishedProcessing, (ulong)IDeferredNetworkMessageManager.TriggerType.OnGhostSpawned, reader, ref context, k_Name);
                return false;
            }
#endif
            m_ReceivedData = reader;
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            networkManager.SceneManager.HandleSceneEvent(context.SenderId, m_ReceivedData);
        }
    }
}
