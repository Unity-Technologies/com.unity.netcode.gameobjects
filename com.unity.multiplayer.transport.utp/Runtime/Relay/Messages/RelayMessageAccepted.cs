using System.Runtime.InteropServices;
using MLAPI.Transports;

namespace Unity.Networking.Transport.Relay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RelayMessageAccepted
    {
        public const int Length = RelayMessageHeader.Length + RelayAllocationId.k_Length * 2; // Header + FromAllocationId + ToAllocationId

        public RelayMessageHeader Header;

        public RelayAllocationId FromAllocationId;
        public RelayAllocationId ToAllocationId;

        public static RelayMessageAccepted Create(RelayAllocationId fromAllocationId, RelayAllocationId toAllocationId, ushort dataLength)
        {
            return new RelayMessageAccepted
            {
                Header = RelayMessageHeader.Create(RelayMessageType.Accepted),
                FromAllocationId = fromAllocationId,
                ToAllocationId = toAllocationId
            };
        }
    }
}
