namespace MLAPI.Serialization
{
    /// <summary>
    /// Binary helper class
    /// </summary>
    public static class EndianHelper
    {
        /// <summary>
        /// Swaps the endianness of a given integer
        /// </summary>
        /// <returns>The integer with swapped endianness</returns>
        /// <param name="value">The integer to swap endianess</param>
        public static uint SwapEndian(uint value) => (uint)(((value >> 24) & (255 << 0)) | ((value >> 8) & (255 << 8)) | ((value << 8) & (255 << 16)) | ((value << 24) & (255 << 24)));
        /// <summary>
        /// Swaps the endianness of a given integer
        /// </summary>
        /// <returns>The integer with swapped endianness</returns>
        /// <param name="value">The integer to swap endianess</param>
        public static ulong SwapEndian(ulong value) =>
            ((value >> 56) & 0xFF)           |
            ((value >> 40) & (0xFFUL << 8))  |
            ((value >> 24) & (0xFFUL << 16)) |
            ((value >> 8)  & (0xFFUL << 24)) |
            ((value << 56) & (0xFFUL << 56)) |
            ((value << 40) & (0xFFUL << 48)) |
            ((value << 24) & (0xFFUL << 40)) |
            ((value << 8)  & (0xFFUL << 32)) ;
    }
}