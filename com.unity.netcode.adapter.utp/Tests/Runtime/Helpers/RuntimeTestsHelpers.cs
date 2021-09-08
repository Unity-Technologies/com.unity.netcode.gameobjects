using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode.UTP.RuntimeTests
{
    public static class RuntimeTestsHelpers
    {
        // 50ms should be plenty enough for any network interaction to occur (even roundtrips).
        public const float MaxNetworkEventWaitTime = 0.15f;

        // Wait for an event to appear in the given event list (must be the very next event).
        public static IEnumerator WaitForNetworkEvent(NetworkEvent type, List<TransportEvent> events)
        {
            int initialCount = events.Count;
            float startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < MaxNetworkEventWaitTime)
            {
                if (events.Count > initialCount)
                {
                    Assert.AreEqual(type, events[initialCount].Type);
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Timed out while waiting for network event.");
        }

        // Common code to initialize a UTPTransport that logs its events.
        public static void InitializeTransport(out UTPTransport transport, out List<TransportEvent> events)
        {
            var logger = new TransportEventLogger();
            events = logger.Events;

            transport = new GameObject().AddComponent<UTPTransport>();
            transport.OnTransportEvent += logger.HandleEvent;
            transport.Initialize();
        }

        // Information about an event generated by a transport (basically just the parameters that
        // are normally passed along to a TransportEventDelegate).
        public struct TransportEvent
        {
            public NetworkEvent Type;
            public ulong ClientID;
            public ArraySegment<byte> Data;
            public float ReceiveTime;
        }

        // Utility class that logs events generated by a UTPTransport. Set it up by adding the
        // HandleEvent method as an OnTransportEvent delegate of the transport. The list of events
        // (in order in which they were generated) can be accessed through the Events property.
        public class TransportEventLogger
        {
            private readonly List<TransportEvent> m_Events = new List<TransportEvent>();
            public List<TransportEvent> Events => m_Events;

            public void HandleEvent(NetworkEvent type, ulong clientID, ArraySegment<byte> data, float receiveTime)
            {
                // Copy the data since the backing array will be reused for future messages.
                if (data != default(ArraySegment<byte>))
                {
                    data = new ArraySegment<byte>(data.ToArray());
                }

                m_Events.Add(new TransportEvent
                {
                    Type = type,
                    ClientID = clientID,
                    Data = data,
                    ReceiveTime = receiveTime
                });
            }
        }
    }
}
