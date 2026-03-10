#if UNIFIED_NETCODE && OUT_OF_BAND_RPC
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Netcode.Transports.UTP;

namespace Unity.Netcode.Unified
{
    [BurstCompile]
    internal struct TransportRpc : IOutOfBandRpcCommand, IRpcCommandSerializer<TransportRpc>
    {
        public FixedList4096Bytes<byte> Buffer;
        public ulong Order;
        
        public unsafe void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in TransportRpc data)
        {
            writer.WriteULong(data.Order);
            writer.WriteInt(data.Buffer.Length);
            var span = new Span<byte>(data.Buffer.GetUnsafePtr(), data.Buffer.Length);
            writer.WriteBytes(span);
        }

        public unsafe void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref TransportRpc data)
        {
            data.Order = reader.ReadULong();
            var length = reader.ReadInt();
            data.Buffer = new FixedList4096Bytes<byte>()
            {
                Length = length
            };
            var span = new Span<byte>(data.Buffer.GetUnsafePtr(), length);
            reader.ReadBytes(span);
        }

        [BurstCompile(DisableDirectCall = true)]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<TransportRpc, TransportRpc>(ref parameters);
        }
        
        private static readonly PortableFunctionPointer<RpcExecutor.ExecuteDelegate> k_InvokeExecuteFunctionPointer = new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return k_InvokeExecuteFunctionPointer;
        }
    }

    [UpdateInGroup(typeof(RpcCommandRequestSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    [BurstCompile]
    internal partial struct TransportRpcCommandRequestSystem : ISystem
    {
        private RpcCommandRequest<TransportRpc, TransportRpc> m_Request;

        [BurstCompile]
        internal struct SendRpc : IJobChunk
        {
            public RpcCommandRequest<TransportRpc, TransportRpc>.SendRpcData Data;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Data.Execute(chunk, unfilteredChunkIndex);
            }
        }

        public void OnCreate(ref SystemState state)
        {
            m_Request.OnCreate(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sendJob = new SendRpc { Data = m_Request.InitJobData(ref state) };
            state.Dependency = sendJob.Schedule(m_Request.Query, state.Dependency);
        }
    }
    

    internal partial class UnifiedNetcodeUpdateSystem : SystemBase
    {
        public UnifiedNetcodeTransport Transport;

        public List<Connection> DisconnectQueue = new List<Connection>();
        
        public void Disconnect(Connection connection)
        {
            DisconnectQueue.Add(connection);
        }

        protected override void OnUpdate()
        {
            using var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach(var (request, rpc, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRW<TransportRpc>>().WithEntityAccess())
            {
                var connectionId = SystemAPI.GetComponent<NetworkId>(request.ValueRO.SourceConnection).Value;

                var buffer = rpc.ValueRW.Buffer;
                try
                {
                    Transport.DispatchMessage(connectionId, buffer, rpc.ValueRO.Order);
                }
                finally
                {
                    commandBuffer.DestroyEntity(entity);
                }
            }

            foreach (var connection in DisconnectQueue)
            {
                commandBuffer.AddComponent<NetworkStreamRequestDisconnect>(connection.ConnectionEntity);
            }
            DisconnectQueue.Clear();

            commandBuffer.Playback(EntityManager);
                
        }
    }

    internal class UnifiedNetcodeTransport : NetworkTransport
    {
        private const int k_MaxPacketSize = 1300;

        private int m_ServerClientId = -1;
        public override ulong ServerClientId => (ulong)m_ServerClientId;

        private NetworkManager m_NetworkManager;
        
        private IRealTimeProvider m_RealTimeProvider;

        private class ConnectionInfo
        {
            public BatchedSendQueue SendQueue;
            public BatchedReceiveQueue ReceiveQueue;
            public Connection Connection;
            public ulong LastSent;
            public ulong LastReceived;
            public Dictionary<ulong, FixedList4096Bytes<byte>> DeferredMessages;
        }
 
        private Dictionary<int, ConnectionInfo> m_Connections;
        
        internal void DispatchMessage(int connectionId, FixedList4096Bytes<byte> buffer, ulong order)
        {
            var connectionInfo = m_Connections[connectionId];

            if (order != connectionInfo.LastReceived + 1)
            {
                if (connectionInfo.DeferredMessages == null)
                {
                    connectionInfo.DeferredMessages = new Dictionary<ulong, FixedList4096Bytes<byte>>();
                }

                connectionInfo.DeferredMessages[order] = buffer;
                return;
            }
            
            var reader = new DataStreamReader(buffer.ToNativeArray(Allocator.Temp));
            if (connectionInfo.ReceiveQueue == null)
            {
                connectionInfo.ReceiveQueue = new BatchedReceiveQueue(reader);
            }
            else
            {
                connectionInfo.ReceiveQueue.PushReader(reader);
            }

            connectionInfo.LastReceived = order;
            if (connectionInfo.DeferredMessages != null)
            {
                var next = order + 1;
                while (connectionInfo.DeferredMessages.Remove(next, out var nextBuffer))
                {
                    reader = new DataStreamReader(nextBuffer.ToNativeArray(Allocator.Temp));
                    connectionInfo.ReceiveQueue.PushReader(reader);
                    connectionInfo.LastReceived = next;
                    ++next;
                }
            }

            var message = connectionInfo.ReceiveQueue.PopMessage();
            while (message.Count != 0)
            {
                InvokeOnTransportEvent(NetworkEvent.Data, (ulong)connectionId, message,
                    m_RealTimeProvider.RealTimeSinceStartup);
                message = connectionInfo.ReceiveQueue.PopMessage();
            }
        }

        public override unsafe void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if (!m_Connections.TryGetValue((int)clientId, out ConnectionInfo connectionInfo))
            {
                return;
            }

            connectionInfo.SendQueue.PushMessage(payload);

            while (!connectionInfo.SendQueue.IsEmpty)
            {
                var rpc = new TransportRpc
                {
                    Buffer = new FixedList4096Bytes<byte>(),
                };
                
                var writer = new DataStreamWriter(rpc.Buffer.GetUnsafePtr(), k_MaxPacketSize);

                var amount = connectionInfo.SendQueue.FillWriterWithBytes(ref writer, k_MaxPacketSize);
                rpc.Buffer.Length = amount;
                rpc.Order = ++connectionInfo.LastSent;

                connectionInfo.Connection.SendOutOfBandMessage(rpc);
                
                connectionInfo.SendQueue.Consume(amount);
            }
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = default;
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }
        
        private void OnClientConnectedToServer(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            m_Connections[connection.NetworkId.Value] = new ConnectionInfo
            {
                ReceiveQueue = null,
                SendQueue = new BatchedSendQueue(BatchedSendQueue.MaximumMaximumCapacity),
                Connection = connection
            };
            m_ServerClientId = connection.NetworkId.Value;
            InvokeOnTransportEvent(NetworkEvent.Connect, (ulong)connection.NetworkId.Value, default,  m_RealTimeProvider.RealTimeSinceStartup);
        }
        
        private void OnServerNewClientConnection(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            m_Connections[connection.NetworkId.Value] = new ConnectionInfo
            {
                ReceiveQueue = null,
                SendQueue = new BatchedSendQueue(BatchedSendQueue.MaximumMaximumCapacity),
                Connection = connection
            };;
            InvokeOnTransportEvent(NetworkEvent.Connect, (ulong)connection.NetworkId.Value, default,  m_RealTimeProvider.RealTimeSinceStartup);
        }

        private void OnClientDisconnectFromServer(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            InvokeOnTransportEvent(NetworkEvent.Disconnect, (ulong)connection.NetworkId.Value, default,  m_RealTimeProvider.RealTimeSinceStartup);
        }

        private void OnServerClientDisconnected(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            InvokeOnTransportEvent(NetworkEvent.Disconnect, (ulong)connection.NetworkId.Value, default,  m_RealTimeProvider.RealTimeSinceStartup);
        }

        public override bool StartClient()
        {
            NetCode.Netcode.Client.OnConnect = OnClientConnectedToServer;
            NetCode.Netcode.Client.OnDisconnect = OnClientDisconnectFromServer;
            var updateSystem = NetCode.Netcode.GetWorld(false).GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Transport = this;
            return true;
        }

        public override bool StartServer()
        {
            foreach (var connection in NetCode.Netcode.Server.Connections)
            {
                OnServerNewClientConnection(connection, default);
            }

            NetCode.Netcode.Server.OnConnect = OnServerNewClientConnection;
            NetCode.Netcode.Server.OnDisconnect = OnServerClientDisconnected;
            var updateSystem = NetCode.Netcode.GetWorld(true).GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Transport = this;
            return true;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            var updateSystem = NetCode.Netcode.GetWorld(true).GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Disconnect(m_Connections[(int)clientId].Connection);
            m_Connections.Remove((int)clientId);
        }

        public override void DisconnectLocalClient()
        {
            var updateSystem = NetCode.Netcode.GetWorld(false).GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Disconnect(m_Connections[(int)ServerClientId].Connection);
            m_Connections.Remove((int)ServerClientId);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            var (transportId, _) = m_NetworkManager.ConnectionManager.ClientIdToTransportId(clientId);
            return (ulong)m_Connections[(int)transportId].Connection.RTT;
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
            m_Connections = new Dictionary<int, ConnectionInfo>();
            m_RealTimeProvider = networkManager.RealTimeProvider;
            m_NetworkManager = networkManager;
        }

        public override void Shutdown()
        {

        }
    }
}
#endif
