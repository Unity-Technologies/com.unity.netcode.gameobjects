using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    internal class ByteUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe byte ToByte(bool b) => *(byte*)&b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetBit(byte bitField, ushort bitPosition)
        {
            return (bitField & (1 << bitPosition)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetBit(ref byte bitField, ushort bitPosition, bool value)
        {
            bitField = (byte)((bitField & ~(1 << bitPosition)) | (ToByte(value) << bitPosition));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetBit(ushort bitField, ushort bitPosition)
        {
            return (bitField & (1 << bitPosition)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetBit(ref ushort bitField, ushort bitPosition, bool value)
        {
            bitField = (ushort)((bitField & ~(1 << bitPosition)) | (ToByte(value) << bitPosition));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetBit(uint bitField, ushort bitPosition)
        {
            return (bitField & (1 << bitPosition)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetBit(ref uint bitField, ushort bitPosition, bool value)
        {
            bitField = (uint)((bitField & ~(1 << bitPosition)) | ((uint)ToByte(value) << bitPosition));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetBit(ulong bitField, ushort bitPosition)
        {
            return (bitField & (ulong)(1 << bitPosition)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetBit(ref ulong bitField, ushort bitPosition, bool value)
        {
            bitField = ((bitField & (ulong)~(1 << bitPosition)) | ((ulong)ToByte(value) << bitPosition));
        }
    }
}
