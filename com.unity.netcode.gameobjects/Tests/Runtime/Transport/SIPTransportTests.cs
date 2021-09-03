using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests.Transport
{
    public class SIPTransportTests
    {
        [Test]
        public void SendReceiveData()
        {
            SIPTransport server = new GameObject("Server").AddComponent<SIPTransport>();
            SIPTransport client = new GameObject("Client").AddComponent<SIPTransport>();

            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();

            NetworkEvent serverEvent = server.PollEvent(out ulong clientId, out _, out _);
            NetworkEvent clientEvent = client.PollEvent(out ulong serverId, out _, out _);

            // Make sure both connected
            Assert.True(serverEvent == NetworkEvent.Connect);
            Assert.True(clientEvent == NetworkEvent.Connect);

            // Send data
            server.Send(clientId, new ArraySegment<byte>(Encoding.ASCII.GetBytes("Hello Client")), NetworkDelivery.ReliableSequenced);
            client.Send(serverId, new ArraySegment<byte>(Encoding.ASCII.GetBytes("Hello Server")), NetworkDelivery.ReliableSequenced);

            serverEvent = server.PollEvent(out ulong newClientId, out ArraySegment<byte> serverPayload, out _);
            clientEvent = client.PollEvent(out ulong newServerId, out ArraySegment<byte> clientPayload, out _);

            // Make sure we got data
            Assert.True(serverEvent == NetworkEvent.Data);
            Assert.True(clientEvent == NetworkEvent.Data);

            // Make sure the ID is correct
            Assert.True(newClientId == clientId);
            Assert.True(newServerId == serverId);

            // Make sure the payload was correct
            Assert.That(serverPayload, Is.EquivalentTo(Encoding.ASCII.GetBytes("Hello Server")));
            Assert.That(clientPayload, Is.EquivalentTo(Encoding.ASCII.GetBytes("Hello Client")));

            server.Shutdown();
            client.Shutdown();
        }
    }
}
