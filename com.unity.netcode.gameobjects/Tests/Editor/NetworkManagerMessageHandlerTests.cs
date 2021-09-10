using System;
using NUnit.Framework;
using Unity.Netcode.Editor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.EditorTests
{
    public class NetworkManagerMessageHandlerTests
    {
        [Test]
        public void MessageHandlerReceivedMessageServerClient()
        {
            // Init
            var gameObject = new GameObject(nameof(MessageHandlerReceivedMessageServerClient));
            var networkManager = gameObject.AddComponent<NetworkManager>();
            var transport = gameObject.AddComponent<DummyTransport>();

            networkManager.NetworkConfig = new NetworkConfig();
            // Set dummy transport that does nothing
            networkManager.NetworkConfig.NetworkTransport = transport;

            // Replace the real message handler with a dummy one that just prints a result
            networkManager.MessageHandler = new DummyMessageHandler(networkManager);

            // Have to force the update stage to something valid. It starts at Unset.
            NetworkUpdateLoop.UpdateStage = NetworkUpdateStage.Update;

            using var inputBuffer = new NetworkBuffer();
            // Start server since pre-message-handler passes IsServer & IsClient checks
            networkManager.StartServer();

            // Disable batching to make the RPCs come straight through
            // This has to be done post start
            networkManager.MessageQueueContainer.EnableBatchedMessages(false);

            // Should not cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleSceneEvent));
            using var messageStream4 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SceneEvent, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, new ArraySegment<byte>(messageStream4.GetBuffer(), 0, (int)messageStream4.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleUnnamedMessage));
            using var messageStream8 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, new ArraySegment<byte>(messageStream8.GetBuffer(), 0, (int)messageStream8.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNamedMessage));
            using var messageStream9 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, new ArraySegment<byte>(messageStream9.GetBuffer(), 0, (int)messageStream9.Length), 0);

            // Stop server to trigger full shutdown
            networkManager.Shutdown();

            // Replace the real message handler with a dummy one that just prints a result
            networkManager.MessageHandler = new DummyMessageHandler(networkManager);

            // Start client since pre-message-handler passes IsServer & IsClient checks
            networkManager.StartClient();

            // Disable batching to make the RPCs come straight through
            // This has to be done post start (and post restart since the queue container is reset)
            networkManager.MessageQueueContainer.EnableBatchedMessages(false);
            // Should cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleSceneEvent));
            using var messageStream15 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SceneEvent, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, new ArraySegment<byte>(messageStream15.GetBuffer(), 0, (int)messageStream15.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleUnnamedMessage));
            using var messageStream19 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, new ArraySegment<byte>(messageStream19.GetBuffer(), 0, (int)messageStream19.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNamedMessage));
            using var messageStream20 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, new ArraySegment<byte>(messageStream20.GetBuffer(), 0, (int)messageStream20.Length), 0);

            // Full cleanup
            networkManager.Shutdown();

            // Ensure no missmatches with expectations
            LogAssert.NoUnexpectedReceived();

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }
    }

    // Should probably have one of these for more files? In the future we could use the SIPTransport?
    [DontShowInTransportDropdown]
    internal class DummyTransport : NetworkTransport
    {
        public override ulong ServerClientId { get; } = 0;
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = new ArraySegment<byte>();
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }

        public override SocketTasks StartClient()
        {
            return SocketTask.Done.AsTasks();
        }

        public override SocketTasks StartServer()
        {
            return SocketTask.Done.AsTasks();

        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
        }

        public override void DisconnectLocalClient()
        {
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        public override void Shutdown()
        {
        }

        public override void Initialize()
        {
        }
    }
}
