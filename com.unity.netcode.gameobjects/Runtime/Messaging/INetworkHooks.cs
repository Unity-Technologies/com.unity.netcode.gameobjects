using System;

namespace Unity.Netcode
{
    /// <summary>
    /// Used to react to different events in the messaging system. Primary use case is for
    /// collecting profiling data and metrics data. Additionally, it provides OnVerifyCanSend and OnVerifyCanReceive
    /// to allow for networking implementations to put limits on when certain messages can or can't be sent or received.
    /// </summary>
    internal interface INetworkHooks
    {
        /// <summary>
        /// Called before an individual message is sent.
        /// </summary>
        /// <param name="clientId">The destination clientId</param>
        /// <param name="message">The message being sent</param>
        /// <param name="delivery"></param>
        void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage;

        /// <summary>
        /// Called after an individual message is sent.
        /// </summary>
        /// <param name="clientId">The destination clientId</param>
        /// <param name="message">The message being sent</param>
        /// <param name="delivery"></param>
        /// <param name="messageSizeBytes">Number of bytes in the message, not including the message header</param>
        void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage;

        /// <summary>
        /// Called before an individual message is received.
        /// </summary>
        /// <param name="senderId">The source clientId</param>
        /// <param name="messageType">The type of the message being sent</param>
        /// <param name="messageSizeBytes">Number of bytes in the message, not including the message header</param>
        void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes);

        /// <summary>
        /// Called after an individual message is received.
        /// </summary>
        /// <param name="senderId">The source clientId</param>
        /// <param name="messageType">The type of the message being sent</param>
        /// <param name="messageSizeBytes">Number of bytes in the message, not including the message header</param>
        void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes);

        /// <summary>
        /// Called before a batch of messages is sent
        /// </summary>
        /// <param name="clientId">The destination clientId</param>
        /// <param name="messageCount">Number of messages in the batch</param>
        /// <param name="batchSizeInBytes">Number of bytes in the batch, including the batch header</param>
        /// <param name="delivery"></param>
        void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery);

        /// <summary>
        /// Called after a batch of messages is sent
        /// </summary>
        /// <param name="clientId">The destination clientId</param>
        /// <param name="messageCount">Number of messages in the batch</param>
        /// <param name="batchSizeInBytes">Number of bytes in the batch, including the batch header</param>
        /// <param name="delivery"></param>
        void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery);

        /// <summary>
        /// Called before a batch of messages is received
        /// </summary>
        /// <param name="senderId">The source clientId</param>
        /// <param name="messageCount">Number of messages in the batch</param>
        /// <param name="batchSizeInBytes">Number of bytes in the batch, including the batch header</param>
        void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes);

        /// <summary>
        /// Called after a batch of messages is received
        /// </summary>
        /// <param name="senderId">The source clientId</param>
        /// <param name="messageCount">Number of messages in the batch</param>
        /// <param name="batchSizeInBytes">Number of bytes in the batch, including the batch header</param>
        void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes);


        /// <summary>
        /// Called before a message is sent. If this returns false, the message will be discarded.
        /// </summary>
        /// <param name="destinationId">The destination clientId</param>
        /// <param name="messageType">The type of the message</param>
        /// <param name="delivery"></param>
        /// <returns></returns>
        bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery);

        /// <summary>
        /// Called before a message is received. If this returns false, the message will be discarded.
        /// </summary>
        /// <param name="senderId">The source clientId</param>
        /// <param name="messageType">The type of the message</param>
        /// <param name="messageContent">The FastBufferReader containing the message</param>
        /// <param name="context">The NetworkContext the message is being processed in</param>
        /// <returns></returns>
        bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context);

        /// <summary>
        /// Called after a message is serialized, but before it's handled.
        /// Differs from OnBeforeReceiveMessage in that the actual message object is passed and can be inspected.
        /// </summary>
        /// <param name="message">The message object</param>
        /// <param name="context">The network context the message is being ahandled in</param>
        /// <typeparam name="T"></typeparam>
        void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage;

        /// <summary>
        /// Called after a message is serialized and handled.
        /// Differs from OnAfterReceiveMessage in that the actual message object is passed and can be inspected.
        /// </summary>
        /// <param name="message">The message object</param>
        /// <param name="context">The network context the message is being ahandled in</param>
        /// <typeparam name="T"></typeparam>
        void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage;
    }
}
