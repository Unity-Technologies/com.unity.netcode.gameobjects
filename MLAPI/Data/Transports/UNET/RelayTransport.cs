using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MLAPI.Data.Transports.UNET
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

        public static int Connect(int hostId, string serverAddress, int serverPort, string relayAddress, int relayPort, int exceptionConnectionId, out byte error)
        {
            isClient = true;
            RelayTransport.address = serverAddress;
            RelayTransport.port = (ushort)serverPort;
            relayConnectionId = NetworkTransport.Connect(hostId, relayAddress, relayPort, exceptionConnectionId, out error); // Requests connection
            return relayConnectionId;
            // Wait here until connect event is accepted
        }

        public static int ConnectWithSimulator(int hostId, string serverAddress, int serverPort, string relayAddress, int relayPort, int exceptionConnectionId, out byte error, ConnectionSimulatorConfig conf)
        {
            isClient = true;
            RelayTransport.address = serverAddress;
            RelayTransport.port = (ushort)serverPort;
            relayConnectionId = NetworkTransport.ConnectWithSimulator(hostId, relayAddress, relayPort, exceptionConnectionId, out error, conf); // Requests connection
            return relayConnectionId;
        }

        public static int ConnectEndPoint(int hostId, EndPoint endPoint, string relayAddress, int relayPort, int exceptionConnectionId, out byte error)
        {
            isClient = true;
            RelayTransport.address = ((IPEndPoint)endPoint).Address.ToString();
            RelayTransport.port = (ushort)((IPEndPoint)endPoint).Port;
            relayConnectionId = NetworkTransport.Connect(hostId, relayAddress, relayPort, exceptionConnectionId, out error); // Requests connection
            return relayConnectionId;
        }

        private static void SetChannelsFromTopology(HostTopology topology) => channels = topology.DefaultConfig.Channels;

        public static int AddHost(HostTopology topology, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHost(topology);
            if (createServer)
            {
                relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
                Debug.Log((NetworkError)b);
            }
            return ret;
        }
        public static int AddHost(HostTopology topology, int port, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHost(topology, port);
            if (createServer)
            {
                relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
                Debug.Log((NetworkError)b);
            }
            return ret;
        }
        public static int AddHost(HostTopology topology, int port, string ip, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHost(topology, port, ip);
            if (createServer)
            {
                relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
                Debug.Log((NetworkError)b);
            }
            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port, string ip, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port, ip);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
            return ret;
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
            return ret;
        }

        public static int AddWebsocketHost(HostTopology topology, int port, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddWebsocketHost(topology, port);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
            return ret;
        }

        public static int AddWebsocketHost(HostTopology topology, int port, string ip, bool createServer, string relayAddress, int relayPort)
        {
            isClient = !createServer;
            SetChannelsFromTopology(topology);
            int ret = NetworkTransport.AddWebsocketHost(topology, port, ip);
            if (createServer) relayConnectionId = NetworkTransport.Connect(ret, relayAddress, relayPort, 0, out byte b);
            return ret;
        }

        private static byte[] disconnectBuffer = new byte[3] { (byte)MessageType.ClientDisconnect, 0, 0 };
        public static bool Disconnect(int hostId, int connectionId, out byte error)
        {
            if (!isClient)
            {
                disconnectBuffer.ToBytes((ushort)connectionId, 1);
                return NetworkTransport.Send(hostId, relayConnectionId, GetReliableChannel(), disconnectBuffer, 3, out error);
            }
            else
            {
                return NetworkTransport.Disconnect(hostId, connectionId, out error);
            }
        }

        public static bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (isClient)
            {
                ForwardOffset(buffer, 1, size); // Offsets just the bytes we're sending
                ++size;
            }
            else
            {
                ForwardOffset(buffer, 3, size); // Offsets just the bytes we're sending
                size += 3;

                buffer[1] = (byte)connectionId;
                buffer[2] = (byte)(connectionId >> 8);
            }
            buffer[0] = (byte)MessageType.Data;

            return NetworkTransport.Send(hostId, relayConnectionId, channelId, buffer, size, out error);
        }

        public static bool QueueMessageForSending(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (isClient)
            {
                ForwardOffset(buffer, 1, size); // Offsets just the bytes we're sending
                ++size;
            }
            else
            {
                ForwardOffset(buffer, 3, size); // Offsets just the bytes we're sending
                size += 3;

                buffer[1] = (byte)connectionId;
                buffer[2] = (byte)(connectionId >> 8);
            }
            buffer[0] = (byte)MessageType.Data;

            return NetworkTransport.QueueMessageForSending(hostId, relayConnectionId, channelId, buffer, size, out error);
        }

        public static bool SendQueuedMessages(int hostId, int connectionId, out byte error)
        {
            return NetworkTransport.SendQueuedMessages(hostId, relayConnectionId, out error);
        }

        public static NetworkEventType ReceiveFromHost(int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            NetworkEventType @event = NetworkTransport.ReceiveFromHost(hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);
            return BaseReceive(@event, hostId, ref connectionId, ref channelId, buffer, bufferSize, ref receivedSize, ref error);
        }

        public static NetworkEventType Receive(out int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            NetworkEventType @event = NetworkTransport.Receive(out hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);
            return BaseReceive(@event, hostId, ref connectionId, ref channelId, buffer, bufferSize, ref receivedSize, ref error);
        }

        private static NetworkEventType BaseReceive(NetworkEventType @event, int hostId, ref int connectionId, ref int channelId, byte[] buffer, int bufferSize, ref int receivedSize, ref byte error)
        {
            switch (@event)
            {
                case NetworkEventType.DataEvent:
                    MessageType messageType = (MessageType)buffer[0];
                    switch (messageType)
                    {
                        case MessageType.ConnectToServer: // Connection approved
                            {
                                if (!isClient)
                                {
                                    connectionId = (ushort)(buffer[1] | (buffer[2] << 8)); // Parse connection id
                                }
                                return NetworkEventType.ConnectEvent;
                            }
                        case MessageType.Data:
                            {
                                if (isClient)
                                {
                                    // Remove our headers
                                    ReverseOffset(buffer, 1, receivedSize);
                                    --receivedSize;
                                }
                                else
                                {
                                    // Remove our headers
                                    connectionId = buffer.FromBytes(1);
                                    ReverseOffset(buffer, 3, receivedSize);
                                    receivedSize -= 3;
                                }
                                return NetworkEventType.DataEvent;
                            }
                        case MessageType.ClientDisconnect:
                            {
                                connectionId = (ushort)(buffer[1] | (buffer[2] << 8)); // Parse connection id
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
                            buffer[0] = (byte)MessageType.ConnectToServer;
                            buffer[1] = (byte)s.Length;
                            for (int i = 0; i < s.Length; i++)
                            {
                                buffer[i + 2] = (byte)s[i]; // Get ASCII characters
                            }

                            NetworkTransport.Send(hostId, connectionId, GetReliableChannel(), buffer, s.Length + 2, out error);
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
                        if ((NetworkError)error == NetworkError.CRCMismatch) Debug.LogError("[MLAPI.Relay] The MLAPI Relay detected a CRC missmatch. This could be due to the maxClients or other connectionConfig settings not being the same");
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
