using System.IO;

namespace MLAPI.Metrics
{
    public static class StreamExtensions
    {
        public static long SafeGetLengthOrDefault(this Stream stream)
        {
            return stream.CanSeek ? stream.Length : 0;
        }
    }
}