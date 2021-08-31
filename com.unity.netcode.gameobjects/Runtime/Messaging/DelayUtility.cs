using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode
{
    public class DelayUtility: IDisposable
    {
        public enum DelayStage: byte
        {
            PreFixedUpdate = NetworkUpdateStage.FixedUpdate,
            PreUpdate = NetworkUpdateStage.Update,
            PreLateUpdate = NetworkUpdateStage.PreLateUpdate,
            PostLateUpdate = NetworkUpdateStage.PostLateUpdate
        }
        
        public delegate void DelayedNetworkAction();

        private Dictionary<DelayStage, List<DelayedNetworkAction>> m_DelayedCallbacks = new Dictionary<DelayStage, List<DelayedNetworkAction>>
        {
            [DelayStage.PreFixedUpdate] = new List<DelayedNetworkAction>(),
            [DelayStage.PreUpdate] = new List<DelayedNetworkAction>(),
            [DelayStage.PreLateUpdate] = new List<DelayedNetworkAction>(),
            [DelayStage.PostLateUpdate] = new List<DelayedNetworkAction>(),
        };
        
        private struct DelayedMessageHandlerData
        {
            public FastBufferReader Reader;
            public MessageHeader Header;
            public ulong SenderId;
            public float Timestamp;
        }

        private NativeHashMap<byte, Ref<DynamicUnmanagedArray<DelayedMessageHandlerData>>> m_DelayedMessageHandlers =
            new NativeHashMap<byte, Ref<DynamicUnmanagedArray<DelayedMessageHandlerData>>>((int)DelayStage.PostLateUpdate, Allocator.Persistent);

        private IMessageHandler m_MessageHandler;
        private bool m_disposed;
        
        public DelayUtility(IMessageHandler messageHandler)
        {
            m_MessageHandler = messageHandler;
            m_DelayedMessageHandlers[(byte) DelayStage.PreFixedUpdate] =
                DynamicUnmanagedArray<DelayedMessageHandlerData>.CreateRef();
            m_DelayedMessageHandlers[(byte) DelayStage.PreUpdate] =
                DynamicUnmanagedArray<DelayedMessageHandlerData>.CreateRef();
            m_DelayedMessageHandlers[(byte) DelayStage.PreLateUpdate] =
                DynamicUnmanagedArray<DelayedMessageHandlerData>.CreateRef();
            m_DelayedMessageHandlers[(byte) DelayStage.PostLateUpdate] =
                DynamicUnmanagedArray<DelayedMessageHandlerData>.CreateRef();
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                foreach (var handler in m_DelayedMessageHandlers)
                {
                    handler.Value.Value.Dispose();
                }
                m_DelayedMessageHandlers.Dispose();
                m_disposed = true;
            }
        }

        ~DelayUtility()
        {
            Dispose();
        }

        public void DelayUntil(DelayStage stage, DelayedNetworkAction callback)
        {
            m_DelayedCallbacks[stage].Add(callback);
        }

        public enum DelayResult
        {
            Delay,
            Continue
        }
        
        
        public unsafe DelayResult DelayUntil(DelayStage stage, ref FastBufferReader reader, NetworkContext context)
        {
            if ((NetworkUpdateStage)stage == NetworkUpdateLoop.UpdateStage)
            {
                return DelayResult.Continue;
            }
            m_DelayedMessageHandlers[(byte)stage].Value.Add(new DelayedMessageHandlerData
            {
                // Have to make a copy so we can manage the lifetime.
                Reader = new FastBufferReader(reader.GetUnsafePtr(), Allocator.TempJob, reader.Length),
                Header = context.Header,
                SenderId = context.SenderId,
                Timestamp = context.Timestamp
            });
            return DelayResult.Delay;
        }

        public void NetworkUpdate(NetworkUpdateStage stage)
        {
            ref var delayedMessageHandlers = ref m_DelayedMessageHandlers[(byte) stage].Value;
            for (var i = 0; i < delayedMessageHandlers.Count; ++i)
            {
                ref var data = ref delayedMessageHandlers.GetValueRef(i);
                m_MessageHandler.HandleMessage(data.Header, ref data.Reader, data.SenderId, data.Timestamp);
            }
            delayedMessageHandlers.Clear();

            var delayedCallbacks = m_DelayedCallbacks[(DelayStage) stage];
            for (var i = 0; i < delayedCallbacks.Count; ++i)
            {
                delayedCallbacks[i].Invoke();
            }
            delayedCallbacks.Clear();
        }

    }
}
