using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.TestTools;
using UTPNetworkEvent = Unity.Networking.Transport.NetworkEvent;

namespace Unity.Netcode.UTP.RuntimeTests
{
    using static RuntimeTestsHelpers;

    public class ChannelsTests
    {
        // Check that we receive the correct channel (one message after the other).
        [UnityTest]
        public IEnumerator ReceiveCorrectChannelSequenced()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            int eventIndex = 1;
            foreach (var transportChannel in server.NETCODE_CHANNELS)
            {
                server.Send(serverEvents[0].ClientID, default(ArraySegment<byte>), transportChannel.Channel);

                yield return WaitForNetworkEvent(NetworkEvent.Data, clientEvents);

                Assert.AreEqual(transportChannel.Channel, clientEvents[eventIndex].Channel);

                eventIndex++;
            }

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }

        // Check that we receive the correct channel (all messages received at once).
        [UnityTest]
        public IEnumerator ReceiveCorrectChannelSameFrame()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            foreach (var transportChannel in server.NETCODE_CHANNELS)
                client.Send(client.ServerClientId, default(ArraySegment<byte>), transportChannel.Channel);

            yield return WaitForNetworkEvent(NetworkEvent.Data, serverEvents);

            Assert.AreEqual(server.NETCODE_CHANNELS.Length + 1, serverEvents.Count);

            int eventIndex = 1;
            foreach (var transportChannel in server.NETCODE_CHANNELS)
            {
                Assert.AreEqual(transportChannel.Channel, serverEvents[eventIndex].Channel);
                eventIndex++;
            }

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }

        // Check pipeline mapping for every default channel (except fragmented).
        [UnityTest]
        public IEnumerator ChannelPipelineMapping()
        {
            UTPTransport server;
            List<TransportEvent> serverEvents;
            DriverClient client = new GameObject().AddComponent<DriverClient>();

            InitializeTransport(out server, out serverEvents);

            server.StartServer();
            client.Connect();

            yield return client.WaitForNetworkEvent(UTPNetworkEvent.Type.Connect);

            foreach (var transportChannel in server.NETCODE_CHANNELS)
            {
                // Skip over fragmented channels (covered by different test).
                if (transportChannel.Delivery == NetworkDelivery.ReliableFragmentedSequenced)
                    continue;

                server.Send(serverEvents[0].ClientID, default(ArraySegment<byte>), transportChannel.Channel);

                yield return client.WaitForNetworkEvent(UTPNetworkEvent.Type.Data);

                // Check that data's pipeline is what's expected for the delivery.
                switch (transportChannel.Delivery)
                {
                    case NetworkDelivery.Unreliable:
                        Assert.AreEqual(NetworkPipeline.Null, client.LastEventPipeline);
                        break;

                    case NetworkDelivery.UnreliableSequenced:
                        Assert.AreEqual(client.UnreliableSequencedPipeline, client.LastEventPipeline);
                        break;

                    case NetworkDelivery.Reliable:
                    case NetworkDelivery.ReliableSequenced:
                        Assert.AreEqual(client.ReliableSequencedPipeline, client.LastEventPipeline);
                        break;
                }
            }

            server.Shutdown();

            yield return null;
        }

        // Check pipeline mapping for every default channel that has fragmentation.
        [UnityTest]
        public IEnumerator ChannelPipelineMappingFragmented()
        {
            UTPTransport server;
            List<TransportEvent> serverEvents;
            DriverClient client = new GameObject().AddComponent<DriverClient>();

            InitializeTransport(out server, out serverEvents);

            server.StartServer();
            client.Connect();

            yield return client.WaitForNetworkEvent(UTPNetworkEvent.Type.Connect);

            foreach (var transportChannel in server.NETCODE_CHANNELS)
            {
                // Skip over non-fragmented channels (covered by different test).
                if (transportChannel.Delivery != NetworkDelivery.ReliableFragmentedSequenced)
                    continue;

                // Check that data smaller than MTU doesn't trigger fragmented pipeline.

                server.Send(serverEvents[0].ClientID, default(ArraySegment<byte>), transportChannel.Channel);

                yield return client.WaitForNetworkEvent(UTPNetworkEvent.Type.Data);

                Assert.AreEqual(client.ReliableSequencedPipeline, client.LastEventPipeline);

                // Check that data larger than MTU does trigger fragmented pipeline.

                var data = new ArraySegment<byte>(new byte[UTPTransport.MaximumMessageLength]);
                server.Send(serverEvents[0].ClientID, data, transportChannel.Channel);

                yield return client.WaitForNetworkEvent(UTPNetworkEvent.Type.Data);

                Assert.AreEqual(client.ReliableSequencedFragmentedPipeline, client.LastEventPipeline);
            }

            server.Shutdown();

            yield return null;
        }

        // Check fragmentation on channels where it is expected.
        [UnityTest]
        public IEnumerator FragmentedDelivery()
        {
            UTPTransport server, client;
            List<TransportEvent> serverEvents, clientEvents;

            InitializeTransport(out server, out serverEvents);
            InitializeTransport(out client, out clientEvents);

            server.StartServer();
            client.StartClient();

            yield return WaitForNetworkEvent(NetworkEvent.Connect, serverEvents);

            int eventIndex = 1;
            foreach (var transportChannel in server.NETCODE_CHANNELS)
            {
                // Only want to test fragmentation-enabled channels.
                if (transportChannel.Delivery != NetworkDelivery.ReliableFragmentedSequenced)
                    continue;

                var data = new byte[UTPTransport.MaximumMessageLength];
                for (int i = 0; i < data.Length; i++)
                    data[i] = (byte) i;

                client.Send(client.ServerClientId, new ArraySegment<byte>(data), transportChannel.Channel);

                yield return WaitForNetworkEvent(NetworkEvent.Data, serverEvents);

                Assert.True(serverEvents[eventIndex].Data.SequenceEqual(data));

                eventIndex++;
            }

            server.Shutdown();
            client.Shutdown();

            yield return null;
        }
    }
}
