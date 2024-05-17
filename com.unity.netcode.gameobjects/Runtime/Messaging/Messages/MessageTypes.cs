using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    internal class MessageTypeDefines
    {
        /// <summary>
        /// Enum representing the different types of messages that can be sent over the network.
        /// The values cannot be changed, as they are used to serialize and deserialize messages.
        /// Adding new messages should be done by adding new values to the end of the enum
        /// (using the next free value).
        /// </summary>
        /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /// Add any new Message types to this table at the END
        /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        internal enum NetworkMessageTypes : uint
        {
            ConnectionApproved = 0,
            ConnectionRequest = 1,
            ChangeOwnership = 2,
            ClientConnected = 3,
            ClientDisconnected = 4,
            ClientRpc = 5,
            CreateObject = 6,
            DestroyObject = 7,
            DisconnectReason = 8,
            ForwardClientRpc = 9,
            ForwardServerRpc = 10,
            NamedMessage = 11,
            NetworkTransformMessage = 12,
            NetworkVariableDelta = 13,
            ParentSync = 14,
            Proxy = 15,
            Rpc = 16,
            SceneEvent = 17,
            ServerLog = 18,
            ServerRpc = 19,
            TimeSync = 20,
            Unnamed = 21,
            SessionOwner = 22
        }

        internal static Dictionary<Type, NetworkMessageTypes> MessageTypes;

        /// <summary>
        /// Orders messages based on <see cref="NetworkMessageTypes"/>
        /// </summary>
        /// <param name="allowedTypes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static List<NetworkMessageManager.MessageWithHandler> Initialize(INetworkMessageProvider networkMessageProvider)
        {
            var allowedTypes = networkMessageProvider.GetMessages();
            var messageTypeCount = Enum.GetValues(typeof(NetworkMessageTypes)).Length;
            // Assure the allowed types count is the same as our NetworkMessageType enum count
            if (allowedTypes.Count != messageTypeCount)
            {
                throw new Exception($"Allowed types is not equal to the number of message type indices! Allowed Count: {allowedTypes.Count} | Index Count: {messageTypeCount}");
            }

            // Populate with blanks to be replaced later
            var adjustedMessageTypes = new List<NetworkMessageManager.MessageWithHandler>();
            var blank = new NetworkMessageManager.MessageWithHandler();
            for (int i = 0; i < messageTypeCount; i++) 
            {
                adjustedMessageTypes.Add(blank);
            }

            // Create a type to index lookup table
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // Add any new Message types to this table
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            MessageTypes = new Dictionary<Type, NetworkMessageTypes>
            {
                { typeof(ConnectionApprovedMessage), NetworkMessageTypes.ConnectionApproved },
                { typeof(ConnectionRequestMessage), NetworkMessageTypes.ConnectionRequest },
                { typeof(ChangeOwnershipMessage), NetworkMessageTypes.ChangeOwnership },
                { typeof(ClientConnectedMessage), NetworkMessageTypes.ClientConnected },
                { typeof(ClientDisconnectedMessage), NetworkMessageTypes.ClientDisconnected },
                { typeof(ClientRpcMessage), NetworkMessageTypes.ClientRpc },
                { typeof(CreateObjectMessage), NetworkMessageTypes.CreateObject },
                { typeof(DestroyObjectMessage), NetworkMessageTypes.DestroyObject },
                { typeof(DisconnectReasonMessage), NetworkMessageTypes.DisconnectReason },
                { typeof(ForwardClientRpcMessage), NetworkMessageTypes.ForwardClientRpc },
                { typeof(ForwardServerRpcMessage), NetworkMessageTypes.ForwardServerRpc },
                { typeof(NamedMessage), NetworkMessageTypes.NamedMessage },
                { typeof(NetworkTransformMessage), NetworkMessageTypes.NetworkTransformMessage },
                { typeof(NetworkVariableDeltaMessage), NetworkMessageTypes.NetworkVariableDelta },
                { typeof(ParentSyncMessage), NetworkMessageTypes.ParentSync },
                { typeof(ProxyMessage), NetworkMessageTypes.Proxy },
                { typeof(RpcMessage), NetworkMessageTypes.Rpc },
                { typeof(SceneEventMessage), NetworkMessageTypes.SceneEvent },
                { typeof(ServerLogMessage), NetworkMessageTypes.ServerLog },
                { typeof(ServerRpcMessage), NetworkMessageTypes.ServerRpc },
                { typeof(TimeSyncMessage), NetworkMessageTypes.TimeSync },
                { typeof(UnnamedMessage), NetworkMessageTypes.Unnamed },
                { typeof(SessionOwnerMessage), NetworkMessageTypes.SessionOwner }
            };

            // Assure the type to lookup table count and NetworkMessageType enum count matches (i.e. to catch human error when adding new messages)
            if (MessageTypes.Count != messageTypeCount)
            {
                throw new Exception($"Message type to Message type index count mistmatch! Table Count: {MessageTypes.Count} | Index Count: {messageTypeCount}");
            }

            // Now order the allowed types list based on the order of the NetworkMessageType enum
            foreach (var messageHandler in allowedTypes) 
            { 
                if (!MessageTypes.ContainsKey(messageHandler.MessageType))
                {
                    throw new Exception($"Missing message type from lookup table: {messageHandler.MessageType}");
                }
                adjustedMessageTypes[(int)MessageTypes[messageHandler.MessageType]] = messageHandler;
            }

            // return the NetworkMessageType enum ordered list
            return adjustedMessageTypes;
        }
    }
}
