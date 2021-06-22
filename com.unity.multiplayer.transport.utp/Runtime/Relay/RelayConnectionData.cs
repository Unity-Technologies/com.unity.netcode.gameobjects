using System;
using Unity.Collections.LowLevel.Unsafe;

namespace MLAPI.Transports
{
    /// <summary>
    /// This is the encrypted data that the Relay server uses for describing a connection.
    /// Used mainly in the connection stablishing process (Binding)
    /// </summary>
    public unsafe struct RelayConnectionData
    {
        public const int k_Length = 255;
        public fixed byte Value[k_Length];

        // Used by Relay SDK
        public static RelayConnectionData FromBytePointer(byte* dataPtr, int length)
        {
            if (length != k_Length)
                throw new ArgumentException($"Provided byte array length is invalid, must be {k_Length} but got {length}.");

            var connectionData = new RelayConnectionData();
            UnsafeUtility.MemCpy(connectionData.Value, dataPtr, length);
            return connectionData;
        }
    }
}
