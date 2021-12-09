using NUnit.Framework;
using Unity.Netcode.RuntimeTests;

namespace TestProject.RuntimeTests
{
    public class NetworkManagerTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        [Test]
        public void ValidateHostLocalClient()
        {
            Assert.IsTrue(m_ServerNetworkManager.LocalClient != null);
        }
    }
}
