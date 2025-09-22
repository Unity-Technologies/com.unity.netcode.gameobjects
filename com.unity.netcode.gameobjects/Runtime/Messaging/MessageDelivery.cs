using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
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
        UpdateMessageTypes();
    }

    private static void UpdateMessageTypes()
    {
        s_MessageToDelivery.Clear();
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

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    [InitializeOnEnterPlayMode]
    private static void OnEnterPlayMode()
    {
        UpdateMessageTypes();
    }
#endif
    internal static NetworkDelivery GetDelivery(Type type)
    {
        // Return the default if not registered or null
        if (type == null || !s_MessageToMessageType.ContainsKey(type))
        {
            return NetworkDelivery.ReliableFragmentedSequenced;
        }
        return GetDelivery(s_MessageToMessageType[type]);
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
