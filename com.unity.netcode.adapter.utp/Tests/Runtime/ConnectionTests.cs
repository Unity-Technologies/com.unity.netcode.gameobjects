using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using static Unity.Netcode.UTP.RuntimeTests.RuntimeTestsHelpers;

namespace Unity.Netcode.RuntimeTests
{
    public class ConnectionTests
    {
        // For tests using multiple clients.
        private const int k_NumClients = 5;
        private UTPTransport server;
        private UTPTransport[] clients = new UTPTransport[k_NumClients];
        private List<TransportEvent> serverEvents;
        private List<TransportEvent>[] clientsEvents = new List<TransportEvent>[k_NumClients];

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Debug.Log("Calling Cleanup");

            if (server)
            {
                server.Shutdown();
                Object.DestroyImmediate(server);
            }

            foreach (var transport in clients)
            {
                if (transport)
                {
                    transport.Shutdown();
                    Object.DestroyImmediate(transport);
                }
            }

            foreach (var transportEvents in clientsEvents)
            {
                transportEvents?.Clear();
            }

            yield return null;
        }

        // Check connection with a single client.
        [UnityTest]
        public IEnumerator ConnectSingleClient()
        {

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out clients[0], out clientsEvents[0]);

            server.StartServer();
            clients[0].StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Check we've received Connect event on client too.
            Assert.AreEqual(1, clientsEvents[0].Count);
            Assert.AreEqual(NetworkEvent.Connect, clientsEvents[0][0].Type);

            yield return null;
        }

        // Check connection with multiple clients.
        [UnityTest]
        public IEnumerator ConnectMultipleClients()
        {

            InitializeTransport(out server, out serverEvents);
            server.StartServer();

            for (int i = 0; i < k_NumClients; i++)
            {
                InitializeTransport(out clients[i], out clientsEvents[i]);
                clients[i].StartClient();
            }

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Check that every client also received a Connect event.
            Assert.True(clientsEvents.All(evs => evs.Count == 1));
            Assert.True(clientsEvents.All(evs => evs[0].Type == NetworkEvent.Connect));

            yield return null;
        }

        // Check server disconnection with a single client.
        [UnityTest]
        public IEnumerator ServerDisconnectSingleClient()
        {

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out clients[0], out clientsEvents[0]);

            server.StartServer();
            clients[0].StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            server.DisconnectRemoteClient(serverEvents[0].ClientID);

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, clientsEvents[0]);

            yield return null;
        }

        // Check server disconnection with multiple clients.
        [UnityTest]
        public IEnumerator ServerDisconnectMultipleClients()
        {
            InitializeTransport(out server, out serverEvents);
            server.StartServer();

            for (int i = 0; i < k_NumClients; i++)
            {
                InitializeTransport(out clients[i], out clientsEvents[i]);
                clients[i].StartClient();
            }

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Disconnect a single client.
            server.DisconnectRemoteClient(serverEvents[0].ClientID);

            // Need to manually wait since we don't know which client will get the Disconnect.
            yield return new WaitForSeconds(MaxNetworkEventWaitTime);

            // Check that we received a Disconnect event on only one client.
            Assert.AreEqual(1, clientsEvents.Count(evs => evs.Count == 2 && evs[1].Type == NetworkEvent.Disconnect));

            // Disconnect all the other clients.
            for (int i = 1; i < k_NumClients; i++)
            {
                server.DisconnectRemoteClient(serverEvents[i].ClientID);
            }

            // Need to manually wait since we don't know which client got the Disconnect.
            yield return new WaitForSeconds(MaxNetworkEventWaitTime);

            // Check that all clients got a Disconnect event.
            Assert.True(clientsEvents.All(evs => evs.Count == 2));
            Assert.True(clientsEvents.All(evs => evs[1].Type == NetworkEvent.Disconnect));

            yield return null;
        }

        // Check client disconnection from a single client.
        [UnityTest]
        public IEnumerator ClientDisconnectSingleClient()
        {

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out clients[0], out clientsEvents[0]);

            server.StartServer();
            clients[0].StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            clients[0].DisconnectLocalClient();

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            yield return null;
        }

        // Check client disconnection with multiple clients.
        [UnityTest]
        public IEnumerator ClientDisconnectMultipleClients()
        {
            InitializeTransport(out server, out serverEvents);
            server.StartServer();

            for (int i = 0; i < k_NumClients; i++)
            {
                InitializeTransport(out clients[i], out clientsEvents[i]);
                clients[i].StartClient();
            }

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Disconnect a single client.
            clients[0].DisconnectLocalClient();

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            // Disconnect all the other clients.
            for (int i = 1; i < k_NumClients; i++)
            {
                clients[i].DisconnectLocalClient();
            }

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            // Check that we got the correct number of Disconnect events on the server.
            Assert.AreEqual(k_NumClients * 2, serverEvents.Count);
            Assert.AreEqual(k_NumClients, serverEvents.Count(e => e.Type == NetworkEvent.Disconnect));

            yield return null;
        }

        // Check that server re-disconnects are no-ops.
        [UnityTest]
        public IEnumerator RepeatedServerDisconnectsNoop()
        {
            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out clients[0], out clientsEvents[0]);

            server.StartServer();
            clients[0].StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            server.DisconnectRemoteClient(serverEvents[0].ClientID);

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, clientsEvents[0]);

            var previousServerEventsCount = serverEvents.Count;
            var previousClientEventsCount = clientsEvents[0].Count;

            server.DisconnectRemoteClient(serverEvents[0].ClientID);

            // Need to wait manually since no event should be generated.
            yield return new WaitForSeconds(MaxNetworkEventWaitTime);

            // Check we haven't received anything else on the client or server.
            Assert.AreEqual(serverEvents.Count, previousServerEventsCount);
            Assert.AreEqual(clientsEvents[0].Count, previousClientEventsCount);

            yield return null;
        }

        // Check that client re-disconnects are no-ops.
        [UnityTest]
        public IEnumerator RepeatedClientDisconnectsNoop()
        {

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out clients[0], out clientsEvents[0]);

            server.StartServer();
            clients[0].StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            clients[0].DisconnectLocalClient();

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            var previousServerEventsCount = serverEvents.Count;
            var previousClientEventsCount = clientsEvents[0].Count;

            clients[0].DisconnectLocalClient();

            // Need to wait manually since no event should be generated.
            yield return new WaitForSeconds(MaxNetworkEventWaitTime);

            // Check we haven't received anything else on the client or server.
            Assert.AreEqual(serverEvents.Count, previousServerEventsCount);
            Assert.AreEqual(clientsEvents[0].Count, previousClientEventsCount);

            server.Shutdown();

            yield return null;
        }
    }
}
