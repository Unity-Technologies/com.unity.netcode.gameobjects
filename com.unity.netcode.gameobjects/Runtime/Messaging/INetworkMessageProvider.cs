using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface INetworkMessageProvider
    {
        List<NetworkMessageManager.MessageWithHandler> GetMessages();
    }
}
