using System.Runtime.InteropServices;
using MLAPI.Transports;

namespace Unity.Networking.Transport.Relay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RelayMessageRelay
    {
        public const int Length = RelayMessageHeader.Length + RelayAllocationId.k_Length * 2 + 2; // Header + FromAllocationId + ToAllocationId + DataLength

        public RelayMessageHeader Header;

        public RelayAllocationId FromAllocationId;
        public RelayAllocationId ToAllocationId;
        public ushort DataLength;

        public static RelayMessageRelay Create(RelayAllocationId fromAllocationId, RelayAllocationId toAllocationId, ushort dataLength)
        {
            return new RelayMessageRelay
            {
                Header = RelayMessageHeader.Create(RelayMessageType.Relay),
                FromAllocationId = fromAllocationId,
                ToAllocationId = toAllocationId,
                DataLength = RelayNetworkProtocol.SwitchEndianness(dataLength),
            };
        }
    }
}
