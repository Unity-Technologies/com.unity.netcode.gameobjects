using System;
using System.Collections.Generic;
using System.IO;

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
        /// <param name="stream">The stream containing the message data</param>
        public delegate void UnnamedMessageDelegate(ulong clientId, Stream stream);

        /// <summary>
        /// Event invoked when unnamed messages arrive
        /// </summary>
        public event UnnamedMessageDelegate OnUnnamedMessage;

        internal void InvokeUnnamedMessage(ulong clientId, Stream stream)
        {
            OnUnnamedMessage?.Invoke(clientId, stream);
            m_NetworkManager.NetworkMetrics.TrackUnnamedMessageReceived(clientId, stream.SafeGetLengthOrDefault());
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
                throw new InvalidOperationException("Can not send unnamed messages to multiple users as a client");
            }

            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(
                MessageQueueContainer.MessageType.UnnamedMessage, networkChannel,
                clientIds.ToArray(), NetworkUpdateLoop.UpdateStage);
            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    buffer.Position = 0;
                    buffer.CopyTo(nonNullContext.NetworkWriter.GetStream());
                }
            }

            m_NetworkManager.NetworkMetrics.TrackUnnamedMessageSent(clientIds, buffer.Length);
        }

        /// <summary>
        /// Sends a unnamed message to a specific client
        /// </summary>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="buffer">The message stream containing the data</param>
        /// <param name="networkChannel">The channel tos end the data on</param>
        public void SendUnnamedMessage(ulong clientId, NetworkBuffer buffer, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(
                MessageQueueContainer.MessageType.UnnamedMessage, networkChannel,
                new[] { clientId }, NetworkUpdateLoop.UpdateStage);
            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    m_NetworkManager.NetworkMetrics.TrackUnnamedMessageSent(clientId, buffer.Position);
                    buffer.Position = 0;
                    buffer.CopyTo(nonNullContext.NetworkWriter.GetStream());
                }
            }
        }

        /// <summary>
        /// Delegate used to handle named messages
        /// </summary>
        public delegate void HandleNamedMessageDelegate(ulong sender, Stream payload);

        private Dictionary<ulong, HandleNamedMessageDelegate> m_NamedMessageHandlers32 = new Dictionary<ulong, HandleNamedMessageDelegate>();
        private Dictionary<ulong, HandleNamedMessageDelegate> m_NamedMessageHandlers64 = new Dictionary<ulong, HandleNamedMessageDelegate>();

        private Dictionary<ulong, string> m_MessageHandlerNameLookup32 = new Dictionary<ulong, string>();
        private Dictionary<ulong, string> m_MessageHandlerNameLookup64 = new Dictionary<ulong, string>();

        internal void InvokeNamedMessage(ulong hash, ulong sender, Stream stream)
        {
            var bytesCount = stream.SafeGetLengthOrDefault();

            if (m_NetworkManager == null)
            {
                // We dont know what size to use. Try every (more collision prone)
                if (m_NamedMessageHandlers32.TryGetValue(hash, out HandleNamedMessageDelegate messageHandler32))
                {
                    messageHandler32(sender, stream);
                    m_NetworkManager.NetworkMetrics.TrackNamedMessageReceived(sender, m_MessageHandlerNameLookup32[hash], bytesCount);
                }

                if (m_NamedMessageHandlers64.TryGetValue(hash, out HandleNamedMessageDelegate messageHandler64))
                {
                    messageHandler64(sender, stream);
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
                            messageHandler32(sender, stream);
                            m_NetworkManager.NetworkMetrics.TrackNamedMessageReceived(sender, m_MessageHandlerNameLookup32[hash], bytesCount);
                        }
                        break;
                    case HashSize.VarIntEightBytes:
                        if (m_NamedMessageHandlers64.TryGetValue(hash, out HandleNamedMessageDelegate messageHandler64))
                        {
                            messageHandler64(sender, stream);
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
                case HashSize.VarIntFourBytes:
                    hash = XXHash.Hash32(name);
                    break;
                case HashSize.VarIntEightBytes:
                    hash = XXHash.Hash64(name);
                    break;
            }

            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(
                MessageQueueContainer.MessageType.NamedMessage, networkChannel,
                new[] { clientId }, NetworkUpdateLoop.UpdateStage);
            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    var bufferSizeCapture = new CommandContextSizeCapture(nonNullContext);
                    bufferSizeCapture.StartMeasureSegment();

                    nonNullContext.NetworkWriter.WriteUInt64Packed(hash);

                    stream.Position = 0;
                    stream.CopyTo(nonNullContext.NetworkWriter.GetStream());

                    var size = bufferSizeCapture.StopMeasureSegment();

                    m_NetworkManager.NetworkMetrics.TrackNamedMessageSent(clientId, name, size);
                }
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
            if (!m_NetworkManager.IsServer)
            {
                throw new InvalidOperationException("Can not send unnamed messages to multiple users as a client");
            }

            ulong hash = 0;
            switch (m_NetworkManager.NetworkConfig.RpcHashSize)
            {
                case HashSize.VarIntFourBytes:
                    hash = XXHash.Hash32(name);
                    break;
                case HashSize.VarIntEightBytes:
                    hash = XXHash.Hash64(name);
                    break;
            }

            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(
                MessageQueueContainer.MessageType.NamedMessage, networkChannel,
                clientIds.ToArray(), NetworkUpdateLoop.UpdateStage);
            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    var bufferSizeCapture = new CommandContextSizeCapture(nonNullContext);
                    bufferSizeCapture.StartMeasureSegment();

                    nonNullContext.NetworkWriter.WriteUInt64Packed(hash);

                    stream.Position = 0;
                    stream.CopyTo(nonNullContext.NetworkWriter.GetStream());

                    var size = bufferSizeCapture.StopMeasureSegment();
                    m_NetworkManager.NetworkMetrics.TrackNamedMessageSent(clientIds, name, size);
                }
            }
        }
    }
}
