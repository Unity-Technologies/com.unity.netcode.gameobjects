using System.IO;

namespace Unity.Netcode
{
    public static class StreamExtensions
    {
        public static long SafeGetLengthOrDefault(this Stream stream)
        {
            return stream.CanSeek ? stream.Length : 0;
        }
    }
}
