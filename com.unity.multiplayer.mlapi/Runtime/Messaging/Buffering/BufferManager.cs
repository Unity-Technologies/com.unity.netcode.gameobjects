using System;
using System.Collections.Generic;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using Unity.Profiling;
using UnityEngine;

namespace MLAPI.Messaging.Buffering
{
    internal static class BufferManager
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public static ProfilerMarker s_CleanBuffer = new ProfilerMarker("MLAPI.BufferManager.CleanBuffer");
#endif

        private static readonly Dictionary<ulong, Queue<BufferedMessage>> bufferQueues = new Dictionary<ulong, Queue<BufferedMessage>>();

        internal struct BufferedMessage
        {
            internal ulong sender;
            internal Channel channel;
            internal PooledBitStream payload;
            internal float receiveTime;
            internal float bufferTime;
        }

        internal static Queue<BufferedMessage> ConsumeBuffersForNetworkId(ulong networkId)
        {
            if (bufferQueues.ContainsKey(networkId))
            {
                Queue<BufferedMessage> message = bufferQueues[networkId];

                bufferQueues.Remove(networkId);

                return message;
            }
            else
            {
                return null;
            }
        }

        internal static void RecycleConsumedBufferedMessage(BufferedMessage message)
        {
            message.payload.Dispose();
        }

        internal static void BufferMessageForNetworkId(ulong networkId, ulong sender, Channel channel, float receiveTime, ArraySegment<byte> payload)
        {
            if (!bufferQueues.ContainsKey(networkId))
            {
                bufferQueues.Add(networkId, new Queue<BufferedMessage>());
            }

            Queue<BufferedMessage> queue = bufferQueues[networkId];

            PooledBitStream payloadStream = PooledBitStream.Get();

            payloadStream.Write(payload.Array, payload.Offset, payload.Count);
            payloadStream.Position = 0;

            queue.Enqueue(new BufferedMessage()
            {
                bufferTime = Time.realtimeSinceStartup,
                channel = channel,
                payload = payloadStream,
                receiveTime = receiveTime,
                sender = sender
            });
        }

        private static readonly List<ulong> _keysToDestroy = new List<ulong>();
        internal static void CleanBuffer()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_CleanBuffer.Begin();
#endif
            foreach (KeyValuePair<ulong, Queue<BufferedMessage>> pair in bufferQueues)
            {
                while (pair.Value.Count > 0 && Time.realtimeSinceStartup - pair.Value.Peek().bufferTime >= NetworkManager.Singleton.NetworkConfig.MessageBufferTimeout)
                {
                    BufferedMessage message = pair.Value.Dequeue();

                    RecycleConsumedBufferedMessage(message);
                }

                if (pair.Value.Count == 0)
                {
                    _keysToDestroy.Add(pair.Key);
                }
            }

            for (int i = 0; i < _keysToDestroy.Count; i++)
            {
                bufferQueues.Remove(_keysToDestroy[i]);
            }

            _keysToDestroy.Clear();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_CleanBuffer.End();
#endif
        }
    }
}
