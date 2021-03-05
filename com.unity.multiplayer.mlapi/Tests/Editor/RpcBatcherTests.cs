using System;
using System.Collections.Generic;
using System.Text;
using MLAPI.Messaging;
using MLAPI.Serialization;
using MLAPI.Transports;
using NUnit.Framework;

namespace MLAPI.EditorTests
{
    public class RpcBatcherTests
    {
        [Test]
        public void SendWithThreshold()
        {
            const int k_BatchThreshold = 256;
            const int k_QueueItemCount = 128;

            var sendBatcher = new RpcBatcher();
            var sendStreamQueue = new Queue<NetworkBuffer>();
            for (int i = 0; i < k_QueueItemCount; ++i)
            {
                var randomData = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());
                var queueItem = new RpcFrameQueueItem
                {
                    NetworkId = 123,
                    ClientNetworkIds = new ulong[] { 123 },
                    NetworkChannel = NetworkChannel.ChannelUnused + 123,
                    QueueItemType = i % 2 == 0 ? RpcQueueContainer.QueueItemType.ServerRpc : RpcQueueContainer.QueueItemType.ClientRpc,
                    MessageData = new ArraySegment<byte>(randomData, 0, randomData.Length)
                };
                sendBatcher.QueueItem(queueItem);
                sendBatcher.SendItems(k_BatchThreshold,
                    (networkId, sendStream) =>
                    {
                        var queueStream = new NetworkBuffer();
                        sendStream.Buffer.CopyTo(queueStream);
                        sendStreamQueue.Enqueue(queueStream);
                    });
            }

            // batch the rest
            sendBatcher.SendItems( /* thresholdBytes = */ 0,
                (networkId, sendStream) =>
                {
                    var queueStream = new NetworkBuffer();
                    sendStream.Buffer.CopyTo(queueStream);
                    sendStreamQueue.Enqueue(queueStream);
                });

            var recvBatcher = new RpcBatcher();
            var recvItemCounter = 0;
            foreach (var recvStream in sendStreamQueue)
            {
                recvStream.Position = 0;

                // todo: revisit
                // The following line is sub-optimal
                // The stream returned by SendItems() includes:
                // - 8 bits for the MLAPI message types.
                // ReceiveItems expects those to have been stripped by the receive code.
                // In order to replicate this behaviour, we'll read 8 bits before calling ReceiveItems()
                recvStream.ReadByte();

                recvBatcher.ReceiveItems(recvStream, (stream, type, id, time) => ++recvItemCounter, default, default, default);
            }

            Assert.AreEqual(k_QueueItemCount, recvItemCounter);
        }

        [Test]
        public void SendWithoutThreshold()
        {
            const int k_BatchThreshold = 0;
            const int k_QueueItemCount = 128;

            var sendBatcher = new RpcBatcher();
            var sendStreamQueue = new Queue<NetworkBuffer>();
            for (int i = 0; i < k_QueueItemCount; ++i)
            {
                var randomData = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());
                var queueItem = new RpcFrameQueueItem
                {
                    NetworkId = 123,
                    ClientNetworkIds = new ulong[] { 123 },
                    NetworkChannel = NetworkChannel.ChannelUnused + 123,
                    QueueItemType = i % 2 == 0 ? RpcQueueContainer.QueueItemType.ServerRpc : RpcQueueContainer.QueueItemType.ClientRpc,
                    MessageData = new ArraySegment<byte>(randomData, 0, randomData.Length)
                };
                sendBatcher.QueueItem(queueItem);
                sendBatcher.SendItems(k_BatchThreshold,
                    (networkId, sendStream) =>
                    {
                        var queueStream = new NetworkBuffer();
                        sendStream.Buffer.CopyTo(queueStream);
                        sendStreamQueue.Enqueue(queueStream);
                    });
            }

            // batch the rest
            sendBatcher.SendItems( /* thresholdBytes = */ 0,
                (networkId, sendStream) =>
                {
                    var queueStream = new NetworkBuffer();
                    sendStream.Buffer.CopyTo(queueStream);
                    sendStreamQueue.Enqueue(queueStream);
                });

            var recvBatcher = new RpcBatcher();
            var recvItemCounter = 0;
            foreach (var recvStream in sendStreamQueue)
            {
                recvStream.Position = 0;

                // todo: revisit
                // The following line is sub-optimal
                // The stream returned by SendItems() includes:
                // - 8 bits for the MLAPI message types.
                // ReceiveItems expects those to have been stripped by the receive code.
                // In order to replicate this behaviour, we'll read 8 bits before calling ReceiveItems()
                recvStream.ReadByte();

                recvBatcher.ReceiveItems(recvStream, (stream, type, id, time) => ++recvItemCounter, default, default, default);
            }

            Assert.AreEqual(k_QueueItemCount, recvItemCounter);
        }
    }
}