using System;
using System.Runtime.InteropServices;

using Unity.Netcode;

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;

using UnityEngine;
using UnityEngine.Assertions;

using NetworkEvent = Unity.Networking.Transport.NetworkEvent;
using NetcodeEvent = Unity.Netcode.NetworkEvent;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RawNetworkMessage
{
    [FieldOffset(0)] public int Length;
    [FieldOffset(4)] public uint Type;
    [FieldOffset(8)] public int Id;
    [FieldOffset(12)] public byte Padding;
    [FieldOffset(13)] public byte ChannelId;
    [FieldOffset(14)] public fixed byte Data[NetworkParameterConstants.MTU];
}

[BurstCompile]
internal struct ClientUpdateJob : IJob
{
    public NetworkDriver Driver;
    public NativeArray<NetworkConnection> Connection;
    public NativeQueue<RawNetworkMessage> PacketData;

    unsafe public void Execute()
    {
        if (!Connection[0].IsCreated)
        {
            return;
        }

        DataStreamReader streamReader;
        NetworkEvent.Type cmd;

        while ((cmd = Connection[0].PopEvent(Driver, out streamReader)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                var d = new RawNetworkMessage() { Length = 0, Type = (uint)NetcodeEvent.Connect, Id = Connection[0].InternalId };
                PacketData.Enqueue(d);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                byte channelId = streamReader.ReadByte();
                int messageSize = streamReader.ReadInt();

                var temp = new NativeArray<byte>(messageSize, Allocator.Temp);
                streamReader.ReadBytes(temp);

                var d = new RawNetworkMessage()
                {
                    Length = messageSize,
                    Type = (uint)NetcodeEvent.Data,
                    Id = Connection[0].InternalId,
                    ChannelId = channelId
                };

                UnsafeUtility.MemCpy(d.Data, temp.GetUnsafePtr(), d.Length);

                PacketData.Enqueue(d);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Connection[0] = default;
            }
        }
    }
}

[BurstCompile]
internal struct ServerUpdateJob : IJobParallelForDefer
{
    public NetworkDriver.Concurrent Driver;
    public NativeArray<NetworkConnection> Connections;
    public NativeQueue<RawNetworkMessage>.ParallelWriter PacketData;

    private unsafe void QueueMessage(ref DataStreamReader streamReader, int index)
    {
        byte channelId = streamReader.ReadByte();
        int messageSize = streamReader.ReadInt();

        var temp = new NativeArray<byte>(messageSize, Allocator.Temp);
        streamReader.ReadBytes(temp);

        //  Debug.Log($"Server: Got a message {channelId} {messageSize} ");

        var d = new RawNetworkMessage()
        {
            Length = messageSize,
            Type = (uint)NetcodeEvent.Data,
            Id = index,
            ChannelId = channelId
        };

        UnsafeUtility.MemCpy(d.Data, temp.GetUnsafePtr(), d.Length);
        PacketData.Enqueue(d);
    }

    public unsafe void Execute(int index)
    {
        DataStreamReader streamReader;
        Assert.IsTrue(Connections[index].IsCreated);

        NetworkEvent.Type command;
        while ((command = Driver.PopEventForConnection(Connections[index], out streamReader)) != NetworkEvent.Type.Empty)
        {
            if (command == NetworkEvent.Type.Data)
            {
                QueueMessage(ref streamReader, index);
            }
            else if (command == NetworkEvent.Type.Connect)
            {
                var d = new RawNetworkMessage() { Length = 0, Type = (uint)NetcodeEvent.Connect, Id = index };
                PacketData.Enqueue(d);
            }
            else if (command == NetworkEvent.Type.Disconnect)
            {
                var d = new RawNetworkMessage() { Length = 0, Type = (uint)NetcodeEvent.Disconnect, Id = index };
                PacketData.Enqueue(d);
                Connections[index] = default;
            }
        }
    }
}

[BurstCompile]
internal struct ServerUpdateConnectionsJob : IJob
{
    public NetworkDriver Driver;
    public NativeList<NetworkConnection> Connections;
    public NativeQueue<RawNetworkMessage>.ParallelWriter PacketData;

    public void Execute()
    {
        // Clean up connections
        for (int i = 0; i < Connections.Length; i++)
        {
            if (!Connections[i].IsCreated)
            {
                Connections.RemoveAtSwapBack(i);
                --i;
            }
        }
        // Accept new connections
        NetworkConnection c;
        while ((c = Driver.Accept()) != default(NetworkConnection))
        {
            Connections.Add(c);
            var d = new RawNetworkMessage() { Length = 0, Type = (uint)NetcodeEvent.Connect, Id = c.InternalId };
            PacketData.Enqueue(d);
            Debug.Log("Accepted a connection");
        }
    }
}

public class UTPTransport : NetworkTransport
{
    public ushort Port = 7777;
    public string Address = "127.0.0.1";

    [Serializable]
    public struct UTPChannel
    {
        [HideInInspector]
        public byte Id;
        public string Name;
        public UTPDelivery Flags;
    }

    public enum UTPDelivery
    {
        UnreliableSequenced,
        ReliableSequenced,
        Unreliable
    }

    public NetworkDriver Driver;
    public NativeList<NetworkConnection> Connections;
    public NativeQueue<RawNetworkMessage> PacketData;
    private NativeArray<byte> m_PacketProcessBuffer;

    private JobHandle m_JobHandle;

    private bool m_IsClient = false;
    private bool m_IsServer = false;


    public override ulong ServerClientId => 0;

    public override void DisconnectLocalClient() { _ = Driver.Disconnect(Connections[0]); }
    public override void DisconnectRemoteClient(ulong clientId)
    {
        GetUTPConnectionDetails(clientId, out uint peerId);
        var con = GetConnection(peerId);
        if (con != default)
        {
            Driver.Disconnect(con);
        }
    }

    private NetworkConnection GetConnection(uint id)
    {
        foreach (var item in Connections)
        {
            if (item.InternalId == id)
            {
                return item;
            }
        }

        return default;
    }

    private NetworkPipeline[] m_NetworkPipelines = new NetworkPipeline[3];

    public override void Init()
    {
        Driver = NetworkDriver.Create();

        // So we have a bunch of different pipelines we can send :D
        m_NetworkPipelines[0] = Driver.CreatePipeline(typeof(NullPipelineStage));
        m_NetworkPipelines[1] = Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        m_NetworkPipelines[2] = Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

        PacketData = new NativeQueue<RawNetworkMessage>(Allocator.Persistent);
        m_PacketProcessBuffer = new NativeArray<byte>(1000, Allocator.Persistent);
    }

    [BurstCompile]
    public void SendToClient(NativeArray<byte> packet, ulong clientId, int index)
    {
        for (int i = 0; i < Connections.Length; i++)
        {
            if (Connections[i].InternalId != (int)clientId)
            {
                continue;
            }

            var writer = Driver.BeginSend(m_NetworkPipelines[index], Connections[i]);

            if (!writer.IsCreated)
            {
                continue;
            }

            writer.WriteBytes(packet);

            Driver.EndSend(writer);
        }
    }

    public override unsafe void Send(ulong clientId, ArraySegment<byte> data, NetworkChannel networkChannel)
    {
        var pipelineIndex = 0;

        GetUTPConnectionDetails(clientId, out uint peerId);

        var writer = new DataStreamWriter(data.Count + 1 + 4, Allocator.Temp);
        writer.WriteByte((byte)networkChannel);
        writer.WriteInt(data.Count);

        fixed (byte* dataArrayPtr = data.Array)
        {
            writer.WriteBytes(dataArrayPtr, data.Count);
        }

        SendToClient(writer.AsNativeArray(), peerId, pipelineIndex);
    }

    public override NetcodeEvent PollEvent(out ulong clientId, out NetworkChannel networkChannel, out ArraySegment<byte> payload, out float receiveTime)
    {
        clientId = 0;
        networkChannel = NetworkChannel.ChannelUnused;

        payload = new ArraySegment<byte>(Array.Empty<byte>());
        receiveTime = 0;

        return NetcodeEvent.Nothing;
    }

    public override ulong GetCurrentRtt(ulong clientId) => 0;

    private void Update()
    {
        if (m_IsServer || m_IsClient)
        {
            RawNetworkMessage message;
            while (PacketData.TryDequeue(out message))
            {
                var data = m_PacketProcessBuffer.Slice(0, message.Length);
                unsafe
                {
                    UnsafeUtility.MemClear(data.GetUnsafePtr(), message.Length);
                    UnsafeUtility.MemCpy(data.GetUnsafePtr(), message.Data, message.Length);
                }
                var clientId = GetNetcodeClientId((uint)message.Id, false);

                switch ((NetcodeEvent)message.Type)
                {
                    case NetcodeEvent.Data:
                        int size = message.Length;
                        byte[] arr = new byte[size];
                        unsafe
                        {
                            Marshal.Copy((IntPtr)message.Data, arr, 0, size);
                            var payload = new ArraySegment<byte>(arr);
                            InvokeOnTransportEvent((NetcodeEvent)message.Type, clientId, (NetworkChannel)message.ChannelId, payload, Time.realtimeSinceStartup);
                        }

                        break;
                    case NetcodeEvent.Connect:
                        {
                            InvokeOnTransportEvent((NetcodeEvent)message.Type, clientId, NetworkChannel.ChannelUnused, new ArraySegment<byte>(), Time.realtimeSinceStartup);
                        }
                        break;
                    case NetcodeEvent.Disconnect:
                        InvokeOnTransportEvent((NetcodeEvent)message.Type, clientId, NetworkChannel.ChannelUnused, new ArraySegment<byte>(), Time.realtimeSinceStartup);
                        break;
                    case NetcodeEvent.Nothing:
                        InvokeOnTransportEvent((NetcodeEvent)message.Type, clientId, NetworkChannel.ChannelUnused, new ArraySegment<byte>(), Time.realtimeSinceStartup);
                        break;
                }
            }


            if (m_JobHandle.IsCompleted)
            {

                if (m_IsServer)
                {
                    var connectionJob = new ServerUpdateConnectionsJob
                    {
                        Driver = Driver,
                        Connections = Connections,
                        PacketData = PacketData.AsParallelWriter()

                    };

                    var serverUpdateJob = new ServerUpdateJob
                    {
                        Driver = Driver.ToConcurrent(),
                        Connections = Connections.AsDeferredJobArray(),
                        PacketData = PacketData.AsParallelWriter()
                    };

                    m_JobHandle = Driver.ScheduleUpdate();
                    m_JobHandle = connectionJob.Schedule(m_JobHandle);
                    m_JobHandle = serverUpdateJob.Schedule(Connections, 1, m_JobHandle);
                }

                if (m_IsClient)
                {
                    var job = new ClientUpdateJob
                    {
                        Driver = Driver,
                        Connection = Connections,
                        PacketData = PacketData
                    };
                    m_JobHandle = Driver.ScheduleUpdate();
                    m_JobHandle = job.Schedule(m_JobHandle);
                }
            }

            m_JobHandle.Complete();
        }
    }

    public override void Shutdown()
    {
        m_JobHandle.Complete();

        if (PacketData.IsCreated)
        {
            PacketData.Dispose();
        }

        if (Connections.IsCreated)
        {
            Connections.Dispose();
        }

        Driver.Dispose();
        m_PacketProcessBuffer.Dispose();
    }

    // This is kind of a mess!
    public override SocketTasks StartClient()
    {
        Connections = new NativeList<NetworkConnection>(1, Allocator.Persistent);
        var endpoint = NetworkEndPoint.Parse(Address, Port);
        Connections.Add(Driver.Connect(endpoint));
        m_IsClient = true;

        Debug.Log("StartClient");
        return SocketTask.Working.AsTasks();
    }

    public int NetcodeChannelToPipeline(UTPDelivery type)
    {
        switch (type)
        {
            case UTPDelivery.UnreliableSequenced:
                return 2;
            case UTPDelivery.ReliableSequenced:
                return 1;
            case UTPDelivery.Unreliable:
                return 0;
        }

        return 0;
    }

    public ulong GetNetcodeClientId(uint peerId, bool isServer)
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

    public void GetUTPConnectionDetails(ulong clientId, out uint peerId)
    {
        if (clientId == 0)
        {
            peerId = (uint)ServerClientId;
        }
        else
        {
            peerId = (uint)clientId - 1;
        }
    }

    public override SocketTasks StartServer()
    {
        Connections = new NativeList<NetworkConnection>(300, Allocator.Persistent);
        var endpoint = NetworkEndPoint.Parse(Address, Port);
        m_IsServer = true;

        Debug.Log("StartServer");

        if (Driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port " + Port);
        }
        else
        {
            Driver.Listen();
        }

        return SocketTask.Working.AsTasks();
    }
}
