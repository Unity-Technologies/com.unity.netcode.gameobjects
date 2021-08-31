using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode
{

    public class InvalidMessageStructureException : SystemException
    {
        public InvalidMessageStructureException() { }
        public InvalidMessageStructureException(string issue) : base(issue) { }
    }
    
    public class MessagingSystem: IMessageHandler, IDisposable
    {
#region Internal Types
        private struct ReceiveQueueItem
        {
            public FastBufferReader Reader;
            public MessageHeader Header;
            public ulong SenderId;
            public float Timestamp;
        }

        private struct SendQueueItem
        {
            public BatchHeader BatchHeader;
            public FastBufferWriter Writer;
            public readonly NetworkDelivery NetworkDelivery;

            public SendQueueItem(NetworkDelivery delivery, int writerSize, Allocator writerAllocator, int maxWriterSize = -1)
            {
                Writer = new FastBufferWriter(writerSize, writerAllocator, maxWriterSize);
                NetworkDelivery = delivery;
                BatchHeader = default;
            }
        }
        
        internal delegate void MessageHandler(ref FastBufferReader reader, NetworkContext context);
#endregion

#region Private Members
        private DynamicUnmanagedArray<ReceiveQueueItem> m_IncomingMessageQueue = new DynamicUnmanagedArray<ReceiveQueueItem>(16);

        private MessageHandler[] m_MessageHandlers = new MessageHandler[255];
        private Type[] m_ReverseTypeMap = new Type[255];
                
        private Dictionary<Type, byte> m_MessageTypes = new Dictionary<Type, byte>();
        private NativeHashMap<ulong, Ref<DynamicUnmanagedArray<SendQueueItem>>> m_SendQueues = new NativeHashMap<ulong, Ref<DynamicUnmanagedArray<SendQueueItem>>>(64, Allocator.Persistent);

        private List<INetworkHooks> m_Hooks = new List<INetworkHooks>();

        private byte m_HighMessageType;
        private object m_Owner;
        private IMessageSender m_MessageSender;
        private ulong m_LocalClientId;
        private bool m_Disposed;
#endregion

        internal Type[] MessageTypes => m_ReverseTypeMap;
        internal MessageHandler[] MessageHandlers => m_MessageHandlers;
        internal int MessageHandlerCount => m_HighMessageType;

        internal byte GetMessageType(Type t)
        {
            return m_MessageTypes[t];
        }

        public MessagingSystem(IMessageSender messageSender, object owner, ulong localClientId = Int64.MaxValue)
        {
            try
            {
                m_LocalClientId = localClientId;
                m_MessageSender = messageSender;
                m_Owner = owner;
                
                var interfaceType = typeof(INetworkMessage);
                var implementationTypes = new List<Type>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsInterface || type.IsAbstract)
                        {
                            continue;
                        }

                        if (interfaceType.IsAssignableFrom(type))
                        {
                            var attributes = type.GetCustomAttributes(typeof(Bind), false);
                            // If [Bind(ownerType)] isn't provided, it defaults to being bound to NetworkManager
                            // This is technically a breach of domain by having MessagingSystem know about the existence
                            // of NetworkManager... but ultimately, Bind is provided to support testing, not to support
                            // general use of MessagingSystem outside of Netcode for GameObjects, so having MessagingSystem
                            // know about NetworkManager isn't so bad. Especially since it's just a default value.
                            // This is just a convenience to keep us and our users from having to use
                            // [Bind(typeof(NetworkManager))] on every message - only tests that don't want to use
                            // the full NetworkManager need to worry about it.
                            var allowedToBind = attributes.Length == 0 && m_Owner is NetworkManager;
                            for (var i = 0; i < attributes.Length; ++i)
                            {
                                Bind bindAttribute = (Bind) attributes[i];
                                if (
                                    (bindAttribute.BoundType != null &&
                                     bindAttribute.BoundType.IsInstanceOfType(m_Owner)) ||
                                    (m_Owner == null && bindAttribute.BoundType == null))
                                {
                                    allowedToBind = true;
                                    break;
                                }
                            }

                            if (!allowedToBind)
                            {
                                continue;
                            }

                            implementationTypes.Add(type);
                        }
                    }
                }

                implementationTypes.Sort((a, b) => String.CompareOrdinal(a.FullName, b.FullName));
                foreach(var type in implementationTypes)
                {
                    RegisterMessageType(type);
                }
            }
            catch(Exception e)
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            foreach (var queue in m_SendQueues)
            {
                queue.Value.Value.Dispose();
            }
            m_SendQueues.Dispose();
            m_IncomingMessageQueue.Dispose();
            m_Disposed = true;
        }

        ~MessagingSystem()
        {
            Dispose();
        }

        public void SetLocalClientId(ulong localClientId)
        {
            m_LocalClientId = localClientId;
        }

        public void Hook(INetworkHooks hooks)
        {
            m_Hooks.Add(hooks);
        }
        
        private void RegisterMessageType(Type messageType)
        {
            if (!typeof(INetworkMessage).IsAssignableFrom(messageType))
            {
                throw new ArgumentException("RegisterMessageType types must be INetworkMessage types.");
            }
            
            var method = messageType.GetMethod("Receive");
            if (method == null)
            {
                throw new InvalidMessageStructureException(
                    "All INetworkMessage types must implement public static void Receive(ref FastBufferReader reader, NetworkContext context)");
            }

            var asDelegate = Delegate.CreateDelegate(typeof(MessageHandler), method, false);
            if (asDelegate == null)
            {
                throw new InvalidMessageStructureException(
                    "All INetworkMessage types must implement public static void Receive(ref FastBufferReader reader, NetworkContext context)");
            }

            m_MessageHandlers[m_HighMessageType] = (MessageHandler) asDelegate;
            m_ReverseTypeMap[m_HighMessageType] = messageType;
            m_MessageTypes[messageType] = m_HighMessageType++;
        }
        
        internal void HandleIncomingData(ulong clientId, ArraySegment<byte> data, float receiveTime)
        {
            BatchHeader header;
            unsafe
            {
                fixed (byte* nativeData = data.Array)
                {
                    FastBufferReader batchReader =
                        new FastBufferReader(nativeData, Allocator.None, data.Count, data.Offset);
                    if (!batchReader.TryBeginRead(sizeof(BatchHeader)))
                    {
                        NetworkLog.LogWarning("Received a packet too small to contain a BatchHeader. Ignoring it.");
                        return;
                    }
                    batchReader.ReadValue(out header);

                    for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                    {
                        m_Hooks[hookIdx].OnReceiveBatch(clientId, header.BatchSize, batchReader.Length);
                    }

                    for (var messageIdx = 0; messageIdx < header.BatchSize; ++messageIdx)
                    {
                        if (!batchReader.TryBeginRead(sizeof(MessageHeader)))
                        {
                            NetworkLog.LogWarning("Received a batch that didn't have enough data for all of its batches, ending early!");
                            return;
                        }
                        batchReader.ReadValue(out MessageHeader messageHeader);
                        if (!batchReader.TryBeginRead(messageHeader.MessageSize))
                        {
                            NetworkLog.LogWarning("Received a message that claimed a size larger than the packet, ending early!");
                            return;
                        }
                        m_IncomingMessageQueue.Add(new ReceiveQueueItem
                        {
                            Header = messageHeader,
                            SenderId = clientId,
                            Timestamp = receiveTime,
                            // Copy the data for this message into a new FastBufferReader that owns that memory.
                            // We can't guarantee the memory in the ArraySegment stays valid because we don't own it,
                            // so we must move it to memory we do own.
                            Reader = new FastBufferReader(batchReader.GetUnsafePtrAtCurrentPosition(), Allocator.TempJob, messageHeader.MessageSize)
                        });
                        batchReader.Seek(batchReader.Position + messageHeader.MessageSize);
                    }
                }
            }
        }

        public void HandleMessage(in MessageHeader header, ref FastBufferReader reader, ulong senderId, float timestamp)
        {
            var context = new NetworkContext
            {
                SystemOwner = m_Owner,
                SenderId = senderId,
                ReceivingChannel = header.NetworkChannel,
                Timestamp = timestamp,
                Header = header
            };
            for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
            {
                m_Hooks[hookIdx].OnReceiveMessage(senderId, m_ReverseTypeMap[header.MessageType], header.NetworkChannel);
            }
            var handler = m_MessageHandlers[header.MessageType];
            using (reader)
            {
                handler.Invoke(ref reader, context);
            }
        }

        internal void ProcessIncomingMessageQueue()
        {
            for (var i = 0; i < m_IncomingMessageQueue.Count; ++i)
            {
                // Avoid copies...
                ref var item = ref m_IncomingMessageQueue.GetValueRef(i);
                HandleMessage(item.Header, ref item.Reader, item.SenderId, item.Timestamp);
            }

            m_IncomingMessageQueue.Clear();
        }

        internal void ClientConnected(ulong clientId)
        {
            m_SendQueues[clientId] = DynamicUnmanagedArray<SendQueueItem>.CreateRef();
        }

        internal void ClientDisconnected(ulong clientId)
        {
            var queue = m_SendQueues[clientId];
            for (var i = 0; i < queue.Value.Count; ++i)
            {
                queue.Value.GetValueRef(i).Writer.Dispose();
            }
            queue.Value.Dispose();

            m_SendQueues.Remove(clientId);
            DynamicUnmanagedArray<SendQueueItem>.ReleaseRef(queue);
        }

        internal unsafe void SendMessage<T, U>(in T message, NetworkChannel channel, NetworkDelivery delivery, in U clients) 
            where T: INetworkMessage
            where U: IReadOnlyList<ulong>
        {
            var maxSize = delivery == NetworkDelivery.ReliableFragmentedSequenced ? 64000 : 1300;
            var tmpSerializer = new FastBufferWriter(1300, Allocator.Temp, maxSize);
            using (tmpSerializer)
            {
                message.Serialize(ref tmpSerializer);

                for (var i = 0; i < clients.Count; ++i)
                {
                    var clientId = clients[i];
                    
                    for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                    {
                        m_Hooks[hookIdx].OnSendMessage(clientId, typeof(T), channel, delivery);
                    }
                    
                    ref var sendQueueItem = ref m_SendQueues[clientId].Value;
                    if (sendQueueItem.Count == 0)
                    {
                        sendQueueItem.Add(new SendQueueItem(delivery, 1300, Allocator.TempJob,
                            maxSize));
                        sendQueueItem.GetValueRef(0).Writer.Seek(sizeof(BatchHeader));
                    }
                    else
                    {
                        ref var lastQueueItem = ref sendQueueItem.GetValueRef(sendQueueItem.Count - 1);
                        if (lastQueueItem.NetworkDelivery != delivery ||
                            lastQueueItem.Writer.MaxCapacity - lastQueueItem.Writer.Position < tmpSerializer.Length)
                        {
                            sendQueueItem.Add(new SendQueueItem(delivery, 1300, Allocator.TempJob,
                                maxSize));
                            sendQueueItem.GetValueRef(0).Writer.Seek(sizeof(BatchHeader));
                        }
                    }

                    ref var writeQueueItem = ref sendQueueItem.GetValueRef(sendQueueItem.Count - 1);
                    writeQueueItem.Writer.TryBeginWrite(sizeof(MessageHeader) + tmpSerializer.Length);
                    MessageHeader header = new MessageHeader
                    {
                        MessageSize = (short) tmpSerializer.Length,
                        MessageType = m_MessageTypes[typeof(T)],
                        NetworkChannel = channel
                    };


                    if (clientId == m_LocalClientId)
                    {
                        m_IncomingMessageQueue.Add(new ReceiveQueueItem
                        {
                            Header = header,
                            Reader = new FastBufferReader(ref tmpSerializer, Allocator.TempJob),
                            SenderId = clientId,
                            Timestamp = Time.realtimeSinceStartup
                        });
                        continue;
                    }

                    writeQueueItem.Writer.WriteValue(header);
                    writeQueueItem.Writer.WriteBytes(tmpSerializer.GetUnsafePtr(), tmpSerializer.Length);
                    writeQueueItem.BatchHeader.BatchSize++;
                }
            }
        }
        
        private struct PointerListWrapper<T> : IReadOnlyList<T>
            where T : unmanaged
        {
            private unsafe T* m_Value;
            private int m_Length;

            internal unsafe PointerListWrapper(T* ptr, int length)
            {
                m_Value = ptr;
                m_Length = length;
            }

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Length;
            }

            public unsafe T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Value[index];
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal unsafe void SendMessage<T>(in T message, NetworkChannel channel, NetworkDelivery delivery,
            ulong* clientIds, int numClientIds)
            where T: INetworkMessage
        {
            SendMessage(message, channel, delivery, new PointerListWrapper<ulong>(clientIds, numClientIds));
        }

        internal void ProcessSendQueues()
        {
            foreach (var kvp in m_SendQueues)
            {
                var clientId = kvp.Key;
                ref var sendQueueItem = ref kvp.Value.Value;
                for (var i = 0; i < sendQueueItem.Count; ++i)
                {
                    ref var queueItem = ref sendQueueItem.GetValueRef(i);
                    if (queueItem.BatchHeader.BatchSize == 0)
                    {
                        queueItem.Writer.Dispose();
                        continue;
                    }
                    
                    for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                    {
                        m_Hooks[hookIdx].OnSendBatch(clientId, queueItem.BatchHeader.BatchSize, queueItem.Writer.Length, queueItem.NetworkDelivery);
                    }
                    
                    queueItem.Writer.Seek(0);
                    // Skipping the Verify and sneaking the write mark in because we know it's fine.
                    queueItem.Writer.AllowedWriteMark = 2;
                    queueItem.Writer.WriteValue(queueItem.BatchHeader);
                    
                    m_MessageSender.Send(clientId, queueItem.NetworkDelivery, ref queueItem.Writer);
                    queueItem.Writer.Dispose();
                }
                sendQueueItem.Clear();
            }
        }
    }
}
