namespace MLAPI.NetworkingManagerComponents.Binary
{
    public static class Arithmetic
    {
        // Sign bits for different data types
        public const long SIGN_BIT_64 = -9223372036854775808;
        public const int SIGN_BIT_32 = -2147483648;
        public const short SIGN_BIT_16 = -32768;
        public const sbyte SIGN_BIT_8 = -128;

        // Ceiling function that doesn't deal with floating point values
        public static ulong CeilingExact(ulong u1, ulong u2) => (u1 / u2) + (u1 % u2 == 0 ? 0UL : 1UL);
        public static long CeilingExact(long u1, long u2) => (u1 / u2) + (u1 % u2 == 0 ? 0L : 1L | ((u1 & SIGN_BIT_64) ^ (u2 & SIGN_BIT_64)));
        public static uint CeilingExact(uint u1, uint u2) => (u1 / u2) + (u1 % u2 == 0 ? 0U : 1U);
        public static int CeilingExact(int u1, int u2) => (u1 / u2) + (u1 % u2 == 0 ? 0 : 1 | ((u1 & SIGN_BIT_32) ^ (u2 & SIGN_BIT_32)));
        public static ushort CeilingExact(ushort u1, ushort u2) => (ushort)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1));
        public static short CeilingExact(short u1, short u2) => (short)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1 | ((u1 & SIGN_BIT_32) ^ (u2 & SIGN_BIT_32))));
        public static byte CeilingExact(byte u1, byte u2) => (byte)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1));
        public static sbyte CeilingExact(sbyte u1, sbyte u2) => (sbyte)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1 | ((u1 & SIGN_BIT_32) ^ (u2 & SIGN_BIT_32))));

        // ZigZag
        public static ulong ZigZagEncode(long value) => (ulong)((value >> 63) ^ (value << 1));
        public static long ZigZagDecode(ulong value) => (((long)(value >> 1) & 0x7FFFFFFFFFFFFFFFL) ^ ((long)(value << 63) >> 63));

        // Var int helper stuff
        public static int VarIntSize(ulong value) =>
                value <= 240 ? 1 :
                value <= 2287 ? 2 :
                value <= 67823 ? 3 :
                value <= 16777215 ? 4 :
                value <= 4294967295 ? 5 :
                value <= 1099511627775 ? 6 :
                value <= 281474976710655 ? 7 :
                value <= 72057594037927935 ? 8 :
                9;
    }
}
