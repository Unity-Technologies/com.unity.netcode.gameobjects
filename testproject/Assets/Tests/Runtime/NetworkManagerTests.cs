using NUnit.Framework;
using Unity.Netcode.TestHelpers;


namespace TestProject.RuntimeTests
{
    public class NetworkManagerTests : NetcodeIntegrationTest
    {
        protected override int NbClients => 1;

        [Test]
        public void ValidateHostLocalClient()
        {
            Assert.IsTrue(m_ServerNetworkManager.LocalClient != null);
        }
    }
}
