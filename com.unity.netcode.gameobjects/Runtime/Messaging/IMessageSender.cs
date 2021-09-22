using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode
{
    internal interface IMessageSender
    {
        int SendMessage<TMessageType, TClientIdListType>(in TMessageType message, NetworkDelivery delivery, in TClientIdListType clientIds) where TMessageType : INetworkMessage where TClientIdListType : IReadOnlyList<ulong>;
        unsafe int SendMessage<T>(in T message, NetworkDelivery delivery, ulong* clientIds, int numClientIds) where T : INetworkMessage;
        int SendMessage<T>(in T message, NetworkDelivery delivery, ulong clientId) where T : INetworkMessage;
        int SendMessage<T>(in T message, NetworkDelivery delivery, in NativeArray<ulong> clientIds) where T : INetworkMessage;
    }
}
