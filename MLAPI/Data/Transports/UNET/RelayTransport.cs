#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
            ClientDisconnect
        }

        //State
        private static int relayConnectionId;
        private static bool isClient = false;
        private static string address;
        private static ushort port;
        private static List<ChannelQOS> channels = new List<ChannelQOS>();

        public static bool Enabled => NetworkingManager.Singleton.NetworkConfig.RelayEnabled;
        public static string RelayAddress => NetworkingManager.Singleton.NetworkConfig.RelayAddress;
        public static ushort RelayPort => NetworkingManager.Singleton.NetworkConfig.RelayPort;

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
            if (!Enabled) return NetworkTransport.AddHost(topology);

            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHost(topology);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);
            return ret;
        }
        public static int AddHost(HostTopology topology, int port, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHost(topology, port);

            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHost(topology, port);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);
            return ret;
        }
        public static int AddHost(HostTopology topology, int port, string ip, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHost(topology, port, ip);

            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHost(topology, port, ip);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);
            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port, string ip, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout);

            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port, ip);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);
            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout);

            isClient = !createServer;
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
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddWebsocketHost(topology, port);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);
            return ret;
        }

        public static int AddWebsocketHost(HostTopology topology, int port, string ip, bool createServer)
        {
            if (!Enabled) return NetworkTransport.AddWebsocketHost(topology, port, ip);

            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddWebsocketHost(topology, port, ip);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, RelayAddress, RelayPort, 0, out byte b);
            return ret;
        }

        private static byte[] disconnectBuffer = new byte[] { 0, 0, (byte)MessageType.ClientDisconnect };
        public static bool Disconnect(int hostId, int connectionId, out byte error)
        {
            if (!Enabled) NetworkTransport.Disconnect(hostId, connectionId, out error);

            if (!isClient)
            {
                disconnectBuffer.ToBytes((ushort)connectionId); // Tell relay who to drop
                return NetworkTransport.Send(hostId, relayConnectionId, GetReliableChannel(), disconnectBuffer, 3, out error);
            }
            else return NetworkTransport.Disconnect(hostId, connectionId, out error);
        }

        public static bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (!Enabled) return NetworkTransport.Send(hostId, connectionId, channelId, buffer, size, out error);
            //ForwardOffset(buffer, 1, size); // Offsets just the bytes we're sending (isClient old)
            ++size;
            if (!isClient)
            {
                //ForwardOffset(buffer, 3, size); // Offsets just the bytes we're sending
                size += 2;

                buffer[size - 3] = (byte)connectionId;
                buffer[size - 2] = (byte)(connectionId >> 8);
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
                //ForwardOffset(buffer, 3, size); // Offsets just the bytes we're sending
                size += 2;

                buffer[size - 3] = (byte)connectionId;
                buffer[size - 2] = (byte)(connectionId >> 8);
            }
            buffer[size - 1] = (byte)MessageType.Data;

            return NetworkTransport.QueueMessageForSending(hostId, relayConnectionId, channelId, buffer, size, out error);
        }

        public static bool SendQueuedMessages(int hostId, int connectionId, out byte error) => NetworkTransport.SendQueuedMessages(hostId, relayConnectionId, out error);

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
                    MessageType messageType = (MessageType)buffer[receivedSize - 1];
                    switch (messageType)
                    {
                        case MessageType.ConnectToServer: // Connection approved
                            {
                                if (!isClient)
                                    connectionId = (ushort)(buffer[receivedSize - 3] | (buffer[receivedSize - 2] << 8)); // Parse connection id
                                return NetworkEventType.ConnectEvent;
                            }
                        case MessageType.Data:
                            {
                                // Implicitly remove header
                                if (isClient) --receivedSize;
                                else connectionId = buffer.FromBytes(receivedSize -= 3);

                                return NetworkEventType.DataEvent;
                            }
                        case MessageType.ClientDisconnect:
                            {
                                connectionId = (ushort)(buffer[0] | (buffer[1] << 8)); // Parse connection id
                                return NetworkEventType.DisconnectEvent;
                            }
                    }
                    break;
                case NetworkEventType.ConnectEvent:
                    {
                        if (isClient)
                        {
                            //Connect via relay
                            string s = new StringBuilder(address).Append(':').Append(port).ToString();
                            buffer[s.Length] = (byte)MessageType.ConnectToServer;

                            // Address data length is implied
                            for (int i = 0; i < s.Length; i++)
                                buffer[i] = (byte)s[i]; // Get ASCII characters

                            NetworkTransport.Send(hostId, connectionId, GetReliableChannel(), buffer, s.Length + 1, out error);
                        }
                        else
                        {
                            //Register us as a server
                            buffer[0] = (byte)MessageType.StartServer;
                            NetworkTransport.Send(hostId, connectionId, GetReliableChannel(), buffer, 1, out error);
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

        public static void ReverseOffset(byte[] b, int offset, int dLen)
        {
            for (int i = offset; i < dLen; ++i)
                b[i - offset] = b[i];
        }

        public static void ForwardOffset(byte[] b, int offset, int dLen)
        {
            for (int i = dLen; i >= 0; --i)
                b[i + offset] = b[i];
        }

        //TODO: Fix
        public static byte GetReliableChannel()
        {
            for (byte i = 0; i < channels.Count; i++)
            {
                switch (channels[i].QOS)
                {
                    case QosType.Reliable:
                    case QosType.ReliableFragmented:
                    case QosType.ReliableFragmentedSequenced:
                    case QosType.ReliableSequenced:
                    case QosType.ReliableStateUpdate:
                        return i;
                }
            }
            throw new InvalidConfigException("A reliable channel is required");
        }
    }

    public static class Helpers
    {
        public static void ToBytes(this byte[] b, ushort data, int offset = 0)
        {
            b[offset] = (byte)data;
            b[offset + 1] = (byte)(data >> 8);
        }
        public static ushort FromBytes(this byte[] b, int offset = 0)
        {
            return (ushort)(b[offset] | (b[offset + 1] << 8));
        }
    }

    public class InvalidConfigException : SystemException
    {
        public InvalidConfigException() { }
        public InvalidConfigException(string issue) : base(issue) { }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
