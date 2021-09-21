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
        /// <param name="messageType">The type of the message being sent</param>
        /// <param name="delivery"></param>
        void OnBeforeSendMessage(ulong clientId, Type messageType, NetworkDelivery delivery);

        /// <summary>
        /// Called after an individual message is sent.
        /// </summary>
        /// <param name="clientId">The destination clientId</param>
        /// <param name="messageType">The type of the message being sent</param>
        /// <param name="delivery"></param>
        /// <param name="messageSizeBytes">Number of bytes in the message, not including the message header</param>
        void OnAfterSendMessage(ulong clientId, Type messageType, NetworkDelivery delivery, int messageSizeBytes);

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
        /// <returns></returns>
        bool OnVerifyCanReceive(ulong senderId, Type messageType);
    }
}
