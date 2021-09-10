using System.IO;

namespace Unity.Netcode
{
    internal interface IInternalMessageHandler
    {
        NetworkManager NetworkManager { get; }
        void HandleSceneEvent(ulong clientId, Stream stream);
        void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType);
        void HandleUnnamedMessage(ulong clientId, Stream stream);
        void HandleNamedMessage(ulong clientId, Stream stream);
    }
}
