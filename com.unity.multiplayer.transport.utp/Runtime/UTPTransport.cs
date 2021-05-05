using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using MLAPI.Transports.Tasks;
using UnityEngine;

using UTPNetworkEvent = Unity.Networking.Transport.NetworkEvent;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Services.Relay.Models;
using System.Linq;
using Unity.Services.Core;

namespace MLAPI.Transports
{
    public class UTPTransport : NetworkTransport
    {
        public enum ProtocolType
        {
            UnityTransport,
            RelayUnityTransport,
        }

        private enum State
        {
            Disconnected,
            Listening,
            Connected,
        }

        [SerializeField] private ProtocolType m_ProtocolType;
        [SerializeField] private int m_MessageBufferSize;
        [SerializeField] private string m_ServerAddress = "127.0.0.1";
        [SerializeField] private ushort m_ServerPort = 7777;
        [SerializeField] private int m_RelayMaxPlayers = 10;
        [SerializeField] private string m_RelayServer = "https://relay-allocations-test.cloud.unity3d.com";

        private State m_State = State.Disconnected;
        private NetworkDriver m_Driver;
        private List<INetworkParameter> m_NetworkParameters;
        private byte[] m_MessageBuffer;
        private string m_RelayJoinCode;
        private ulong m_ServerClientId;
        
        public override ulong ServerClientId => m_ServerClientId;

        public string RelayJoinCode => m_RelayJoinCode;

        private void InitDriver()
        {
            if (m_NetworkParameters.Count > 0)
                m_Driver = NetworkDriver.Create(m_NetworkParameters.ToArray());
            else
                m_Driver = NetworkDriver.Create();
        }

        private void DisposeDriver()
        {
            if (m_Driver.IsCreated)
                m_Driver.Dispose();
        }

        private IEnumerator ClientBindAndConnect(SocketTask task)
        {
            var serverEndpoint = default(NetworkEndPoint);

            if (m_ProtocolType == ProtocolType.RelayUnityTransport)
            {
                var joinTask = RelayService.AllocationsApiClient.JoinRelayAsync(new JoinRelayRequest(new JoinRequest(m_RelayJoinCode)));

                while(!joinTask.IsCompleted)
                    yield return null;
                
                if (joinTask.IsFaulted)
                {
                    Debug.LogError("Join Relay request failed");
                    task.IsDone = true;
                    task.Success = false;
                    yield break;
                }

                var allocation = joinTask.Result.Result.Data.Allocation;

                serverEndpoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port);
#if RELAY_BIGENDIAN
                // TODO: endianess of Relay server does not match
                var allocationIdArray = allocation.AllocationId.ToByteArray();
                Array.Reverse(allocationIdArray, 0, 4);
                Array.Reverse(allocationIdArray, 4, 2);
                Array.Reverse(allocationIdArray, 6, 2);
                var allocationId = RelayAllocationId.FromByteArray(allocationIdArray);
#else
                var allocationId = RelayAllocationId.FromByteArray(allocation.AllocationId.ToByteArray());
#endif
                
                // TODO: workaround for receiving 271 bytes in connection data
                var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData.Take(255).ToArray());
                var hostConnectionData = RelayConnectionData.FromByteArray(allocation.HostConnectionData.Take(255).ToArray());
                var key = RelayHMACKey.FromByteArray(allocation.Key);

                Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
                Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");

                Debug.Log($"client: {allocation.AllocationId}");

                var relayServerData = new RelayServerData(serverEndpoint, 0, allocationId, connectionData, hostConnectionData, key);
                relayServerData.ComputeNewNonce();

                m_NetworkParameters.Add(new RelayNetworkParameter{ ServerData = relayServerData });
            }
            else
            {
                serverEndpoint = NetworkEndPoint.Parse(m_ServerAddress, m_ServerPort);
            }

            InitDriver();

            if (m_Driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.LogError("Client failed to bind");
            }
            else
            {
                while (!m_Driver.Bound)
                {
                    yield return null;
                }

                var serverConnection = m_Driver.Connect(serverEndpoint);
                m_ServerClientId = ParseClientId(serverConnection);

                while (m_Driver.GetConnectionState(serverConnection) == NetworkConnection.State.Connecting)
                {
                    yield return null;
                }

                if (m_Driver.GetConnectionState(serverConnection) == NetworkConnection.State.Connected)
                {
                    task.Success = true;
                    m_State = State.Connected;
                }
                else
                {
                    Debug.LogError("Client failed to connect to server");
                }
            }

            task.IsDone = true;
        }

        private IEnumerator ServerBindAndListen(SocketTask task)
        {
            var endpoint = NetworkEndPoint.Parse(m_ServerAddress, m_ServerPort);

            InitDriver();

            if (m_Driver.Bind(endpoint) != 0)
            {
                Debug.LogError("Server failed to bind");
            }
            else
            {
                while (!m_Driver.Bound)
                {
                    yield return null;
                }

                if (m_Driver.Listen() == 0)
                {
                    task.Success = true;
                    m_State = State.Listening;
                }
                else
                {
                    Debug.LogError("Server failed to listen");
                }
            }

            task.IsDone = true;
        }

        private IEnumerator StartRelayServer(SocketTask task)
        {
            var allocationTask = RelayService.AllocationsApiClient.CreateAllocationAsync(new CreateAllocationRequest(new AllocationRequest(m_RelayMaxPlayers)));

            while(!allocationTask.IsCompleted)
            {
                yield return null;
            }

            if (allocationTask.IsFaulted)
            {
                Debug.LogError("Create allocation request failed");
                task.IsDone = true;
                task.Success = false;
                yield break;
            }

            var allocation = allocationTask.Result.Result.Data.Allocation;

            var joinCodeTask = RelayService.AllocationsApiClient.CreateJoincodeAsync(new CreateJoincodeRequest(new JoinCodeRequest(allocation.AllocationId)));

            while(!joinCodeTask.IsCompleted)
            {
                yield return null;
            }

            if (joinCodeTask.IsFaulted)
            {
                Debug.LogError("Create join code request failed");
                task.IsDone = true;
                task.Success = false;
                yield break;
            }

            m_RelayJoinCode = joinCodeTask.Result.Result.Data.JoinCode;

            var serverEndpoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port);
#if RELAY_BIGENDIAN
            var allocationIdArray = allocation.AllocationId.ToByteArray();
            Array.Reverse(allocationIdArray, 0, 4);
            Array.Reverse(allocationIdArray, 4, 2);
            Array.Reverse(allocationIdArray, 6, 2);
            var allocationId = RelayAllocationId.FromByteArray(allocationIdArray);
#else
            var allocationId = RelayAllocationId.FromByteArray(allocation.AllocationId.ToByteArray());
#endif
            // TODO: connectionData should be 255 bytes, but we are getting 16 extra bytes
            var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData.Take(255).ToArray());
            var key = RelayHMACKey.FromByteArray(allocation.Key);

            var relayServerData = new RelayServerData(serverEndpoint, 0, allocationId, connectionData, connectionData, key);
            relayServerData.ComputeNewNonce();

            m_NetworkParameters.Add(new RelayNetworkParameter{ ServerData = relayServerData });
            
            yield return ServerBindAndListen(task);
        }

        private bool ProcessEvent()
        {
            var eventType = m_Driver.PopEvent(out var networkConnection, out var reader);

            switch (eventType)
            {
                case UTPNetworkEvent.Type.Connect:
                    InvokeOnTransportEvent(NetworkEvent.Connect,
                        ParseClientId(networkConnection),
                        NetworkChannel.Internal,
                        default(ArraySegment<byte>),
                        Time.realtimeSinceStartup);
                    return true;

                case UTPNetworkEvent.Type.Disconnect:
                    InvokeOnTransportEvent(NetworkEvent.Disconnect,
                        ParseClientId(networkConnection),
                        NetworkChannel.Internal,
                        default(ArraySegment<byte>),
                        Time.realtimeSinceStartup);
                    return true;

                case UTPNetworkEvent.Type.Data:
                    var channelId = reader.ReadByte();
                    var size = reader.ReadInt();

                    if (size > m_MessageBufferSize)
                    {
                        Debug.LogError("The received message does not fit into the message buffer");
                    }
                    else
                    {
                        unsafe
                        {
                            fixed(byte* buffer = &m_MessageBuffer[0])
                            {
                                reader.ReadBytes(buffer, size);
                            }
                        }
                        InvokeOnTransportEvent(NetworkEvent.Data,
                            ParseClientId(networkConnection),
                            (NetworkChannel)channelId,
                            new ArraySegment<byte>(m_MessageBuffer, 0, size),
                            Time.realtimeSinceStartup
                        );
                    }
                    return true;
            }

            return false;
        }

        private void Update()
        {
            if (m_Driver.IsCreated)
            {
                m_Driver.ScheduleUpdate().Complete();
                while(ProcessEvent() && m_Driver.IsCreated);
            }
        }

        private static unsafe ulong ParseClientId(NetworkConnection utpConnectionId)
        {
            return *(ulong*)&utpConnectionId;
        }

        private static unsafe NetworkConnection ParseClientId(ulong mlapiConnectionId)
        {
            return *(NetworkConnection*)&mlapiConnectionId;
        }

        public void SetRelayJoinCode(string value)
        {
            if (m_State == State.Disconnected)
            {
                m_RelayJoinCode = value;
            }
        }

        public override void DisconnectLocalClient()
        {
            Debug.Assert(m_State == State.Connected, "DisconnectLocalClient should be called on a connected client");

            if (m_State == State.Connected)
            {
                m_Driver.Disconnect(ParseClientId(m_ServerClientId));
            }
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            Debug.Assert(m_State == State.Connected, "DisconnectRemoteClient should be called on a listening server");

            Debug.Log("Disconnecting");

            if (m_State == State.Listening)
            {
                var connection = ParseClientId(clientId);

                if (m_Driver.GetConnectionState(connection) != NetworkConnection.State.Disconnected)
                {
                    m_Driver.Disconnect(connection);
                }
            }
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        public override void Init()
        {
            Debug.Assert(sizeof(ulong) == UnsafeUtility.SizeOf<NetworkConnection>(), "MLAPI connection id size does not match UTP connection id size");
            Debug.Assert(m_MessageBufferSize > 5, "Message buffer size must be greater than 5");

            m_NetworkParameters = new List<INetworkParameter>();
            m_MessageBuffer = new byte[m_MessageBufferSize];

            if (m_ProtocolType == ProtocolType.RelayUnityTransport) {
                Unity.Services.Relay.Configuration.BasePath = m_RelayServer;
                UnityServices.Initialize();
            }
        }

        public override NetworkEvent PollEvent(out ulong clientId, out NetworkChannel networkChannel, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = default;
            networkChannel = default;
            payload = default;
            receiveTime = default;
            return NetworkEvent.Nothing;
        }

        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkChannel networkChannel)
        {
            var size = data.Count + 5;

            if (m_Driver.BeginSend(ParseClientId(clientId), out var writer, size) == 0)
            {
                writer.WriteByte((byte)networkChannel);
                writer.WriteInt(data.Count);

                unsafe
                {
                    fixed(byte* dataPtr = &data.Array[data.Offset])
                    {
                        writer.WriteBytes(dataPtr, data.Count);
                    }
                }

                if (m_Driver.EndSend(writer) == size)
                    return;
            }

            Debug.LogError("Error sending the message");
        }

        public override void Shutdown()
        {
            DisposeDriver();
        }

        public override SocketTasks StartClient()
        {
            var task = SocketTask.Working;

            StartCoroutine(ClientBindAndConnect(task));

            return task.AsTasks();
        }

        public override SocketTasks StartServer()
        {
            var task = SocketTask.Working;

            switch (m_ProtocolType)
            {
                case ProtocolType.UnityTransport:
                    StartCoroutine(ServerBindAndListen(task));
                    break;
                case ProtocolType.RelayUnityTransport:
                    StartCoroutine(StartRelayServer(task));
                    break;
            }

            return task.AsTasks();
        }
    }
}
