using System.Collections.Generic;

namespace MLAPI.Internal
{
    internal static class MessageManager
    {
        internal static readonly Dictionary<string, int> channels = new Dictionary<string, int>();
        internal static readonly Dictionary<int, string> reverseChannels = new Dictionary<int, string>();
        internal static readonly Dictionary<string, ushort> messageTypes = new Dictionary<string, ushort>();
        internal static readonly Dictionary<ushort, string> reverseMessageTypes = new Dictionary<ushort, string>();
    }
}
