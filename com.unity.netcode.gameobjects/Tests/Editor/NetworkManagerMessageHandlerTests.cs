using System;
using NUnit.Framework;
#if UNITY_EDITOR
using Unity.Netcode.Editor;
#endif
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

            // Should cause log (server only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleConnectionRequest));
            using var messageStream0 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionRequest, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream0.GetBuffer(), 0, (int)messageStream0.Length), 0);

            // Should not cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            using var messageStream1 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionApproved, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream1.GetBuffer(), 0, (int)messageStream1.Length), 0);

            // Should not cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            using var messageStream2 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream2.GetBuffer(), 0, (int)messageStream2.Length), 0);

            // Should not cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            using var messageStream3 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream3.GetBuffer(), 0, (int)messageStream3.Length), 0);

            // Should not cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleSceneEvent));
            using var messageStream4 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SceneEvent, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream4.GetBuffer(), 0, (int)messageStream4.Length), 0);

            // Should not cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            using var messageStream5 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ChangeOwner, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream5.GetBuffer(), 0, (int)messageStream5.Length), 0);

            // Should not cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            using var messageStream6 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.TimeSync, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream6.GetBuffer(), 0, (int)messageStream6.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNetworkVariableDelta));
            using var messageStream7 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NetworkVariableDelta, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream7.GetBuffer(), 0, (int)messageStream7.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleUnnamedMessage));
            using var messageStream8 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream8.GetBuffer(), 0, (int)messageStream8.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNamedMessage));
            using var messageStream9 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream9.GetBuffer(), 0, (int)messageStream9.Length), 0);

            // Should cause log (server only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNetworkLog));
            using var messageStream10 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ServerLog, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream10.GetBuffer(), 0, (int)messageStream10.Length), 0);

            // Stop server to trigger full shutdown
            networkManager.Shutdown();

            // Replace the real message handler with a dummy one that just prints a result
            networkManager.MessageHandler = new DummyMessageHandler(networkManager);

            // Start client since pre-message-handler passes IsServer & IsClient checks
            networkManager.StartClient();

            // Disable batching to make the RPCs come straight through
            // This has to be done post start (and post restart since the queue container is reset)
            networkManager.MessageQueueContainer.EnableBatchedMessages(false);

            // Should not cause log (server only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            using var messageStream11 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionRequest, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream11.GetBuffer(), 0, (int)messageStream11.Length), 0);

            // Should cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleConnectionApproved));
            using var messageStream12 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionApproved, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream12.GetBuffer(), 0, (int)messageStream12.Length), 0);

            // Should cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleAddObject));
            using var messageStream13 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream13.GetBuffer(), 0, (int)messageStream13.Length), 0);

            // Should cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleDestroyObject));
            using var messageStream14 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream14.GetBuffer(), 0, (int)messageStream14.Length), 0);

            // Should cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleSceneEvent));
            using var messageStream15 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SceneEvent, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream15.GetBuffer(), 0, (int)messageStream15.Length), 0);

            // Should cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleChangeOwner));
            using var messageStream16 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ChangeOwner, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream16.GetBuffer(), 0, (int)messageStream16.Length), 0);

            // Should cause log (client only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleTimeSync));
            using var messageStream17 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.TimeSync, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream17.GetBuffer(), 0, (int)messageStream17.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNetworkVariableDelta));
            using var messageStream18 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NetworkVariableDelta, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream18.GetBuffer(), 0, (int)messageStream18.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleUnnamedMessage));
            using var messageStream19 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream19.GetBuffer(), 0, (int)messageStream19.Length), 0);

            // Should cause log (server and client)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNamedMessage));
            using var messageStream20 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream20.GetBuffer(), 0, (int)messageStream20.Length), 0);

            // Should not cause log (server only)
            // Everything should log MessageReceiveQueueItem even if ignored
            LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
            using var messageStream21 = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ServerLog, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching());
            networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream21.GetBuffer(), 0, (int)messageStream21.Length), 0);

            // Full cleanup
            networkManager.Shutdown();

            // Ensure no missmatches with expectations
            LogAssert.NoUnexpectedReceived();

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }
    }

    // Should probably have one of these for more files? In the future we could use the SIPTransport?
#if UNITY_EDITOR
    [DontShowInTransportDropdown]
#endif
    internal class DummyTransport : NetworkTransport
    {
        public override ulong ServerClientId { get; } = 0;
        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkChannel networkChannel)
        {
        }

        public override NetworkEvent PollEvent(out ulong clientId, out NetworkChannel networkChannel, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            networkChannel = NetworkChannel.Internal;
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

        public override void Init()
        {
        }
    }
}
