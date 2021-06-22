using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Networking.Transport.Relay
{
    public static class RelayMessageBind
    {
        private const byte k_ConnectionDataLength = 255;
        private const byte k_HMACLength = 32;
        public const int Length = RelayMessageHeader.Length + 1 + 2 + 1 + k_ConnectionDataLength + k_HMACLength; // Header + AcceptMode + Nonce + ConnectionDataLength + ConnectionData + HMAC;

        // public RelayMessageHeader Header;
        // public byte AcceptMode;
        // public ushort Nonce;
        // public byte ConnectionDataLength;
        // public fixed byte ConnectionData[k_ConnectionDataLength];
        // public fixed byte HMAC[k_HMACLength];

        public static unsafe void Write(DataStreamWriter writer, byte acceptMode, ushort nonce, byte* connectionDataPtr, byte* hmac)
        {
            var header = RelayMessageHeader.Create(RelayMessageType.Bind);

            writer.WriteBytes((byte*)&header, RelayMessageHeader.Length);
            writer.WriteByte(acceptMode);
            writer.WriteUShort(nonce);
            writer.WriteByte(k_ConnectionDataLength);
            writer.WriteBytes(connectionDataPtr, k_ConnectionDataLength);
            writer.WriteBytes(hmac, k_HMACLength);
        }
    }
}