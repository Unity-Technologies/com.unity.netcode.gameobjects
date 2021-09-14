using System;
using System.Runtime.InteropServices;

namespace Unity.Netcode.Messages
{
    internal struct ConnectionRequestMessage: INetworkMessage
    {
        public ulong ConfigHash;
        
        [StructLayout(LayoutKind.Explicit, Size = 512)]
        public struct ConnectionDataStorage: IFixedArrayStorage
        {
            
        }
        
        public FixedUnmanagedArray<byte, ConnectionDataStorage> ConnectionData;

        public bool ShouldSendConnectionData;

        public void Serialize(ref FastBufferWriter writer)
        {
            if (ShouldSendConnectionData)
            {
                if(!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(ConfigHash) +
                                     FastBufferWriter.GetWriteSize(ConnectionData)))
                {
                    throw new OverflowException(
                        $"Not enough space in the write buffer to serialize {nameof(ConnectionRequestMessage)}");
                }
                writer.WriteValue(ConfigHash);
                writer.WriteValue(ConnectionData.Count);
                writer.WriteValue(ConnectionData, ConnectionData.Count);
            }
            else
            {
                if(!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(ConfigHash)))
                {
                    throw new OverflowException(
                        $"Not enough space in the write buffer to serialize {nameof(ConnectionRequestMessage)}");
                }
                writer.WriteValue(ConfigHash);
            }
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            var networkManager = (NetworkManager) context.SystemOwner;
            if (!networkManager.IsServer)
            {
                return;
            }
            
            ConnectionRequestMessage message = new ConnectionRequestMessage();
            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(message.ConfigHash) +
                                         FastBufferWriter.GetWriteSize<int>()))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Incomplete connection request message given config - possible {nameof(NetworkConfig)} mismatch.");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return;
                }
                reader.ReadValue(out message.ConfigHash);
                
                if (!networkManager.NetworkConfig.CompareConfig(message.ConfigHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return;
                }
                
                reader.ReadValue(out int length);
                if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize<byte>() * length))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Incomplete connection request message.");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return;
                }
                reader.ReadValue(out message.ConnectionData, length);
            }
            else
            {
                if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(message.ConfigHash)))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Incomplete connection request message.");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return;
                }
                reader.ReadValue(out message.ConfigHash);
                
                if (!networkManager.NetworkConfig.CompareConfig(message.ConfigHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return;
                }
            }
            message.Handle(networkManager, context.SenderId);
        }

        public void Handle(NetworkManager networkManager, ulong senderId)
        {
            if (networkManager.PendingClients.TryGetValue(senderId, out PendingClient client))
            {
                // Set to pending approval to prevent future connection requests from being approved
                client.ConnectionState = PendingClient.State.PendingApproval;
            }

            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                // Note: Delegate creation allocates.
                // Note: ToArray() also allocates. :(
                networkManager.InvokeConnectionApproval(ConnectionData.ToArray(), senderId,
                    (createPlayerObject, playerPrefabHash, approved, position, rotation) =>
                    {
                        var localCreatePlayerObject = createPlayerObject;
                        networkManager.HandleApproval(senderId, localCreatePlayerObject, playerPrefabHash, approved,
                            position, rotation);
                    });
            }
            else
            {
                networkManager.HandleApproval(senderId, networkManager.NetworkConfig.PlayerPrefab != null, null, true, null, null);
            }
        }
    }
}