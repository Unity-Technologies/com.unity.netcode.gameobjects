#if UNIFIED_NETCODE
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.Netcode.Unified
{
    internal struct TransportRpc : IRpcCommand, IRpcCommandSerializer<TransportRpc>
    {
        public FixedList4096Bytes<byte> Buffer;
        
        internal static string ByteArrayToString(FixedList4096Bytes<byte> ba, int offset, int count)
        {
            var hex = new StringBuilder(ba.Length * 2);
            for (int i = offset; i < offset + count; ++i)
            {
                hex.AppendFormat("{0:x2} ", ba[i]);
            }

            return hex.ToString();
        }
        internal static string ByteArrayToString(NativeArray<byte> ba, int offset, int count)
        {
            var hex = new StringBuilder(ba.Length * 2);
            for (int i = offset; i < offset + count; ++i)
            {
                hex.AppendFormat("{0:x2} ", ba[i]);
            }

            return hex.ToString();
        }
        
        public unsafe void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in TransportRpc data)
        {
            writer.WriteInt(data.Buffer.Length);
            var span = new Span<byte>(data.Buffer.GetUnsafePtr(), data.Buffer.Length);
            writer.WriteBytes(span);
        }

        public unsafe void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref TransportRpc data)
        {
            var length = reader.ReadInt();
            data.Buffer = new FixedList4096Bytes<byte>();
            data.Buffer.Length = length;
            var span = new Span<byte>(data.Buffer.GetUnsafePtr(), length);
            reader.ReadBytes(span);
        }

        [BurstCompile(DisableDirectCall = true)]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<TransportRpc, TransportRpc>(ref parameters);
        }
        
        static readonly PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer = new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }

    [UpdateInGroup(typeof(RpcCommandRequestSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    [BurstCompile]
    partial struct TransportRpcCommandRequestSystem : ISystem
    {
        private RpcCommandRequest<TransportRpc, TransportRpc> m_Request;

        [BurstCompile]
        struct SendRpc : IJobChunk
        {
            public RpcCommandRequest<TransportRpc, TransportRpc>.SendRpcData data;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                data.Execute(chunk, unfilteredChunkIndex);
            }
        }

        public void OnCreate(ref SystemState state)
        {
            m_Request.OnCreate(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sendJob = new SendRpc { data = m_Request.InitJobData(ref state) };
            state.Dependency = sendJob.Schedule(m_Request.Query, state.Dependency);
        }
    }
    

    internal partial class UnifiedNetcodeUpdateSystem : SystemBase
    {
        public UnifiedNetcodeTransport Transport;

        public List<Connection> DiscconedtQueue = new List<Connection>();

        public void Disconnect(Connection connection)
        {
            DiscconedtQueue.Add(connection);
        }
        
        protected override void OnUpdate()
        {
            using var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach(var (request, rpc, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRW<TransportRpc>>().WithEntityAccess())
            {
                var connectionId = SystemAPI.GetComponent<NetworkId>(request.ValueRO.SourceConnection).Value;

                var buffer = rpc.ValueRW.Buffer;
                Transport.DispatchMessage(connectionId, buffer);
                commandBuffer.DestroyEntity(entity);
            }

            foreach (var connection in DiscconedtQueue)
            {
                commandBuffer.AddComponent<NetworkStreamRequestDisconnect>(connection.ConnectionEntity);
            }
            commandBuffer.Playback(EntityManager);
            DiscconedtQueue.Clear();
        }
    }

    internal class UnifiedNetcodeTransport : NetworkTransport
    {
        private int m_ServerClientId = -1;
        public override ulong ServerClientId => (ulong)m_ServerClientId;

        private bool m_IsClient;
        private bool m_IsServer;
        private bool m_StartedServerWorld = false;
        private bool m_StartedClientWorld = false;
        
        private IRealTimeProvider m_RealTimeProvider;

        private Dictionary<int, Connection> m_Connections;

        internal void DispatchMessage(int connectionId, FixedList4096Bytes<byte> buffer)
        {
            ArraySegment<byte> data = new ArraySegment<byte>(buffer.ToArray());
            InvokeOnTransportEvent(NetworkEvent.Data, (ulong)connectionId, data, m_RealTimeProvider.RealTimeSinceStartup);
        }
        
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if (!m_Connections.TryGetValue((int)clientId, out Connection connection))
            {
                return;
            }
            
            var rpc = new TransportRpc
            {
                Buffer = new FixedList4096Bytes<byte>(),
            };
            
            unsafe
            {
                rpc.Buffer.Length = payload.Count;
                fixed (byte* data = payload.Array)
                {
                    UnsafeUtility.MemCpy(rpc.Buffer.GetUnsafePtr(), (void*)(data + payload.Offset), payload.Count);
                }
            }

            connection.SendMessage(rpc);
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
            m_Connections[connection.NetworkId.Value] = connection;
            m_ServerClientId = connection.NetworkId.Value;
            InvokeOnTransportEvent(NetworkEvent.Connect, (ulong)connection.NetworkId.Value, default,  m_RealTimeProvider.RealTimeSinceStartup);
        }
        
        private void OnServerNewClientConnection(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            m_Connections[connection.NetworkId.Value] = connection;
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
            if (!UnifiedBootStrap.HasClientWorlds)
            {
                UnifiedBootStrap.CreateClientWorld("ClientWorld");
                m_StartedClientWorld = true;
            }

            NetCode.Netcode.Client.OnConnect = OnClientConnectedToServer;
            NetCode.Netcode.Client.OnDisconnect = OnClientDisconnectFromServer;
            var updateSystem = NetCode.Netcode.GetWorld(false).GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Transport = this;
            return true;
        }

        public override bool StartServer()
        {
            if (!UnifiedBootStrap.HasServerWorld)
            {
                UnifiedBootStrap.CreateServerWorld("ServerWorld");
                m_StartedClientWorld = true;
            }
            else
            {
                foreach (var connection in NetCode.Netcode.Server.Connections)
                {
                    OnServerNewClientConnection(connection, default);
                }
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
            updateSystem.Disconnect(m_Connections[(int)clientId]);
            m_Connections.Remove((int)clientId);
        }

        public override void DisconnectLocalClient()
        {
            var updateSystem = NetCode.Netcode.GetWorld(false).GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Disconnect(m_Connections[(int)ServerClientId]);
            m_Connections.Remove((int)ServerClientId);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            // todo
            return 0;
            //return (ulong)m_Connections[(int)clientId].RTT;
        }

        public override void Shutdown()
        {
            if (m_StartedClientWorld)
            {
                UnifiedBootStrap.StopClient();
            }
            if (m_StartedServerWorld)
            {
                UnifiedBootStrap.StopServer();
            }
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
            m_Connections = new Dictionary<int, Connection>();
            m_RealTimeProvider = networkManager.RealTimeProvider;
        }
    }
}
#endif