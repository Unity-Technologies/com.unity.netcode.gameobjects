using System;
using Unity.Collections;
using Unity.Netcode.Logging;

namespace Unity.Netcode
{
    internal static class RpcMessageHelpers
    {
        public static unsafe void Serialize(ref FastBufferWriter writer, ref RpcMetadata metadata, ref FastBufferWriter payload)
        {
            BytePacker.WriteValueBitPacked(writer, metadata.NetworkObjectId);
            BytePacker.WriteValueBitPacked(writer, metadata.NetworkBehaviourId);
            BytePacker.WriteValueBitPacked(writer, metadata.NetworkRpcMethodId);
            writer.WriteBytesSafe(payload.GetUnsafePtr(), payload.Length);
        }

        public static unsafe bool Deserialize(ref FastBufferReader reader, ref NetworkContext context, ref RpcMetadata metadata, ref FastBufferReader payload, string messageType)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkObjectId);

            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(metadata.NetworkObjectId, out var networkObject))
            {
                networkManager.Log.Info(new Context(LogLevel.Developer, $"Received RPC message for {nameof(NetworkObject)} that doesn't exist yet. Deferring the message.").AddInfo("SenderClientId", context.SenderId).AddInfo(nameof(NetworkObject.NetworkObjectId), metadata.NetworkObjectId).AddInfo(nameof(RpcMetadata.NetworkRpcMethodId), metadata.NetworkRpcMethodId));
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, metadata.NetworkObjectId, reader, ref context, messageType);
                return false;
            }

            ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkBehaviourId);
            ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkRpcMethodId);

            payload = new FastBufferReader(reader.GetUnsafePtrAtCurrentPosition(), Allocator.None, reader.Length - reader.Position);
            return true;
        }

        public static void Handle(ref NetworkContext context, ref RpcMetadata metadata, ref FastBufferReader payload, ref __RpcParams rpcParams)
        {
            var networkManager = (NetworkManager)context.SystemOwner;

            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(metadata.NetworkObjectId, out var networkObject))
            {
                // If the NetworkObject no longer exists then just log a warning when developer mode logging is enabled and exit.
                // This can happen if NetworkObject is despawned and a client sends an RPC before receiving the despawn message.
                networkManager.Log.Warning(new Context(LogLevel.Developer, $"Received RPC message for {nameof(NetworkObject)} that doesn't exist yet. Deferring the message.").AddInfo(nameof(NetworkObject.NetworkObjectId), metadata.NetworkObjectId).AddInfo(nameof(RpcMetadata.NetworkRpcMethodId), metadata.NetworkRpcMethodId));
                return;
            }

            var networkBehaviour = networkObject.GetNetworkBehaviourAtOrderIndex(metadata.NetworkBehaviourId);
            if (networkBehaviour == null)
            {
                networkManager.Log.Error(new Context(LogLevel.Normal, $"Received RPC message for {nameof(NetworkBehaviour)} that doesn't exist. Dropping RPC message").AddNetworkObject(networkObject).AddInfo(nameof(NetworkBehaviour.NetworkBehaviourId), networkBehaviour.NetworkBehaviourId));
                return;
            }

            var type = networkBehaviour.GetType();
            if (!NetworkBehaviour.__rpc_func_table.TryGetValue(type, out var rpcsForBehaviour) || !NetworkBehaviour.__rpc_permission_table.TryGetValue(type, out var permissionsTable))
            {
                networkManager.Log.Error(new Context(LogLevel.Normal, $"Rpc table doesn't have RPCs registered for this {nameof(NetworkBehaviour)}. Dropping RPC message").AddNetworkObject(networkObject).AddInfo(nameof(NetworkBehaviour.NetworkBehaviourId), networkBehaviour.NetworkBehaviourId).AddInfo(nameof(NetworkBehaviour), type));
                return;
            }
            if (!rpcsForBehaviour.TryGetValue(metadata.NetworkRpcMethodId, out var receiveHandler) || !permissionsTable.TryGetValue(metadata.NetworkRpcMethodId, out var permission))
            {
                networkManager.Log.Error(new Context(LogLevel.Normal, "Received RPC message for RPC receiver that doesn't exist. Dropping RPC message").AddNetworkBehaviour(networkBehaviour).AddInfo(nameof(RpcMetadata.NetworkRpcMethodId), metadata.NetworkRpcMethodId));
                return;
            }

            if ((permission == RpcInvokePermission.Server && rpcParams.SenderId != NetworkManager.ServerClientId) ||
                (permission == RpcInvokePermission.Owner && rpcParams.SenderId != networkObject.OwnerClientId))
            {
                networkManager.Log.ErrorServer(new Context(LogLevel.Normal, "Rpc message received from a client without permission to perform this operation!. Dropping RPC message").AddNetworkBehaviour(networkBehaviour));
                return;
            }

#if MULTIPLAYER_TOOLS && (DEBUG || UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE)
            networkBehaviour.TrackRpcMetricsReceive(ref metadata, ref context, payload.Length);
#endif

            try
            {
                receiveHandler(networkBehaviour, payload, rpcParams);
            }
            catch (Exception ex)
            {
                networkManager.Log.Exception(ex, new Context(LogLevel.Error, "Unhandled RPC exception!").AddNetworkBehaviour(networkBehaviour));

                var methodId = metadata.NetworkRpcMethodId;
                networkManager.Log.Info(new Context(LogLevel.Developer, "RPC Table Contents").AddCollection(rpcsForBehaviour, entry =>
                {
                    var invokePermission = NetworkBehaviour.__rpc_permission_table[networkBehaviour.GetType()][methodId];
                    return $"{entry.Key} | {entry.Value.Method.Name} | {invokePermission}";
                }));
            }
        }
    }

    internal struct RpcMetadata : INetworkSerializeByMemcpy
    {
        public ulong NetworkObjectId;
        public ushort NetworkBehaviourId;
        public uint NetworkRpcMethodId;
    }

    internal struct ServerRpcMessage : INetworkMessage
    {
        public int Version => 0;

        public RpcMetadata Metadata;

        public FastBufferWriter WriteBuffer;
        public FastBufferReader ReadBuffer;

        private const string k_Name = "ServerRpcMessage";

        public unsafe void Serialize(FastBufferWriter writer, int targetVersion)
        {
            RpcMessageHelpers.Serialize(ref writer, ref Metadata, ref WriteBuffer);
        }

        public unsafe bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            return RpcMessageHelpers.Deserialize(ref reader, ref context, ref Metadata, ref ReadBuffer, k_Name);
        }

        public void Handle(ref NetworkContext context)
        {
            var rpcParams = new __RpcParams
            {
                SenderId = context.SenderId,
                Server = new ServerRpcParams
                {
                    Receive = new ServerRpcReceiveParams
                    {
                        SenderClientId = context.SenderId
                    }
                }
            };
            RpcMessageHelpers.Handle(ref context, ref Metadata, ref ReadBuffer, ref rpcParams);
        }
    }

    internal struct ClientRpcMessage : INetworkMessage
    {
        public int Version => 0;

        public RpcMetadata Metadata;

        public FastBufferWriter WriteBuffer;
        public FastBufferReader ReadBuffer;

        private const string k_Name = "ClientRpcMessage";

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            RpcMessageHelpers.Serialize(ref writer, ref Metadata, ref WriteBuffer);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            return RpcMessageHelpers.Deserialize(ref reader, ref context, ref Metadata, ref ReadBuffer, k_Name);
        }

        public void Handle(ref NetworkContext context)
        {
            var rpcParams = new __RpcParams
            {
                SenderId = NetworkManager.ServerClientId,
                Client = new ClientRpcParams
                {
                    Receive = new ClientRpcReceiveParams
                    {
                    }
                }
            };
            RpcMessageHelpers.Handle(ref context, ref Metadata, ref ReadBuffer, ref rpcParams);
        }
    }

    internal struct RpcMessage : INetworkMessage
    {
        public int Version => 0;

        public RpcMetadata Metadata;
        public ulong SenderClientId;

        public FastBufferWriter WriteBuffer;
        public FastBufferReader ReadBuffer;

        private const string k_Name = "RpcMessage";

        public unsafe void Serialize(FastBufferWriter writer, int targetVersion)
        {
            BytePacker.WriteValuePacked(writer, SenderClientId);
            RpcMessageHelpers.Serialize(ref writer, ref Metadata, ref WriteBuffer);
        }

        public unsafe bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            ByteUnpacker.ReadValuePacked(reader, out SenderClientId);

            return RpcMessageHelpers.Deserialize(ref reader, ref context, ref Metadata, ref ReadBuffer, k_Name);
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;

            // If the server is receiving, always trust the transportId for the SenderClientId
            // Otherwise, use the proxied id.
            var senderId = networkManager.IsServer ? context.SenderId : SenderClientId;

            var rpcParams = new __RpcParams
            {
                SenderId = senderId,
                Ext = new RpcParams
                {
                    Receive = new RpcReceiveParams
                    {
                        SenderClientId = senderId
                    }
                }
            };
            RpcMessageHelpers.Handle(ref context, ref Metadata, ref ReadBuffer, ref rpcParams);
        }
    }

    // DANGO-EXP TODO: REMOVE THIS
    internal struct ForwardServerRpcMessage : INetworkMessage
    {
        public int Version => 0;
        public ulong OwnerId;
        public NetworkDelivery NetworkDelivery;
        public ServerRpcMessage ServerRpcMessage;

        public unsafe void Serialize(FastBufferWriter writer, int targetVersion)
        {
            writer.WriteValueSafe(OwnerId);
            writer.WriteValueSafe(NetworkDelivery);
            ServerRpcMessage.Serialize(writer, targetVersion);
        }

        public unsafe bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            reader.ReadValueSafe(out OwnerId);
            reader.ReadValueSafe(out NetworkDelivery);
            ServerRpcMessage.ReadBuffer = new FastBufferReader(reader, Allocator.Persistent, reader.Length - reader.Position, sizeof(RpcMetadata));

            // If deserializing failed or this message was deferred.
            if (!ServerRpcMessage.Deserialize(reader, ref context, receivedMessageVersion))
            {
                // release this reader as the handler will either be invoked later (deferred) or will not be invoked at all.
                ServerRpcMessage.ReadBuffer.Dispose();
                return false;
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (networkManager.DAHost)
            {
                try
                {
                    // Since this is temporary, we will not be collection metrics for this.
                    // DAHost just forwards the message to the owner
                    ServerRpcMessage.WriteBuffer = new FastBufferWriter(ServerRpcMessage.ReadBuffer.Length, Allocator.TempJob);
                    ServerRpcMessage.WriteBuffer.WriteBytesSafe(ServerRpcMessage.ReadBuffer.ToArray());
                    networkManager.ConnectionManager.SendMessage(ref ServerRpcMessage, NetworkDelivery, OwnerId);
                }
                catch (Exception ex)
                {
                    networkManager.Log.Exception(ex);
                }
            }
            else
            {
                networkManager.Log.ErrorServer(new Context(LogLevel.Error, $"Received {nameof(ForwardServerRpcMessage)} when not the DAHost! Only DAHost may forward RPC messages!").AddInfo("SenderClientId", context.SenderId));
            }
            ServerRpcMessage.ReadBuffer.Dispose();
            ServerRpcMessage.WriteBuffer.Dispose();
        }

    }

    // DANGO-EXP TODO: REMOVE THIS
    internal struct ForwardClientRpcMessage : INetworkMessage
    {
        public int Version => 0;
        public bool BroadCast;
        public ulong[] TargetClientIds;
        public NetworkDelivery NetworkDelivery;
        public ClientRpcMessage ClientRpcMessage;

        public unsafe void Serialize(FastBufferWriter writer, int targetVersion)
        {
            if (TargetClientIds == null)
            {
                BroadCast = true;
                writer.WriteValueSafe(BroadCast);
            }
            else
            {
                BroadCast = false;
                writer.WriteValueSafe(BroadCast);
                writer.WriteValueSafe(TargetClientIds);
            }
            writer.WriteValueSafe(NetworkDelivery);
            ClientRpcMessage.Serialize(writer, targetVersion);
        }

        public unsafe bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            reader.ReadValueSafe(out BroadCast);

            if (!BroadCast)
            {
                reader.ReadValueSafe(out TargetClientIds);
            }

            reader.ReadValueSafe(out NetworkDelivery);

            ClientRpcMessage.ReadBuffer = new FastBufferReader(reader, Allocator.Persistent, reader.Length - reader.Position, sizeof(RpcMetadata));
            // If deserializing failed or this message was deferred.
            if (!ClientRpcMessage.Deserialize(reader, ref context, receivedMessageVersion))
            {
                // release this reader as the handler will either be invoked later (deferred) or will not be invoked at all.
                ClientRpcMessage.ReadBuffer.Dispose();
                return false;
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (networkManager.DAHost)
            {
                ClientRpcMessage.WriteBuffer = new FastBufferWriter(ClientRpcMessage.ReadBuffer.Length, Allocator.TempJob);
                ClientRpcMessage.WriteBuffer.WriteBytesSafe(ClientRpcMessage.ReadBuffer.ToArray());
                // Since this is temporary, we will not be collection metrics for this.
                // DAHost just forwards the message to the clients
                if (BroadCast)
                {
                    networkManager.ConnectionManager.SendMessage(ref ClientRpcMessage, NetworkDelivery, networkManager.ConnectedClientsIds);
                }
                else
                {
                    networkManager.ConnectionManager.SendMessage(ref ClientRpcMessage, NetworkDelivery, TargetClientIds);
                }
            }
            else
            {
                networkManager.Log.ErrorServer(new Context(LogLevel.Error, $"Received {nameof(ForwardClientRpcMessage)} when not the DAHost! Only DAHost may forward RPC messages!").AddInfo("SenderClientId", context.SenderId));
            }
            ClientRpcMessage.WriteBuffer.Dispose();
            ClientRpcMessage.ReadBuffer.Dispose();
        }
    }
}
