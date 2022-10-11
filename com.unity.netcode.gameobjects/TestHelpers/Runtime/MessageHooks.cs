using System;

namespace Unity.Netcode.TestHelpers.Runtime
{
    internal class MessageHooks : INetworkHooks
    {
        public bool IsWaiting = true;
        public delegate bool MessageReceiptCheck(Type receivedMessageType);
        public MessageReceiptCheck ReceiptCheck;
        public delegate bool MessageHandleCheck(object receivedMessage);
        public MessageHandleCheck HandleCheck;

        public static bool CurrentMessageHasTriggerdAHook = false;

        public static bool CheckForMessageOfTypeHandled<T>(object receivedMessage) where T : INetworkMessage
        {
            return receivedMessage is T;
        }
        public static bool CheckForMessageOfTypeReceived<T>(Type receivedMessageType) where T : INetworkMessage
        {
            return receivedMessageType == typeof(T);
        }

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
        {
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
        {
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
            // The way the system works, it goes through all hooks and calls OnBeforeHandleMessage, then handles the message,
            // then goes thorugh all hooks and calls OnAfterHandleMessage.
            // This ensures each message only manages to activate a single message hook - because we know that only
            // one message will ever be handled between OnBeforeHandleMessage and OnAfterHandleMessage,
            // we can reset the flag here, and then in OnAfterHandleMessage, the moment the message matches a hook,
            // it'll flip this flag back on, and then other hooks will stop checking that message.
            // Without this flag, waiting for 10 messages of the same type isn't possible - all 10 hooks would get
            // tripped by the first message.
            CurrentMessageHasTriggerdAHook = false;
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
            if (!CurrentMessageHasTriggerdAHook && IsWaiting && (HandleCheck == null || HandleCheck.Invoke(messageType)))
            {
                IsWaiting = false;
                CurrentMessageHasTriggerdAHook = true;
            }
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery)
        {
            return true;
        }

        public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context)
        {
            return true;
        }

        public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            // The way the system works, it goes through all hooks and calls OnBeforeHandleMessage, then handles the message,
            // then goes thorugh all hooks and calls OnAfterHandleMessage.
            // This ensures each message only manages to activate a single message hook - because we know that only
            // one message will ever be handled between OnBeforeHandleMessage and OnAfterHandleMessage,
            // we can reset the flag here, and then in OnAfterHandleMessage, the moment the message matches a hook,
            // it'll flip this flag back on, and then other hooks will stop checking that message.
            // Without this flag, waiting for 10 messages of the same type isn't possible - all 10 hooks would get
            // tripped by the first message.
            CurrentMessageHasTriggerdAHook = false;
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            if (!CurrentMessageHasTriggerdAHook && IsWaiting && (HandleCheck == null || HandleCheck.Invoke(message)))
            {
                IsWaiting = false;
                CurrentMessageHasTriggerdAHook = true;
            }
        }
    }
}
