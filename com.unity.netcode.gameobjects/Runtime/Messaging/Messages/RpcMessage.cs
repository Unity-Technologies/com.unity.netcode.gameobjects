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
        public FastBufferWriter RpcWriteData;
        internal FastBufferReader RpcReadData;
        internal NetworkObject ReceivingNetworkObject;
        internal NetworkBehaviour ReceivingNetworkBehaviour;


        public unsafe void Serialize(FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(Header) + RpcWriteData.Length))
            {
                throw new OverflowException("Not enough space in the buffer to store RPC data.");
            }
            writer.WriteValue(Header);
            writer.WriteBytes(RpcWriteData.GetUnsafePtr(), RpcWriteData.Length);
        }

        public bool Deserialize(FastBufferReader reader, in NetworkContext context)
        {
            if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(Header)))
            {
                throw new OverflowException("Not enough space in the buffer to read RPC data.");
            }
            reader.ReadValue(out Header);

            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(Header.NetworkObjectId))
            {
                networkManager.SpawnManager.TriggerOnSpawn(Header.NetworkObjectId, reader, context);
                return false;
            }

            ReceivingNetworkObject = networkManager.SpawnManager.SpawnedObjects[Header.NetworkObjectId];
            ReceivingNetworkBehaviour = ReceivingNetworkObject.GetNetworkBehaviourAtOrderIndex(Header.NetworkBehaviourId);
            if (ReceivingNetworkBehaviour == null)
            {
                return false;
            }

            RpcReadData = reader;
            return true;
        }

        public void Handle(in NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var senderId = context.SenderId;
            if (NetworkManager.__rpc_func_table.ContainsKey(Header.NetworkMethodId))
            {

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

                NetworkManager.__rpc_func_table[Header.NetworkMethodId](ReceivingNetworkBehaviour, RpcReadData, rpcParams);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (NetworkManager.__rpc_name_table.TryGetValue(Header.NetworkMethodId, out var rpcMethodName))
                {
                    networkManager.NetworkMetrics.TrackRpcReceived(
                        senderId,
                        ReceivingNetworkObject,
                        rpcMethodName,
                        ReceivingNetworkBehaviour.__getTypeName(),
                        RpcReadData.Length);
                }
#endif
            }
        }
    }
}
