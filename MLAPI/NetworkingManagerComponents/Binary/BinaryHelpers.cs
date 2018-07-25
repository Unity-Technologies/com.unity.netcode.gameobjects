#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace MLAPI.Serialization
{
    public static class BinaryHelpers
    {
        // Swap endianness of a given integer
        public static uint SwapEndian(uint value) => (uint)(((value >> 24) & (255 << 0)) | ((value >> 8) & (255 << 8)) | ((value << 8) & (255 << 16)) | ((value << 24) & (255 << 24)));
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
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
