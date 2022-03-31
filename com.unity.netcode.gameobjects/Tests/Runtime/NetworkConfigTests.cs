using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkConfigTests
    {
        [UnityTest]
        public IEnumerator TestNetworkConfigMismatch()
        {
            NetcodeIntegrationTestHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            // Make a diff in the network config
            server.NetworkConfig.ProtocolVersion++;

            NetcodeIntegrationTestHelpers.Start(true, server, clients);

            // Wait for a full shutdown on the client
            yield return NetcodeIntegrationTestHelpers.WaitForCondition(() => !clients[0].IsListening, null, 10);
            Assert.False(clients[0].IsListening);

            // If exception is thrown, test will fail

            NetcodeIntegrationTestHelpers.Destroy();
        }
    }
}
