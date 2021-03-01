using System.Runtime.InteropServices;

namespace MLAPI.Serialization
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ByteBool
    {
        [FieldOffset(0)]
        public bool BoolValue;

        [FieldOffset(0)]
        public byte ByteValue;

        public byte Collapse() =>
            ByteValue = (byte)((
                // Collapse all bits to position 1 and reassign as bit
                (ByteValue >> 7) |
                (ByteValue >> 6) |
                (ByteValue >> 5) |
                (ByteValue >> 4) |
                (ByteValue >> 3) |
                (ByteValue >> 2) |
                (ByteValue >> 1) |
                ByteValue
            ) & 1);

        public byte Collapse(bool b)
        {
            BoolValue = b;
            return Collapse();
        }
    }
}