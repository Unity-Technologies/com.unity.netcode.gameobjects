// todo @simon-lemay-unity: un-guard/re-enable after validating UTP on consoles
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.UTP.Utilities;
using static Unity.Netcode.UTP.RuntimeTests.RuntimeTestsHelpers;

namespace Unity.Netcode.UTP.RuntimeTests
{
    public class TransportTests
    {
        // No need to test all reliable delivery methods since they all map to the same pipeline.
        private static readonly NetworkDelivery[] k_DeliveryParameters =
        {
            NetworkDelivery.Unreliable,
            NetworkDelivery.UnreliableSequenced,
            NetworkDelivery.Reliable
        };

        private UnityTransport m_Server, m_Client1, m_Client2;
        private List<TransportEvent> m_ServerEvents, m_Client1Events, m_Client2Events;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (m_Server)
            {
                m_Server.Shutdown();

                // Need to destroy the GameObject (all assigned components will get destroyed too)
                UnityEngine.Object.DestroyImmediate(m_Server.gameObject);
            }

            if (m_Client1)
            {
                m_Client1.Shutdown();

                // Need to destroy the GameObject (all assigned components will get destroyed too)
                UnityEngine.Object.DestroyImmediate(m_Client1.gameObject);
            }

            if (m_Client2)
            {
                m_Client2.Shutdown();

                // Need to destroy the GameObject (all assigned components will get destroyed too)
                UnityEngine.Object.DestroyImmediate(m_Client2.gameObject);
            }

            m_ServerEvents?.Clear();
            m_Client1Events?.Clear();
            m_Client2Events?.Clear();

            yield return null;
        }

        // Check if can make a simple data exchange.
        [UnityTest]
        public IEnumerator PingPong([ValueSource("k_DeliveryParameters")] NetworkDelivery delivery)
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var ping = new ArraySegment<byte>(Encoding.ASCII.GetBytes("ping"));
            m_Client1.Send(m_Client1.ServerClientId, ping, delivery);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.That(m_ServerEvents[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));

            var pong = new ArraySegment<byte>(Encoding.ASCII.GetBytes("pong"));
            m_Server.Send(m_ServerEvents[0].ClientID, pong, delivery);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_Client1Events);

            Assert.That(m_Client1Events[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));

            yield return null;
        }

        // Check if can make a simple data exchange (both ways at a time).
        [UnityTest]
        public IEnumerator PingPongSimultaneous([ValueSource("k_DeliveryParameters")] NetworkDelivery delivery)
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var ping = new ArraySegment<byte>(Encoding.ASCII.GetBytes("ping"));
            m_Server.Send(m_ServerEvents[0].ClientID, ping, delivery);
            m_Client1.Send(m_Client1.ServerClientId, ping, delivery);

            // Once one event is in the other should be too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.That(m_ServerEvents[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));
            Assert.That(m_Client1Events[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));

            var pong = new ArraySegment<byte>(Encoding.ASCII.GetBytes("pong"));
            m_Server.Send(m_ServerEvents[0].ClientID, pong, delivery);
            m_Client1.Send(m_Client1.ServerClientId, pong, delivery);

            // Once one event is in the other should be too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.That(m_ServerEvents[2].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));
            Assert.That(m_Client1Events[2].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));

            yield return null;
        }

        [UnityTest]
        public IEnumerator SendMaximumPayloadSize([ValueSource("k_DeliveryParameters")] NetworkDelivery delivery)
        {
            // We want something that's over the old limit of ~44KB for reliable payloads.
            var payloadSize = 64 * 1024;

            InitializeTransport(out m_Server, out m_ServerEvents, payloadSize);
            InitializeTransport(out m_Client1, out m_Client1Events, payloadSize);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var payloadData = new byte[payloadSize];
            for (int i = 0; i < payloadData.Length; i++)
            {
                payloadData[i] = (byte)i;
            }

            var payload = new ArraySegment<byte>(payloadData);
            m_Client1.Send(m_Client1.ServerClientId, payload, delivery);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents, MaxNetworkEventWaitTime * 2);

            Assert.AreEqual(payloadSize, m_ServerEvents[1].Data.Count);

            var receivedArray = m_ServerEvents[1].Data.Array;
            var receivedArrayOffset = m_ServerEvents[1].Data.Offset;
            for (int i = 0; i < payloadSize; i++)
            {
                Assert.AreEqual(payloadData[i], receivedArray[receivedArrayOffset + i]);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FilledSendQueueMultipleSends([ValueSource("k_DeliveryParameters")] NetworkDelivery delivery)
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var numSends = UnityTransport.InitialMaxSendQueueSize / 1024;

            for (int i = 0; i < numSends; i++)
            {
                // We remove 4 bytes because each send carries a 4 bytes overhead in the send queue.
                // Without that we wouldn't fill the send queue; it would get flushed right when we
                // try to send the last message.
                var payload = new ArraySegment<byte>(new byte[1024 - BatchedSendQueue.PerMessageOverhead]);
                m_Client1.Send(m_Client1.ServerClientId, payload, delivery);
            }

            // Manually wait. This ends up generating quite a bit of packets and it might take a
            // while for everything to make it to the server.
            yield return new WaitForSeconds(numSends * 0.02f);

            // Extra event is the connect event.
            Assert.AreEqual(numSends + 1, m_ServerEvents.Count);

            for (int i = 1; i <= numSends; i++)
            {
                Assert.AreEqual(NetworkEvent.Data, m_ServerEvents[i].Type);
                Assert.AreEqual(1024 - BatchedSendQueue.PerMessageOverhead, m_ServerEvents[i].Data.Count);
            }

            yield return null;
        }

        // Check making multiple sends to a client in a single frame.
        [UnityTest]
        public IEnumerator MultipleSendsSingleFrame([ValueSource("k_DeliveryParameters")] NetworkDelivery delivery)
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var data1 = new ArraySegment<byte>(new byte[] { 11 });
            m_Client1.Send(m_Client1.ServerClientId, data1, delivery);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            m_Client1.Send(m_Client1.ServerClientId, data2, delivery);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.AreEqual(3, m_ServerEvents.Count);
            Assert.AreEqual(NetworkEvent.Data, m_ServerEvents[2].Type);

            Assert.AreEqual(11, m_ServerEvents[1].Data.First());
            Assert.AreEqual(22, m_ServerEvents[2].Data.First());

            yield return null;
        }

        // Check sending data to multiple clients.
        [UnityTest]
        public IEnumerator SendMultipleClients([ValueSource("k_DeliveryParameters")] NetworkDelivery delivery)
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);
            InitializeTransport(out m_Client2, out m_Client2Events);

            m_Server.StartServer();
            m_Client1.StartClient();
            m_Client2.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);
            if (m_Client2Events.Count == 0)
            {
                yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client2Events);
            }

            // Ensure we got both Connect events.
            Assert.AreEqual(2, m_ServerEvents.Count);

            var data1 = new ArraySegment<byte>(new byte[] { 11 });
            m_Server.Send(m_ServerEvents[0].ClientID, data1, delivery);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            m_Server.Send(m_ServerEvents[1].ClientID, data2, delivery);

            // Once one has received its data, the other should have too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, m_Client1Events);

            // Do make sure the other client got its Data event.
            Assert.AreEqual(2, m_Client2Events.Count);
            Assert.AreEqual(NetworkEvent.Data, m_Client2Events[1].Type);

            byte c1Data = m_Client1Events[1].Data.First();
            byte c2Data = m_Client2Events[1].Data.First();
            Assert.That((c1Data == 11 && c2Data == 22) || (c1Data == 22 && c2Data == 11));

            yield return null;
        }

        // Check receiving data from multiple clients.
        [UnityTest]
        public IEnumerator ReceiveMultipleClients([ValueSource("k_DeliveryParameters")] NetworkDelivery delivery)
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);
            InitializeTransport(out m_Client2, out m_Client2Events);

            m_Server.StartServer();
            m_Client1.StartClient();
            m_Client2.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);
            if (m_Client2Events.Count == 0)
            {
                yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client2Events);
            }

            var data1 = new ArraySegment<byte>(new byte[] { 11 });
            m_Client1.Send(m_Client1.ServerClientId, data1, delivery);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            m_Client2.Send(m_Client2.ServerClientId, data2, delivery);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            // Make sure we got both data messages.
            Assert.AreEqual(4, m_ServerEvents.Count);
            Assert.AreEqual(NetworkEvent.Data, m_ServerEvents[3].Type);

            byte sData1 = m_ServerEvents[2].Data.First();
            byte sData2 = m_ServerEvents[3].Data.First();
            Assert.That((sData1 == 11 && sData2 == 22) || (sData1 == 22 && sData2 == 11));

            yield return null;
        }
    }
}
#endif
