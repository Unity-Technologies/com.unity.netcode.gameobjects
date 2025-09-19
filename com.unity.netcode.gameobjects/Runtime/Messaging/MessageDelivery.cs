using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

internal static class MessageDelivery
{
    private static Dictionary<NetworkMessageTypes, NetworkDelivery> s_MessageToDelivery = new Dictionary<NetworkMessageTypes, NetworkDelivery>();

    private static Dictionary<Type, NetworkMessageTypes> s_MessageToMessageType = new Dictionary<Type, NetworkMessageTypes>();

    /// <summary>
    /// - Skip named and unnamed since they inherently can have their network delivery type adjusted
    /// when sending the message via public API.
    /// - Skip the time sync messages since it has always used unreliable network delivery.
    /// </summary>
    private static HashSet<NetworkMessageTypes> s_SkipMessageTypes = new HashSet<NetworkMessageTypes>(){
        NetworkMessageTypes.NamedMessage, NetworkMessageTypes.Unnamed};

    [RuntimeInitializeOnLoadMethod]
    private static void OnApplicationStart()
    {
        var networkMessageTypes = Enum.GetValues(typeof(NetworkMessageTypes));
        foreach (var messageTypeObject in networkMessageTypes)
        {
            var messageType = (NetworkMessageTypes)messageTypeObject;
            if (s_SkipMessageTypes.Contains(messageType))
            {
                continue;
            }
            s_MessageToDelivery.Add(messageType, NetworkDelivery.ReliableFragmentedSequenced);
        }
        s_MessageToMessageType = ILPPMessageProvider.GetMessageTypesMap();
    }

    internal static NetworkMessageTypes GetMessageTypeEnum(Type type)
    {
        if (type == null || !s_MessageToMessageType.ContainsKey(type))
        {
            var name = type == null ? "null" : type.Name;
            throw new Exception($"{name} is not registered in the message to {nameof(NetworkMessageTypes)} table!");
        }
        return s_MessageToMessageType[type];
    }

    internal static NetworkDelivery GetDelivery(Type type)
    {
        var messageType = GetMessageTypeEnum(type);
        return GetDelivery(messageType);
    }

    internal static NetworkDelivery GetDelivery(NetworkMessageTypes messageType)
    {
        if (s_SkipMessageTypes.Contains(messageType))
        {
            throw new Exception($"{messageType} is not registered in the message type to network delivery map!");
        }
        return s_MessageToDelivery[messageType];
    }
}
