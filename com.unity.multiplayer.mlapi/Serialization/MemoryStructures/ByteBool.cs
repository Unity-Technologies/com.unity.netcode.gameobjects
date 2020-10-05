using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MLAPI.Serialization
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ByteBool
    {
        [FieldOffset(0)]
        public bool boolValue;

        [FieldOffset(0)]
        public byte byteValue;

        public byte Collapse() =>
            byteValue = (byte)((
            // Collapse all bits to position 1 and reassign as bit
            (byteValue >> 7) |
            (byteValue >> 6) |
            (byteValue >> 5) |
            (byteValue >> 4) |
            (byteValue >> 3) |
            (byteValue >> 2) |
            (byteValue >> 1) |
            byteValue
            )&1);

        public byte Collapse(bool b)
        {
            boolValue = b;
            return Collapse();
        }
    }
}
