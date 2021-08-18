using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;
using Unity.Netcode.UTP.RuntimeTests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    using static RuntimeTestsHelpers;

    public class ConnectionTests
    {
        // For tests using multiple clients.
        private const int NumClients = 5;

        // Check connection with a single client.
        [UnityTest]
        public IEnumerator ConnectSingleClient()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Check we've received Connect event on client too.
            Assert.AreEqual(1, clientEvents.Count);
            Assert.AreEqual(NetworkEvent.Connect, clientEvents[0].Type);

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }

        // Check connection with multiple clients.
        [UnityTest]
        public IEnumerator ConnectMultipleClients()
        {
            UTPTransport server;
            var clients = new UTPTransport[NumClients];

            List<TransportEvent> serverEvents;
            var clientsEvents = new List<TransportEvent>[NumClients];

            InitializeTransport(out server, out serverEvents);
            server.StartServer();

            for (int i = 0; i < NumClients; i++)
            {
                InitializeTransport(out clients[i], out clientsEvents[i]);
                clients[i].StartClient();
            }

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Check that every client also received a Connect event.
            Assert.True(clientsEvents.All(evs => evs.Count == 1));
            Assert.True(clientsEvents.All(evs => evs[0].Type == NetworkEvent.Connect));

            server.Shutdown();
            for (int i = 0; i < NumClients; i++)
                clients[i].Shutdown();

            yield return null;
        }

        // Check server disconnection with a single client.
        [UnityTest]
        public IEnumerator ServerDisconnectSingleClient()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            server.DisconnectRemoteClient(serverEvents[0].ClientID);

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, clientEvents);

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }

        // Check server disconnection with multiple clients.
        [UnityTest]
        public IEnumerator ServerDisconnectMultipleClients()
        {
            UTPTransport server;
            var clients = new UTPTransport[NumClients];

            List<TransportEvent> serverEvents;
            var clientsEvents = new List<TransportEvent>[NumClients];

            InitializeTransport(out server, out serverEvents);
            server.StartServer();

            for (int i = 0; i < NumClients; i++)
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
            for (int i = 1; i < NumClients; i++)
                server.DisconnectRemoteClient(serverEvents[i].ClientID);

            // Need to manually wait since we don't know which client got the Disconnect.
            yield return new WaitForSeconds(MaxNetworkEventWaitTime);

            // Check that all clients got a Disconnect event.
            Assert.True(clientsEvents.All(evs => evs.Count == 2));
            Assert.True(clientsEvents.All(evs => evs[1].Type == NetworkEvent.Disconnect));

            server.Shutdown();
            for (int i = 0; i < NumClients; i++)
                clients[i].Shutdown();

            yield return null;
        }

        // Check client disconnection from a single client.
        [UnityTest]
        public IEnumerator ClientDisconnectSingleClient()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            client.DisconnectLocalClient();

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }

        // Check client disconnection with multiple clients.
        [UnityTest]
        public IEnumerator ClientDisconnectMultipleClients()
        {
            UTPTransport server;
            var clients = new UTPTransport[NumClients];

            List<TransportEvent> serverEvents;
            var clientsEvents = new List<TransportEvent>[NumClients];

            InitializeTransport(out server, out serverEvents);
            server.StartServer();

            for (int i = 0; i < NumClients; i++)
            {
                InitializeTransport(out clients[i], out clientsEvents[i]);
                clients[i].StartClient();
            }

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            // Disconnect a single client.
            clients[0].DisconnectLocalClient();

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            // Disconnect all the other clients.
            for (int i = 1; i < NumClients; i++)
                clients[i].DisconnectLocalClient();

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            // Check that we got the correct number of Disconnect events on the server.
            Assert.AreEqual(NumClients * 2, serverEvents.Count);
            Assert.AreEqual(NumClients, serverEvents.Count(e => e.Type == NetworkEvent.Disconnect));

            server.Shutdown();
            for (int i = 0; i < NumClients; i++)
                clients[i].Shutdown();

            yield return null;
        }

        // Check that server re-disconnects are no-ops.
        [UnityTest]
        public IEnumerator RepeatedServerDisconnectsNoop()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            server.DisconnectRemoteClient(serverEvents[0].ClientID);

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, clientEvents);

            var previousServerEventsCount = serverEvents.Count;
            var previousClientEventsCount = clientEvents.Count;

            server.DisconnectRemoteClient(serverEvents[0].ClientID);

            // Need to wait manually since no event should be generated.
            yield return new WaitForSeconds(MaxNetworkEventWaitTime);

            // Check we haven't received anything else on the client or server.
            Assert.AreEqual(serverEvents.Count, previousServerEventsCount);
            Assert.AreEqual(clientEvents.Count, previousClientEventsCount);

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }

        // Check that client re-disconnects are no-ops.
        [UnityTest]
        public IEnumerator RepeatedClientDisconnectsNoop()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            client.DisconnectLocalClient();

            yield return WaitForNetworkEvent(NetworkEvent.Disconnect, serverEvents);

            var previousServerEventsCount = serverEvents.Count;
            var previousClientEventsCount = clientEvents.Count;

            client.DisconnectLocalClient();

            // Need to wait manually since no event should be generated.
            yield return new WaitForSeconds(MaxNetworkEventWaitTime);

            // Check we haven't received anything else on the client or server.
            Assert.AreEqual(serverEvents.Count, previousServerEventsCount);
            Assert.AreEqual(clientEvents.Count, previousClientEventsCount);

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }
    }
}
