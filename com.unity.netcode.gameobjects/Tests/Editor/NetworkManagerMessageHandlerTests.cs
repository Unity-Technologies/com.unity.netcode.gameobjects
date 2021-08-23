using System;
using NUnit.Framework;
using Unity.Netcode.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.EditorTests
{
    public class NetworkManagerMessageHandlerTests
    {
        [Test]
        public void MessageHandlerReceivedMessageServerClient()
        {
            ScenesInBuild.IsTesting = true;
            // Init
            var gameObject = new GameObject(nameof(MessageHandlerReceivedMessageServerClient));
            var networkManager = gameObject.AddComponent<NetworkManager>();
            var transport = gameObject.AddComponent<DummyTransport>();

            networkManager.PopulateScenesInBuild(true);
            networkManager.ScenesInBuild.Scenes.Add(SceneManager.GetActiveScene().name);
            networkManager.NetworkConfig = new NetworkConfig();
            // Set dummy transport that does nothing
            networkManager.NetworkConfig.NetworkTransport = transport;

            // Replace the real message handler with a dummy one that just prints a result
            networkManager.MessageHandler = new DummyMessageHandler(networkManager);

            // Have to force the update stage to something valid. It starts at Unset.
            NetworkUpdateLoop.UpdateStage = NetworkUpdateStage.Update;

            using (var inputBuffer = new NetworkBuffer())
            {
                // Start server since pre-message-handler passes IsServer & IsClient checks
                networkManager.StartServer();

                // Disable batching to make the RPCs come straight through
                // This has to be done post start
                networkManager.MessageQueueContainer.EnableBatchedMessages(false);

                // Should cause log (server only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleConnectionRequest));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionRequest, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionApproved, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleSceneEvent));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SceneEvent, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ChangeOwner, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.TimeSync, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNetworkVariableDelta));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NetworkVariableDelta, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleUnnamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNetworkLog));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ServerLog, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Stop server to trigger full shutdown
                networkManager.StopServer();

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
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionRequest, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleConnectionApproved));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionApproved, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleAddObject));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleDestroyObject));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObject, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleSceneEvent));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SceneEvent, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleChangeOwner));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ChangeOwner, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleTimeSync));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.TimeSync, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNetworkVariableDelta));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NetworkVariableDelta, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleUnnamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.HandleNamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (server only)
                // Everything should log MessageReceiveQueueItem even if ignored
                LogAssert.Expect(LogType.Log, nameof(DummyMessageHandler.MessageReceiveQueueItem));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ServerLog, inputBuffer, networkManager.MessageQueueContainer.IsUsingBatching()))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Full cleanup
                networkManager.StopClient();

                ScenesInBuild.IsTesting = false;
            }

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
