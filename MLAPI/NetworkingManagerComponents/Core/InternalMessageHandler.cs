using System.Collections.Generic;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
        private static NetworkingManager netManager => NetworkingManager.Singleton;
        
        internal static readonly Dictionary<ulong, RpcResponseBase> Responses = new Dictionary<ulong, RpcResponseBase>();

        private static ulong messageIdCounter;
        
        internal static ulong GenerateMessageId()
        {
            return messageIdCounter++;
        }
    }
}
