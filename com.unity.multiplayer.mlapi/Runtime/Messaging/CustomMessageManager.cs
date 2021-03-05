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
    public static class CustomMessagingManager
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
        public static event UnnamedMessageDelegate OnUnnamedMessage;

        internal static void InvokeUnnamedMessage(ulong clientId, Stream stream) => OnUnnamedMessage?.Invoke(clientId, stream);

        /// <summary>
        /// Sends unnamed message to a list of clients
        /// </summary>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="buffer">The message stream containing the data</param>
        /// <param name="networkChannel">The channel to send the data on</param>
        public static void SendUnnamedMessage(List<ulong> clientIds, NetworkBuffer buffer, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogWarning("Can not send unnamed messages to multiple users as a client");
                return;
            }

            InternalMessageSender.Send(NetworkConstants.UNNAMED_MESSAGE, networkChannel, clientIds, buffer);
        }

        /// <summary>
        /// Sends a unnamed message to a specific client
        /// </summary>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="buffer">The message stream containing the data</param>
        /// <param name="networkChannel">The channel tos end the data on</param>
        public static void SendUnnamedMessage(ulong clientId, NetworkBuffer buffer, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            InternalMessageSender.Send(clientId, NetworkConstants.UNNAMED_MESSAGE, networkChannel, buffer);
        }

        #endregion

        #region Named

        /// <summary>
        /// Delegate used to handle named messages
        /// </summary>
        public delegate void HandleNamedMessageDelegate(ulong sender, Stream payload);

        private static Dictionary<ulong, HandleNamedMessageDelegate> s_NamedMessageHandlers16 = new Dictionary<ulong, HandleNamedMessageDelegate>();
        private static Dictionary<ulong, HandleNamedMessageDelegate> s_NamedMessageHandlers32 = new Dictionary<ulong, HandleNamedMessageDelegate>();
        private static Dictionary<ulong, HandleNamedMessageDelegate> s_NamedMessageHandlers64 = new Dictionary<ulong, HandleNamedMessageDelegate>();

        internal static void InvokeNamedMessage(ulong hash, ulong sender, Stream stream)
        {
            if (NetworkManager.Singleton == null)
            {
                // We dont know what size to use. Try every (more collision prone)
                if (s_NamedMessageHandlers16.ContainsKey(hash))
                {
                    s_NamedMessageHandlers16[hash](sender, stream);
                }

                if (s_NamedMessageHandlers32.ContainsKey(hash))
                {
                    s_NamedMessageHandlers32[hash](sender, stream);
                }

                if (s_NamedMessageHandlers64.ContainsKey(hash))
                {
                    s_NamedMessageHandlers64[hash](sender, stream);
                }
            }
            else
            {
                // Only check the right size.
                if (NetworkManager.Singleton.NetworkConfig.RpcHashSize == HashSize.VarIntTwoBytes)
                {
                    if (s_NamedMessageHandlers16.ContainsKey(hash))
                    {
                        s_NamedMessageHandlers16[hash](sender, stream);
                    }
                }
                else if (NetworkManager.Singleton.NetworkConfig.RpcHashSize == HashSize.VarIntFourBytes)
                {
                    if (s_NamedMessageHandlers32.ContainsKey(hash))
                    {
                        s_NamedMessageHandlers32[hash](sender, stream);
                    }
                }
                else if (NetworkManager.Singleton.NetworkConfig.RpcHashSize == HashSize.VarIntEightBytes)
                {
                    if (s_NamedMessageHandlers64.ContainsKey(hash))
                    {
                        s_NamedMessageHandlers64[hash](sender, stream);
                    }
                }
            }
        }

        /// <summary>
        /// Registers a named message handler delegate.
        /// </summary>
        /// <param name="name">Name of the message.</param>
        /// <param name="callback">The callback to run when a named message is received.</param>
        public static void RegisterNamedMessageHandler(string name, HandleNamedMessageDelegate callback)
        {
            s_NamedMessageHandlers16[name.GetStableHash16()] = callback;
            s_NamedMessageHandlers32[name.GetStableHash32()] = callback;
            s_NamedMessageHandlers64[name.GetStableHash64()] = callback;
        }

        /// <summary>
        /// Unregisters a named message handler.
        /// </summary>
        /// <param name="name">The name of the message.</param>
        public static void UnregisterNamedMessageHandler(string name)
        {
            s_NamedMessageHandlers16.Remove(name.GetStableHash16());
            s_NamedMessageHandlers32.Remove(name.GetStableHash32());
            s_NamedMessageHandlers64.Remove(name.GetStableHash64());
        }

        /// <summary>
        /// Sends a named message
        /// </summary>
        /// <param name="name">The message name to send</param>
        /// <param name="clientId">The client to send the message to</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="networkChannel">The channel to send the data on</param>
        public static void SendNamedMessage(string name, ulong clientId, Stream stream, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            ulong hash = 0;
            switch (NetworkManager.Singleton.NetworkConfig.RpcHashSize)
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
            using (var writer = PooledNetworkWriter.Get(messageBuffer))
            {
                writer.WriteUInt64Packed(hash);

                messageBuffer.CopyFrom(stream);

                InternalMessageSender.Send(clientId, NetworkConstants.NAMED_MESSAGE, networkChannel, messageBuffer);
            }
        }

        /// <summary>
        /// Sends the named message
        /// </summary>
        /// <param name="name">The message name to send</param>
        /// <param name="clientIds">The clients to send to, sends to everyone if null</param>
        /// <param name="stream">The message stream containing the data</param>
        /// <param name="networkChannel">The channel to send the data on</param>
        public static void SendNamedMessage(string name, List<ulong> clientIds, Stream stream, NetworkChannel networkChannel = NetworkChannel.Internal)
        {
            ulong hash = 0;
            switch (NetworkManager.Singleton.NetworkConfig.RpcHashSize)
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
            using (var writer = PooledNetworkWriter.Get(messageBuffer))
            {
                writer.WriteUInt64Packed(hash);

                messageBuffer.CopyFrom(stream);

                if (!NetworkManager.Singleton.IsServer)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogWarning("Can not send named messages to multiple users as a client");
                    return;
                }

                InternalMessageSender.Send(NetworkConstants.NAMED_MESSAGE, networkChannel, clientIds, messageBuffer);
            }
        }

        #endregion
    }
}