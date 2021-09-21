namespace Unity.Netcode
{
    // Todo: Would be lovely to get this one nicely formatted with all the data it sends in the struct
    // like most of the other messages when we have some more time and can come back and refactor this.
    internal struct SceneEventMessage : INetworkMessage
    {
        public SceneEventData EventData;

        public void Serialize(FastBufferWriter writer)
        {
            EventData.Serialize(writer);
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).SceneManager.HandleSceneEvent(context.SenderId, reader);
        }
    }
}
