using System;

namespace Unity.Netcode
{
    internal struct RpcMessage : INetworkMessage
    {
        public enum RpcType : byte
        {
            Server,
            Client
        }

        public struct HeaderData
        {
            public RpcType Type;
            public ulong NetworkObjectId;
            public ushort NetworkBehaviourId;
            public uint NetworkMethodId;
        }

        public HeaderData Header;
        public FastBufferWriter RpcData;


        public unsafe void Serialize(FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(Header) + RpcData.Length))
            {
                throw new OverflowException("Not enough space in the buffer to store RPC data.");
            }
            writer.WriteValue(Header);
            writer.WriteBytes(RpcData.GetUnsafePtr(), RpcData.Length);
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var message = new RpcMessage();
            if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(message.Header)))
            {
                throw new OverflowException("Not enough space in the buffer to read RPC data.");
            }
            reader.ReadValue(out message.Header);
            message.Handle(reader, context, (NetworkManager)context.SystemOwner, context.SenderId, true);
        }

        public void Handle(FastBufferReader reader, in NetworkContext context, NetworkManager networkManager, ulong senderId, bool canDefer)
        {
            if (NetworkManager.__rpc_func_table.ContainsKey(Header.NetworkMethodId))
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(Header.NetworkObjectId))
                {
                    if (canDefer)
                    {
                        networkManager.SpawnManager.TriggerOnSpawn(Header.NetworkObjectId, reader, context);
                    }
                    else
                    {
                        NetworkLog.LogError($"Tried to invoke an RPC on a non-existent {nameof(NetworkObject)} with {nameof(canDefer)}=false");
                    }
                    return;
                }

                var networkObject = networkManager.SpawnManager.SpawnedObjects[Header.NetworkObjectId];

                var networkBehaviour = networkObject.GetNetworkBehaviourAtOrderIndex(Header.NetworkBehaviourId);
                if (networkBehaviour == null)
                {
                    return;
                }

                var rpcParams = new __RpcParams();
                switch (Header.Type)
                {
                    case RpcType.Server:
                        rpcParams.Server = new ServerRpcParams
                        {
                            Receive = new ServerRpcReceiveParams
                            {
                                SenderClientId = senderId
                            }
                        };
                        break;
                    case RpcType.Client:
                        rpcParams.Client = new ClientRpcParams
                        {
                            Receive = new ClientRpcReceiveParams
                            {
                            }
                        };
                        break;
                }

                NetworkManager.__rpc_func_table[Header.NetworkMethodId](networkBehaviour, reader, rpcParams);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (NetworkManager.__rpc_name_table.TryGetValue(Header.NetworkMethodId, out var rpcMethodName))
                {
                    networkManager.NetworkMetrics.TrackRpcReceived(
                        senderId,
                        networkObject,
                        rpcMethodName,
                        networkBehaviour.__getTypeName(),
                        reader.Length);
                }
#endif
            }
        }
    }
}
