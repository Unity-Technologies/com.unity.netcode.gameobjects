using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using MLAPI.Transports;
using MLAPI.Transports.Tasks;

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;

using UnityEngine;
using UnityEngine.Assertions;


[StructLayout(LayoutKind.Explicit)]
public unsafe struct RawNetworkMessage
{
    [FieldOffset(0)] public int length;
    [FieldOffset(4)] public uint type;
    [FieldOffset(8)] public int id;
    [FieldOffset(12)] public byte padding;
    [FieldOffset(13)] public byte channelId;
    [FieldOffset(14)] public fixed byte data[NetworkParameterConstants.MTU];
}

[BurstCompile]
struct ClientUpdateJob : IJob
{
    public NetworkDriver driver;
    public NativeArray<NetworkConnection> connection;
    public NativeQueue<RawNetworkMessage> packetData;

    unsafe public void Execute()
    {
        if (!connection[0].IsCreated) {
            return;
        }

        DataStreamReader streamReader;
        NetworkEvent.Type cmd;

        while ((cmd = connection[0].PopEvent(driver, out streamReader)) != NetworkEvent.Type.Empty) {
            if (cmd == NetworkEvent.Type.Connect) {
                var d = new RawNetworkMessage() { length = 0, type = (uint)NetEventType.Connect, id = connection[0].InternalId };
                packetData.Enqueue(d);
            }
            else if (cmd == NetworkEvent.Type.Data) {
                byte channelId = streamReader.ReadByte();
                int messageSize = streamReader.ReadInt();

                var temp = new NativeArray<byte>(messageSize, Allocator.Temp);
                streamReader.ReadBytes(temp);

                var d = new RawNetworkMessage()
                {
                        length = messageSize,
                        type = (uint)NetEventType.Data,
                        id = connection[0].InternalId,
                        channelId = channelId
                };

                UnsafeUtility.MemCpy(d.data, temp.GetUnsafePtr(), d.length);

                packetData.Enqueue(d);
            }
            else if (cmd == NetworkEvent.Type.Disconnect) {
                connection[0] = default;
            }
        }
    }
}

[BurstCompile]
struct ServerUpdateJob : IJobParallelForDefer
{
    public NetworkDriver.Concurrent driver;
    public NativeArray<NetworkConnection> connections;
    public NativeQueue<RawNetworkMessage>.ParallelWriter packetData;

    private unsafe void QueueMessage(ref DataStreamReader streamReader, int index)
    {
        byte channelId = streamReader.ReadByte();
        int messageSize = streamReader.ReadInt();

        var temp = new NativeArray<byte>(messageSize, Allocator.Temp);
        streamReader.ReadBytes(temp);

      //  Debug.Log($"Server: Got a message {channelId} {messageSize} ");

        var d = new RawNetworkMessage() {
            length = messageSize,
            type = (uint)NetEventType.Data,
            id = index,
            channelId = channelId
        };

        UnsafeUtility.MemCpy(d.data, temp.GetUnsafePtr(), d.length);
        packetData.Enqueue(d);
    }

    public unsafe void Execute(int index)
    {
        DataStreamReader streamReader;
        Assert.IsTrue(connections[index].IsCreated);

        NetworkEvent.Type command;
        while ((command = driver.PopEventForConnection(connections[index], out streamReader)) != NetworkEvent.Type.Empty) {
            if (command == NetworkEvent.Type.Data) {
                QueueMessage(ref streamReader, index);
            }
            else if (command == NetworkEvent.Type.Connect) {
                var d = new RawNetworkMessage() { length = 0, type = (uint)NetEventType.Connect, id = index };
                packetData.Enqueue(d);
            }
            else if (command == NetworkEvent.Type.Disconnect) {
                var d = new RawNetworkMessage() { length = 0, type = (uint)NetEventType.Disconnect, id = index };
                packetData.Enqueue(d);
                connections[index] = default;
            }
        }
    }
}

[BurstCompile]
struct ServerUpdateConnectionsJob : IJob
{
    public NetworkDriver driver;
    public NativeList<NetworkConnection> connections;
    public NativeQueue<RawNetworkMessage>.ParallelWriter packetData;

    public void Execute()
    {
        // Clean up connections
        for (int i = 0; i < connections.Length; i++) {
            if (!connections[i].IsCreated) {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
        // Accept new connections
        NetworkConnection c;
        while ((c = driver.Accept()) != default(NetworkConnection)) {
            connections.Add(c);
            var d = new RawNetworkMessage() { length = 0, type = (uint)NetEventType.Connect, id = c.InternalId };
            packetData.Enqueue(d);
            Debug.Log("Accepted a connection");
        }
    }
}

public class UTPTransport : Transport
{
    public ushort Port = 7777;
    public string Address = "127.0.0.1";


    [Serializable]
    public struct UTPChannel
    {
        [UnityEngine.HideInInspector]
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

    public NetworkDriver m_Driver;
    public NativeList<NetworkConnection> m_Connections;
    public NativeQueue<RawNetworkMessage> m_packetData;
    private NativeArray<byte> m_packetProcessBuffer;

    private JobHandle m_jobHandle;

    private bool isClient = false;
    private bool isServer = false;


    public override ulong ServerClientId => 0;

    public override void DisconnectLocalClient() { m_Driver.Disconnect(m_Connections[0]); }
    public override void DisconnectRemoteClient(ulong clientId)
    {
        GetUTPConnectionDetails(clientId, out uint peerId);
        var con = GetConnection(peerId);
        if (con != default)
            m_Driver.Disconnect(con);
    }

    private NetworkConnection GetConnection(uint id)
    {
        foreach (var item in m_Connections) {
            if (item.InternalId == id)
                return item;
        }

        return default;
    }

    public override ulong GetCurrentRtt(ulong clientId) => 0;

    private NetworkPipeline[] networkPipelines = new NetworkPipeline[3];
    private readonly Dictionary<string, byte> channelNameToId = new Dictionary<string, byte>();
    private readonly Dictionary<byte, string> channelIdToName = new Dictionary<byte, string>();
    private readonly Dictionary<byte, UTPChannel> internalChannels = new Dictionary<byte, UTPChannel>();

    public override void Init()
    {
        m_Driver = NetworkDriver.Create();

        // So we have a bunch of different pipelines we can send :D
        networkPipelines[0] = m_Driver.CreatePipeline(typeof(NullPipelineStage));
        networkPipelines[1] = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        networkPipelines[2] = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

        internalChannels.Clear();
        channelIdToName.Clear();
        channelNameToId.Clear();

        // MLAPI Channels
        for (byte i = 0; i < MLAPI_CHANNELS.Length; i++) {
            channelIdToName.Add(i, MLAPI_CHANNELS[i].Name);
            channelNameToId.Add(MLAPI_CHANNELS[i].Name, i);
            internalChannels.Add(i, new UTPChannel() {
                Id = i,
                Name = MLAPI_CHANNELS[i].Name,
                Flags = MLAPIChannelTypeToPacketFlag(MLAPI_CHANNELS[i].Type)
            });
        }

        m_packetData = new NativeQueue<RawNetworkMessage>(Allocator.Persistent);
        m_packetProcessBuffer = new NativeArray<byte>(1000, Allocator.Persistent);
    }

    public UTPDelivery MLAPIChannelTypeToPacketFlag(ChannelType type)
    {
        switch (type) {
            case ChannelType.Unreliable: {
                return UTPDelivery.Unreliable;
            }
            case ChannelType.Reliable: {

                return UTPDelivery.ReliableSequenced;
            }
            case ChannelType.ReliableSequenced: {
                return UTPDelivery.ReliableSequenced;
            }
            case ChannelType.ReliableFragmentedSequenced: {
                return UTPDelivery.ReliableSequenced;
            }
            case ChannelType.UnreliableSequenced: {
                return UTPDelivery.UnreliableSequenced;
            }
            default: {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    public override NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload, out float receiveTime)
    {
        clientId = 0;
        channelName = "";
        payload = new ArraySegment<byte>(Array.Empty<byte>());
        receiveTime = 0;

        return NetEventType.Nothing;
    }

    void Update()
    {
        if (isServer || isClient) {
            RawNetworkMessage message;
            while (m_packetData.TryDequeue(out message)) {
                var data = m_packetProcessBuffer.Slice(0, message.length);
                unsafe {
                    UnsafeUtility.MemClear(data.GetUnsafePtr(), message.length);
                    UnsafeUtility.MemCpy(data.GetUnsafePtr(), message.data, message.length);
                }
                var clientId = GetMLAPIClientId((uint)message.id, false);

                switch ((NetEventType)message.type) {
                    case NetEventType.Data:
                        int size = message.length;
                        byte[] arr = new byte[size];
                        unsafe {
                            Marshal.Copy((IntPtr)message.data, arr, 0, size);
                            var payload = new ArraySegment<byte>(arr);
                            InvokeOnTransportEvent((NetEventType)message.type, clientId, channelIdToName[message.channelId], payload, Time.realtimeSinceStartup);
                        }

                    break;
                    case NetEventType.Connect: {
                        InvokeOnTransportEvent((NetEventType)message.type, clientId, null, new ArraySegment<byte>(), Time.realtimeSinceStartup);
                    }
                    break;
                    case NetEventType.Disconnect:
                        InvokeOnTransportEvent((NetEventType)message.type, clientId, null, new ArraySegment<byte>(), Time.realtimeSinceStartup);
                    break;
                    case NetEventType.Nothing:
                        InvokeOnTransportEvent((NetEventType)message.type, clientId, null, new ArraySegment<byte>(), Time.realtimeSinceStartup);
                    break;
                }
            }


            if (m_jobHandle.IsCompleted) {

                if (isServer) {
                    var connectionJob = new ServerUpdateConnectionsJob {
                        driver = m_Driver,
                        connections = m_Connections,
                        packetData = m_packetData.AsParallelWriter()

                    };

                    var serverUpdateJob = new ServerUpdateJob {
                        driver = m_Driver.ToConcurrent(),
                        connections = m_Connections.AsDeferredJobArray(),
                        packetData = m_packetData.AsParallelWriter()
                    };

                    m_jobHandle = m_Driver.ScheduleUpdate();
                    m_jobHandle = connectionJob.Schedule(m_jobHandle);
                    m_jobHandle = serverUpdateJob.Schedule(m_Connections, 1, m_jobHandle);
                }

                if (isClient) {
                    var job = new ClientUpdateJob {
                        driver = m_Driver,
                        connection = m_Connections,
                        packetData = m_packetData
                    };
                    m_jobHandle = m_Driver.ScheduleUpdate();
                    m_jobHandle = job.Schedule(m_jobHandle);
                }
            }

            m_jobHandle.Complete();
        }
    }

    [BurstCompile]
    public void SendToClient(NativeArray<byte> packet, ulong clientId, int index)
    {
        for (int i = 0; i < m_Connections.Length; i++) {
            if (m_Connections[i].InternalId != (int)clientId)
                continue;

            var writer = m_Driver.BeginSend(networkPipelines[index], m_Connections[i]);

            if (!writer.IsCreated)
                continue;

            writer.WriteBytes(packet);

            m_Driver.EndSend(writer);
        }
    }

    public unsafe override void Send(ulong clientId, ArraySegment<byte> data, string channelName)
    {
        var pipelineIndex = MLAPIChannelToPipeline(internalChannels[channelNameToId[channelName]].Flags);

        GetUTPConnectionDetails(clientId, out uint peerId);

        DataStreamWriter writer = new DataStreamWriter(data.Count + 1 + 4, Allocator.Temp);
        writer.WriteByte(channelNameToId[channelName]);
        writer.WriteInt(data.Count);

        fixed (byte* dataArrayPtr = data.Array) {
            writer.WriteBytes(dataArrayPtr, data.Count);
        }

        SendToClient(writer.AsNativeArray(), peerId, pipelineIndex);
    }

    public override void Shutdown()
    {
        m_jobHandle.Complete();
        m_packetData.Dispose();
        m_Connections.Dispose();
        m_Driver.Dispose();
        m_packetProcessBuffer.Dispose();
    }

    // This is kind of a mess!
    public override SocketTasks StartClient()
    {
        m_Connections = new NativeList<NetworkConnection>(1, Allocator.Persistent);
        var endpoint = NetworkEndPoint.Parse(Address, Port);
        m_Connections.Add(m_Driver.Connect(endpoint));
        isClient = true;

        Debug.Log("StartClient");
        return SocketTask.Working.AsTasks();

    }

    public int MLAPIChannelToPipeline(UTPDelivery type)
    {
        switch (type) {
            case UTPDelivery.UnreliableSequenced:
            return 2;
            case UTPDelivery.ReliableSequenced:
            return 1;
            case UTPDelivery.Unreliable:
            return 0;
        }

        return 0;
    }

    public ulong GetMLAPIClientId(uint peerId, bool isServer)
    {
        if (isServer) {
            return 0;
        }
        else {
            return peerId + 1;
        }
    }

    public void GetUTPConnectionDetails(ulong clientId, out uint peerId)
    {
        if (clientId == 0) {
            peerId = (uint)ServerClientId;
        }
        else {
            peerId = (uint)clientId - 1;
        }
    }

    public override SocketTasks StartServer()
    {
        m_Connections = new NativeList<NetworkConnection>(300, Allocator.Persistent);
        var endpoint = NetworkEndPoint.Parse(Address, Port);
        isServer = true;

        Debug.Log("StartServer");

        if (m_Driver.Bind(endpoint) != 0) {
            Debug.LogError("Failed to bind to port " + Port);
        }
        else {
            m_Driver.Listen();
        }

        return SocketTask.Working.AsTasks();
    }
}

