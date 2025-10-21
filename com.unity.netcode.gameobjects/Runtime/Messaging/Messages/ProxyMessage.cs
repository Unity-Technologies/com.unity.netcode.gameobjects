using Unity.Collections;

namespace Unity.Netcode
{
    internal struct ProxyMessage : INetworkMessage
    {
        public NativeArray<ulong> TargetClientIds;
        public NetworkDelivery Delivery;
        public RpcMessage WrappedMessage;

        // Version of ProxyMessage and RpcMessage must always match.
        // If ProxyMessage needs to change, increment RpcMessage's version
        public int Version => new RpcMessage().Version;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            writer.WriteValueSafe(TargetClientIds);
            BytePacker.WriteValuePacked(writer, Delivery);
            WrappedMessage.Serialize(writer, targetVersion);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            reader.ReadValueSafe(out TargetClientIds, Allocator.Temp);
            ByteUnpacker.ReadValuePacked(reader, out Delivery);
            WrappedMessage = new RpcMessage();
            WrappedMessage.Deserialize(reader, ref context, receivedMessageVersion);
            return true;
        }

        public unsafe void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(WrappedMessage.Metadata.NetworkObjectId, out var networkObject))
            {
                // If the NetworkObject no longer exists then just log a warning when developer mode logging is enabled and exit.
                // This can happen if NetworkObject is despawned and a client sends an RPC before receiving the despawn message.
                if (networkManager.LogLevel == LogLevel.Developer)
                {
                    NetworkLog.LogWarning($"[{WrappedMessage.Metadata.NetworkObjectId}, {WrappedMessage.Metadata.NetworkBehaviourId}, {WrappedMessage.Metadata.NetworkRpcMethodId}] An RPC called on a {nameof(NetworkObject)} that is not in the spawned objects list. Please make sure the {nameof(NetworkObject)} is spawned before calling RPCs.");
                }
                return;
            }
            var observers = networkObject.Observers;

            // Validate message if server
            if (networkManager.IsServer)
            {
                var networkBehaviour = networkObject.GetNetworkBehaviourAtOrderIndex(WrappedMessage.Metadata.NetworkBehaviourId);

                RpcInvokePermission permission = NetworkBehaviour.__rpc_permission_table[networkBehaviour.GetType()][WrappedMessage.Metadata.NetworkRpcMethodId];
                bool hasPermission = permission switch
                {
                    RpcInvokePermission.Everyone => true,
                    RpcInvokePermission.Server => context.SenderId == networkManager.LocalClientId,
                    RpcInvokePermission.Owner => context.SenderId == networkBehaviour.OwnerClientId,
                    _ => false,
                };

                // Do not handle the message if the sender does not have permission to do so.
                if (!hasPermission)
                {
                    if (networkManager.LogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogErrorServer($"Rpc message received from client-{context.SenderId} who does not have permission to perform this operation!");
                    }
                    return;
                }

                WrappedMessage.SenderClientId = context.SenderId;
            }


            var nonServerIds = new NativeList<ulong>(Allocator.Temp);
            foreach (var client in TargetClientIds)
            {
                if (!observers.Contains(client))
                {
                    continue;
                }

                if (client == NetworkManager.ServerClientId)
                {
                    WrappedMessage.Handle(ref context);
                }
                else
                {
                    nonServerIds.Add(client);
                }
            }

            WrappedMessage.WriteBuffer = new FastBufferWriter(WrappedMessage.ReadBuffer.Length, Allocator.Temp);

            using (WrappedMessage.WriteBuffer)
            {
                WrappedMessage.WriteBuffer.WriteBytesSafe(WrappedMessage.ReadBuffer.GetUnsafePtr(), WrappedMessage.ReadBuffer.Length);
                networkManager.MessageManager.SendMessage(ref WrappedMessage, Delivery, nonServerIds);
            }
        }
    }
}
