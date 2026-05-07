#if UNIFIED_NETCODE && OUT_OF_BAND_RPC
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode.Unified
{
    [BurstCompile]
    internal unsafe struct FixedBytes1280
    {
        public fixed byte Buffer[1280];
        public int Length;

        // Returns a direct pointer to the data in the buffer.
        // Implemented as a static with an in-parameter to avoid the buffer being copied while keeping its memory allocation fixed/non-heap
        // Note that the buffer MUST outlive the returned pointer, as it is an alias.
        public static byte* GetUnsafePtr(in FixedBytes1280 data)
        {
            fixed (byte* buffer = data.Buffer)
            {
                return buffer;
            }
        }

        // Returns a native array that is an alias of the existing data without copying it
        // Implemented as a static with an in-parameter to avoid the buffer being copied while keeping its memory allocation fixed/non-heap
        // Note that the buffer MUST outlive the returned array, as it is an alias.
        public static NativeArray<byte> ToNativeArray(in FixedBytes1280 data)
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(GetUnsafePtr(data), data.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = CollectionHelper.CreateSafetyHandle(Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);
#endif
            return array;
        }
    }

    internal struct TransportRpcData : IBufferElementData
    {
        public FixedBytes1280 Buffer;
    }

    [BurstCompile]
    internal struct TransportRpc : IOutOfBandRpcCommand, IRpcCommandSerializer<TransportRpc>
    {
        public TransportRpcData Value;

        public unsafe void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in TransportRpc data)
        {
            writer.WriteInt(data.Value.Buffer.Length);
            var span = new Span<byte>(FixedBytes1280.GetUnsafePtr(data.Value.Buffer), data.Value.Buffer.Length);
            writer.WriteBytes(span);
        }

        public unsafe void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref TransportRpc data)
        {
            var length = reader.ReadInt();
            data.Value.Buffer = new FixedBytes1280
            {
                Length = length
            };

            var span = new Span<byte>(FixedBytes1280.GetUnsafePtr(data.Value.Buffer), length);
            reader.ReadBytes(span);
        }

        [BurstCompile(DisableDirectCall = true)]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            var element = new TransportRpc();
            element.Deserialize(ref parameters.Reader, parameters.DeserializerState, ref element);
            parameters.CommandBuffer.AppendToBuffer(parameters.JobIndex, parameters.Connection, element.Value);
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

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(RpcSystem))]
    internal partial class UnifiedNetcodeUpdateSystem : SystemBase
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RpcCollection>();
            state.RequireForUpdate<NetworkId>();
        }
        
        public UnifiedNetcodeTransport Transport;
        public NetworkManager NetworkManager;

        public List<Connection> DisconnectQueue = new List<Connection>();

        public void Disconnect(Connection connection)
        {
            DisconnectQueue.Add(connection);
        }
        
        public void SendRpc(TransportRpc rpc)
        {
            var rpcQueue = SystemAPI.GetSingleton<RpcCollection>().GetRpcQueue<TransportRpc, TransportRpc>();
            var ghostInstance = GetComponentLookup<GhostInstance>();
            foreach (var rpcDataStreamBuffer in SystemAPI.Query<DynamicBuffer<OutgoingOutOfBandRpcDataStreamBuffer>>())
            {
                rpcQueue.Schedule(rpcDataStreamBuffer, ghostInstance, rpc);
            }
        }

        protected override void OnUpdate()
        {
            NetworkManager.MessageManager.ProcessSendQueues();
            
            using var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach(var (networkId, _, entity) in SystemAPI.Query<RefRO<NetworkId>, RefRO<NetworkStreamConnection>>().WithEntityAccess())
            {
                var connectionId = networkId.ValueRO.Value;
                DynamicBuffer<TransportRpcData> rpcs = EntityManager.GetBuffer<TransportRpcData>(entity);
                foreach (var rpc in rpcs)
                {
                    var buffer = rpc.Buffer;
                    try
                    {
                        Transport.DispatchMessage(connectionId, buffer);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                rpcs.Clear();
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
        private const int k_MaxPacketSize = 1280;

        private int m_ServerClientId = -1;
        public override ulong ServerClientId => (ulong)m_ServerClientId;

        private NetworkManager m_NetworkManager;

        private IRealTimeProvider m_RealTimeProvider;

        private class ConnectionInfo
        {
            public BatchedSendQueue SendQueue;
            public BatchedReceiveQueue ReceiveQueue;
            public Connection Connection;
            public Dictionary<ulong, FixedBytes1280> DeferredMessages;
        }

        private Dictionary<int, ConnectionInfo> m_Connections;

        internal void DispatchMessage(int connectionId, in FixedBytes1280 buffer)
        {
            var connectionInfo = m_Connections[connectionId];

            using var arr = FixedBytes1280.ToNativeArray(buffer);
            var reader = new DataStreamReader(arr);
            if (connectionInfo.ReceiveQueue == null)
            {
                connectionInfo.ReceiveQueue = new BatchedReceiveQueue(reader);
            }
            else
            {
                connectionInfo.ReceiveQueue.PushReader(reader);
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
                var rpc = new TransportRpc();

                var writer = new DataStreamWriter(FixedBytes1280.GetUnsafePtr(rpc.Value.Buffer), k_MaxPacketSize);

                var amount = connectionInfo.SendQueue.FillWriterWithBytes(ref writer, k_MaxPacketSize);
                rpc.Value.Buffer.Length = amount;
                
                var updateSystem = m_NetworkManager.NetcodeWorld.GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
                updateSystem.SendRpc(rpc);

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
            InvokeOnTransportEvent(NetworkEvent.Connect, (ulong)connection.NetworkId.Value, default, m_RealTimeProvider.RealTimeSinceStartup);
            var updateSystem = m_NetworkManager.NetcodeWorld.GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.EntityManager.AddBuffer<TransportRpcData>(connection.ConnectionEntity);
        }

        private void OnServerNewClientConnection(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            m_Connections[connection.NetworkId.Value] = new ConnectionInfo
            {
                ReceiveQueue = null,
                SendQueue = new BatchedSendQueue(BatchedSendQueue.MaximumMaximumCapacity),
                Connection = connection
            }; ;
            InvokeOnTransportEvent(NetworkEvent.Connect, (ulong)connection.NetworkId.Value, default, m_RealTimeProvider.RealTimeSinceStartup);
            var updateSystem = m_NetworkManager.NetcodeWorld.GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.EntityManager.AddBuffer<TransportRpcData>(connection.ConnectionEntity);
        }

        private const string k_InvalidRpcMessage = "An invalid RPC was received";
        private const string k_HandshakeTimeoutMessage = "The connection was closed because the handshake timed out.";
        private const string k_ApprovalFailureMessage = "The connection was closed because the connection was not approved by the server.";
        private const string k_ApprovalTimeoutMessage = "The connection was closed because the connection approval process timed out.";

        private string GetDisconnectMessageFromNetworkStreamDisconnectReason(NetworkStreamDisconnectReason reason)
        {
            switch (reason)
            {
                case NetworkStreamDisconnectReason.ConnectionClose:
                    return UnityTransportNotificationHandler.DisconnectedMessage;
                case NetworkStreamDisconnectReason.Timeout:
                    return UnityTransportNotificationHandler.TimeoutMessage;
                case NetworkStreamDisconnectReason.MaxConnectionAttempts:
                    return UnityTransportNotificationHandler.MaxConnectionAttemptsMessage;
                case NetworkStreamDisconnectReason.ClosedByRemote:
                    return UnityTransportNotificationHandler.ClosedRemoteConnectionMessage;
                case NetworkStreamDisconnectReason.BadProtocolVersion:
                    return UnityTransportNotificationHandler.ProtocolErrorMessage;
                case NetworkStreamDisconnectReason.InvalidRpc:
                    return k_InvalidRpcMessage;
                case NetworkStreamDisconnectReason.AuthenticationFailure:
                    return UnityTransportNotificationHandler.AuthenticationFailureMessage;
                case NetworkStreamDisconnectReason.ProtocolError:
                    return UnityTransportNotificationHandler.ProtocolErrorMessage;
                case NetworkStreamDisconnectReason.HandshakeTimeout:
                    return k_HandshakeTimeoutMessage;
                case NetworkStreamDisconnectReason.ApprovalFailure:
                    return k_ApprovalFailureMessage;
                case NetworkStreamDisconnectReason.ApprovalTimeout:
                    return k_ApprovalTimeoutMessage;
            }
            return "Unknown reason";
        }

        private DisconnectEvents GetDisconnectEventFromNetworkStreamDisconnectReason(NetworkStreamDisconnectReason reason)
        {
            switch (reason)
            {
                case NetworkStreamDisconnectReason.ConnectionClose:
                    return DisconnectEvents.Disconnected;
                case NetworkStreamDisconnectReason.Timeout:
                    return DisconnectEvents.ProtocolTimeout;
                case NetworkStreamDisconnectReason.MaxConnectionAttempts:
                    return DisconnectEvents.MaxConnectionAttempts;
                case NetworkStreamDisconnectReason.ClosedByRemote:
                    return DisconnectEvents.ClosedByRemote;
                case NetworkStreamDisconnectReason.BadProtocolVersion:
                    return DisconnectEvents.ProtocolError;
                case NetworkStreamDisconnectReason.InvalidRpc:
                    return DisconnectEvents.ProtocolError;
                case NetworkStreamDisconnectReason.AuthenticationFailure:
                    return DisconnectEvents.AuthenticationFailure;
                case NetworkStreamDisconnectReason.ProtocolError:
                    return DisconnectEvents.ProtocolError;
                case NetworkStreamDisconnectReason.HandshakeTimeout:
                    return DisconnectEvents.ProtocolError;
                case NetworkStreamDisconnectReason.ApprovalFailure:
                    return DisconnectEvents.AuthenticationFailure;
                case NetworkStreamDisconnectReason.ApprovalTimeout:
                    return DisconnectEvents.ProtocolTimeout;
            }
            return DisconnectEvents.Disconnected;
        }

        private void OnClientDisconnectFromServer(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            SetDisconnectEvent(
                GetDisconnectEventFromNetworkStreamDisconnectReason(connectionEvent.DisconnectReason),
                GetDisconnectMessageFromNetworkStreamDisconnectReason(connectionEvent.DisconnectReason)
            );
            InvokeOnTransportEvent(NetworkEvent.Disconnect, (ulong)connection.NetworkId.Value, default, m_RealTimeProvider.RealTimeSinceStartup);
        }

        private void OnServerClientDisconnected(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            InvokeOnTransportEvent(NetworkEvent.Disconnect, (ulong)connection.NetworkId.Value, default, m_RealTimeProvider.RealTimeSinceStartup);
        }

        private void OnClientConnectionEvent(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            switch (connectionEvent.State)
            {
                case ConnectionState.State.Connected:
                    OnClientConnectedToServer(connection, connectionEvent);
                    break;
                case ConnectionState.State.Disconnected:
                    OnClientDisconnectFromServer(connection, connectionEvent);
                    break;
            }
        }

        private void OnServerConnectionEvent(Connection connection, NetCodeConnectionEvent connectionEvent)
        {
            switch (connectionEvent.State)
            {
                case ConnectionState.State.Connected:
                    OnServerNewClientConnection(connection, connectionEvent);
                    break;
                case ConnectionState.State.Disconnected:
                    OnServerClientDisconnected(connection, connectionEvent);
                    break;
            }
        }

        public override bool StartClient()
        {
            m_NetworkManager.NetcodeWorld.OnConnectionEvent += OnClientConnectionEvent;
            var updateSystem = m_NetworkManager.NetcodeWorld.GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Transport = this;
            updateSystem.NetworkManager = m_NetworkManager;
            return true;
        }

        public override bool StartServer()
        {
            foreach (var connection in m_NetworkManager.NetcodeWorld.AllConnections)
            {
                OnServerNewClientConnection(connection, default);
            }

            m_NetworkManager.NetcodeWorld.OnConnectionEvent += OnServerConnectionEvent;
            var updateSystem = m_NetworkManager.NetcodeWorld.GetExistingSystemManaged<UnifiedNetcodeUpdateSystem>();
            updateSystem.Transport = this;
            updateSystem.NetworkManager = m_NetworkManager;
            return true;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            m_NetworkManager.NetcodeWorld.DisconnectAClient(m_Connections[(int)clientId].Connection);
            m_Connections.Remove((int)clientId);
        }

        public override void DisconnectLocalClient()
        {
            // Remove the connection 1st (the world might not be available)
            m_Connections.Remove((int)ServerClientId);

            // TODO-FIX-REVIEW-ME:
            // This was causing errors to occur upon shutdown during an integration test.
            // The cases being trapped for below yield no errors, but there might be some
            // form of other underlying issue here:

            if (m_NetworkManager.NetcodeWorld == null || !m_NetworkManager.NetcodeWorld.IsCreated)
            {
                return;
            }

            if (m_NetworkManager.IsServer || m_NetworkManager.NetcodeWorld.IsHost())
            {
                if (m_NetworkManager.LogLevel <= LogLevel.Developer)
                {
                    Debug.LogWarning("Host is attempting to shutdown the local client which is not required with a single world host.");
                }
                return;
            }
            m_NetworkManager.NetcodeWorld.RequestDisconnectFromServer();

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
