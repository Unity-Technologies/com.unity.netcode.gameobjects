using System.Runtime.InteropServices;
using MLAPI.Transports;

namespace Unity.Networking.Transport.Relay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RelayMessagePing
    {
        public const int Length = RelayMessageHeader.Length + RelayAllocationId.k_Length + 2; // Header + FromAllocationId + SequenceNumber

        public RelayMessageHeader Header;
        public RelayAllocationId FromAllocationId;
        public ushort SequenceNumber;

        public static RelayMessagePing Create(RelayAllocationId fromAllocationId, ushort dataLength)
        {
            return new RelayMessagePing {
                Header = RelayMessageHeader.Create(RelayMessageType.Ping),
                FromAllocationId = fromAllocationId,
                SequenceNumber = 1
            };
        }
    }
}
