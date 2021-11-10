using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    /// <summary>
    /// The manager class to manage custom messages, note that this is different from the NetworkManager custom messages.
    /// These are named and are much easier to use.
    /// </summary>
    public class CustomMessagingManager
    {
        private readonly NetworkManager m_NetworkManager;

        internal CustomMessagingManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        /// <summary>
        /// Delegate used for incoming unnamed messages
        /// </summary>
        /// <param name="clientId">The clientId that sent the message</param>
        /// <param name="reader">The stream containing the message data</param>
        public delegate void UnnamedMessageDelegate(ulong clientId, FastBufferReader reader);

        /// <summary>
        /// Event invoked when unnamed messages arrive
        /// </summary>
        public event UnnamedMessageDelegate OnUnnamedMessage;

        internal void InvokeUnnamedMessage(ulong clientId, FastBufferReader reader)
        {
            if (OnUnnamedMessage != null)
            {
                var pos = reader.Position;
                var delegates = OnUnnamedMessage.GetInvocationList();
                foreach (var handler in delegates)
                {
                    reader.Seek(pos);
                    ((UnnamedMessageDelegate)handler).Invoke(clientId, reader);
                }
            }
            m_NetworkManager.NetworkMetrics.TrackUnnamedMessageReceived(clientId, reader.Length + FastBufferWriter.GetWriteSize<MessageHeader>());
        }

        /// <summary>
        /// Sends unnamed message to all clients
        /// </summary>
        /// <param name="messageBuffer">The message stream containing the data</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        public void SendUnnamedMessageToAll(FastBufferWriter messageBuffer, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            SendUnnamedMessage(m_NetworkManager.ConnectedClientsIds, messageBuffer, networkDelivery);
        }

        /// <summary>
        /// Sends unnamed message to a list of clients
        /// </summary>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="messageBuffer">The message stream containing the data</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        public void SendUnnamedMessage(IReadOnlyList<ulong> clientIds, FastBufferWriter messageBuffer, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            if (!m_NetworkManager.IsServer)
            {
                throw new InvalidOperationException("Can not send unnamed messages to multiple users as a client");
            }

            if (clientIds == null)
            {
                throw new ArgumentNullException("You must pass in a valid clientId List");
            }

            var message = new UnnamedMessage
            {
                Data = messageBuffer
            };
            var size = m_NetworkManager.SendMessage(message, networkDelivery, clientIds);

            // Size is zero if we were only sending the message to ourself in which case it isn't sent.
            if (size != 0)
            {
                m_NetworkManager.NetworkMetrics.TrackUnnamedMessageSent(clientIds, size);
            }
        }

        /// <summary>
        /// Sends a unnamed message to a specific client
        /// </summary>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="messageBuffer">The message stream containing the data</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        public void SendUnnamedMessage(ulong clientId, FastBufferWriter messageBuffer, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            var message = new UnnamedMessage
            {
                Data = messageBuffer
            };
            var size = m_NetworkManager.SendMessage(message, networkDelivery, clientId);
            // Size is zero if we were only sending the message to ourself in which case it isn't sent.
            if (size != 0)
            {
                m_NetworkManager.NetworkMetrics.TrackUnnamedMessageSent(clientId, size);
            }
        }

        /// <summary>
        /// Delegate used to handle named messages
        /// </summary>
        public delegate void HandleNamedMessageDelegate(ulong senderClientId, FastBufferReader messagePayload);

        private Dictionary<ulong, HandleNamedMessageDelegate> m_NamedMessageHandlers32 = new Dictionary<ulong, HandleNamedMessageDelegate>();
        private Dictionary<ulong, HandleNamedMessageDelegate> m_NamedMessageHandlers64 = new Dictionary<ulong, HandleNamedMessageDelegate>();

        private Dictionary<ulong, string> m_MessageHandlerNameLookup32 = new Dictionary<ulong, string>();
        private Dictionary<ulong, string> m_MessageHandlerNameLookup64 = new Dictionary<ulong, string>();

        internal void InvokeNamedMessage(ulong hash, ulong sender, FastBufferReader reader)
        {
            var bytesCount = reader.Length + FastBufferWriter.GetWriteSize<MessageHeader>();

            if (m_NetworkManager == null)
            {
                // We dont know what size to use. Try every (more collision prone)
                if (m_NamedMessageHandlers32.TryGetValue(hash, out HandleNamedMessageDelegate messageHandler32))
                {
                    messageHandler32(sender, reader);
                    m_NetworkManager.NetworkMetrics.TrackNamedMessageReceived(sender, m_MessageHandlerNameLookup32[hash], bytesCount);
                }

                if (m_NamedMessageHandlers64.TryGetValue(hash, out HandleNamedMessageDelegate messageHandler64))
                {
                    messageHandler64(sender, reader);
                    m_NetworkManager.NetworkMetrics.TrackNamedMessageReceived(sender, m_MessageHandlerNameLookup64[hash], bytesCount);
                }
            }
            else
            {
                // Only check the right size.
                switch (m_NetworkManager.NetworkConfig.RpcHashSize)
                {
                    case HashSize.VarIntFourBytes:
                        if (m_NamedMessageHandlers32.TryGetValue(hash, out HandleNamedMessageDelegate messageHandler32))
                        {
                            messageHandler32(sender, reader);
                            m_NetworkManager.NetworkMetrics.TrackNamedMessageReceived(sender, m_MessageHandlerNameLookup32[hash], bytesCount);
                        }
                        break;
                    case HashSize.VarIntEightBytes:
                        if (m_NamedMessageHandlers64.TryGetValue(hash, out HandleNamedMessageDelegate messageHandler64))
                        {
                            messageHandler64(sender, reader);
                            m_NetworkManager.NetworkMetrics.TrackNamedMessageReceived(sender, m_MessageHandlerNameLookup64[hash], bytesCount);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Registers a named message handler delegate.
        /// </summary>
        /// <param name="name">Name of the message.</param>
        /// <param name="callback">The callback to run when a named message is received.</param>
        public void RegisterNamedMessageHandler(string name, HandleNamedMessageDelegate callback)
        {
            var hash32 = XXHash.Hash32(name);
            var hash64 = XXHash.Hash64(name);

            m_NamedMessageHandlers32[hash32] = callback;
            m_NamedMessageHandlers64[hash64] = callback;

            m_MessageHandlerNameLookup32[hash32] = name;
            m_MessageHandlerNameLookup64[hash64] = name;
        }

        /// <summary>
        /// Unregisters a named message handler.
        /// </summary>
        /// <param name="name">The name of the message.</param>
        public void UnregisterNamedMessageHandler(string name)
        {
            var hash32 = XXHash.Hash32(name);
            var hash64 = XXHash.Hash64(name);

            m_NamedMessageHandlers32.Remove(hash32);
            m_NamedMessageHandlers64.Remove(hash64);

            m_MessageHandlerNameLookup32.Remove(hash32);
            m_MessageHandlerNameLookup64.Remove(hash64);
        }

        /// <summary>
        /// Sends a named message to all clients
        /// </summary>
        /// <param name="messageStream">The message stream containing the data</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        public void SendNamedMessageToAll(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            SendNamedMessage(messageName, m_NetworkManager.ConnectedClientsIds, messageStream, networkDelivery);
        }

        /// <summary>
        /// Sends a named message
        /// </summary>
        /// <param name="messageName">The message name to send</param>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="messageStream">The message stream containing the data</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        public void SendNamedMessage(string messageName, ulong clientId, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            ulong hash = 0;
            switch (m_NetworkManager.NetworkConfig.RpcHashSize)
            {
                case HashSize.VarIntFourBytes:
                    hash = XXHash.Hash32(messageName);
                    break;
                case HashSize.VarIntEightBytes:
                    hash = XXHash.Hash64(messageName);
                    break;
            }

            var message = new NamedMessage
            {
                Hash = hash,
                Data = messageStream
            };
            var size = m_NetworkManager.SendMessage(message, networkDelivery, clientId);

            // Size is zero if we were only sending the message to ourself in which case it isn't sent.
            if (size != 0)
            {
                m_NetworkManager.NetworkMetrics.TrackNamedMessageSent(clientId, messageName, size);
            }
        }

        /// <summary>
        /// Sends the named message
        /// </summary>
        /// <param name="messageName">The message name to send</param>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="messageStream">The message stream containing the data</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        public void SendNamedMessage(string messageName, IReadOnlyList<ulong> clientIds, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            if (!m_NetworkManager.IsServer)
            {
                throw new InvalidOperationException("Can not send unnamed messages to multiple users as a client");
            }

            if (clientIds == null)
            {
                throw new ArgumentNullException("You must pass in a valid clientId List");
            }

            ulong hash = 0;
            switch (m_NetworkManager.NetworkConfig.RpcHashSize)
            {
                case HashSize.VarIntFourBytes:
                    hash = XXHash.Hash32(messageName);
                    break;
                case HashSize.VarIntEightBytes:
                    hash = XXHash.Hash64(messageName);
                    break;
            }
            var message = new NamedMessage
            {
                Hash = hash,
                Data = messageStream
            };
            var size = m_NetworkManager.SendMessage(message, networkDelivery, clientIds);

            // Size is zero if we were only sending the message to ourself in which case it isn't sent.
            if (size != 0)
            {
                m_NetworkManager.NetworkMetrics.TrackNamedMessageSent(clientIds, messageName, size);
            }
        }
    }
}
