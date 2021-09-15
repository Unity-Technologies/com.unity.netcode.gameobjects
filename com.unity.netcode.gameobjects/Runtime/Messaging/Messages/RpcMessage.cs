using System;

namespace Unity.Netcode.Messages
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
        public FastBufferWriter RPCData;


        public unsafe void Serialize(ref FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(Header) + RPCData.Length))
            {
                throw new OverflowException("Not enough space in the buffer to store RPC data.");
            }
            writer.WriteValue(Header);
            writer.WriteBytes(RPCData.GetUnsafePtr(), RPCData.Length);
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            var message = new RpcMessage();
            if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(message.Header)))
            {
                throw new OverflowException("Not enough space in the buffer to read RPC data.");
            }
            reader.ReadValue(out message.Header);
            message.Handle(ref reader, (NetworkManager)context.SystemOwner, context.SenderId);
        }

        public void Handle(ref FastBufferReader reader, NetworkManager networkManager, ulong senderId)
        {
            if (NetworkManager.__rpc_func_table.ContainsKey(Header.NetworkMethodId))
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(Header.NetworkObjectId))
                {
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

                NetworkManager.__rpc_func_table[Header.NetworkMethodId](networkBehaviour, ref reader, rpcParams);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (NetworkManager.__rpc_name_table.TryGetValue(Header.NetworkMethodId, out var rpcMethodName))
                {
                    networkManager.NetworkMetrics.TrackRpcReceived(
                        senderId,
                        Header.NetworkObjectId,
                        rpcMethodName,
                        networkBehaviour.__getTypeName(),
                        reader.Length);
                }
#endif
            }
        }
    }
}
