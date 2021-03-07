using System.Collections.Generic;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Logging;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.Hashing;
using MLAPI.Transports;

namespace MLAPI.Messaging
{
    /// <summary>
    /// The manager class to manage custom messages, note that this is different from the NetworkManager custom messages.
    /// These are named and are much easier to use.
    /// </summary>
    public class CustomMessagingManager
    {
        #region Unnamed

        /// <summary>
        /// Delegate used for incoming unnamed messages
        /// </summary>
        /// <param name="clientId">The clientId that sent the message</param>
        /// <param name="stream">The stream containing the message data</param>
        public delegate void UnnamedMessageDelegate(ulong clientId, Stream stream);

        /// <summary>
        /// Event invoked when unnamed messages arrive
        /// </summary>
        public event UnnamedMessageDelegate OnUnnamedMessage;

        internal void InvokeUnnamedMessage(ulong clientId, Stream stream) => OnUnnamedMessage?.Invoke(clientId, stream);

        private NetworkManager m_NetworkManager;

        internal CustomMessagingManager(NetworkManager manager)
        {
            m_NetworkManager = manager;

        }

        /// <summary>
        /// Sends unnamed message to a list of clients
        /// </summary>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="buffer">The message stream containing the data</param>
        /// <param name="networkChannel">The channel to send the data on</param>
        public void SendUnnamedMessage(List<ulong> clientIds, NetworkBuffer buffer, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            if (!m_NetworkManager.IsServer)
            {
                if (m_NetworkManager.LogLevel <= LogLevel.Error) m_NetworkManager.NetworkLog.LogWarning("Can not send unnamed messages to multiple users as a client");
                return;
            }

            m_NetworkManager.InternalMessageSender.Send(NetworkConstants.UNNAMED_MESSAGE, networkChannel, clientIds, buffer);
        }

        /// <summary>
        /// Sends a unnamed message to a specific client
        /// </summary>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="buffer">The message stream containing the data</param>
        /// <param name="networkChannel">The channel tos end the data on</param>
        public void SendUnnamedMessage(ulong clientId, NetworkBuffer buffer, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            m_NetworkManager.InternalMessageSender.Send(clientId, NetworkConstants.UNNAMED_MESSAGE, networkChannel, buffer);
        }

        #endregion

        #region Named

        /// <summary>
        /// Delegate used to handle named messages
        /// </summary>
        public delegate void HandleNamedMessageDelegate(ulong sender, Stream payload);

        private Dictionary<ulong, HandleNamedMessageDelegate> m_NamedMessageHandlers16 = new Dictionary<ulong, HandleNamedMessageDelegate>();
        private Dictionary<ulong, HandleNamedMessageDelegate> m_NamedMessageHandlers32 = new Dictionary<ulong, HandleNamedMessageDelegate>();
        private Dictionary<ulong, HandleNamedMessageDelegate> m_NamedMessageHandlers64 = new Dictionary<ulong, HandleNamedMessageDelegate>();

        internal void InvokeNamedMessage(ulong hash, ulong sender, Stream stream)
        {
            
                // Only check the right size.
                if (m_NetworkManager.NetworkConfig.RpcHashSize == HashSize.VarIntTwoBytes)
                {
                    if (m_NamedMessageHandlers16.ContainsKey(hash))
                    {
                        m_NamedMessageHandlers16[hash](sender, stream);
                    }
                }
                else if (m_NetworkManager.NetworkConfig.RpcHashSize == HashSize.VarIntFourBytes)
                {
                    if (m_NamedMessageHandlers32.ContainsKey(hash))
                    {
                        m_NamedMessageHandlers32[hash](sender, stream);
                    }
                }
                else if (m_NetworkManager.NetworkConfig.RpcHashSize == HashSize.VarIntEightBytes)
                {
                    if (m_NamedMessageHandlers64.ContainsKey(hash))
                    {
                        m_NamedMessageHandlers64[hash](sender, stream);
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
            m_NamedMessageHandlers16[name.GetStableHash16()] = callback;
            m_NamedMessageHandlers32[name.GetStableHash32()] = callback;
            m_NamedMessageHandlers64[name.GetStableHash64()] = callback;
        }

        /// <summary>
        /// Unregisters a named message handler.
        /// </summary>
        /// <param name="name">The name of the message.</param>
        public void UnregisterNamedMessageHandler(string name)
        {
            m_NamedMessageHandlers16.Remove(name.GetStableHash16());
            m_NamedMessageHandlers32.Remove(name.GetStableHash32());
            m_NamedMessageHandlers64.Remove(name.GetStableHash64());
        }

        /// <summary>
        /// Sends a named message
        /// </summary>
        /// <param name="name">The message name to send</param>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="networkChannel">The channel to send the data on</param>
        public void SendNamedMessage(string name, ulong clientId, Stream stream, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            ulong hash = 0;
            switch (m_NetworkManager.NetworkConfig.RpcHashSize)
            {
                case HashSize.VarIntTwoBytes:
                    hash = name.GetStableHash16();
                    break;
                case HashSize.VarIntFourBytes:
                    hash = name.GetStableHash32();
                    break;
                case HashSize.VarIntEightBytes:
                    hash = name.GetStableHash64();
                    break;
            }

            using (var messageBuffer = PooledNetworkBuffer.Get())
            using (var writer = m_NetworkManager.NetworkWriterPool.GetWriter(messageBuffer))
            {
                writer.WriteUInt64Packed(hash);

                messageBuffer.CopyFrom(stream);

                m_NetworkManager.InternalMessageSender.Send(clientId, NetworkConstants.NAMED_MESSAGE, networkChannel, messageBuffer);
            }
        }

        /// <summary>
        /// Sends the named message
        /// </summary>
        /// <param name="name">The message name to send</param>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="networkChannel">The channel to send the data on</param>
        public void SendNamedMessage(string name, List<ulong> clientIds, Stream stream, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            ulong hash = 0;
            switch (m_NetworkManager.NetworkConfig.RpcHashSize)
            {
                case HashSize.VarIntTwoBytes:
                    hash = name.GetStableHash16();
                    break;
                case HashSize.VarIntFourBytes:
                    hash = name.GetStableHash32();
                    break;
                case HashSize.VarIntEightBytes:
                    hash = name.GetStableHash64();
                    break;
            }

            using (var messageBuffer = PooledNetworkBuffer.Get())
            using (var writer = m_NetworkManager.NetworkWriterPool.GetWriter(messageBuffer))
            {
                writer.WriteUInt64Packed(hash);

                messageBuffer.CopyFrom(stream);

                if (!m_NetworkManager.IsServer)
                {
                    if (m_NetworkManager.LogLevel <= LogLevel.Error) m_NetworkManager.NetworkLog.LogWarning("Can not send named messages to multiple users as a client");
                    return;
                }

                m_NetworkManager.InternalMessageSender.Send(NetworkConstants.NAMED_MESSAGE, networkChannel, clientIds, messageBuffer);
            }
        }

        #endregion
    }
}
