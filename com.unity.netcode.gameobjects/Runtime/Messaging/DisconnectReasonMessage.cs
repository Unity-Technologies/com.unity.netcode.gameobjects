using UnityEngine;

namespace Unity.Netcode
{
    internal struct DisconnectReasonMessage : INetworkMessage
    {
        public string Reason;

        public void Serialize(FastBufferWriter writer)
        {
            string reasonSent = Reason;
            if (reasonSent == null)
            {
                reasonSent = string.Empty;
            }

            if (writer.TryBeginWrite(sizeof(int) + FastBufferWriter.GetWriteSize(reasonSent)))
            {
                writer.WriteValue(reasonSent.Length);
                writer.WriteValue(reasonSent);
            }
            else
            {
                writer.TryBeginWrite(sizeof(int) + FastBufferWriter.GetWriteSize(string.Empty));
                writer.WriteValue(0);
                writer.WriteValue(string.Empty);
                Debug.LogWarning(
                    "Disconnect reason didn't fit. Disconnected without sending a reason. Consider shortening the reason string.");
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            int size;
            reader.TryBeginRead(sizeof(int));
            reader.ReadValue(out size);
            Reason = new string(' ', size);
            reader.TryBeginRead(FastBufferWriter.GetWriteSize(Reason));
            reader.ReadValue(out Reason);

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).DisconnectReason = Reason;
        }
    };
}
