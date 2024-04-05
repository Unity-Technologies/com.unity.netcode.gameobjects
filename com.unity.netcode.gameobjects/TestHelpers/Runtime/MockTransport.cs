using System;
using System.Collections.Generic;
using Unity.Collections;
using Random = UnityEngine.Random;

namespace Unity.Netcode.TestHelpers.Runtime
{
    internal class MockTransport : NetworkTransport
    {
        private struct MessageData
        {
            public ulong FromClientId;
            public FastBufferReader Payload;
            public NetworkEvent Event;
            public float AvailableTime;
            public int Sequence;
            public NetworkDelivery Delivery;
        }

        private static Dictionary<ulong, List<MessageData>> s_MessageQueue = new Dictionary<ulong, List<MessageData>>();

        public override ulong ServerClientId { get; } = 0;

        public static ulong HighTransportId = 0;
        public ulong TransportId = 0;

        public float SimulatedLatencySeconds;
        public float PacketDropRate;
        public float LatencyJitter;

        public Dictionary<ulong, int> LastSentSequence = new Dictionary<ulong, int>();
        public Dictionary<ulong, int> LastReceivedSequence = new Dictionary<ulong, int>();

        public NetworkManager NetworkManager;

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if ((networkDelivery == NetworkDelivery.Unreliable || networkDelivery == NetworkDelivery.UnreliableSequenced) && Random.Range(0, 1) < PacketDropRate)
            {
                return;
            }

            if (!LastSentSequence.ContainsKey(clientId))
            {
                LastSentSequence[clientId] = 1;
            }

            var reader = new FastBufferReader(payload, Allocator.TempJob);
            s_MessageQueue[clientId].Add(new MessageData
            {
                FromClientId = TransportId,
                Payload = reader,
                Event = NetworkEvent.Data,
                AvailableTime = NetworkManager.RealTimeProvider.UnscaledTime + SimulatedLatencySeconds + Random.Range(-LatencyJitter, LatencyJitter),
                Sequence = ++LastSentSequence[clientId],
                Delivery = networkDelivery
            });
            s_MessageQueue[clientId].Sort(((a, b) => a.AvailableTime.CompareTo(b.AvailableTime)));
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (s_MessageQueue[TransportId].Count > 0)
            {
                MessageData data;
                for (; ; )
                {
                    data = s_MessageQueue[TransportId][0];
                    if (data.AvailableTime > NetworkManager.RealTimeProvider.UnscaledTime)
                    {
                        clientId = 0;
                        payload = new ArraySegment<byte>();
                        receiveTime = 0;
                        return NetworkEvent.Nothing;
                    }

                    s_MessageQueue[TransportId].RemoveAt(0);
                    clientId = data.FromClientId;
                    if (data.Event == NetworkEvent.Data && data.Delivery == NetworkDelivery.UnreliableSequenced && LastReceivedSequence.ContainsKey(clientId) && data.Sequence <= LastReceivedSequence[clientId])
                    {
                        continue;
                    }

                    break;
                }

                if (data.Delivery == NetworkDelivery.UnreliableSequenced)
                {
                    LastReceivedSequence[clientId] = data.Sequence;
                }

                payload = new ArraySegment<byte>();
                if (data.Event == NetworkEvent.Data)
                {
                    payload = data.Payload.ToArray();
                    data.Payload.Dispose();
                }

                receiveTime = NetworkManager.RealTimeProvider.RealTimeSinceStartup;
                if (NetworkManager.IsServer && data.Event == NetworkEvent.Connect)
                {
                    if (!LastSentSequence.ContainsKey(data.FromClientId))
                    {
                        LastSentSequence[data.FromClientId] = 1;
                    }
                    s_MessageQueue[data.FromClientId].Add(
                        new MessageData
                        {
                            Event = NetworkEvent.Connect,
                            FromClientId = ServerClientId,
                            AvailableTime = NetworkManager.RealTimeProvider.UnscaledTime + SimulatedLatencySeconds + Random.Range(-LatencyJitter, LatencyJitter),
                            Sequence = ++LastSentSequence[data.FromClientId]
                        });
                }
                return data.Event;
            }
            clientId = 0;
            payload = new ArraySegment<byte>();
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }

        public override bool StartClient()
        {
            TransportId = ++HighTransportId;
            s_MessageQueue[TransportId] = new List<MessageData>();
            s_MessageQueue[ServerClientId].Add(
                new MessageData
                {
                    Event = NetworkEvent.Connect,
                    FromClientId = TransportId,
                });
            return true;
        }

        public override bool StartServer()
        {
            s_MessageQueue[ServerClientId] = new List<MessageData>();
            return true;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            s_MessageQueue[clientId].Add(
                new MessageData
                {
                    Event = NetworkEvent.Disconnect,
                    FromClientId = TransportId,
                });
        }

        public override void DisconnectLocalClient()
        {
            s_MessageQueue[ServerClientId].Add(
                new MessageData
                {
                    Event = NetworkEvent.Disconnect,
                    FromClientId = TransportId,
                });
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            return (ulong)(SimulatedLatencySeconds * 1000);
        }

        public override void Shutdown()
        {
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
            NetworkManager = networkManager;
        }

        protected static void DisposeQueueItems()
        {
            foreach (var kvp in s_MessageQueue)
            {
                foreach (var value in kvp.Value)
                {
                    if (value.Event == NetworkEvent.Data)
                    {
                        value.Payload.Dispose();
                    }
                }
            }
        }

        public static void Reset()
        {
            DisposeQueueItems();
            s_MessageQueue.Clear();
            HighTransportId = 0;
        }

        public static void ClearQueues()
        {
            DisposeQueueItems();
            foreach (var kvp in s_MessageQueue)
            {
                kvp.Value.Clear();
            }
        }
    }
}
