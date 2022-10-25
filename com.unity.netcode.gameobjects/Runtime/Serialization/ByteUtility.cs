using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    internal class ByteUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe byte ToByte(bool b) => *(byte*)&b;
    }
}
