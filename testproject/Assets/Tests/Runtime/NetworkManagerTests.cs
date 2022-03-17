using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;


namespace TestProject.RuntimeTests
{
    public class NetworkManagerTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        [Test]
        public void ValidateHostLocalClient()
        {
            Assert.IsTrue(m_ServerNetworkManager.LocalClient != null);
        }
    }
}
