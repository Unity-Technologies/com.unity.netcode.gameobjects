using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    internal class HandlerNotRegisteredException : SystemException
    {
        public HandlerNotRegisteredException() { }
        public HandlerNotRegisteredException(string issue) : base(issue) { }
    }

    internal class InvalidMessageStructureException : SystemException
    {
        public InvalidMessageStructureException() { }
        public InvalidMessageStructureException(string issue) : base(issue) { }
    }

    internal class MessagingSystem : IDisposable
    {
        private struct ReceiveQueueItem
        {
            public FastBufferReader Reader;
            public MessageHeader Header;
            public ulong SenderId;
            public float Timestamp;
            public int MessageHeaderSerializedSize;
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

        internal delegate void MessageHandler(FastBufferReader reader, ref NetworkContext context, MessagingSystem system);

        private NativeList<ReceiveQueueItem> m_IncomingMessageQueue = new NativeList<ReceiveQueueItem>(16, Allocator.Persistent);

        // These array will grow as we need more message handlers. 4 is just a starting size.
        private MessageHandler[] m_MessageHandlers = new MessageHandler[4];
        private Type[] m_ReverseTypeMap = new Type[4];

        private Dictionary<Type, uint> m_MessageTypes = new Dictionary<Type, uint>();
        private Dictionary<ulong, NativeList<SendQueueItem>> m_SendQueues = new Dictionary<ulong, NativeList<SendQueueItem>>();

        private List<INetworkHooks> m_Hooks = new List<INetworkHooks>();

        private uint m_HighMessageType;
        private object m_Owner;
        private IMessageSender m_MessageSender;
        private bool m_Disposed;

        internal Type[] MessageTypes => m_ReverseTypeMap;
        internal MessageHandler[] MessageHandlers => m_MessageHandlers;

        internal uint MessageHandlerCount => m_HighMessageType;

        internal uint GetMessageType(Type t)
        {
            return m_MessageTypes[t];
        }

        public const int NON_FRAGMENTED_MESSAGE_MAX_SIZE = 1300;
        public const int FRAGMENTED_MESSAGE_MAX_SIZE = BytePacker.BitPackedIntMax;

        internal struct MessageWithHandler
        {
            public Type MessageType;
            public MessageHandler Handler;
        }

        internal List<MessageWithHandler> PrioritizeMessageOrder(List<MessageWithHandler> allowedTypes)
        {
            var prioritizedTypes = new List<MessageWithHandler>();

            // first pass puts the priority message in the first indices
            // Those are the messages that must be delivered in order to allow re-ordering the others later
            foreach (var t in allowedTypes)
            {
                if (t.MessageType.FullName == "Unity.Netcode.ConnectionRequestMessage" ||
                    t.MessageType.FullName == "Unity.Netcode.ConnectionApprovedMessage" ||
                    t.MessageType.FullName == "Unity.Netcode.OrderingMessage")
                {
                    prioritizedTypes.Add(t);
                }
            }

            foreach (var t in allowedTypes)
            {
                if (t.MessageType.FullName != "Unity.Netcode.ConnectionRequestMessage" &&
                    t.MessageType.FullName != "Unity.Netcode.ConnectionApprovedMessage" &&
                    t.MessageType.FullName != "Unity.Netcode.OrderingMessage")
                {
                    prioritizedTypes.Add(t);
                }
            }

            return prioritizedTypes;
        }

        public MessagingSystem(IMessageSender messageSender, object owner, IMessageProvider provider = null)
        {
            try
            {
                m_MessageSender = messageSender;
                m_Owner = owner;

                if (provider == null)
                {
                    provider = new ILPPMessageProvider();
                }
                var allowedTypes = provider.GetMessages();

                allowedTypes.Sort((a, b) => string.CompareOrdinal(a.MessageType.FullName, b.MessageType.FullName));
                allowedTypes = PrioritizeMessageOrder(allowedTypes);
                foreach (var type in allowedTypes)
                {
                    RegisterMessageType(type);
                }
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        public unsafe void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            // Can't just iterate SendQueues or SendQueues.Keys because ClientDisconnected removes
            // from the queue.
            foreach (var kvp in m_SendQueues)
            {
                CleanupDisconnectedClient(kvp.Key);
            }

            for (var queueIndex = 0; queueIndex < m_IncomingMessageQueue.Length; ++queueIndex)
            {
                // Avoid copies...
                ref var item = ref m_IncomingMessageQueue.ElementAt(queueIndex);
                item.Reader.Dispose();
            }

            m_IncomingMessageQueue.Dispose();
            m_Disposed = true;
        }

        ~MessagingSystem()
        {
            Dispose();
        }

        public void Hook(INetworkHooks hooks)
        {
            m_Hooks.Add(hooks);
        }

        public void Unhook(INetworkHooks hooks)
        {
            m_Hooks.Remove(hooks);
        }

        private void RegisterMessageType(MessageWithHandler messageWithHandler)
        {
            // if we are out of space, perform amortized linear growth
            if (m_HighMessageType == m_MessageHandlers.Length)
            {
                Array.Resize(ref m_MessageHandlers, 2 * m_MessageHandlers.Length);
                Array.Resize(ref m_ReverseTypeMap, 2 * m_ReverseTypeMap.Length);
            }

            m_MessageHandlers[m_HighMessageType] = messageWithHandler.Handler;
            m_ReverseTypeMap[m_HighMessageType] = messageWithHandler.MessageType;
            m_MessageTypes[messageWithHandler.MessageType] = m_HighMessageType++;
        }

        internal void HandleIncomingData(ulong clientId, ArraySegment<byte> data, float receiveTime)
        {
            unsafe
            {
                fixed (byte* nativeData = data.Array)
                {
                    var batchReader =
                        new FastBufferReader(nativeData + data.Offset, Allocator.None, data.Count);
                    if (!batchReader.TryBeginRead(sizeof(BatchHeader)))
                    {
                        NetworkLog.LogWarning("Received a packet too small to contain a BatchHeader. Ignoring it.");
                        return;
                    }

                    batchReader.ReadValue(out BatchHeader batchHeader);

                    for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                    {
                        m_Hooks[hookIdx].OnBeforeReceiveBatch(clientId, batchHeader.BatchSize, batchReader.Length);
                    }

                    for (var messageIdx = 0; messageIdx < batchHeader.BatchSize; ++messageIdx)
                    {

                        var messageHeader = new MessageHeader();
                        var position = batchReader.Position;
                        try
                        {
                            ByteUnpacker.ReadValueBitPacked(batchReader, out messageHeader.MessageType);
                            ByteUnpacker.ReadValueBitPacked(batchReader, out messageHeader.MessageSize);
                        }
                        catch (OverflowException)
                        {
                            NetworkLog.LogWarning("Received a batch that didn't have enough data for all of its batches, ending early!");
                            throw;
                        }

                        var receivedHeaderSize = batchReader.Position - position;

                        if (!batchReader.TryBeginRead((int)messageHeader.MessageSize))
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
                            Reader = new FastBufferReader(batchReader.GetUnsafePtrAtCurrentPosition(), Allocator.TempJob, (int)messageHeader.MessageSize),
                            MessageHeaderSerializedSize = receivedHeaderSize,
                        });
                        batchReader.Seek(batchReader.Position + (int)messageHeader.MessageSize);
                    }
                    for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                    {
                        m_Hooks[hookIdx].OnAfterReceiveBatch(clientId, batchHeader.BatchSize, batchReader.Length);
                    }
                }
            }
        }

        private bool CanReceive(ulong clientId, Type messageType, FastBufferReader messageContent, ref NetworkContext context)
        {
            for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
            {
                if (!m_Hooks[hookIdx].OnVerifyCanReceive(clientId, messageType, messageContent, ref context))
                {
                    return false;
                }
            }

            return true;
        }

        // Moves the handler for the type having hash `targetHash` to the `desiredOrder` position, in the handler list
        // This allows the server to tell the client which id it is using for which message and make sure the right
        // message is used when deserializing.
        internal void ReorderMessage(int desiredOrder, uint targetHash)
        {
            if (desiredOrder < 0)
            {
                throw new ArgumentException("ReorderMessage desiredOrder must be positive");
            }

            if (desiredOrder < m_ReverseTypeMap.Length &&
                XXHash.Hash32(m_ReverseTypeMap[desiredOrder].FullName) == targetHash)
            {
                // matching positions and hashes. All good.
                return;
            }

            Debug.Log($"Unexpected hash for {desiredOrder}");

            // Since the message at `desiredOrder` is not the expected one,
            // insert an empty placeholder and move the messages down
            var typesAsList = new List<Type>(m_ReverseTypeMap);

            typesAsList.Insert(desiredOrder, null);
            var handlersAsList = new List<MessageHandler>(m_MessageHandlers);
            handlersAsList.Insert(desiredOrder, null);

            // we added a dummy message, bump the end up
            m_HighMessageType++;

            // Here, we rely on the server telling us about all messages, in order.
            // So, we know the handlers before desiredOrder are correct.
            // We start at desiredOrder to not shift them when we insert.
            int position = desiredOrder;
            bool found = false;
            while (position < typesAsList.Count)
            {
                if (typesAsList[position] != null &&
                    XXHash.Hash32(typesAsList[position].FullName) == targetHash)
                {
                    found = true;
                    break;
                }

                position++;
            }

            if (found)
            {
                // Copy the handler and type to the right index

                typesAsList[desiredOrder] = typesAsList[position];
                handlersAsList[desiredOrder] = handlersAsList[position];
                typesAsList.RemoveAt(position);
                handlersAsList.RemoveAt(position);

                // we removed a copy after moving a message, reduce the high message index
                m_HighMessageType--;
            }

            m_ReverseTypeMap = typesAsList.ToArray();
            m_MessageHandlers = handlersAsList.ToArray();
        }

        public void HandleMessage(in MessageHeader header, FastBufferReader reader, ulong senderId, float timestamp, int serializedHeaderSize)
        {
            if (header.MessageType >= m_HighMessageType)
            {
                Debug.LogWarning($"Received a message with invalid message type value {header.MessageType}");
                reader.Dispose();
                return;
            }
            var context = new NetworkContext
            {
                SystemOwner = m_Owner,
                SenderId = senderId,
                Timestamp = timestamp,
                Header = header,
                SerializedHeaderSize = serializedHeaderSize,
                MessageSize = header.MessageSize,
            };

            var type = m_ReverseTypeMap[header.MessageType];
            if (!CanReceive(senderId, type, reader, ref context))
            {
                reader.Dispose();
                return;
            }

            for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
            {
                m_Hooks[hookIdx].OnBeforeReceiveMessage(senderId, type, reader.Length + FastBufferWriter.GetWriteSize<MessageHeader>());
            }

            var handler = m_MessageHandlers[header.MessageType];
            using (reader)
            {
                // This will also log an exception is if the server knows about a message type the client doesn't know
                // about. In this case the handler will be null. It is still an issue the user must deal with: If the
                // two connecting builds know about different messages, the server should not send a message to a client
                // that doesn't know about it
                if (handler == null)
                {
                    Debug.LogException(new HandlerNotRegisteredException(header.MessageType.ToString()));
                }
                else
                {
                    // No user-land message handler exceptions should escape the receive loop.
                    // If an exception is throw, the message is ignored.
                    // Example use case: A bad message is received that can't be deserialized and throws
                    // an OverflowException because it specifies a length greater than the number of bytes in it
                    // for some dynamic-length value.
                    try
                    {
                        handler.Invoke(reader, ref context, this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
            {
                m_Hooks[hookIdx].OnAfterReceiveMessage(senderId, type, reader.Length + FastBufferWriter.GetWriteSize<MessageHeader>());
            }
        }

        internal unsafe void ProcessIncomingMessageQueue()
        {
            for (var index = 0; index < m_IncomingMessageQueue.Length; ++index)
            {
                // Avoid copies...
                ref var item = ref m_IncomingMessageQueue.ElementAt(index);
                HandleMessage(item.Header, item.Reader, item.SenderId, item.Timestamp, item.MessageHeaderSerializedSize);
                if (m_Disposed)
                {
                    return;
                }
            }

            m_IncomingMessageQueue.Clear();
        }

        internal void ClientConnected(ulong clientId)
        {
            if (m_SendQueues.ContainsKey(clientId))
            {
                return;
            }
            m_SendQueues[clientId] = new NativeList<SendQueueItem>(16, Allocator.Persistent);
        }

        internal void ClientDisconnected(ulong clientId)
        {
            if (!m_SendQueues.ContainsKey(clientId))
            {
                return;
            }
            CleanupDisconnectedClient(clientId);
            m_SendQueues.Remove(clientId);
        }

        private unsafe void CleanupDisconnectedClient(ulong clientId)
        {
            var queue = m_SendQueues[clientId];
            for (var i = 0; i < queue.Length; ++i)
            {
                queue.ElementAt(i).Writer.Dispose();
            }

            queue.Dispose();
        }

        public static void ReceiveMessage<T>(FastBufferReader reader, ref NetworkContext context, MessagingSystem system) where T : INetworkMessage, new()
        {
            var message = new T();
            if (message.Deserialize(reader, ref context))
            {
                for (var hookIdx = 0; hookIdx < system.m_Hooks.Count; ++hookIdx)
                {
                    system.m_Hooks[hookIdx].OnBeforeHandleMessage(ref message, ref context);
                }

                message.Handle(ref context);

                for (var hookIdx = 0; hookIdx < system.m_Hooks.Count; ++hookIdx)
                {
                    system.m_Hooks[hookIdx].OnAfterHandleMessage(ref message, ref context);
                }
            }
        }

        private bool CanSend(ulong clientId, Type messageType, NetworkDelivery delivery)
        {
            for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
            {
                if (!m_Hooks[hookIdx].OnVerifyCanSend(clientId, messageType, delivery))
                {
                    return false;
                }
            }

            return true;
        }

        internal int SendMessage<TMessageType, TClientIdListType>(ref TMessageType message, NetworkDelivery delivery, in TClientIdListType clientIds)
            where TMessageType : INetworkMessage
            where TClientIdListType : IReadOnlyList<ulong>
        {
            if (clientIds.Count == 0)
            {
                return 0;
            }

            var maxSize = delivery == NetworkDelivery.ReliableFragmentedSequenced ? FRAGMENTED_MESSAGE_MAX_SIZE : NON_FRAGMENTED_MESSAGE_MAX_SIZE;

            using var tmpSerializer = new FastBufferWriter(NON_FRAGMENTED_MESSAGE_MAX_SIZE - FastBufferWriter.GetWriteSize<MessageHeader>(), Allocator.Temp, maxSize - FastBufferWriter.GetWriteSize<MessageHeader>());

            message.Serialize(tmpSerializer);

            return SendPreSerializedMessage(tmpSerializer, maxSize, ref message, delivery, clientIds);
        }

        internal unsafe int SendPreSerializedMessage<TMessageType>(in FastBufferWriter tmpSerializer, int maxSize, ref TMessageType message, NetworkDelivery delivery, in IReadOnlyList<ulong> clientIds)
            where TMessageType : INetworkMessage
        {
            using var headerSerializer = new FastBufferWriter(FastBufferWriter.GetWriteSize<MessageHeader>(), Allocator.Temp);

            var header = new MessageHeader
            {
                MessageSize = (uint)tmpSerializer.Length,
                MessageType = m_MessageTypes[typeof(TMessageType)],
            };
            BytePacker.WriteValueBitPacked(headerSerializer, header.MessageType);
            BytePacker.WriteValueBitPacked(headerSerializer, header.MessageSize);

            for (var i = 0; i < clientIds.Count; ++i)
            {
                var clientId = clientIds[i];

                if (!CanSend(clientId, typeof(TMessageType), delivery))
                {
                    continue;
                }

                for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                {
                    m_Hooks[hookIdx].OnBeforeSendMessage(clientId, ref message, delivery);
                }

                var sendQueueItem = m_SendQueues[clientId];
                if (sendQueueItem.Length == 0)
                {
                    sendQueueItem.Add(new SendQueueItem(delivery, NON_FRAGMENTED_MESSAGE_MAX_SIZE, Allocator.TempJob,
                        maxSize));
                    sendQueueItem.ElementAt(0).Writer.Seek(sizeof(BatchHeader));
                }
                else
                {
                    ref var lastQueueItem = ref sendQueueItem.ElementAt(sendQueueItem.Length - 1);
                    if (lastQueueItem.NetworkDelivery != delivery ||
                        lastQueueItem.Writer.MaxCapacity - lastQueueItem.Writer.Position
                        < tmpSerializer.Length + headerSerializer.Length)
                    {
                        sendQueueItem.Add(new SendQueueItem(delivery, NON_FRAGMENTED_MESSAGE_MAX_SIZE, Allocator.TempJob,
                            maxSize));
                        sendQueueItem.ElementAt(sendQueueItem.Length - 1).Writer.Seek(sizeof(BatchHeader));
                    }
                }

                ref var writeQueueItem = ref sendQueueItem.ElementAt(sendQueueItem.Length - 1);
                writeQueueItem.Writer.TryBeginWrite(tmpSerializer.Length + headerSerializer.Length);

                writeQueueItem.Writer.WriteBytes(headerSerializer.GetUnsafePtr(), headerSerializer.Length);
                writeQueueItem.Writer.WriteBytes(tmpSerializer.GetUnsafePtr(), tmpSerializer.Length);
                writeQueueItem.BatchHeader.BatchSize++;
                for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                {
                    m_Hooks[hookIdx].OnAfterSendMessage(clientId, ref message, delivery, tmpSerializer.Length + headerSerializer.Length);
                }
            }

            return tmpSerializer.Length + headerSerializer.Length;
        }

        internal unsafe int SendPreSerializedMessage<TMessageType>(in FastBufferWriter tmpSerializer, int maxSize, ref TMessageType message, NetworkDelivery delivery, ulong clientId)
            where TMessageType : INetworkMessage
        {
            ulong* clientIds = stackalloc ulong[] { clientId };
            return SendPreSerializedMessage(tmpSerializer, maxSize, ref message, delivery, new PointerListWrapper<ulong>(clientIds, 1));
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

        internal unsafe int SendMessage<T>(ref T message, NetworkDelivery delivery,
            ulong* clientIds, int numClientIds)
            where T : INetworkMessage
        {
            return SendMessage(ref message, delivery, new PointerListWrapper<ulong>(clientIds, numClientIds));
        }

        internal unsafe int SendMessage<T>(ref T message, NetworkDelivery delivery, ulong clientId)
            where T : INetworkMessage
        {
            ulong* clientIds = stackalloc ulong[] { clientId };
            return SendMessage(ref message, delivery, new PointerListWrapper<ulong>(clientIds, 1));
        }

        internal unsafe int SendMessage<T>(ref T message, NetworkDelivery delivery, in NativeArray<ulong> clientIds)
            where T : INetworkMessage
        {
            return SendMessage(ref message, delivery, new PointerListWrapper<ulong>((ulong*)clientIds.GetUnsafePtr(), clientIds.Length));
        }

        internal unsafe void ProcessSendQueues()
        {
            foreach (var kvp in m_SendQueues)
            {
                var clientId = kvp.Key;
                var sendQueueItem = kvp.Value;
                for (var i = 0; i < sendQueueItem.Length; ++i)
                {
                    ref var queueItem = ref sendQueueItem.ElementAt(i);
                    if (queueItem.BatchHeader.BatchSize == 0)
                    {
                        queueItem.Writer.Dispose();
                        continue;
                    }

                    for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                    {
                        m_Hooks[hookIdx].OnBeforeSendBatch(clientId, queueItem.BatchHeader.BatchSize, queueItem.Writer.Length, queueItem.NetworkDelivery);
                    }

                    queueItem.Writer.Seek(0);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    // Skipping the Verify and sneaking the write mark in because we know it's fine.
                    queueItem.Writer.Handle->AllowedWriteMark = 2;
#endif
                    queueItem.Writer.WriteValue(queueItem.BatchHeader);

                    try
                    {
                        m_MessageSender.Send(clientId, queueItem.NetworkDelivery, queueItem.Writer);

                        for (var hookIdx = 0; hookIdx < m_Hooks.Count; ++hookIdx)
                        {
                            m_Hooks[hookIdx].OnAfterSendBatch(clientId, queueItem.BatchHeader.BatchSize, queueItem.Writer.Length, queueItem.NetworkDelivery);
                        }
                    }
                    finally
                    {
                        queueItem.Writer.Dispose();
                    }
                }
                sendQueueItem.Clear();
            }
        }
    }
}
