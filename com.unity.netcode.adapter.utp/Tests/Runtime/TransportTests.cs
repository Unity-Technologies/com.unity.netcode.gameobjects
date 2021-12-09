using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.TestTools;
using static Unity.Netcode.UTP.RuntimeTests.RuntimeTestsHelpers;

namespace Unity.Netcode.UTP.RuntimeTests
{
    public class TransportTests
    {
        private UnityTransport m_Server, m_Client1, m_Client2;
        private List<TransportEvent> m_ServerEvents, m_Client1Events, m_Client2Events;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Debug.Log("Calling Cleanup");
            if (m_Server)
            {
                m_Server.Shutdown();
                UnityEngine.Object.DestroyImmediate(m_Server);
            }

            if (m_Client1)
            {
                m_Client1.Shutdown();
                UnityEngine.Object.DestroyImmediate(m_Client1);
            }

            if (m_Client2)
            {
                m_Client2.Shutdown();
                UnityEngine.Object.DestroyImmediate(m_Client2);
            }

            m_ServerEvents?.Clear();
            m_Client1Events?.Clear();
            m_Client2Events?.Clear();

            yield return null;
        }

        // Check if can make a simple data exchange.
        [UnityTest]
        public IEnumerator PingPong()
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var ping = new ArraySegment<byte>(Encoding.ASCII.GetBytes("ping"));
            m_Client1.Send(m_Client1.ServerClientId, ping, NetworkDelivery.ReliableSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.That(m_ServerEvents[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));

            var pong = new ArraySegment<byte>(Encoding.ASCII.GetBytes("pong"));
            m_Server.Send(m_ServerEvents[0].ClientID, pong, NetworkDelivery.ReliableSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_Client1Events);

            Assert.That(m_Client1Events[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));

            // server.Shutdown();
            // client.Shutdown();

            yield return null;
        }

        // Check if can make a simple data exchange (both ways at a time).
        [UnityTest]
        public IEnumerator PingPongSimultaneous()
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var ping = new ArraySegment<byte>(Encoding.ASCII.GetBytes("ping"));
            m_Server.Send(m_ServerEvents[0].ClientID, ping, NetworkDelivery.ReliableSequenced);
            m_Client1.Send(m_Client1.ServerClientId, ping, NetworkDelivery.ReliableSequenced);

            // Once one event is in the other should be too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.That(m_ServerEvents[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));
            Assert.That(m_Client1Events[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));

            var pong = new ArraySegment<byte>(Encoding.ASCII.GetBytes("pong"));
            m_Server.Send(m_ServerEvents[0].ClientID, pong, NetworkDelivery.ReliableSequenced);
            m_Client1.Send(m_Client1.ServerClientId, pong, NetworkDelivery.ReliableSequenced);

            // Once one event is in the other should be too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.That(m_ServerEvents[2].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));
            Assert.That(m_Client1Events[2].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));

            yield return null;
        }

        [UnityTest]
        public IEnumerator FilledSendQueueSingleSend()
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var payload = new ArraySegment<byte>(new byte[UnityTransport.InitialBatchQueueSize]);
            m_Client1.Send(m_Client1.ServerClientId, payload, NetworkDelivery.ReliableFragmentedSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            yield return null;
        }

        [UnityTest]
        public IEnumerator FilledSendQueueMultipleSends()
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var numSends = UnityTransport.InitialBatchQueueSize / 1024;

            for (int i = 0; i < numSends; i++)
            {
                // We remove 4 bytes because each send carries a 4 bytes overhead in the send queue.
                // Without that we wouldn't fill the send queue; it would get flushed right when we
                // try to send the last message.
                var payload = new ArraySegment<byte>(new byte[1024 - 4]);
                m_Client1.Send(m_Client1.ServerClientId, payload, NetworkDelivery.ReliableFragmentedSequenced);
            }

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            // Extra event is for the Connect event.
            Assert.AreEqual(numSends + 1, m_ServerEvents.Count);

            yield return null;
        }

        // Check making multiple sends to a client in a single frame.
        [UnityTest]
        public IEnumerator MultipleSendsSingleFrame()
        {
            InitializeTransport(out m_Server, out m_ServerEvents);
            InitializeTransport(out m_Client1, out m_Client1Events);

            m_Server.StartServer();
            m_Client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, m_Client1Events);

            var data1 = new ArraySegment<byte>(new byte[] { 11 });
            m_Client1.Send(m_Client1.ServerClientId, data1, NetworkDelivery.ReliableSequenced);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            m_Client1.Send(m_Client1.ServerClientId, data2, NetworkDelivery.ReliableSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, m_ServerEvents);

            Assert.AreEqual(3, m_ServerEvents.Count);
            Assert.AreEqual(NetworkEvent.Data, m_ServerEvents[2].Type);

            Assert.AreEqual(11, m_ServerEvents[1].Data.First());
            Assert.AreEqual(22, m_ServerEvents[2].Data.First());

            yield return null;
        }

        // Check sending data to multiple clients.
        [UnityTest]
        public IEnumerator SendMultipleClients()
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
            m_Server.Send(m_ServerEvents[0].ClientID, data1, NetworkDelivery.ReliableSequenced);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            m_Server.Send(m_ServerEvents[1].ClientID, data2, NetworkDelivery.ReliableSequenced);

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
        public IEnumerator ReceiveMultipleClients()
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
            m_Client1.Send(m_Client1.ServerClientId, data1, NetworkDelivery.ReliableSequenced);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            m_Client2.Send(m_Client2.ServerClientId, data2, NetworkDelivery.ReliableSequenced);

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
