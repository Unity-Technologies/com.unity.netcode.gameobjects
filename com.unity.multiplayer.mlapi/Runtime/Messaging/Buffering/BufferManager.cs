using System;
using System.Collections.Generic;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using Unity.Profiling;
using UnityEngine;

namespace MLAPI.Messaging.Buffering
{
    internal class BufferManager
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static ProfilerMarker s_CleanBuffer = new ProfilerMarker($"{nameof(BufferManager)}.{nameof(CleanBuffer)}");
#endif

        private readonly Dictionary<ulong, Queue<BufferedMessage>> m_BufferQueues = new Dictionary<ulong, Queue<BufferedMessage>>();

        internal struct BufferedMessage
        {
            internal ulong SenderClientId;
            internal NetworkChannel NetworkChannel;
            internal PooledNetworkBuffer NetworkBuffer;
            internal float ReceiveTime;
            internal float BufferTime;
        }

        private NetworkManager m_NetworkManager { get; }

        internal BufferManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        internal Queue<BufferedMessage> ConsumeBuffersForNetworkId(ulong networkId)
        {
            if (m_BufferQueues.TryGetValue(networkId, out Queue<BufferedMessage> message))
            {
                m_BufferQueues.Remove(networkId);
                return message;
            }
            else
            {
                return null;
            }
        }

        internal static void RecycleConsumedBufferedMessage(BufferedMessage message)
        {
            message.NetworkBuffer.Dispose();
        }

        internal void BufferMessageForNetworkId(ulong networkId, ulong senderClientId, NetworkChannel networkChannel, float receiveTime, ArraySegment<byte> payload)
        {
            if (!m_BufferQueues.ContainsKey(networkId))
            {
                m_BufferQueues.Add(networkId, new Queue<BufferedMessage>());
            }

            Queue<BufferedMessage> queue = m_BufferQueues[networkId];

            var payloadBuffer = PooledNetworkBuffer.Get();
            payloadBuffer.Write(payload.Array, payload.Offset, payload.Count);
            payloadBuffer.Position = 0;

            queue.Enqueue(new BufferedMessage()
            {
                BufferTime = Time.realtimeSinceStartup,
                NetworkChannel = networkChannel,
                NetworkBuffer = payloadBuffer,
                ReceiveTime = receiveTime,
                SenderClientId = senderClientId
            });
        }

        private static readonly List<ulong> k_KeysToDestroy = new List<ulong>();

        internal void CleanBuffer()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_CleanBuffer.Begin();
#endif
            foreach (var pair in m_BufferQueues)
            {
                while (pair.Value.Count > 0 && Time.realtimeSinceStartup - pair.Value.Peek().BufferTime >= m_NetworkManager.NetworkConfig.MessageBufferTimeout)
                {
                    BufferedMessage message = pair.Value.Dequeue();

                    RecycleConsumedBufferedMessage(message);
                }

                if (pair.Value.Count == 0)
                {
                    k_KeysToDestroy.Add(pair.Key);
                }
            }

            for (int i = 0; i < k_KeysToDestroy.Count; i++)
            {
                m_BufferQueues.Remove(k_KeysToDestroy[i]);
            }

            k_KeysToDestroy.Clear();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_CleanBuffer.End();
#endif
        }
    }
}
