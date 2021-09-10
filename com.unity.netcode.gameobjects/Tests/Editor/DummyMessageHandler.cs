using System.IO;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    internal class DummyMessageHandler : IInternalMessageHandler
    {
        public NetworkManager NetworkManager { get; }

        public DummyMessageHandler(NetworkManager networkManager)
        {
            NetworkManager = networkManager;
        }

        public void HandleSceneEvent(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleSceneEvent));
        
        public void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType)
        {
            VerifyCalled(nameof(MessageReceiveQueueItem));
            if (NetworkManager)
            {
                // To actually process the message we have to add it to the inbound frame queue for the current update stage
                // and then process and flush the queue for the current update stage to actually get it to run through
                // MessageQueueContainer.ProcessMessage, which is where the actual code handling the message lives.
                // That's what will then call back into this for the others.
                var messageQueueContainer = NetworkManager.MessageQueueContainer;
                messageQueueContainer.AddQueueItemToInboundFrame(messageType, receiveTime, clientId, (NetworkBuffer)stream);
                messageQueueContainer.ProcessAndFlushMessageQueue(MessageQueueContainer.MessageQueueProcessingTypes.Receive, NetworkUpdateLoop.UpdateStage);
                messageQueueContainer.AdvanceFrameHistory(MessageQueueHistoryFrame.QueueFrameType.Inbound);
            }
        }

        public void HandleUnnamedMessage(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleUnnamedMessage));

        public void HandleNamedMessage(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleNamedMessage));

        public void HandleAllClientsSwitchSceneCompleted(ulong clientId, Stream stream) => VerifyCalled(nameof(HandleAllClientsSwitchSceneCompleted));

        private void VerifyCalled(string method)
        {
            Debug.Log(method);
        }
    }
}
