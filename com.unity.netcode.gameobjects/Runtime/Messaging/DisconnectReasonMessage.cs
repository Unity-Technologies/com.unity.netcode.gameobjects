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
                writer.WriteValueSafe(reasonSent);
            }
            else
            {
                writer.WriteValueSafe(string.Empty);
                Debug.LogWarning(
                    "Disconnect reason didn't fit. Disconnected without sending a reason. Consider shortening the reason string.");
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            reader.ReadValueSafe(out Reason);
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).DisconnectReason = Reason;
        }
    };
}
