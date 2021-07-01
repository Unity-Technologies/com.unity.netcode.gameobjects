using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Editor;
using MLAPI.Messaging;
using MLAPI.Serialization;
using MLAPI.Transports;
using MLAPI.Transports.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MLAPI.EditorTests
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

            // MLAPI sets this in validate
            networkManager.NetworkConfig = new NetworkConfig()
            {
                // Set the current scene to prevent unexpected log messages which would trigger a failure
                RegisteredScenes = new List<string>() { SceneManager.GetActiveScene().name }
            };

            // Set dummy transport that does nothing
            networkManager.NetworkConfig.NetworkTransport = transport;

            // Replace the real message handler with a dummy one that just prints a result
            networkManager.MessageHandler = new DummyMessageHandler();

            using (var inputBuffer = new NetworkBuffer())
            {
                // Start server since pre-message-handler passes IsServer & IsClient checks
                networkManager.StartServer();

                // Disable batching to make the RPCs come straight through
                // This has to be done post start
                networkManager.MessageQueueContainer.EnableBatchedRpcs(false);

                // Should cause log (server only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleConnectionRequest));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionRequest, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionApproved, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObject, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObject, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SwitchScene, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ChangeOwner, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObjects, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObjects, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (client only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.TimeSync, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleNetworkVariableDelta));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NetworkVariableDelta, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleUnnamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleNamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleClientSwitchSceneCompleted));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ClientSwitchSceneCompleted, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleNetworkLog));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ServerLog, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.MessageReceiveQueueItem));
                networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(inputBuffer.GetBuffer(), 0, (int)inputBuffer.Length), 0);

                    // Should not cause log (client only)
                networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(inputBuffer.GetBuffer(), 0, (int)inputBuffer.Length), 0);


                // Stop server to trigger full shutdown
                networkManager.StopServer();

                // Replace the real message handler with a dummy one that just prints a result
                networkManager.MessageHandler = new DummyMessageHandler();

                // Start client since pre-message-handler passes IsServer & IsClient checks
                networkManager.StartClient();

                // Disable batching to make the RPCs come straight through
                // This has to be done post start (and post restart since the queue container is reset)
                networkManager.MessageQueueContainer.EnableBatchedRpcs(false);

                // Should not cause log (server only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionRequest, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleConnectionApproved));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ConnectionApproved, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleAddObject));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObject, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleDestroyObject));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObject, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleSwitchScene));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.SwitchScene, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleChangeOwner));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ChangeOwner, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleAddObjects));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.CreateObjects, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleDestroyObjects));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.DestroyObjects, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleTimeSync));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.TimeSync, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleNetworkVariableDelta));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NetworkVariableDelta, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleUnnamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.UnnamedMessage, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should cause log (server and client)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.HandleNamedMessage));
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.NamedMessage, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (server only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ClientSwitchSceneCompleted, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (server only)
                using (var messageStream = MessagePacker.WrapMessage(MessageQueueContainer.MessageType.ServerLog, inputBuffer))
                {
                    networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length), 0);
                }

                // Should not cause log (server only)
                networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(inputBuffer.GetBuffer(), 0, (int)inputBuffer.Length), 0);

                    // Should cause log (client only)
                LogAssert.Expect(LogType.Log, nameof(MessageHandlerReceivedMessageServerClient) + " " + nameof(DummyMessageHandler.MessageReceiveQueueItem));
                networkManager.HandleIncomingData(0, NetworkChannel.Internal, new ArraySegment<byte>(inputBuffer.GetBuffer(), 0, (int)inputBuffer.Length), 0);

                    // Full cleanup
                networkManager.StopClient();
            }

            // Ensure no missmatches with expectations
            LogAssert.NoUnexpectedReceived();

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }
    }

    internal class DummyMessageHandler : IInternalMessageHandler
    {
        public NetworkManager NetworkManager { get; }

        public void HandleConnectionRequest(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleConnectionRequest));

        public void HandleConnectionApproved(ulong clientId, Stream stream, float receiveTime) => VerifyCalled(nameof(HandleConnectionApproved));

        public void HandleAddObject(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleAddObject));

        public void HandleDestroyObject(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleDestroyObject));

        public void HandleSwitchScene(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleSwitchScene));

        public void HandleClientSwitchSceneCompleted(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleClientSwitchSceneCompleted));

        public void HandleChangeOwner(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleChangeOwner));

        public void HandleAddObjects(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleAddObjects));

        public void HandleDestroyObjects(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleDestroyObjects));

        public void HandleTimeSync(ulong clientId, Stream stream, float receiveTime) => VerifyCalled(nameof(HandleTimeSync));

        public void HandleNetworkVariableDelta(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleNetworkVariableDelta));

        public void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType,
            NetworkChannel receiveChannel) => VerifyCalled(nameof(MessageReceiveQueueItem));

        public void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType) => VerifyCalled(nameof(MessageReceiveQueueItem));

        public void HandleUnnamedMessage(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleUnnamedMessage));

        public void HandleNamedMessage(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleNamedMessage));

        public void HandleNetworkLog(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleNetworkLog));

        public void HandleAllClientsSwitchSceneCompleted(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleAllClientsSwitchSceneCompleted));

        private void VerifyCalled(string method)
        {
            Debug.Log(nameof(NetworkManagerMessageHandlerTests.MessageHandlerReceivedMessageServerClient) + " " + method);
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
