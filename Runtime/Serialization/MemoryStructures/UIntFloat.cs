using System.Runtime.InteropServices;

namespace Unity.Netcode
{
    /// <summary>
    /// A struct with a explicit memory layout. The struct has 4 fields. float,uint,double and ulong.
    /// Every field has the same starting point in memory. If you insert a float value, it can be extracted as a uint.
    /// This is to allow for lockless and garbage free conversion from float to uint and double to ulong.
    /// This allows for VarInt encoding and other integer encodings.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)]
        public float FloatValue;

        [FieldOffset(0)]
        public uint UIntValue;

        [FieldOffset(0)]
        public double DoubleValue;

        [FieldOffset(0)]
        public ulong ULongValue;
    }
}
