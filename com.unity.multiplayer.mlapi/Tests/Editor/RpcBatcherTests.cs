using System;
using System.Collections.Generic;
using System.Text;
using MLAPI.Messaging;
using MLAPI.Serialization;
using NUnit.Framework;

namespace MLAPI.Tests
{
    public class RpcBatcherTests
    {
        [Test]
        public void SendWithThreshold()
        {
            const int k_BatchThreshold = 256;
            const int k_QueueItemCount = 128;

            var sendBatcher = new RpcBatcher();
            var sendStreamQueue = new Queue<BitStream>();
            for (int i = 0; i < k_QueueItemCount; ++i)
            {
                var randomData = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());
                var queueItem = new RpcFrameQueueItem
                {
                    networkId = 123,
                    clientIds = new ulong[] { 123 },
                    channel = 123,
                    queueItemType = i % 2 == 0 ? RpcQueueContainer.QueueItemType.ServerRpc : RpcQueueContainer.QueueItemType.ClientRpc,
                    messageData = new ArraySegment<byte>(randomData, 0, randomData.Length)
                };
                sendBatcher.QueueItem(queueItem);
                sendBatcher.SendItems(k_BatchThreshold,
                    (networkId, sendStream) =>
                    {
                        var queueStream = new BitStream();
                        sendStream.Stream.CopyTo(queueStream);
                        sendStreamQueue.Enqueue(queueStream);
                    });
            }

            sendBatcher.SendItems( /* thresholdBytes = */ 0,
                (networkId, sendStream) =>
                {
                    var queueStream = new BitStream();
                    sendStream.Stream.CopyTo(queueStream);
                    sendStreamQueue.Enqueue(queueStream);
                });

            var recvBatcher = new RpcBatcher();
            var recvItemCounter = 0;
            foreach (var recvStream in sendStreamQueue)
            {
                recvStream.Position = 0;
                recvBatcher.ReceiveItems(recvStream, (stream, type, id, time) => ++recvItemCounter, default, default, default);
            }

            Assert.AreEqual(k_QueueItemCount, recvItemCounter);
        }

        [Test]
        public void SendWithoutThreshold()
        {
            const int k_BatchThreshold = 0;
            const int k_QueueItemCount = 128;

            // todo: mfatihmar (Unity)
        }
    }
}