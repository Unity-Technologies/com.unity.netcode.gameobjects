using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class SnapshotRttTests
    {
        private const double k_Epsilon = 0.0001;

        [Test]
        public void TestBasicRtt()
        {
            var snapshot = new SnapshotSystem(default);
            var client1 = snapshot.GetConnectionRtt(0);

            client1.NotifySend(0, 0.0);
            client1.NotifySend(1, 10.0);

            client1.NotifyAck(1, 15.0);

            client1.NotifySend(2, 20.0);
            client1.NotifySend(3, 30.0);
            client1.NotifySend(4, 32.0);

            client1.NotifyAck(4, 38.0);
            client1.NotifyAck(3, 40.0);

            ConnectionRtt.Rtt ret = client1.GetRtt();
            Assert.True(ret.AverageSec < 7.0 + k_Epsilon);
            Assert.True(ret.AverageSec > 7.0 - k_Epsilon);
            Assert.True(ret.WorstSec < 10.0 + k_Epsilon);
            Assert.True(ret.WorstSec > 10.0 - k_Epsilon);
            Assert.True(ret.BestSec < 5.0 + k_Epsilon);
            Assert.True(ret.BestSec > 5.0 - k_Epsilon);

            // note: `last` latency is latest received Ack, not latest sent sequence.
            Assert.True(ret.LastSec < 10.0 + k_Epsilon);
            Assert.True(ret.LastSec > 10.0 - k_Epsilon);
        }

        [Test]
        public void TestEdgeCasesRtt()
        {
            var snapshot = new SnapshotSystem(NetworkManager.Singleton);
            var client1 = snapshot.GetConnectionRtt(0);
            var iterationCount = NetworkConfig.RttWindowSize * 3;
            var extraCount = NetworkConfig.RttWindowSize * 2;

            // feed in some messages
            for (var iteration = 0; iteration < iterationCount; iteration++)
            {
                client1.NotifySend(iteration, 25.0 * iteration);
            }
            // ack some random ones in there (1 out of each 9), always 7.0 later
            for (var iteration = 0; iteration < iterationCount; iteration += 9)
            {
                client1.NotifyAck(iteration, 25.0 * iteration + 7.0);
            }
            // ack some unused key, to check it doesn't throw off the values
            for (var iteration = iterationCount; iteration < iterationCount + extraCount; iteration++)
            {
                client1.NotifyAck(iteration, 42.0);
            }

            ConnectionRtt.Rtt ret = client1.GetRtt();
            Assert.True(ret.AverageSec < 7.0 + k_Epsilon);
            Assert.True(ret.AverageSec > 7.0 - k_Epsilon);
            Assert.True(ret.WorstSec < 7.0 + k_Epsilon);
            Assert.True(ret.WorstSec > 7.0 - k_Epsilon);
            Assert.True(ret.BestSec < 7.0 + k_Epsilon);
            Assert.True(ret.BestSec > 7.0 - k_Epsilon);
        }
    }
}
