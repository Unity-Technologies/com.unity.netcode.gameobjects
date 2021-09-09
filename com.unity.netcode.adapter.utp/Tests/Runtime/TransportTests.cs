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
        UTPTransport server, client1, client2;
        List<TransportEvent> serverEvents, client1Events, client2Events;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Debug.Log("Calling Cleanup");
            if (server)
            {
                server.Shutdown();
                GameObject.DestroyImmediate(server);
            }

            if (client1)
            {
                client1.Shutdown();
                GameObject.DestroyImmediate(client1);
            }

            if (client2)
            {
                client2.Shutdown();
                GameObject.DestroyImmediate(client2);
            }

            serverEvents?.Clear();
            client1Events?.Clear();
            client2Events?.Clear();

            yield return null;
        }

        // Check if can make a simple data exchange.
        [UnityTest]
        public IEnumerator PingPong()
        {
            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client1, out client1Events);

            server.StartServer();
            client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            var ping = new ArraySegment<byte>(Encoding.ASCII.GetBytes("ping"));
            client1.Send(client1.ServerClientId, ping, NetworkDelivery.ReliableSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, serverEvents);

            Assert.That(serverEvents[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));

            var pong = new ArraySegment<byte>(Encoding.ASCII.GetBytes("pong"));
            server.Send(serverEvents[0].ClientID, pong, NetworkDelivery.ReliableSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, client1Events);

            Assert.That(client1Events[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));

            // server.Shutdown();
            // client.Shutdown();

            yield return null;
        }



        // Check if can make a simple data exchange (both ways at a time).
        [UnityTest]
        public IEnumerator PingPongSimultaneous()
        {
            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client1, out client1Events);

            server.StartServer();
            client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            var ping = new ArraySegment<byte>(Encoding.ASCII.GetBytes("ping"));
            server.Send(serverEvents[0].ClientID, ping, NetworkDelivery.ReliableSequenced);
            client1.Send(client1.ServerClientId, ping, NetworkDelivery.ReliableSequenced);

            // Once one event is in the other should be too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, serverEvents);

            Assert.That(serverEvents[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));
            Assert.That(client1Events[1].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("ping")));

            var pong = new ArraySegment<byte>(Encoding.ASCII.GetBytes("pong"));
            server.Send(serverEvents[0].ClientID, pong, NetworkDelivery.ReliableSequenced);
            client1.Send(client1.ServerClientId, pong, NetworkDelivery.ReliableSequenced);

            // Once one event is in the other should be too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, serverEvents);

            Assert.That(serverEvents[2].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));
            Assert.That(client1Events[2].Data, Is.EquivalentTo(Encoding.ASCII.GetBytes("pong")));

            yield return null;
        }

        // Check making multiple sends to a client in a single frame.
        [UnityTest]
        public IEnumerator MultipleSendsSingleFrame()
        {
            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client1, out client1Events);

            server.StartServer();
            client1.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            var data1 = new ArraySegment<byte>(new byte[] { 11 });
            client1.Send(client1.ServerClientId, data1, NetworkDelivery.ReliableSequenced);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            client1.Send(client1.ServerClientId, data2, NetworkDelivery.ReliableSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, serverEvents);

            Assert.AreEqual(3, serverEvents.Count);
            Assert.AreEqual(NetworkEvent.Data, serverEvents[2].Type);

            Assert.AreEqual(11, serverEvents[1].Data.First());
            Assert.AreEqual(22, serverEvents[2].Data.First());

            yield return null;
        }

        // Check sending data to multiple clients.
        [UnityTest]
        public IEnumerator SendMultipleClients()
        {
            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client1, out client1Events);
            InitializeTransport(out client2, out client2Events);

            server.StartServer();
            client1.StartClient();
            client2.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Ensure we got both Connect events.
            Assert.AreEqual(2, serverEvents.Count);

            var data1 = new ArraySegment<byte>(new byte[] { 11 });
            server.Send(serverEvents[0].ClientID, data1, NetworkDelivery.ReliableSequenced);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            server.Send(serverEvents[1].ClientID, data2, NetworkDelivery.ReliableSequenced);

            // Once one has received its data, the other should have too.
            yield return WaitForNetworkEvent(NetworkEvent.Data, client1Events);

            // Do make sure the other client got its Data event.
            Assert.AreEqual(2, client2Events.Count);
            Assert.AreEqual(NetworkEvent.Data, client2Events[1].Type);

            byte c1Data = client1Events[1].Data.First();
            byte c2Data = client2Events[1].Data.First();
            Assert.True((c1Data == 11 && c2Data == 22) || (c1Data == 22 && c2Data == 11));

            yield return null;
        }

        // Check receiving data from multiple clients.
        [UnityTest]
        public IEnumerator ReceiveMultipleClients()
        {
            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client1, out client1Events);
            InitializeTransport(out client2, out client2Events);

            server.StartServer();
            client1.StartClient();
            client2.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, client1Events);

            // Ensure we got the Connect event on the other client too.
            Assert.AreEqual(1, client2Events.Count);

            var data1 = new ArraySegment<byte>(new byte[] { 11 });
            client1.Send(client1.ServerClientId, data1, NetworkDelivery.ReliableSequenced);

            var data2 = new ArraySegment<byte>(new byte[] { 22 });
            client2.Send(client2.ServerClientId, data2, NetworkDelivery.ReliableSequenced);

            yield return WaitForNetworkEvent(NetworkEvent.Data, serverEvents);

            // Make sure we got both data messages.
            Assert.AreEqual(4, serverEvents.Count);
            Assert.AreEqual(NetworkEvent.Data, serverEvents[3].Type);

            byte sData1 = serverEvents[2].Data.First();
            byte sData2 = serverEvents[3].Data.First();
            Assert.True((sData1 == 11 && sData2 == 22) || (sData1 == 22 && sData2 == 11));

            yield return null;
        }
    }
}
