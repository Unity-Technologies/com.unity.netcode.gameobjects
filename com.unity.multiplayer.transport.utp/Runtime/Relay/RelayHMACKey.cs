using System;
using Unity.Collections.LowLevel.Unsafe;

namespace MLAPI.Transports
{
    public unsafe struct RelayHMACKey
    {
        public const int k_Length = 64;

        public fixed byte Value[k_Length];

        // Used by Relay SDK
        public static RelayHMACKey FromBytePointer(byte* data, int length)
        {
            if (length != k_Length)
                throw new ArgumentException($"Provided byte array length is invalid, must be {k_Length} but got {length}.");

            var hmacKey = new RelayHMACKey();
            UnsafeUtility.MemCpy(hmacKey.Value, data, length);
            return hmacKey;
        }
    }
}
