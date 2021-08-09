namespace Unity.Netcode
{
    /// <summary>
    /// Arithmetic helper class
    /// </summary>
    public static class Arithmetic
    {
        // Sign bits for different data types
        internal const long SIGN_BIT_64 = -9223372036854775808;
        internal const int SIGN_BIT_32 = -2147483648;
        internal const short SIGN_BIT_16 = -32768;
        internal const sbyte SIGN_BIT_8 = -128;

        // Ceiling function that doesn't deal with floating point values
        // these only work correctly with possitive numbers
        internal static ulong CeilingExact(ulong u1, ulong u2) => (u1 + u2 - 1) / u2;
        internal static long CeilingExact(long u1, long u2) => (u1 + u2 - 1) / u2;
        internal static uint CeilingExact(uint u1, uint u2) => (u1 + u2 - 1) / u2;
        internal static int CeilingExact(int u1, int u2) => (u1 + u2 - 1) / u2;
        internal static ushort CeilingExact(ushort u1, ushort u2) => (ushort)((u1 + u2 - 1) / u2);
        internal static short CeilingExact(short u1, short u2) => (short)((u1 + u2 - 1) / u2);
        internal static byte CeilingExact(byte u1, byte u2) => (byte)((u1 + u2 - 1) / u2);
        internal static sbyte CeilingExact(sbyte u1, sbyte u2) => (sbyte)((u1 + u2 - 1) / u2);

        /// <summary>
        /// ZigZag encodes a signed integer and maps it to a unsigned integer
        /// </summary>
        /// <param name="value">The signed integer to encode</param>
        /// <returns>A ZigZag encoded version of the integer</returns>
        public static ulong ZigZagEncode(long value) => (ulong)((value >> 63) ^ (value << 1));

        /// <summary>
        /// Decides a ZigZag encoded integer back to a signed integer
        /// </summary>
        /// <param name="value">The unsigned integer</param>
        /// <returns>The signed version of the integer</returns>
        public static long ZigZagDecode(ulong value) => (((long)(value >> 1) & 0x7FFFFFFFFFFFFFFFL) ^ ((long)(value << 63) >> 63));

        /// <summary>
        /// Gets the output size in bytes after VarInting a unsigned integer
        /// </summary>
        /// <param name="value">The unsigned integer whose length to get</param>
        /// <returns>The amount of bytes</returns>
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

        internal static long Div8Ceil(ulong value) => (long)((value >> 3) + ((value & 1UL) | ((value >> 1) & 1UL) | ((value >> 2) & 1UL)));
    }
}
