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
                    *dest = *source;
                    break;
                case 2:
                    *dest = *source;
                    *(dest + 1) = *(source + 1);
                    break;
                case 3:
                    *dest = *source;
                    *(dest + 1) = *(source + 1);
                    *(dest + 2) = *(source + 2);
                    break;
                case 4:
                    *dest = *source;
                    *(dest + 1) = *(source + 1);
                    *(dest + 2) = *(source + 2);
                    *(dest + 3) = *(source + 3);
                    break;
                case 5:
                    *dest = *source;
                    *(dest + 1) = *(source + 1);
                    *(dest + 2) = *(source + 2);
                    *(dest + 3) = *(source + 3);
                    *(dest + 4) = *(source + 4);
                    break;
                case 6:
                    *dest = *source;
                    *(dest + 1) = *(source + 1);
                    *(dest + 2) = *(source + 2);
                    *(dest + 3) = *(source + 3);
                    *(dest + 4) = *(source + 4);
                    *(dest + 5) = *(source + 5);
                    break;
                case 7:
                    *dest = *source;
                    *(dest + 1) = *(source + 1);
                    *(dest + 2) = *(source + 2);
                    *(dest + 3) = *(source + 3);
                    *(dest + 4) = *(source + 4);
                    *(dest + 5) = *(source + 5);
                    *(dest + 6) = *(source + 6);
                    break;
                case 8:
                    *dest = *source;
                    *(dest + 1) = *(source + 1);
                    *(dest + 2) = *(source + 2);
                    *(dest + 3) = *(source + 3);
                    *(dest + 4) = *(source + 4);
                    *(dest + 5) = *(source + 5);
                    *(dest + 6) = *(source + 6);
                    *(dest + 7) = *(source + 7);
                    break;
                default:
                    UnsafeUtility.MemCpy(dest, source, amount);
                    break;
            }
        }
    }
}
