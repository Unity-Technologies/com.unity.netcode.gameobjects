using MLAPI;
using NUnit.Framework;

namespace MLAPI.EditorTests
{
    public class SnapshotRttTests
    {
        [Test]
        public void TestRtt()
        {
            var snapshot = new SnapshotSystem();
            var client1 = snapshot.Rtt(0);

            client1.NotifySend(0, 0.0);
            client1.NotifySend(1, 10.0);
            client1.NotifySend(2, 20.0);
            client1.NotifySend(3, 30.0);

            client1.NotifyAck(1, 15.0);
            client1.NotifyAck(3, 40.0);

            double epsilon = 0.0001;

            ClientRtt.Rtt ret = client1.GetRtt();
            Assert.True(ret.Average < 7.5 + epsilon);
            Assert.True(ret.Average > 7.5 - epsilon);
            Assert.True(ret.Worst < 10.0 + epsilon);
            Assert.True(ret.Worst > 10.0 - epsilon);
            Assert.True(ret.Best < 5.0 + epsilon);
            Assert.True(ret.Best > 5.0 - epsilon);
        }
    }
}
