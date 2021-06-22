using System;
using AOT;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Relay;
using UnityEngine.Assertions;

namespace MLAPI.Transports
{
    internal static class ConnectionAddressExtensions
    {
        public static unsafe ref RelayAllocationId AsRelayAllocationId(this NetworkInterfaceEndPoint address)
        {
            return ref *(RelayAllocationId*) address.data;
        }
    }

    public struct RelayNetworkParameter : INetworkParameter
    {
        public RelayServerData ServerData;
        public int RelayConnectionTimeMS;
    }

    [BurstCompile]
    internal struct RelayNetworkProtocol : INetworkProtocol
    {
        public static ushort SwitchEndianness(ushort value)
        {
            if (DataStreamWriter.IsLittleEndian)
                return (ushort) ((value << 8) | (value >> 8));

            return value;
        }

        private enum RelayConnectionState : byte
        {
            Unbound = 0,
            Binding = 1,
            Bound = 2,
            Connecting = 3,
            Connected = 4,
        }

        private struct RelayProtocolData
        {
            public RelayConnectionState ConnectionState;
            public ushort ConnectionReceiveToken;
            public long LastConnectAttempt;
            public long LastUpdateTime;
            public long LastSentTime;
            public int ConnectTimeoutMS;
            public int RelayConnectionTimeMS;
            public RelayAllocationId HostAllocationId;
            public NetworkInterfaceEndPoint ServerEndpoint;
            public RelayServerData ServerData;
        }

        public IntPtr UserData;

        public void Initialize(INetworkParameter[] parameters)
        {
            if (!TryExtractParameters<RelayNetworkParameter>(out var relayConfig, parameters))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Debug.LogWarning("No Relay Protocol configuration parameters were provided");
#endif
            }

            var connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS;
            if (TryExtractParameters<NetworkConfigParameter>(out var config, parameters))
            {
                connectTimeoutMS = config.connectTimeoutMS;
            }
            var relayConnectionTimeMS = 9000;
            if (relayConfig.RelayConnectionTimeMS != 0) {
                relayConnectionTimeMS = relayConfig.RelayConnectionTimeMS;
            }

            unsafe
            {
                UserData = (IntPtr)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<RelayProtocolData>(), UnsafeUtility.AlignOf<RelayProtocolData>(), Allocator.Persistent);
                *(RelayProtocolData*)UserData = new RelayProtocolData
                {
                    ServerData = relayConfig.ServerData,
                    ConnectionState = RelayConnectionState.Unbound,
                    ConnectTimeoutMS = connectTimeoutMS,
                    RelayConnectionTimeMS = relayConnectionTimeMS
                };
            }
        }

        public void Dispose()
        {
            unsafe
            {
                if (UserData != default)
                    UnsafeUtility.Free(UserData.ToPointer(), Allocator.Persistent);

                UserData = default;
            }
        }

        bool TryExtractParameters<T>(out T config, params INetworkParameter[] param)
        {
            for (var i = 0; i < param.Length; ++i)
            {
                if (param[i] is T)
                {
                    config = (T) param[i];
                    return true;
                }
            }

            config = default;
            return false;
        }

        public int Bind(INetworkInterface networkInterface, ref NetworkInterfaceEndPoint localEndPoint)
        {
            if (networkInterface.Bind(localEndPoint) != 0)
                return -1;

            unsafe
            {
                var protocolData = (RelayProtocolData*)UserData;
                // Relay protocol will stablish only one physical connection using the interface (to Relay server).
                // All client connections are virtual. Here we initialize that connection.
                networkInterface.CreateInterfaceEndPoint(protocolData->ServerData.Endpoint, out protocolData->ServerEndpoint);

                // The Relay protocol binding process requires to exchange some messages to stablish the connection
                // with the Relay server, so we set the state to "binding" until the connection with server is confirm.
                protocolData->ConnectionState = RelayConnectionState.Binding;

                return 1; // 1 = Binding for the NetworkDriver, a full stablished bind is 2
            }
        }

        public unsafe int Connect(INetworkInterface networkInterface, NetworkEndPoint endPoint, out NetworkInterfaceEndPoint address)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (UnsafeUtility.SizeOf<NetworkInterfaceEndPoint>() < UnsafeUtility.SizeOf<RelayAllocationId>())
                throw new InvalidOperationException("RelayAllocationId does not fit the ConnectionAddress size");
#endif

            // We need to convert a endpoint address to a allocation id address
            // For Relay that is always the host allocation id.
            var protocolData = (RelayProtocolData*)UserData;
            address = default;
            fixed(byte* addressPtr = address.data)
            {
                *(RelayAllocationId*)addressPtr = protocolData->HostAllocationId;
            }

            return 0;
        }

        public NetworkEndPoint GetRemoteEndPoint(INetworkInterface networkInterface, NetworkInterfaceEndPoint address)
        {
            unsafe
            {
                var protocolData = (RelayProtocolData*)UserData;
                return networkInterface.GetGenericEndPoint(protocolData->ServerEndpoint);
            }
        }

        public NetworkProtocol CreateProtocolInterface()
        {
            return new NetworkProtocol(
                computePacketAllocationSize: new TransportFunctionPointer<NetworkProtocol.ComputePacketAllocationSizeDelegate>(ComputePacketAllocationSize),
                processReceive: new TransportFunctionPointer<NetworkProtocol.ProcessReceiveDelegate>(ProcessReceive),
                processSend: new TransportFunctionPointer<NetworkProtocol.ProcessSendDelegate>(ProcessSend),
                processSendConnectionAccept: new TransportFunctionPointer<NetworkProtocol.ProcessSendConnectionAcceptDelegate>(ProcessSendConnectionAccept),
                processSendConnectionRequest: new TransportFunctionPointer<NetworkProtocol.ProcessSendConnectionRequestDelegate>(ProcessSendConnectionRequest),
                processSendDisconnect: new TransportFunctionPointer<NetworkProtocol.ProcessSendDisconnectDelegate>(ProcessSendDisconnect),
                update: new TransportFunctionPointer<NetworkProtocol.UpdateDelegate>(Update),
                needsUpdate: true,
                userData: UserData,
                maxHeaderSize: RelayMessageRelay.Length + UdpCHeader.Length,
                maxFooterSize: 2
            );
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ComputePacketAllocationSizeDelegate))]
        public static int ComputePacketAllocationSize(ref NetworkDriver.Connection connection, ref int dataCapacity, out int dataOffset)
        {
            var capacityCost = dataCapacity == 0 ? RelayMessageRelay.Length : 0;
            var extraSize = dataCapacity == 0 ? 0 : RelayMessageRelay.Length;

            var size = UnityTransportProtocol.ComputePacketAllocationSize(ref connection, ref dataCapacity, out dataOffset);

            dataOffset += RelayMessageRelay.Length;
            dataCapacity -= capacityCost;

            return size + extraSize;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessReceiveDelegate))]
        public static void ProcessReceive(IntPtr stream, ref NetworkInterfaceEndPoint endpoint, int size, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData, ref ProcessPacketCommand command)
        {
            unsafe
            {
                var protocolData = (RelayProtocolData*)userData;

                if (endpoint != protocolData->ServerEndpoint)
                {
                    command.Type = ProcessPacketCommandType.Drop;
                    return;
                }

                var data = (byte*)stream;
                var header = *(RelayMessageHeader*)data;

                if (size < RelayMessageHeader.Length || !header.IsValid())
                {
                    UnityEngine.Debug.LogError("Received an invalid Relay message header");
                    command.Type = ProcessPacketCommandType.Drop;
                    return;
                }

                switch (header.Type)
                {
                    case RelayMessageType.BindReceived:
                        if (size != RelayMessageHeader.Length)
                        {
                            UnityEngine.Debug.LogError("Received an invalid Relay Bind Received message: Wrong length");
                            command.Type = ProcessPacketCommandType.Drop;
                            return;
                        }

                        protocolData->ConnectionState = RelayConnectionState.Bound;
                        command.Type = ProcessPacketCommandType.BindAccept;
                        return;
                    case RelayMessageType.Accepted:
                        command.Type = ProcessPacketCommandType.Drop;

                        if (size != RelayMessageAccepted.Length)
                        {
                            UnityEngine.Debug.LogError("Received an invalid Relay Accepted message: Wrong length");
                            return;
                        }

                        if (protocolData->HostAllocationId != default(RelayAllocationId))
                            return;

                        var acceptedMessage = *(RelayMessageAccepted*)data;
                        protocolData->HostAllocationId = acceptedMessage.FromAllocationId;

                        command.Type = ProcessPacketCommandType.AddressUpdate;
                        command.AsAddressUpdate.Address = default;
                        command.AsAddressUpdate.NewAddress = default;
                        command.AsAddressUpdate.SessionToken = protocolData->ConnectionReceiveToken;
                        fixed (byte* addressPtr = command.AsAddressUpdate.NewAddress.data)
                        {
                            *(RelayAllocationId*)addressPtr = acceptedMessage.FromAllocationId;
                        }

                        SendConnectionRequestToHost(ref protocolData->ServerData.AllocationId, ref acceptedMessage.FromAllocationId, protocolData->ConnectionReceiveToken,
                            ref protocolData->ServerEndpoint, ref sendInterface, ref queueHandle);

                        return;
                    case RelayMessageType.Relay:
                        var relayMessage = *(RelayMessageRelay*)data;
                        relayMessage.DataLength = RelayNetworkProtocol.SwitchEndianness(relayMessage.DataLength);
                        if (size < RelayMessageRelay.Length || size != RelayMessageRelay.Length + relayMessage.DataLength)
                        {
                            UnityEngine.Debug.LogError($"Received an invalid Relay Received message: Wrong length");
                            command.Type = ProcessPacketCommandType.Drop;
                            return;
                        }

                        // TODO: Make sure UTP protocol is not sending any message back here as it wouldn't be using Relay
                        UnityTransportProtocol.ProcessReceive(stream + RelayMessageRelay.Length, ref endpoint, size - RelayMessageRelay.Length, ref sendInterface, ref queueHandle, IntPtr.Zero, ref command);

                        switch (command.Type)
                        {
                            case ProcessPacketCommandType.ConnectionAccept:
                                protocolData->ConnectionState = RelayConnectionState.Connected;
                                break;

                            case ProcessPacketCommandType.Data:
                                command.AsData.Offset += RelayMessageRelay.Length;
                                break;

                            case ProcessPacketCommandType.DataWithImplicitConnectionAccept:
                                command.AsDataWithImplicitConnectionAccept.Offset += RelayMessageRelay.Length;
                                break;

                        }

                        command.ConnectionAddress = default;
                        fixed (byte* addressPtr = command.ConnectionAddress.data)
                        {
                            *(RelayAllocationId*)addressPtr = relayMessage.FromAllocationId;
                        }

                        return;
                }

                command.Type = ProcessPacketCommandType.Drop;
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendDelegate))]
        public static unsafe int ProcessSend(ref NetworkDriver.Connection connection, bool hasPipeline, ref NetworkSendInterface sendInterface, ref NetworkInterfaceSendHandle sendHandle, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            var relayProtocolData = (RelayProtocolData*)userData;

            var dataLength = (ushort)UnityTransportProtocol.WriteSendMessageHeader(ref connection, hasPipeline, ref sendHandle, RelayMessageRelay.Length);

            var relayMessage = (RelayMessageRelay*)sendHandle.data;
            fixed (byte* addressPtr = connection.Address.data)
            {
                *relayMessage = RelayMessageRelay.Create(relayProtocolData->ServerData.AllocationId, *(RelayAllocationId*)addressPtr, dataLength);
            }
            relayProtocolData->LastSentTime = relayProtocolData->LastUpdateTime;

            return sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref relayProtocolData->ServerEndpoint, sendInterface.UserData, ref queueHandle);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendConnectionAcceptDelegate))]
        public static void ProcessSendConnectionAccept(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            unsafe
            {
                var relayProtocolData = (RelayProtocolData*)userData;

                var toAllocationId = default(RelayAllocationId);

                fixed(byte* addrPtr = connection.Address.data)
                    toAllocationId = *(RelayAllocationId*) addrPtr;

                var maxLengthNeeded = RelayMessageRelay.Length + UnityTransportProtocol.GetConnectionAcceptMessageMaxLength();
                if (sendInterface.BeginSendMessage.Ptr.Invoke(out var sendHandle, sendInterface.UserData, maxLengthNeeded) != 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet");
                    return;
                }

                if (sendHandle.capacity < maxLengthNeeded)
                {
                    sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                    UnityEngine.Debug.LogError("Failed to send a ConnectionAccept packet: size exceeds capacity");
                    return;
                }

                var packet = (byte*) sendHandle.data;
                var size = UnityTransportProtocol.WriteConnectionAcceptMessage(ref connection, packet + RelayMessageRelay.Length, sendHandle.capacity - RelayMessageRelay.Length);

                if (size < 0)
                {
                    sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                    UnityEngine.Debug.LogError("Failed to send a ConnectionAccept packet");
                    return;
                }

                sendHandle.size = RelayMessageRelay.Length + size;

                var relayMessage = (RelayMessageRelay*)packet;
                *relayMessage = RelayMessageRelay.Create(relayProtocolData->ServerData.AllocationId, toAllocationId, (ushort)size);
                Assert.IsTrue(sendHandle.size <= sendHandle.capacity);

                if (sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref relayProtocolData->ServerEndpoint, sendInterface.UserData, ref queueHandle) < 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a ConnectionAccept packet");
                }
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendConnectionRequestDelegate))]
        public static void ProcessSendConnectionRequest(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            unsafe
            {
                var relayProtocolData = (RelayProtocolData*)userData;

                relayProtocolData->ServerData.ConnectionSessionId = connection.ReceiveToken;
                relayProtocolData->ConnectionState = RelayConnectionState.Connecting;
                relayProtocolData->ConnectionReceiveToken = connection.ReceiveToken;

                if (relayProtocolData->HostAllocationId == default)
                {
                    SendConnectionRequestToRelay(ref relayProtocolData->ServerData.AllocationId, ref relayProtocolData->ServerData.HostConnectionData,
                        ref relayProtocolData->ServerEndpoint, ref sendInterface, ref queueHandle);
                }
                else
                {
                    SendConnectionRequestToHost(ref relayProtocolData->ServerData.AllocationId, ref relayProtocolData->HostAllocationId, relayProtocolData->ConnectionReceiveToken,
                        ref relayProtocolData->ServerEndpoint, ref sendInterface, ref queueHandle);
                }
            }
        }

        [BurstCompatible]
        // TODO: As Relay service does not support complete handshake yet, we are using the UdpCHeader handshake for connections for now
        public static unsafe void SendConnectionRequestToHost(ref RelayAllocationId allocationId, ref RelayAllocationId hostAllocationId, ushort receiveToken, ref NetworkInterfaceEndPoint serverEndpoint, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle)
        {
            if (sendInterface.BeginSendMessage.Ptr.Invoke(out var sendHandle, sendInterface.UserData, RelayMessageRelay.Length + UdpCHeader.Length) != 0)
            {
                UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet to host");
                return;
            }

            var packet = (byte*) sendHandle.data;
            sendHandle.size = RelayMessageRelay.Length + UdpCHeader.Length;
            if (sendHandle.size > sendHandle.capacity)
            {
                sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet to host");
                return;
            }

            var relayMessage = (RelayMessageRelay*) packet;
            *relayMessage = RelayMessageRelay.Create(allocationId, hostAllocationId, UdpCHeader.Length);

            var header = (UdpCHeader*) (((byte*)relayMessage) + RelayMessageRelay.Length);
            *header = new UdpCHeader
            {
                Type = (byte) UdpCProtocol.ConnectionRequest,
                SessionToken = receiveToken,
                Flags = 0
            };

            sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref serverEndpoint, sendInterface.UserData, ref queueHandle);
        }

        [BurstCompatible]
        public static unsafe void SendConnectionRequestToRelay(ref RelayAllocationId allocationId, ref RelayConnectionData hostConnectionData, ref NetworkInterfaceEndPoint serverEndpoint, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle)
        {
            if (sendInterface.BeginSendMessage.Ptr.Invoke(out var sendHandle, sendInterface.UserData, RelayMessageConnectRequest.Length) != 0)
            {
                UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet");
                return;
            }

            var packet = (byte*) sendHandle.data;
            sendHandle.size = RelayMessageConnectRequest.Length;
            if (sendHandle.size > sendHandle.capacity)
            {
                sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet");
                return;
            }

            var message = (RelayMessageConnectRequest*) packet;
            *message = RelayMessageConnectRequest.Create(
                allocationId,
                hostConnectionData
            );

            sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref serverEndpoint, sendInterface.UserData, ref queueHandle);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendDisconnectDelegate))]
        public static unsafe void ProcessSendDisconnect(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            var relayProtocolData = (RelayProtocolData*)userData;

            if (sendInterface.BeginSendMessage.Ptr.Invoke(out var sendHandle, sendInterface.UserData, RelayMessageRelay.Length + UdpCHeader.Length) != 0)
            {
                UnityEngine.Debug.LogError("Failed to send a Disconnect packet to host");
                return;
            }

            var packet = (byte*) sendHandle.data;
            sendHandle.size = RelayMessageRelay.Length + UdpCHeader.Length;
            if (sendHandle.size > sendHandle.capacity)
            {
                sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                UnityEngine.Debug.LogError("Failed to send a Disconnect packet to host");
                return;
            }

            var relayMessage = (RelayMessageRelay*) packet;
            *relayMessage = RelayMessageRelay.Create(relayProtocolData->ServerData.AllocationId, connection.Address.AsRelayAllocationId(), UdpCHeader.Length);

            var header = (UdpCHeader*) (((byte*)relayMessage) + RelayMessageRelay.Length);
            *header = new UdpCHeader
            {
                Type = (byte) UdpCProtocol.Disconnect,
                SessionToken = connection.SendToken,
                Flags = 0
            };

            sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref relayProtocolData->ServerEndpoint, sendInterface.UserData, ref queueHandle);

            // Relay Disconnect
            if (sendInterface.BeginSendMessage.Ptr.Invoke(out sendHandle, sendInterface.UserData, RelayMessageDisconnect.Length) != 0)
            {
                UnityEngine.Debug.LogError("Failed to send a Disconnect packet to host");
                return;
            }

            packet = (byte*) sendHandle.data;
            sendHandle.size = RelayMessageDisconnect.Length;
            if (sendHandle.size > sendHandle.capacity)
            {
                sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                UnityEngine.Debug.LogError("Failed to send a Disconnect packet to host");
                return;
            }

            var disconnectMessage = (RelayMessageDisconnect*) packet;
            *disconnectMessage = RelayMessageDisconnect.Create(relayProtocolData->ServerData.AllocationId, connection.Address.AsRelayAllocationId());

            sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref relayProtocolData->ServerEndpoint, sendInterface.UserData, ref queueHandle);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.UpdateDelegate))]
        public static void Update(long updateTime, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            unsafe
            {
                var protocolData = (RelayProtocolData*)userData;

                switch (protocolData->ConnectionState)
                {
                    case RelayConnectionState.Binding:
                        if (updateTime - protocolData->LastConnectAttempt > protocolData->ConnectTimeoutMS || protocolData->LastUpdateTime == 0)
                        {
                            protocolData->LastConnectAttempt = updateTime;
                            protocolData->LastSentTime = updateTime;
                            SendBindMessage(ref protocolData->ServerEndpoint, ref sendInterface, ref queueHandle, userData);
                        }
                        break;
                    case RelayConnectionState.Bound:
                    case RelayConnectionState.Connected:
                    {
                        if (updateTime - protocolData->LastSentTime >= protocolData->RelayConnectionTimeMS) {
                            SendPingMessage(ref protocolData->ServerEndpoint, ref sendInterface, ref queueHandle, userData);
                            protocolData->LastSentTime = updateTime;
                        }
                    }
                    break;
                }

                protocolData->LastUpdateTime = updateTime;
            }
        }

        [BurstCompatible]
        private static unsafe int SendPingMessage(ref NetworkInterfaceEndPoint serverEndpoint, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            var protocolData = (RelayProtocolData*)userData;

            if (sendInterface.BeginSendMessage.Ptr.Invoke(out var sendHandle, sendInterface.UserData, RelayMessagePing.Length) != 0) {
                UnityEngine.Debug.LogError("Failed to send a RelayPingMessage packet");
                return -1;
            }

            var packet = (byte*)sendHandle.data;
            sendHandle.size = RelayMessagePing.Length;
            if (sendHandle.size > sendHandle.capacity)
            {
                sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                UnityEngine.Debug.LogError("Failed to send a RelayPingMessage packet");
                return -1;
            }

            var message = (RelayMessagePing*)packet;
            *message = RelayMessagePing.Create(protocolData->ServerData.AllocationId, 0);

            sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref protocolData->ServerEndpoint, sendInterface.UserData, ref queueHandle);
            return 0;
        }

        [BurstCompatible]
        private static unsafe int SendBindMessage(ref NetworkInterfaceEndPoint serverEndpoint, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            const int requirePayloadSize = RelayMessageBind.Length;

            if (sendInterface.BeginSendMessage.Ptr.Invoke(out var sendHandle, sendInterface.UserData, requirePayloadSize) != 0)
            {
                UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet");
                return -1;
            }

            var writer = WriterForSendBuffer(requirePayloadSize, ref sendHandle);
            if (writer.IsCreated == false)
            {
                sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                UnityEngine.Debug.LogError("Failed to send a RelayBindMessage packet");
                return -1;
            }

            // RelayMessageBind contains unaligned ushort, so we don't want to 'blit' the structure, instead we're using DataStreamWriter
            var protocolData = (RelayProtocolData*)userData;
            RelayMessageBind.Write(writer, 0, protocolData->ServerData.Nonce, protocolData->ServerData.ConnectionData.Value, protocolData->ServerData.HMAC);

            sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref protocolData->ServerEndpoint, sendInterface.UserData, ref queueHandle);
            return 0;
        }

        static DataStreamWriter WriterForSendBuffer(int requestSize, ref NetworkInterfaceSendHandle sendHandle)
        {
            unsafe
            {
                if (requestSize <= sendHandle.capacity)
                {
                    sendHandle.size = requestSize;
                    return new DataStreamWriter((byte*) sendHandle.data, sendHandle.size);
                }
            }

            return default;
        }
    }
}
