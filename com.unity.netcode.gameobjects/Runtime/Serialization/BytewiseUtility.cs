using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Multiplayer.Netcode
{
    public static class BytewiseUtility
    {
        /// <summary>
        /// Helper function optimized for quickly copying small numbers of bytes.
        /// Faster than UnsafeUtil.Memcpy and other alternatives for amount <= 8
        /// Slower for amount > 8
        /// </summary>
        /// <param name="source">Pointer to the source value</param>
        /// <param name="dest">Pointer to the destination value</param>
        /// <param name="amount">Number of bytes to copy</param>
        public static unsafe void FastCopyBytes(byte* dest, byte* source, int amount)
        {
            // Switch statement to write small values with assignments
            // is considerably faster than calling UnsafeUtility.MemCpy
            // in all builds - editor, mono, and ILCPP
            switch (amount)
            {
                case 1:
                    dest[0] = *source;
                    break;
                case 2:
                    *(ushort*) dest = *(ushort*)source;
                    break;
                case 3:
                    *(ushort*) dest = *(ushort*)source;
                    *(dest+2) = *(source+2);
                    break;
                case 4:
                    *(uint*) dest = *(uint*)source;
                    break;
                case 5:
                    *(uint*) dest = *(uint*)source;
                    *(dest+4) = *(source+4);
                    break;
                case 6:
                    *(uint*) dest = *(uint*)source;
                    *(ushort*) (dest+4) = *(ushort*)(source+4);
                    break;
                case 7:
                    *(uint*) dest = *(uint*)source;
                    *(ushort*) (dest+4) = *(ushort*)(source+4);
                    *(dest+6) = *(source+6);
                    break;
                case 8:
                    *(ulong*) dest = *(ulong*)source;
                    break;
                default:
                    UnsafeUtility.MemCpy(dest, source, amount);
                    break;
            }
        }
    }
}