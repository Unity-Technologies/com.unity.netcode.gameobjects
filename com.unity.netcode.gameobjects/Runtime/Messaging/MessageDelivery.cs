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

    /// <summary>
    /// FIrst pass at providing an easier path to configuring the network
    /// delivery type for the message type.
    /// TODO: Once <see cref="NetworkMessageManager"/> coalesces all reliable messages
    /// and/or organizes by a more unified order of operation tracking built into the
    /// buffer and/or converts all places that would normally generate a message to
    /// commands that will, eventually, generate messages.
    /// For now, we are sending all reliable fragmented sequenced.
    /// </summary>
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

        // Fast path look-ups
        MessageDeliveryType<ChangeOwnershipMessage>.Initialize();
        MessageDeliveryType<ClientConnectedMessage>.Initialize();
        MessageDeliveryType<ClientDisconnectedMessage>.Initialize();
        MessageDeliveryType<ConnectionRequestMessage>.Initialize();
        MessageDeliveryType<ConnectionApprovedMessage>.Initialize();
        MessageDeliveryType<CreateObjectMessage>.Initialize();
        MessageDeliveryType<DestroyObjectMessage>.Initialize();
        MessageDeliveryType<NetworkTransformMessage>.Initialize();
        MessageDeliveryType<NetworkVariableDeltaMessage>.Initialize();
        MessageDeliveryType<ParentSyncMessage>.Initialize();
        // RpcMessage.cs
        {
            MessageDeliveryType<RpcMessage>.Initialize();
            MessageDeliveryType<ClientRpcMessage>.Initialize();
            MessageDeliveryType<ServerRpcMessage>.Initialize();
        }
        MessageDeliveryType<SceneEventMessage>.Initialize();
        MessageDeliveryType<ServerLogMessage>.Initialize();
        MessageDeliveryType<SessionOwnerMessage>.Initialize();
        MessageDeliveryType<TimeSyncMessage>.Initialize();
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
        if (type == null || s_SkipMessageTypes.Contains(s_MessageToMessageType[type]))
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
