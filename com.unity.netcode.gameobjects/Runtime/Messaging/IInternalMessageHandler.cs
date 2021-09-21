using System.IO;

namespace Unity.Netcode
{
    internal interface IInternalMessageHandler
    {
        NetworkManager NetworkManager { get; }
        void HandleConnectionRequest(ulong clientId, Stream stream);
        void HandleConnectionApproved(ulong clientId, Stream stream, float receiveTime);
        void HandleAddObject(ulong clientId, Stream stream);
        void HandleDestroyObject(ulong clientId, Stream stream);
        void HandleSceneEvent(ulong clientId, Stream stream);
        void HandleChangeOwner(ulong clientId, Stream stream);
        void HandleTimeSync(ulong clientId, Stream stream);
        void HandleNetworkVariableDelta(ulong clientId, Stream stream);
        void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType);
        void HandleUnnamedMessage(ulong clientId, Stream stream);
        void HandleNamedMessage(ulong clientId, Stream stream);
        void HandleNetworkLog(ulong clientId, Stream stream);
        void HandleSnapshot(ulong clientId, Stream messageStream);
    }
}
