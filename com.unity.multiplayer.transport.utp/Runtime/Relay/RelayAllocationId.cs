using System;
using Unity.Collections.LowLevel.Unsafe;

namespace MLAPI.Transports
{
    /// <summary>
    /// Allocation Id is a unique identifier for a connected client/host to a Relay server.
    /// This identifier is used by the Relay protocol as the address of the client.
    /// </summary>
    public unsafe struct RelayAllocationId : IEquatable<RelayAllocationId>, IComparable<RelayAllocationId>
    {
        public const int k_Length = 16;
        public fixed byte Value[k_Length];

        // Used by Relay SDK
        public static RelayAllocationId FromBytePointer(byte* dataPtr, int length)
        {
            if (length != k_Length)
                throw new ArgumentException($"Provided byte array length is invalid, must be {k_Length} but got {length}.");

            var allocationId = new RelayAllocationId();
            UnsafeUtility.MemCpy(allocationId.Value, dataPtr, k_Length);
            return allocationId;
        }

        public static bool operator ==(RelayAllocationId lhs, RelayAllocationId rhs)
        {
            return lhs.Compare(rhs) == 0;
        }

        public static bool operator !=(RelayAllocationId lhs, RelayAllocationId rhs)
        {
            return lhs.Compare(rhs) != 0;
        }

        public bool Equals(RelayAllocationId other)
        {
            return Compare(other) == 0;
        }

        public int CompareTo(RelayAllocationId other)
        {
            return Compare(other);
        }

        public override bool Equals(object other)
        {
            return other != null && this == (RelayAllocationId) other;
        }

        public override int GetHashCode()
        {
            fixed (byte* p = Value)
                unchecked
                {
                    var result = 0;

                    for (int i = 0; i < k_Length; i++)
                    {
                        result = (result * 31) ^ (int)p[i];
                    }

                    return result;
                }
        }

        int Compare(RelayAllocationId other)
        {
            fixed (void* p = Value)
            {
                return UnsafeUtility.MemCmp(p, other.Value, k_Length);
            }
        }
    }
}
