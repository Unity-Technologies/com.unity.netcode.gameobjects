#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using ENet;
using MLAPI.Transports;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace MLAPI.EnetTransport
{
    public class EnetTransport : Transport
    {
        [Serializable]
        public struct EnetChannel
        {
            public byte Id;
            public string Name;
            public PacketFlags Flags;
        }
        
        public ushort Port = 7777;
        public string Address = "127.0.0.1";
        public int MaxClients = 100;
        public List<EnetChannel> Channels = new List<EnetChannel>();
        public int MessageBufferSize = 1024 * 5;
        
        
        // Runtime / state
        private byte[] messageBuffer;
        private WeakReference temporaryBufferReference;
        
        
        private readonly Dictionary<uint, Peer> connectedEnetPeers = new Dictionary<uint, Peer>();
        
        private readonly Dictionary<string, byte> channelNameToId = new Dictionary<string, byte>();
        private readonly Dictionary<byte, string> channelIdToName = new Dictionary<byte, string>();
        private readonly Dictionary<byte, EnetChannel> internalChannels = new Dictionary<byte, EnetChannel>();
        
        private Host host;

        private uint serverPeerId;

        public override ulong ServerClientId => GetMLAPIClientId(0, true);
        
        public override void Send(ulong clientId, ArraySegment<byte> data, string channelName, bool skipQueue)
        {
            Packet packet = default(Packet);
            
            packet.Create(data.Array, data.Offset, data.Count, internalChannels[channelNameToId[channelName]].Flags);
            
            GetEnetConnectionDetails(clientId, out uint peerId);

            connectedEnetPeers[peerId].Send(channelNameToId[channelName], ref packet);
        }

        public override void FlushSendQueue(ulong clientId)
        {
            // Not needed
        }

        public override NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload)
        {
            Event @event;

            if (host.CheckEvents(out @event) <= 0)
            {
                if (host.Service(0, out @event) <= 0)
                {
                    clientId = 0;
                    channelName = null;
                    payload = new ArraySegment<byte>();

                    return NetEventType.Nothing;
                }
            }

            clientId = GetMLAPIClientId(@event.Peer.ID, false);

            switch (@event.Type)
            {
                case EventType.None:
                {
                    channelName = null;
                    payload = new ArraySegment<byte>();

                    return NetEventType.Nothing;
                }
                case EventType.Connect:
                {
                    channelName = null;
                    payload = new ArraySegment<byte>();

                    connectedEnetPeers.Add(@event.Peer.ID, @event.Peer);

                    return NetEventType.Connect;
                }
                case EventType.Disconnect:
                {
                    channelName = null;
                    payload = new ArraySegment<byte>();

                    connectedEnetPeers.Remove(@event.Peer.ID);

                    return NetEventType.Disconnect;
                }
                case EventType.Receive:
                {
                    channelName = channelIdToName[@event.ChannelID];
                    int size = @event.Packet.Length;

                    if (size > messageBuffer.Length)
                    {
                        byte[] tempBuffer;

                        if (temporaryBufferReference != null && temporaryBufferReference.IsAlive && ((byte[]) temporaryBufferReference.Target).Length >= size)
                        {
                            tempBuffer = (byte[]) temporaryBufferReference.Target;
                        }
                        else
                        {
                            tempBuffer = new byte[size];
                            temporaryBufferReference = new WeakReference(tempBuffer);
                        }

                        @event.Packet.CopyTo(tempBuffer);
                        payload = new ArraySegment<byte>(tempBuffer, 0, size);
                    }
                    else
                    {
                        @event.Packet.CopyTo(messageBuffer);
                        payload = new ArraySegment<byte>(messageBuffer, 0, size);
                    }

                    @event.Packet.Dispose();

                    return NetEventType.Data;
                }
                case EventType.Timeout:
                {
                    channelName = null;
                    payload = new ArraySegment<byte>();

                    connectedEnetPeers.Remove(@event.Peer.ID);

                    return NetEventType.Disconnect;
                }
                default:
                {
                    channelName = null;
                    payload = new ArraySegment<byte>();

                    return NetEventType.Nothing;
                }
            }
        }

        public override void StartClient()
        {
            host = new Host();
            
            host.Create(1, MLAPI_CHANNELS.Length + Channels.Count);

            Address address = new Address();
            address.Port = Port;
            address.SetHost(Address);
            
            Peer serverPeer = host.Connect(address, MLAPI_CHANNELS.Length + Channels.Count);

            serverPeerId = serverPeer.ID;
        }

        public override void StartServer()
        {
            host = new Host();
            
            Address address = new Address();
            address.Port = Port;
            
            host.Create(address, MaxClients, MLAPI_CHANNELS.Length + Channels.Count);
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            GetEnetConnectionDetails(serverPeerId, out uint peerId);

            connectedEnetPeers[peerId].DisconnectLater(0);   
        }

        public override void DisconnectLocalClient()
        {
            host.Flush();
            
            GetEnetConnectionDetails(serverPeerId, out uint peerId);

            connectedEnetPeers[peerId].DisconnectNow(0);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            GetEnetConnectionDetails(serverPeerId, out uint peerId);
            
            return connectedEnetPeers[peerId].RoundTripTime;
        }

        public override void Shutdown()
        {
            host.Flush();
            host.Dispose();
            Library.Deinitialize();
        }

        public override void Init()
        {
            Library.Initialize();

            internalChannels.Clear();
            channelIdToName.Clear();
            channelNameToId.Clear();
            
            
            // MLAPI Channels
            for (byte i = 0; i < MLAPI_CHANNELS.Length; i++)
            {
                channelIdToName.Add(i, MLAPI_CHANNELS[i].Name);
                channelNameToId.Add(MLAPI_CHANNELS[i].Name, i);
                internalChannels.Add(i, new EnetChannel()
                {
                    Id = i,
                    Name = MLAPI_CHANNELS[i].Name,
                    Flags = MLAPIChannelTypeToPacketFlag(MLAPI_CHANNELS[i].Type)
                });
            }
            
            // Internal Channels
            for (int i = 0; i < Channels.Count; i++)
            {
                byte id = (byte) (i + MLAPI_CHANNELS.Length);
                
                channelIdToName.Add(id, Channels[i].Name);
                channelNameToId.Add(Channels[i].Name, id);
                internalChannels.Add(id, new EnetChannel()
                {
                    Id = id,
                    Name = Channels[i].Name,
                    Flags = Channels[i].Flags
                });
            }
            
            messageBuffer = new byte[MessageBufferSize];
        }

        public PacketFlags MLAPIChannelTypeToPacketFlag(ChannelType type)
        {
            switch (type)
            {
                case ChannelType.Unreliable:
                {
                    return PacketFlags.Unsequenced | PacketFlags.UnreliableFragment;
                }
                case ChannelType.Reliable:
                {
                    return PacketFlags.Reliable | PacketFlags.Unsequenced;
                }
                case ChannelType.ReliableSequenced:
                {
                    return PacketFlags.Reliable;
                }
                case ChannelType.StateUpdate:
                {
                    return PacketFlags.None;
                }
                case ChannelType.ReliableFragmentedSequenced:
                {
                    return PacketFlags.Reliable;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
        }
        
        public ulong GetMLAPIClientId(uint peerId, bool isServer)
        {
            if (isServer)
            {
                return 0;
            }
            else
            {
                return peerId + 1;
            }
        }

        public void GetEnetConnectionDetails(ulong clientId, out uint peerId)
        {
            if (clientId == 0)
            {
                peerId = serverPeerId;
            }
            else
            {
                peerId = (uint)clientId - 1;
            }
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member