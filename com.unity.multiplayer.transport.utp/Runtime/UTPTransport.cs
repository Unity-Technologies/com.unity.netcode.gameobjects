using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

using MLAPI.Transports.Tasks;
using UnityEngine;

using UTPNetworkEvent = Unity.Networking.Transport.NetworkEvent;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;
using Unity.Networking.Transport.Utilities;

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

        public const int MaximumMessageLength = 6 * 1024;

        [SerializeField] private ProtocolType m_ProtocolType;
        [SerializeField] private int m_MessageBufferSize = MaximumMessageLength;
        [SerializeField] private string m_ServerAddress = "127.0.0.1";
        [SerializeField] private ushort m_ServerPort = 7777;

        private State m_State = State.Disconnected;
        private NetworkDriver m_Driver;
        private List<INetworkParameter> m_NetworkParameters;
        private byte[] m_MessageBuffer;
        private ulong m_ServerClientId;

        private NetworkPipeline m_UnreliableSequencedPipeline;
        private NetworkPipeline m_ReliableSequencedPipeline;
        private NetworkPipeline m_ReliableSequencedFragmentedPipeline;

        public override ulong ServerClientId => m_ServerClientId;

        public ProtocolType Protocol => m_ProtocolType;

        private RelayServerData m_RelayServerData;

        private static readonly RelayServerData k_DefaultRelayServerData = default(RelayServerData);
        private static RelayServerData DefaultRelayServerData => k_DefaultRelayServerData;

        private void InitDriver()
        {
            if (m_NetworkParameters.Count > 0)
            {
                m_Driver = NetworkDriver.Create(m_NetworkParameters.ToArray());
            }
            else
            {
                m_Driver = NetworkDriver.Create();
            }

            m_UnreliableSequencedPipeline = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            m_ReliableSequencedPipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            m_ReliableSequencedFragmentedPipeline = m_Driver.CreatePipeline(
                typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage)
            );
        }

        private void DisposeDriver()
        {
            if (m_Driver.IsCreated)
            {
                m_Driver.Dispose();
            }
        }

        private NetworkPipeline SelectSendPipeline(NetworkChannel channel, int size)
        {
            TransportChannel transportChannel = Array.Find(MLAPI_CHANNELS, tc => tc.Channel == channel);

            switch (transportChannel.Delivery)
            {
                case NetworkDelivery.Unreliable:
                    return NetworkPipeline.Null;

                case NetworkDelivery.UnreliableSequenced:
                    return m_UnreliableSequencedPipeline;

                case NetworkDelivery.Reliable:
                case NetworkDelivery.ReliableSequenced:
                    return m_ReliableSequencedPipeline;

                case NetworkDelivery.ReliableFragmentedSequenced:
                    // No need to send on the fragmented pipeline if data is smaller than MTU.
                    if (size < NetworkParameterConstants.MTU)
                    {
                        return m_ReliableSequencedPipeline;
                    }

                    return m_ReliableSequencedFragmentedPipeline;

                default:
                    Debug.LogError($"Unknown NetworkDelivery value: {transportChannel.Delivery}");
                    return NetworkPipeline.Null;
            }
        }

        private IEnumerator ClientBindAndConnect(SocketTask task)
        {
            var serverEndpoint = default(NetworkEndPoint);

            if (m_ProtocolType == ProtocolType.RelayUnityTransport)
            {
                //This comparison is currently slow since RelayServerData does not implement a custom comparison operator that doesn't use
                //reflection, but this does not live in the context of a performance-critical loop, it runs once at initial connection time.
                if(m_RelayServerData.Equals(DefaultRelayServerData))
                {
                    Debug.LogError("You must set the RelayServerData property to something different from the default value before calling StartRelayServer.");
                    yield break;
                }

                m_NetworkParameters.Add(new RelayNetworkParameter{ ServerData = m_RelayServerData });
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

        private IEnumerator ServerBindAndListen(SocketTask task, NetworkEndPoint endPoint)
        {
            //var endpoint = NetworkEndPoint.Parse(m_ServerAddress, m_ServerPort);

            InitDriver();

            if (m_Driver.Bind(endPoint) != 0)
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

        private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            unsafe
            {
                fixed (byte* ptr = allocationIdBytes)
                {
                    return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
                }
            }
        }

        private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
            unsafe
            {
                fixed (byte* ptr = hmac)
                {
                    return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
                }
            }
        }

        private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            unsafe
            {
                fixed (byte* ptr = connectionData)
                {
                    return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
                }
            }
        }

        public void SetRelayServerData(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] keyBytes, byte[] connectionDataBytes, byte[] hostConnectionDataBytes = null)
        {
            RelayConnectionData hostConnectionData;

            var serverEndpoint = NetworkEndPoint.Parse(ipv4address, port);
            var allocationId = ConvertFromAllocationIdBytes(allocationIdBytes);
            var key = ConvertFromHMAC(keyBytes);
            var connectionData = ConvertConnectionData(connectionDataBytes);
            
            if (hostConnectionDataBytes != null)
            {
                hostConnectionData = ConvertConnectionData(hostConnectionDataBytes);
            }
            else
            {
                hostConnectionData = connectionData;
            }
            m_RelayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref hostConnectionData, ref key);
            m_RelayServerData.ComputeNewNonce();
        }

        private IEnumerator StartRelayServer(SocketTask task)
        {
            //This comparison is currently slow since RelayServerData does not implement a custom comparison operator that doesn't use
            //reflection, but this does not live in the context of a performance-critical loop, it runs once at initial connection time.
            if (m_RelayServerData.Equals(DefaultRelayServerData))
            {
                Debug.LogError("You must set the RelayServerData property to something different from the default value before calling StartRelayServer.");
                yield break;
            }
            else
            {
                m_NetworkParameters.Add(new RelayNetworkParameter { ServerData = m_RelayServerData });

                yield return ServerBindAndListen(task, NetworkEndPoint.AnyIpv4);
            }
        }

        private bool AcceptConnection()
        {
            var connection = m_Driver.Accept();

            if (connection != default(NetworkConnection))
            {
                InvokeOnTransportEvent(NetworkEvent.Connect,
                    ParseClientId(connection),
                    NetworkChannel.Internal,
                    default(ArraySegment<byte>),
                    Time.realtimeSinceStartup);
                return true;
            }

            return false;
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
                        // Debug.Log($"Receiving: {String.Join(", ", m_MessageBuffer.Take(size).Select(x => string.Format("{0:x}", x)))}");
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
                while(AcceptConnection() && m_Driver.IsCreated)
                {
                    ;
                }

                while (ProcessEvent() && m_Driver.IsCreated)
                {
                    ;
                }
            }

        }

        private void OnDestroy()
        {
            DisposeDriver();
        }

        private static unsafe ulong ParseClientId(NetworkConnection utpConnectionId)
        {
            return *(ulong*)&utpConnectionId;
        }

        private static unsafe NetworkConnection ParseClientId(ulong mlapiConnectionId)
        {
            return *(NetworkConnection*)&mlapiConnectionId;
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
            Debug.Assert(m_State == State.Listening, "DisconnectRemoteClient should be called on a listening server");

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

            // If we want to be able to actually handle messages MaximumMessageLength bytes in
            // size, we need to allow a bit more than that in FragmentationUtility since this needs
            // to account for headers and such. 128 bytes is plenty enough for such overhead.
            var maxFragmentationCapacity = MaximumMessageLength + 128;
            m_NetworkParameters.Add(new FragmentationUtility.Parameters(){PayloadCapacity = maxFragmentationCapacity});

            m_MessageBuffer = new byte[m_MessageBufferSize];
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

            var pipeline = SelectSendPipeline(networkChannel, size);

            if (m_Driver.BeginSend(pipeline, ParseClientId(clientId), out var writer, size) == 0)
            {
                writer.WriteByte((byte)networkChannel);
                writer.WriteInt(data.Count);

                if (data.Array != null)
                {
                    unsafe
                    {
                        fixed(byte* dataPtr = &data.Array[data.Offset])
                        {
                            writer.WriteBytes(dataPtr, data.Count);
                        }
                    }
                }

                if (m_Driver.EndSend(writer) == size)
                {
                    return;
                }
            }

            Debug.LogError("Error sending the message");
        }

        public override SocketTasks StartClient()
        {
            if (m_Driver.IsCreated)
            {
                return SocketTask.Fault.AsTasks();
            }

            var task = SocketTask.Working;
            StartCoroutine(ClientBindAndConnect(task));
            return task.AsTasks();
        }

        public override SocketTasks StartServer()
        {
            if (m_Driver.IsCreated)
            {
                return SocketTask.Fault.AsTasks();
            }

            var task = SocketTask.Working;
            switch (m_ProtocolType)
            {
                case ProtocolType.UnityTransport:
                    StartCoroutine(ServerBindAndListen(task, NetworkEndPoint.Parse(m_ServerAddress, m_ServerPort)));
                    break;
                case ProtocolType.RelayUnityTransport:
                    StartCoroutine(StartRelayServer(task));
                    break;
            }
            return task.AsTasks();
        }

        public override void Shutdown()
        {
            DisposeDriver();
        }
    }
}
