using Unity.Collections;

namespace Unity.Netcode
{
    internal struct ConnectionRequestMessage : INetworkMessage
    {
        public int Version => 0;

        public ulong ConfigHash;

        public byte[] ConnectionData;

        public bool ShouldSendConnectionData;

        public NativeArray<MessageVersionData> MessageVersions;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            // ============================================================
            // BEGIN FORBIDDEN SEGMENT
            // DO NOT CHANGE THIS HEADER. Everything added to this message
            // must go AFTER the message version header.
            // ============================================================
            BytePacker.WriteValueBitPacked(writer, MessageVersions.Length);
            foreach (var messageVersion in MessageVersions)
            {
                messageVersion.Serialize(writer);
            }
            // ============================================================
            // END FORBIDDEN SEGMENT
            // ============================================================

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

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsServer)
            {
                return false;
            }

            // ============================================================
            // BEGIN FORBIDDEN SEGMENT
            // DO NOT CHANGE THIS HEADER. Everything added to this message
            // must go AFTER the message version header.
            // ============================================================
            ByteUnpacker.ReadValueBitPacked(reader, out int length);
            for (var i = 0; i < length; ++i)
            {
                var messageVersion = new MessageVersionData();
                messageVersion.Deserialize(reader);
                networkManager.ConnectionManager.MessageManager.SetVersion(context.SenderId, messageVersion.Hash, messageVersion.Version);

                // Update the received version since this message will always be passed version 0, due to the map not
                // being initialized until just now.
                var messageType = networkManager.ConnectionManager.MessageManager.GetMessageForHash(messageVersion.Hash);
                if (messageType == typeof(ConnectionRequestMessage))
                {
                    receivedMessageVersion = messageVersion.Version;
                }
            }
            // ============================================================
            // END FORBIDDEN SEGMENT
            // ============================================================

            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(ConfigHash) + FastBufferWriter.GetWriteSize<int>()))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Incomplete connection request message given config - possible {nameof(NetworkConfig)} mismatch.");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }

                reader.ReadValue(out ConfigHash);

                if (!networkManager.NetworkConfig.CompareConfig(ConfigHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }

                reader.ReadValueSafe(out ConnectionData);
            }
            else
            {
                if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(ConfigHash)))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Incomplete connection request message.");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }
                reader.ReadValue(out ConfigHash);

                if (!networkManager.NetworkConfig.CompareConfig(ConfigHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }
            }

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var senderId = context.SenderId;

            if (networkManager.ConnectionManager.PendingClients.TryGetValue(senderId, out PendingClient client))
            {
                // Set to pending approval to prevent future connection requests from being approved
                client.ConnectionState = PendingClient.State.PendingApproval;
            }

            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                var messageRequest = this;
                networkManager.ConnectionManager.ApproveConnection(ref messageRequest, ref context);
            }
            else
            {
                var response = new NetworkManager.ConnectionApprovalResponse
                {
                    Approved = true,
                    CreatePlayerObject = networkManager.NetworkConfig.PlayerPrefab != null
                };
                networkManager.ConnectionManager.HandleConnectionApproval(senderId, response);
            }
        }
    }
}
