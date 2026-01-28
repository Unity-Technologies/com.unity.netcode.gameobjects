using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface INetworkMessageProvider
    {
        public List<NetworkMessageManager.MessageWithHandler> GetMessages();
    }
}
