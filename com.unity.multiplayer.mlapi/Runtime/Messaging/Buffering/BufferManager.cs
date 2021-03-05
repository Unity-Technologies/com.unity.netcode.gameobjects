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
        private static ProfilerMarker s_CleanBuffer = new ProfilerMarker($"{nameof(BufferManager)}.{nameof(CleanBuffer)}");
#endif

        private static Dictionary<ulong, Queue<BufferedMessage>> s_BufferQueues = new Dictionary<ulong, Queue<BufferedMessage>>();

        internal struct BufferedMessage
        {
            internal ulong SenderClientId;
            internal NetworkChannel NetworkChannel;
            internal PooledNetworkBuffer NetworkBuffer;
            internal float ReceiveTime;
            internal float BufferTime;
        }

        internal static Queue<BufferedMessage> ConsumeBuffersForNetworkId(ulong networkId)
        {
            if (s_BufferQueues.ContainsKey(networkId))
            {
                Queue<BufferedMessage> message = s_BufferQueues[networkId];

                s_BufferQueues.Remove(networkId);

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

        internal static void BufferMessageForNetworkId(ulong networkId, ulong senderClientId, NetworkChannel networkChannel, float receiveTime, ArraySegment<byte> payload)
        {
            if (!s_BufferQueues.ContainsKey(networkId))
            {
                s_BufferQueues.Add(networkId, new Queue<BufferedMessage>());
            }

            Queue<BufferedMessage> queue = s_BufferQueues[networkId];

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

        private static List<ulong> s_KeysToDestroy = new List<ulong>();

        internal static void CleanBuffer()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_CleanBuffer.Begin();
#endif
            foreach (var pair in s_BufferQueues)
            {
                while (pair.Value.Count > 0 && Time.realtimeSinceStartup - pair.Value.Peek().BufferTime >= NetworkManager.Singleton.NetworkConfig.MessageBufferTimeout)
                {
                    BufferedMessage message = pair.Value.Dequeue();

                    RecycleConsumedBufferedMessage(message);
                }

                if (pair.Value.Count == 0)
                {
                    s_KeysToDestroy.Add(pair.Key);
                }
            }

            for (int i = 0; i < s_KeysToDestroy.Count; i++)
            {
                s_BufferQueues.Remove(s_KeysToDestroy[i]);
            }

            s_KeysToDestroy.Clear();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_CleanBuffer.End();
#endif
        }
    }
}