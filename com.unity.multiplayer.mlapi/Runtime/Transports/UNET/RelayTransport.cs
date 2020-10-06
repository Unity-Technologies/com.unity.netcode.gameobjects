#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace MLAPI.Transports.UNET
{
    public static class RelayTransport
    {
        private enum MessageType
        {
            StartServer,
            ConnectToServer,
            Data,
            ClientDisconnect,
            AddressReport
        }

        private static byte defaultChannelId;
        private static int relayConnectionId;
        private static bool isClient = false;
        private static string address;
        private static ushort port;
        private static List<ChannelQOS> channels = new List<ChannelQOS>();

        public static bool Enabled { get; set; } = true;
        public static string RelayAddress { get; set; } = "127.0.0.1";
        public static ushort RelayPort { get; set; } = 8888;

        public static event Action<IPEndPoint> OnRemoteEndpointReported;

        public static int Connect(int hostId, string serverAddress, int serverPort, int exceptionConnectionId, out byte error)
        {
            if (!Enabled) return NetworkTransport.Connect(hostId, serverAddress, serverPort, exceptionConnectionId, out error);

            isClient = true;

            RelayTransport.address = serverAddress;
            RelayTransport.port = (ushort)serverPort;

            relayConnectionId = NetworkTransport.Connect(hostId, RelayAddress, RelayPort, exceptionConnectionId, out error); // Requests connection

            return relayConnectionId;
        }

        public static int ConnectWithSimulator(int hostId, string serverAddress, int serverPort, int exceptionConnectionId, out byte error, ConnectionSimulatorConfig conf)
        {
            if (!Enabled) return NetworkTransport.ConnectWithSimulator(hostId, serverAddress, serverPort, exceptionConnectionId, out error, conf);

            isClient = true;

            RelayTransport.address = serverAddress;
            RelayTransport.port = (ushort)serverPort;

            relayConnectionId = NetworkTransport.ConnectWithSimulator(hostId, RelayAddress, RelayPort, exceptionConnectionId, out error, conf); // Requests connection

            return relayConnectionId;
        }

        public static int ConnectEndPoint(int hostId, EndPoint endPoint, int exceptionConnectionId, out byte error)
        {
            if (!Enabled) return NetworkTransport.ConnectEndPoint(hostId, endPoint, exceptionConnectionId, out error);

            isClient = true;

            RelayTransport.address = ((IPEndPoint)endPoint).Address.ToString();
            RelayTransport.port = (ushort)((IPEndPoint)endPoint).Port;

            relayConnectionId = NetworkTransport.Connect(hostId, RelayAddress, RelayPort, exceptionConnectionId, out error); // Requests connection

            return relayConnectionId;
        }

        private static void SetChannelsFromTopology(HostTopology topology) => channels = topology.DefaultConfig.Channels;

        public static int AddHost(HostTopology topology, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHost(topology, 0, null);

            isClient = !createServer;

            defaultChannelId = topology.DefaultConfig.AddChannel(QosType.ReliableSequenced);

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddHost(topology, 0, null);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }
        public static int AddHost(HostTopology topology, int port, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHost(topology, port);

            isClient = !createServer;

            defaultChannelId = topology.DefaultConfig.AddChannel(QosType.ReliableSequenced);

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddHost(topology, port);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }
        public static int AddHost(HostTopology topology, int port, string ip, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHost(topology, port, ip);

            isClient = !createServer;

            defaultChannelId = topology.DefaultConfig.AddChannel(QosType.ReliableSequenced);

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddHost(topology, port, ip);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port, string ip, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout);

            isClient = !createServer;

            defaultChannelId = topology.DefaultConfig.AddChannel(QosType.ReliableSequenced);

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port, ip);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout);

            isClient = !createServer;

            defaultChannelId = topology.DefaultConfig.AddChannel(QosType.ReliableSequenced);

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port);

            isClient = !createServer;

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }

        public static int AddWebsocketHost(HostTopology topology, int port, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddWebsocketHost(topology, port);

            isClient = !createServer;

            defaultChannelId = topology.DefaultConfig.AddChannel(QosType.ReliableSequenced);

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddWebsocketHost(topology, port);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }

        public static int AddWebsocketHost(HostTopology topology, int port, string ip, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddWebsocketHost(topology, port, ip);

            isClient = !createServer;

            defaultChannelId = topology.DefaultConfig.AddChannel(QosType.ReliableSequenced);

            SetChannelsFromTopology(topology);

            int ret = NetworkTransport.AddWebsocketHost(topology, port, ip);

            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);

            return ret;
        }

        private static readonly byte[] disconnectBuffer = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, (byte)MessageType.ClientDisconnect };
        public static bool Disconnect(int hostId, int connectionId, out byte error)
        {
            if (!Enabled) return NetworkTransport.Disconnect(hostId, connectionId, out error);

            if (!isClient)
            {
                for (byte i = 0; i < sizeof(ulong); i++) disconnectBuffer[i] = ((byte)((ulong)connectionId >> (i * 8)));

                return NetworkTransport.Send(hostId, relayConnectionId, defaultChannelId, disconnectBuffer, 9, out error);
            }

            return NetworkTransport.Disconnect(hostId, connectionId, out error);
        }

        public static bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (!Enabled) return NetworkTransport.Send(hostId, connectionId, channelId, buffer, size, out error);

            ++size;

            if (!isClient)
            {
                size += 8;

                int connectionIdOffset = size - 9;

                for (byte i = 0; i < sizeof(ulong); i++) buffer[connectionIdOffset + i] = ((byte)((ulong)connectionId >> (i * 8)));
            }

            buffer[size - 1] = (byte)MessageType.Data;

            return NetworkTransport.Send(hostId, relayConnectionId, channelId, buffer, size, out error);
        }

        public static bool QueueMessageForSending(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (!Enabled) return NetworkTransport.QueueMessageForSending(hostId, connectionId, channelId, buffer, size, out error);

            ++size;

            if (!isClient)
            {
                size += 8;

                int connectionIdOffset = size - 9;

                for (byte i = 0; i < sizeof(ulong); i++) buffer[connectionIdOffset + i] = ((byte)((ulong)connectionId >> (i * 8)));
            }

            buffer[size - 1] = (byte)MessageType.Data;

            return NetworkTransport.QueueMessageForSending(hostId, relayConnectionId, channelId, buffer, size, out error);
        }

        public static bool SendQueuedMessages(int hostId, int connectionId, out byte error)
        {
            if (!Enabled) return NetworkTransport.SendQueuedMessages(hostId, connectionId, out error);

            return NetworkTransport.SendQueuedMessages(hostId, relayConnectionId, out error);
        }

        public static NetworkEventType ReceiveFromHost(int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            if (!Enabled) return NetworkTransport.ReceiveFromHost(hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);

            NetworkEventType @event = NetworkTransport.ReceiveFromHost(hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);

            return BaseReceive(@event, hostId, ref connectionId, ref channelId, buffer, bufferSize, ref receivedSize, ref error);
        }

        public static NetworkEventType Receive(out int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            if (!Enabled) return NetworkTransport.Receive(out hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);

            NetworkEventType @event = NetworkTransport.Receive(out hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);

            return BaseReceive(@event, hostId, ref connectionId, ref channelId, buffer, bufferSize, ref receivedSize, ref error);
        }

        private static NetworkEventType BaseReceive(NetworkEventType @event, int hostId, ref int connectionId, ref int channelId, byte[] buffer, int bufferSize, ref int receivedSize, ref byte error)
        {
            switch (@event)
            {
                case NetworkEventType.DataEvent:
                    {
                        MessageType messageType = (MessageType)buffer[receivedSize - 1];

                        switch (messageType)
                        {
                            case MessageType.AddressReport:
                                {
                                    byte[] addressBytes = new byte[16];

                                    for (int i = 0; i < addressBytes.Length; i++)
                                        addressBytes[i] = buffer[i];

                                    ushort remotePort = (ushort)(((ushort)buffer[16]) |
                                                            ((ushort)buffer[17] << 8));

                                    IPEndPoint remoteEndPoint = new IPEndPoint(new IPAddress(addressBytes), remotePort);

                                    if (OnRemoteEndpointReported != null)
                                    {
                                        OnRemoteEndpointReported(remoteEndPoint);
                                    }
                                    break;
                                }
                            case MessageType.ConnectToServer: // Connection approved
                                {
                                    if (!isClient)
                                    {
                                        ulong _connectionId = (((ulong)buffer[receivedSize - 9]) |
                                                                ((ulong)buffer[receivedSize - 8] << 8) |
                                                                ((ulong)buffer[receivedSize - 7] << 16) |
                                                                ((ulong)buffer[receivedSize - 6] << 24) |
                                                                ((ulong)buffer[receivedSize - 5] << 32) |
                                                                ((ulong)buffer[receivedSize - 4] << 40) |
                                                                ((ulong)buffer[receivedSize - 3] << 48) |
                                                                ((ulong)buffer[receivedSize - 2] << 56));

                                        connectionId = (int)_connectionId;
                                    }

                                    return NetworkEventType.ConnectEvent;
                                }
                            case MessageType.Data:
                                {
                                    // Implicitly remove header
                                    if (isClient) --receivedSize;
                                    else
                                    {
                                        receivedSize -= 9;

                                        ulong _connectionId = (((ulong)buffer[receivedSize]) |
                                                                ((ulong)buffer[receivedSize + 1] << 8) |
                                                                ((ulong)buffer[receivedSize + 2] << 16) |
                                                                ((ulong)buffer[receivedSize + 3] << 24) |
                                                                ((ulong)buffer[receivedSize + 4] << 32) |
                                                                ((ulong)buffer[receivedSize + 5] << 40) |
                                                                ((ulong)buffer[receivedSize + 6] << 48) |
                                                                ((ulong)buffer[receivedSize + 7] << 56));

                                        connectionId = (int)_connectionId;
                                    }

                                    return NetworkEventType.DataEvent;
                                }
                            case MessageType.ClientDisconnect:
                                {
                                    ulong _connectionId = (((ulong)buffer[0]) |
                                                            ((ulong)buffer[1] << 8) |
                                                            ((ulong)buffer[2] << 16) |
                                                            ((ulong)buffer[3] << 24) |
                                                            ((ulong)buffer[4] << 32) |
                                                            ((ulong)buffer[5] << 40) |
                                                            ((ulong)buffer[6] << 48) |
                                                            ((ulong)buffer[7] << 56));

                                    connectionId = (int)_connectionId;

                                    return NetworkEventType.DisconnectEvent;
                                }
                        }
                    }
                    break;
                case NetworkEventType.ConnectEvent:
                    {
                        if (isClient)
                        {
                            //Connect via relay

                            byte[] ipv6AddressBuffer;
                            IPAddress ipAddress = IPAddress.Parse(address);

                            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                            {
                                ipv6AddressBuffer = ipAddress.GetAddressBytes();
                            }
                            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                byte[] ipv4Address = ipAddress.GetAddressBytes();
                                ipv6AddressBuffer = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, ipv4Address[0], ipv4Address[1], ipv4Address[2], ipv4Address[3] };
                            }
                            else
                            {
                                // TODO: Throw wrong type
                                ipv6AddressBuffer = null;
                            }

                            // TODO: Throw if address is not 16 bytes. It should always be
                            for (int i = 0; i < ipv6AddressBuffer.Length; i++) buffer[i] = ipv6AddressBuffer[i];

                            for (byte i = 0; i < sizeof(ushort); i++) buffer[16 + i] = ((byte)(port >> (i * 8)));

                            buffer[16 + 2] = (byte)MessageType.ConnectToServer;

                            NetworkTransport.Send(hostId, connectionId, defaultChannelId, buffer, 16 + 2 + 1, out error);
                        }
                        else
                        {
                            //Register us as a server
                            buffer[0] = (byte)MessageType.StartServer;

                            NetworkTransport.Send(hostId, connectionId, defaultChannelId, buffer, 1, out error);
                        }
                        return NetworkEventType.Nothing; // Connect event is ignored
                    }
                case NetworkEventType.DisconnectEvent:
                    {
                        if ((NetworkError)error == NetworkError.CRCMismatch) Debug.LogError("[MLAPI.Relay] The MLAPI Relay detected a CRC mismatch. This could be due to the maxClients or other connectionConfig settings not being the same");

                        return NetworkEventType.DisconnectEvent;
                    }
            }

            return @event;
        }
    }

    public class InvalidConfigException : SystemException
    {
        public InvalidConfigException() { }
        public InvalidConfigException(string issue) : base(issue) { }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
