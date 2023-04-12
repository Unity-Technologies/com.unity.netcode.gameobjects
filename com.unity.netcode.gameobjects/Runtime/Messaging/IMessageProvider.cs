using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface IMessageProvider
    {
        List<NetworkMessageManager.MessageWithHandler> GetMessages();
    }
}
