namespace Unity.Netcode
{
    internal struct ConnectionRequestMessage : INetworkMessage
    {
        public ulong ConfigHash;

        public byte[] ConnectionData;

        public bool ShouldSendConnectionData;

        public void Serialize(FastBufferWriter writer)
        {
            if (ShouldSendConnectionData)
            {
                writer.WriteValueSafe(ConfigHash);
                writer.WriteValueSafe(ConnectionData);
            }
            else
            {
                writer.WriteValueSafe(ConfigHash);
            }
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsServer)
            {
                return;
            }

            var message = new ConnectionRequestMessage();
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

                reader.ReadValueSafe(out message.ConnectionData);
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
                networkManager.InvokeConnectionApproval(ConnectionData, senderId,
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
