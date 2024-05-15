using System;
using Unity.Collections;
using UnityEngine;

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
            ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkObjectId);
            ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkBehaviourId);
            ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkRpcMethodId);

            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(metadata.NetworkObjectId))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, metadata.NetworkObjectId, reader, ref context, messageType);
                return false;
            }

            var networkObject = networkManager.SpawnManager.SpawnedObjects[metadata.NetworkObjectId];
            var networkBehaviour = networkManager.SpawnManager.SpawnedObjects[metadata.NetworkObjectId].GetNetworkBehaviourAtOrderIndex(metadata.NetworkBehaviourId);
            if (networkBehaviour == null)
            {
                return false;
            }

            if (!NetworkBehaviour.__rpc_func_table[networkBehaviour.GetType()].ContainsKey(metadata.NetworkRpcMethodId))
            {
                return false;
            }

            payload = new FastBufferReader(reader.GetUnsafePtrAtCurrentPosition(), Allocator.None, reader.Length - reader.Position);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (NetworkBehaviour.__rpc_name_table[networkBehaviour.GetType()].TryGetValue(metadata.NetworkRpcMethodId, out var rpcMethodName))
            {
                networkManager.NetworkMetrics.TrackRpcReceived(
                    context.SenderId,
                    networkObject,
                    rpcMethodName,
                    networkBehaviour.__getTypeName(),
                    reader.Length);
            }
#endif
            return true;
        }

        public static void Handle(ref NetworkContext context, ref RpcMetadata metadata, ref FastBufferReader payload, ref __RpcParams rpcParams)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(metadata.NetworkObjectId, out var networkObject))
            {
                throw new InvalidOperationException($"An RPC called on a {nameof(NetworkObject)} that is not in the spawned objects list. Please make sure the {nameof(NetworkObject)} is spawned before calling RPCs.");
            }
            var networkBehaviour = networkObject.GetNetworkBehaviourAtOrderIndex(metadata.NetworkBehaviourId);

            try
            {
                NetworkBehaviour.__rpc_func_table[networkBehaviour.GetType()][metadata.NetworkRpcMethodId](networkBehaviour, payload, rpcParams);
            }
            catch (Exception ex)
            {
                Debug.LogException(new Exception("Unhandled RPC exception!", ex));
                if (networkManager.LogLevel == LogLevel.Developer)
                {
                    Debug.Log($"RPC Table Contents");
                    foreach (var entry in NetworkBehaviour.__rpc_func_table[networkBehaviour.GetType()])
                    {
                        Debug.Log($"{entry.Key} | {entry.Value.Method.Name}");
                    }
                }
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

        public NetworkMessageType MessageType => NetworkMessageType.ServerRpc;

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

        public NetworkMessageType MessageType => NetworkMessageType.ClientRpc;

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

        public NetworkMessageType MessageType => NetworkMessageType.Rpc;

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
            var rpcParams = new __RpcParams
            {
                Ext = new RpcParams
                {
                    Receive = new RpcReceiveParams
                    {
                        SenderClientId = SenderClientId
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

        public NetworkMessageType MessageType => NetworkMessageType.ForwardServerRpc;

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
                    Debug.LogException(ex);
                }
            }
            else
            {
                NetworkLog.LogErrorServer($"Received {nameof(ForwardServerRpcMessage)} on client-{networkManager.LocalClientId}! Only DAHost may forward RPC messages!");
            }
            ServerRpcMessage.ReadBuffer.Dispose();
            ServerRpcMessage.WriteBuffer.Dispose();
        }

    }

    // DANGO-EXP TODO: REMOVE THIS
    internal struct ForwardClientRpcMessage : INetworkMessage
    {
        public int Version => 0;

        public NetworkMessageType MessageType => NetworkMessageType.ForwardClientRpc;
        
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
                NetworkLog.LogErrorServer($"Received {nameof(ForwardClientRpcMessage)} on client-{networkManager.LocalClientId}! Only DAHost may forward RPC messages!");
            }
            ClientRpcMessage.WriteBuffer.Dispose();
            ClientRpcMessage.ReadBuffer.Dispose();
        }
    }
}
