using System.Runtime.InteropServices;
using MLAPI.Transports;

namespace Unity.Networking.Transport.Relay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RelayMessageConnectRequest
    {
        public const int Length = RelayMessageHeader.Length + RelayAllocationId.k_Length + 1 + RelayConnectionData.k_Length; // Header + AllocationId + ToConnectionDataLength + ToConnectionData;

        public RelayMessageHeader Header;

        public RelayAllocationId AllocationId;
        public byte ToConnectionDataLength;
        public RelayConnectionData ToConnectionData;

        public static RelayMessageConnectRequest Create(RelayAllocationId allocationId, RelayConnectionData toConnectionData)
        {
            return new RelayMessageConnectRequest
            {
                Header = RelayMessageHeader.Create(RelayMessageType.ConnectRequest),
                AllocationId = allocationId,
                ToConnectionDataLength = 255,
                ToConnectionData = toConnectionData,
            };
        }
    }
}
