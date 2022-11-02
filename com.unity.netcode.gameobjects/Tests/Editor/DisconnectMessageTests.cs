using NUnit.Framework;
using Unity.Collections;

namespace Unity.Netcode.EditorTests
{
    public class DisconnectMessageTests
    {
        [Test]
        public void EmptyDisconnectReason()
        {
            var networkContext = new NetworkContext();
            FastBufferWriter writer = new FastBufferWriter(20, Allocator.Temp, 20);
            DisconnectReasonMessage msg = new DisconnectReasonMessage();
            msg.Reason = string.Empty;
            msg.Serialize(writer);

            FastBufferReader fbr = new FastBufferReader(writer, Allocator.Temp);
            DisconnectReasonMessage recvMsg = new DisconnectReasonMessage();
            recvMsg.Deserialize(fbr, ref networkContext);

            Assert.IsEmpty(recvMsg.Reason);
        }

        [Test]
        public void DisconnectReason()
        {
            var networkContext = new NetworkContext();
            FastBufferWriter writer = new FastBufferWriter(20, Allocator.Temp, 20);
            DisconnectReasonMessage msg = new DisconnectReasonMessage();
            msg.Reason = "Foo";
            msg.Serialize(writer);

            FastBufferReader fbr = new FastBufferReader(writer, Allocator.Temp);
            DisconnectReasonMessage recvMsg = new DisconnectReasonMessage();
            recvMsg.Deserialize(fbr, ref networkContext);

            Assert.AreEqual("Foo", recvMsg.Reason);
        }

        [Test]
        public void DisconnectReasonTooLong()
        {
            var networkContext = new NetworkContext();
            FastBufferWriter writer = new FastBufferWriter(20, Allocator.Temp, 20);
            DisconnectReasonMessage msg = new DisconnectReasonMessage();
            msg.Reason = "ThisStringIsWayLongerThanTwentyBytes";
            msg.Serialize(writer);

            FastBufferReader fbr = new FastBufferReader(writer, Allocator.Temp);
            DisconnectReasonMessage recvMsg = new DisconnectReasonMessage();
            recvMsg.Deserialize(fbr, ref networkContext);

            Assert.IsEmpty(recvMsg.Reason);
        }
    }
}
