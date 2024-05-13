using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkManagerCustomMessageManagerTests
    {
        [Test]
        public void CustomMessageManagerAssigned()
        {
            var gameObject = new GameObject(nameof(CustomMessageManagerAssigned));
            var networkManager = gameObject.AddComponent<NetworkManager>();
            var transport = gameObject.AddComponent<DummyTransport>();

            networkManager.NetworkConfig = new NetworkConfig
            {
                // Set dummy transport that does nothing
                NetworkTransport = transport
            };

            CustomMessagingManager preManager = networkManager.CustomMessagingManager;

            // Start server to cause initialization
            networkManager.StartServer();

            Debug.Assert(preManager == null);
            Debug.Assert(networkManager.CustomMessagingManager != null);


            networkManager.Shutdown();
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator VerifyCustomMessageShutdownOrder()
        {
            Assert.True(NetcodeIntegrationTestHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients));

            bool isHost = false;

            // Start server to cause initialization
            NetcodeIntegrationTestHelpers.Start(isHost, server, clients);

            // [Client-Side] Wait for a connection to the server
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients, null, 512);

            // [Host-Side] Check to make sure all clients are connected
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(server, isHost ? (clients.Length + 1) : clients.Length, null, 512);

            // Create a message to pass directly to the message handler. If we send the message its processed before we get a chance to shutdown

            var dummySendData = new FastBufferWriter(128, Allocator.Temp);
            dummySendData.WriteValueSafe("Dummy Data");

            // make the message
            var unnamedMessage = new UnnamedMessage
            {
                SendData = dummySendData
            };

            // make the message
            using var serializedMessage = new FastBufferWriter(128, Allocator.Temp);
            unnamedMessage.Serialize(serializedMessage, 0);

            // Generate the full message
            var messageHeader = new NetworkMessageHeader
            {
                MessageSize = (uint)serializedMessage.Length,
                MessageType = server.MessageManager.GetMessageType(typeof(UnnamedMessage)),
            };

            var fullMessage = new FastBufferWriter(512, Allocator.Temp);
            BytePacker.WriteValueBitPacked(fullMessage, messageHeader.MessageType);
            BytePacker.WriteValueBitPacked(fullMessage, messageHeader.MessageSize);

            fullMessage.WriteBytesSafe(serializedMessage.ToArray());

            // Pack the message into a batch
            var batchHeader = new NetworkBatchHeader
            {
                BatchCount = 1
            };

            var batchedMessage = new FastBufferWriter(1100, Allocator.Temp);
            using (batchedMessage)
            {
                batchedMessage.TryBeginWrite(FastBufferWriter.GetWriteSize(batchHeader) +
                                     FastBufferWriter.GetWriteSize(fullMessage));
                batchedMessage.WriteValue(batchHeader);
                batchedMessage.WriteBytesSafe(fullMessage.ToArray());

                // Fill out the rest of the batch header
                batchedMessage.Seek(0);
                batchHeader = new NetworkBatchHeader
                {
                    Magic = NetworkBatchHeader.MagicValue,
                    BatchSize = batchedMessage.Length,
                    BatchHash = XXHash.Hash64(fullMessage.ToArray()),
                    BatchCount = 1
                };
                batchedMessage.WriteValue(batchHeader);

                // Handle the message as if it came from the server/client
                server.MessageManager.HandleIncomingData(clients[0].LocalClientId, batchedMessage.ToArray(), 0);

                foreach (var c in clients)
                {
                    c.MessageManager.HandleIncomingData(server.LocalClientId, batchedMessage.ToArray(), 0);
                }
            }

            // shutdown the network managher
            NetcodeIntegrationTestHelpers.Destroy();
        }
    }
}
