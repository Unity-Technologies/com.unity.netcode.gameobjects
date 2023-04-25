namespace Unity.Netcode
{
    internal struct DisconnectReasonMessage : INetworkMessage
    {
        public string Reason;

        public int Version => 0;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            string reasonSent = Reason ?? string.Empty;

            // Since we don't send a ConnectionApprovedMessage, the version for this message is encded with the message itself.
            // However, note that we HAVE received a ConnectionRequestMessage, so we DO have a valid targetVersion on this side of things.
            // We just have to make sure the receiving side knows what version we sent it, since whoever has the higher version number is responsible for versioning and they may be the one with the higher version number.
            BytePacker.WriteValueBitPacked(writer, Version);

            if (writer.TryBeginWrite(FastBufferWriter.GetWriteSize(reasonSent)))
            {
                writer.WriteValue(reasonSent);
            }
            else
            {
                writer.WriteValueSafe(string.Empty);
                NetworkLog.LogWarning("Disconnect reason didn't fit. Disconnected without sending a reason. Consider shortening the reason string.");
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            // Since we don't get a ConnectionApprovedMessage, the version for this message is encded with the message itself.
            // This will override what we got from MessageManager... which will always be 0 here.
            ByteUnpacker.ReadValueBitPacked(reader, out receivedMessageVersion);
            reader.ReadValueSafe(out Reason);
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).ConnectionManager.DisconnectReason = Reason;
        }
    };
}
