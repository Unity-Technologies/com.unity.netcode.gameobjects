using System;
using System.Collections.Generic;

using MLAPI.Transports;
using MLAPI.Transports.Tasks;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.MLPI.UTP;
using Unity.Networking.Transport;

using UnityEngine;
using UnityEngine.Assertions;

public class UTPTransport : NetworkTransport
{
    public int MessageBufferSize = 1024 * 5;
    public int MaxConnections = 100;
    public int MaxSentMessageQueueSize = 128;

    public ushort Port = 7777;
    public string Address = "127.0.0.1";

    private bool isClient = false;
    private bool isServer = false;

    public override ulong ServerClientId => 0;

    public NetworkDriver m_Driver;
    private NetworkDriver.Concurrent m_ConcurrentDriver;

    public NativeList<NetworkConnection> m_Connections;

    private NativeQueue<NetworkConnection> m_newConnections;
    private NativeQueue<int> m_pendingHandlesToProcess;

    [NativeDisableContainerSafetyRestriction]
    private UnsafePayloadBuffer m_payloadsRx;

    [NativeDisableContainerSafetyRestriction]
    private UnsafePayloadBuffer m_payloadsTx;

    private JobHandle m_jobHandle;

    private UnsafeAtomicFreeList m_connectionIdPool;
    private Dictionary<int, int> m_internalIdToClientID = new Dictionary<int, int>();

    [NativeDisableContainerSafetyRestriction]
    private NativeQueue<SendDataMessage> m_pendingSendMessages;


   // [BurstCompile]
   // [BurstCompatible]
    internal unsafe struct ReceiveJob : IJob
    {
        public NetworkDriver.Concurrent driver;

        [NativeDisableUnsafePtrRestriction]
        public UnsafePayloadBuffer Rx;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<NetworkConnection> connections;

        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<NetworkConnection> newConnections;

        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int>.ParallelWriter handles;

        public unsafe void Execute()
        {
            for (int index = 0; index < connections.Length; index++)
            {
                Assert.IsTrue(connections[index].IsCreated);
                Unity.Networking.Transport.NetworkEvent.Type evt;
                while ((evt = driver.PopEventForConnection(connections[index], out var reader)) !=
                       Unity.Networking.Transport.NetworkEvent.Type.Empty)
                {
                    switch (evt)
                    {
                        case Unity.Networking.Transport.NetworkEvent.Type.Connect:
                            newConnections.Enqueue(connections[index]);
                        break;
                        case Unity.Networking.Transport.NetworkEvent.Type.Data:
                        {
                                // we need to get a handle of some data?
                                var handle = Rx.Allocate();
                                if (handle != -1)
                                {
                                    // We don't actually process the data here that needs
                                    // to happen on the main thread
                                    var rawData = Rx[handle];

                                    reader.ReadBytes((byte*)rawData, reader.Length);
                                    handles.Enqueue(handle);
                                }
                                else
                                {
                                    // Log some error here ? 
                                }
                        }
                        break;
                    }
                }
            }
        }
    }

  //  [BurstCompatible]
    public struct SendDataMessage
    {
        public byte channelIndex;
        public int clientId;
        public int length;
        public NetworkConnection connection;
        public NetworkPipeline pipeline;
        public int handle;
    }

  //  [BurstCompile]
  //  [BurstCompatible]
    struct FlushSendJob : IJob
    {
        public NetworkDriver.Concurrent driver;

        [NativeDisableUnsafePtrRestriction]
        public UnsafePayloadBuffer Tx;

        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<SendDataMessage> messages;

        public void Execute()
        {
            SendDataMessage message;
            while (messages.TryDequeue(out message))
            {
                driver.BeginSend(message.pipeline, message.connection, out var writer);

                if (!writer.IsCreated)
                {
                    Tx.Free(message.handle);
                    return;
                }

                unsafe
                {
                    writer.WriteInt(message.clientId);
                    writer.WriteByte(message.channelIndex);
                    writer.WriteInt(message.length);
                    writer.WriteBytes((byte*)Tx[message.handle], message.length);
                }
               
                driver.EndSend(writer);

                // We free up the handle
                Tx.Free(message.handle);
            }
        }
    }

  //  [BurstCompile]
  //  [BurstCompatible]
    struct ConnectionsAcceptJob : IJob
    {
        public NetworkDriver driver;

        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<NetworkConnection> newConnections;

        public void Execute()
        {
            // Accept new connections
            NetworkConnection con;
            while ((con = driver.Accept()) != default(NetworkConnection))
            {
                DataStreamReader reader;
                if (con.PopEvent(driver, out reader) != Unity.Networking.Transport.NetworkEvent.Type.Empty)
                {
                    con.Disconnect(driver);
                    continue;
                }

                newConnections.Enqueue(con);
            }
        }
    }

    public override void DisconnectLocalClient()
    {
        throw new NotImplementedException();
    }

    public override void DisconnectRemoteClient(ulong clientId)
    {
        // We don't need to tell the driver it gets cleaned up in the connection
        // job
        ReleaseClientID((int)clientId);
    }

    public override ulong GetCurrentRtt(ulong clientId)
    {
        throw new NotImplementedException();
    }

    public override void Init()
    {
        m_jobHandle.Complete();

        m_Driver = NetworkDriver.Create();

        //// So we have a bunch of different pipelines we can send :D
        //networkPipelines[0] = m_Driver.CreatePipeline(typeof(NullPipelineStage));
        //networkPipelines[1] = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        //networkPipelines[2] = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

        m_ConcurrentDriver = m_Driver.ToConcurrent();
        m_newConnections = new NativeQueue<NetworkConnection>(Allocator.Persistent);
        m_pendingHandlesToProcess = new NativeQueue<int>(Allocator.Persistent);
        m_connectionIdPool = new UnsafeAtomicFreeList(MaxConnections, Allocator.Persistent);
        m_pendingSendMessages = new NativeQueue<SendDataMessage>(Allocator.Persistent);

        m_payloadsTx = new UnsafePayloadBuffer(MaxSentMessageQueueSize * MessageBufferSize, MessageBufferSize);
        m_payloadsRx = new UnsafePayloadBuffer(MaxSentMessageQueueSize * MessageBufferSize, MessageBufferSize);
    }

    public override MLAPI.Transports.NetworkEvent PollEvent(out ulong clientId, out NetworkChannel networkChannel, out ArraySegment<byte> payload, out float receiveTime)
    {
        clientId = 0;
        networkChannel = 0;
        payload = new ArraySegment<byte>(Array.Empty<byte>());
        receiveTime = 0;

        return MLAPI.Transports.NetworkEvent.Nothing;
    }

    public void Update()
    {
        if (!isServer && !isClient)
            return;


        m_jobHandle.Complete();

        // we should process new connections here!
        while (!m_newConnections.IsEmpty())
        {
            var con = m_newConnections.Dequeue();
            m_Connections.Add(con);

            InvokeOnTransportEvent(MLAPI.Transports.NetworkEvent.Connect, (ulong)AllocateClientID(m_Connections.Length - 1), NetworkChannel.ChannelUnused, new ArraySegment<byte>(), Time.realtimeSinceStartup);
        }

        m_jobHandle = m_Driver.ScheduleUpdate(m_jobHandle);

        if (isServer)
        {
            var connectionJob = new ConnectionsAcceptJob
            {
                driver = m_Driver,
                newConnections = m_newConnections
            };

            m_jobHandle = connectionJob.Schedule(m_jobHandle);
        }

        var updateJob = new ReceiveJob
        {
            driver = m_ConcurrentDriver,
            connections = m_Connections,
            newConnections = m_newConnections,
            handles = m_pendingHandlesToProcess.AsParallelWriter(),
            Rx = m_payloadsRx
        };

        m_jobHandle = updateJob.Schedule(m_jobHandle);

        var sendJob = new FlushSendJob
        {
            driver = m_ConcurrentDriver,
            messages = m_pendingSendMessages,
            Tx = m_payloadsTx
        };
        m_jobHandle = sendJob.Schedule(m_jobHandle);
        m_jobHandle = m_Driver.ScheduleFlushSend(m_jobHandle);

        int handle = 1;
        while(m_pendingHandlesToProcess.TryDequeue(out handle))
        {
            unsafe
            {
                var data = m_payloadsRx[handle];

                var array = new NativeArray<byte>(MessageBufferSize, Allocator.None);
                var ptr = array.GetUnsafePtr();
                ptr = data;

                DataStreamReader reader = new DataStreamReader(array);
                var clientId = reader.ReadInt();
                var channelByte = reader.ReadByte();
                var dataLength = reader.ReadInt();

                var dataArray = new byte[dataLength];
                fixed (byte* pDest = dataArray)
                {
                    reader.ReadBytes(pDest, dataLength);
                }

                var payload = new ArraySegment<byte>(dataArray);
                InvokeOnTransportEvent(MLAPI.Transports.NetworkEvent.Data, (ulong)clientId, (NetworkChannel)channelByte, payload, Time.realtimeSinceStartup);

                m_payloadsRx.Free(handle);
            }
        }
    }


    public override void Send(ulong clientId, ArraySegment<byte> data, NetworkChannel networkChannel)
    {
        int connectionIndex = -1;
        if (m_internalIdToClientID.TryGetValue((int)clientId, out connectionIndex)) {
            var handle = m_payloadsTx.Allocate();
            unsafe
            {
                var allocator = new UnsafeScratchAllocator((void*)m_payloadsTx[handle], MessageBufferSize);
                var dataBytes = (byte*)allocator.Allocate<byte>(data.Count);
                fixed (byte* pDest = data.Array)
                    UnsafeUtility.MemCpy(dataBytes, pDest, data.Count);

                var queuedMessage = new SendDataMessage()
                {
                    channelIndex = (byte)networkChannel,
                    pipeline = NetworkPipeline.Null,
                    connection = m_Connections[connectionIndex],
                    length = data.Count,
                    handle = handle,
                    clientId = (int)clientId
                };

                m_pendingSendMessages.Enqueue(queuedMessage);
            }
        }


        //throw new NotImplementedException();
    }

    public override void Shutdown()
    {
        m_jobHandle.Complete();

        if (m_Connections.IsCreated)
            m_Connections.Dispose();

        if (m_Driver.IsCreated)
            m_Driver.Dispose();

        if (m_pendingSendMessages.IsCreated)
            m_pendingSendMessages.Dispose();

        if (m_pendingHandlesToProcess.IsCreated)
            m_pendingHandlesToProcess.Dispose();

        if (m_newConnections.IsCreated)
            m_newConnections.Dispose();

        m_payloadsRx.Dispose();
        m_payloadsTx.Dispose();

    }

    public override SocketTasks StartClient()
    {
        m_jobHandle.Complete();

        m_Connections = new NativeList<NetworkConnection>(1, Allocator.Persistent);
        var endpoint = NetworkEndPoint.Parse(Address, Port);
        m_Connections.Add(m_Driver.Connect(endpoint));
        isClient = true;

        Debug.Log("StartClient");
        return SocketTask.Working.AsTasks();
    }

    public override SocketTasks StartServer()
    {
        m_jobHandle.Complete();

        m_Connections = new NativeList<NetworkConnection>(MaxConnections, Allocator.Persistent);
        var endpoint = NetworkEndPoint.Parse(Address, Port);
        isServer = true;

        Debug.Log("StartServer");

        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port " + Port);
        }
        else
        {
            m_Driver.Listen();
        }

        return SocketTask.Working.AsTasks();
    }

    private int AllocateClientID(int connectionIndex)
    {
        var clientID = m_connectionIdPool.Pop();
        m_internalIdToClientID[clientID] = connectionIndex;

        return clientID;
    }

    private void ReleaseClientID(int clientID)
    {
        m_connectionIdPool.Push(clientID);
        m_internalIdToClientID.Remove(clientID);
    }
}

