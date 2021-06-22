using System.Runtime.InteropServices;
using MLAPI.Transports;

namespace Unity.Networking.Transport.Relay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RelayMessageDisconnect
    {
        public const int Length = RelayMessageHeader.Length + RelayAllocationId.k_Length * 2; // Header + FromAllocationId + ToAllocationId

        public RelayMessageHeader Header;

        public RelayAllocationId FromAllocationId;
        public RelayAllocationId ToAllocationId;

        public static RelayMessageDisconnect Create(RelayAllocationId fromAllocationId, RelayAllocationId toAllocationId)
        {
            return new RelayMessageDisconnect
            {
                Header = RelayMessageHeader.Create(RelayMessageType.Disconnect),
                FromAllocationId = fromAllocationId,
                ToAllocationId = toAllocationId,
            };
        }
    }
}
