namespace Unity.Netcode
{
    internal struct ServerLogMessage : INetworkMessage
    {
        public NetworkLog.LogType LogType;
        // It'd be lovely to be able to replace this with FixedString or NativeArray...
        // But it's not really practical. On the sending side, the user is likely to want
        // to work with strings and would need to convert, and on the receiving side,
        // we'd have to convert it to a string to be able to pass it to the log system.
        // So an allocation is unavoidable here on both sides.
        public string Message;


        public void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(LogType);
            BytePacker.WriteValuePacked(writer, Message);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (networkManager.IsServer && networkManager.NetworkConfig.EnableNetworkLogs)
            {
                reader.ReadValueSafe(out LogType);
                ByteUnpacker.ReadValuePacked(reader, out Message);
                return true;
            }

            return false;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var senderId = context.SenderId;

            networkManager.NetworkMetrics.TrackServerLogReceived(senderId, (uint)LogType, context.MessageSize);

            switch (LogType)
            {
                case NetworkLog.LogType.Info:
                    NetworkLog.LogInfoServerLocal(Message, senderId);
                    break;
                case NetworkLog.LogType.Warning:
                    NetworkLog.LogWarningServerLocal(Message, senderId);
                    break;
                case NetworkLog.LogType.Error:
                    NetworkLog.LogErrorServerLocal(Message, senderId);
                    break;
            }
        }
    }
}
