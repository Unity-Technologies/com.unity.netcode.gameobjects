using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode.TestHelpers.Runtime
{
    internal class MockTransport : NetworkTransport
    {
        private struct MessageData
        {
            public ulong FromClientId;
            public ArraySegment<byte> Payload;
            public NetworkEvent Event;
        }

        private static Dictionary<ulong, Queue<MessageData>> s_MessageQueue = new Dictionary<ulong, Queue<MessageData>>();

        public override ulong ServerClientId { get; } = 0;

        public static ulong HighTransportId = 0;
        public ulong TransportId = 0;

        public NetworkManager NetworkManager;

        public MockTransport()
        {
            Debug.Log("Mock transport created");
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            Debug.Log($"{TransportId}: Sending data to {clientId}");

            var copy = new byte[payload.Array.Length];
            Array.Copy(payload.Array, copy, payload.Array.Length);
            s_MessageQueue[clientId].Enqueue(new MessageData{FromClientId = TransportId, Payload = new ArraySegment<byte>(copy, payload.Offset, payload.Count), Event = NetworkEvent.Data});
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (s_MessageQueue[TransportId].Count > 0)
            {
                var data = s_MessageQueue[TransportId].Dequeue();
                Debug.Log($"{TransportId}: Got {data.Event} from {data.FromClientId}");
                clientId = data.FromClientId;
                payload = data.Payload;
                receiveTime = NetworkManager.RealTimeProvider.RealTimeSinceStartup;
                if (NetworkManager.IsServer && data.Event == NetworkEvent.Connect)
                {
                    s_MessageQueue[data.FromClientId].Enqueue(new MessageData{Event = NetworkEvent.Connect, FromClientId = ServerClientId, Payload = new ArraySegment<byte>()});
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
            Debug.Log($"Starting {TransportId}");
            s_MessageQueue[TransportId] = new Queue<MessageData>();
            s_MessageQueue[ServerClientId].Enqueue(new MessageData{Event = NetworkEvent.Connect, FromClientId = TransportId, Payload = new ArraySegment<byte>()});
            return true;
        }

        public override bool StartServer()
        {
            Debug.Log($"Starting {ServerClientId}");
            s_MessageQueue[ServerClientId] = new Queue<MessageData>();
            return true;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            s_MessageQueue[clientId].Enqueue(new MessageData{Event = NetworkEvent.Disconnect, FromClientId = TransportId, Payload = new ArraySegment<byte>()});
        }

        public override void DisconnectLocalClient()
        {
            s_MessageQueue[ServerClientId].Enqueue(new MessageData{Event = NetworkEvent.Disconnect, FromClientId = TransportId, Payload = new ArraySegment<byte>()});
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        public override void Shutdown()
        {
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
            Debug.Log("Mock transport initialized");
            NetworkManager = networkManager;
        }
    }
}
