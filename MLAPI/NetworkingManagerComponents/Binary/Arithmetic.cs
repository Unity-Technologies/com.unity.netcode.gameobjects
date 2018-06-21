namespace MLAPI.NetworkingManagerComponents.Binary
{
    public static class Arithmetic
    {
        public static ulong CeilingExact(ulong u1, ulong u2) => (u1 / u2) + (u1 % u2 == 0 ? 0UL : 1UL);
        public static long CeilingExact(long u1, long u2) => (u1 / u2) + (u1 % u2 == 0 ? 0L : 1L);
        public static uint CeilingExact(uint u1, uint u2) => (u1 / u2) + (u1 % u2 == 0 ? 0U : 1U);
        public static int CeilingExact(int u1, int u2) => (u1 / u2) + (u1 % u2 == 0 ? 0 : 1);
        public static ushort CeilingExact(ushort u1, ushort u2) => (ushort)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1));
        public static short CeilingExact(short u1, short u2) => (short)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1));
        public static byte CeilingExact(byte u1, byte u2) => (byte)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1));
        public static sbyte CeilingExact(sbyte u1, sbyte u2) => (sbyte)((u1 / u2) + (u1 % u2 == 0 ? 0 : 1));
        public static ulong ZigZagEncode(long value) => (ulong)((value >> 63) ^ (value << 1));
        public static long ZigZagDecode(ulong value) => (((long)(value >> 1) & 0x7FFFFFFFFFFFFFFFL) ^ ((long)(value << 63) >> 63));
    }
}
