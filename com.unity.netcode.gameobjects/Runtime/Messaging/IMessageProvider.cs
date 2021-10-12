using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface IMessageProvider
    {
        List<MessagingSystem.MessageWithHandler> GetMessages();
    }
}
