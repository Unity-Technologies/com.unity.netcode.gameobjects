using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    internal class NestedNetworkManagerTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // TODO: [CmbServiceTests] if this test is deemed needed to test against the CMB server then update this test.
            NetcodeIntegrationTestHelpers.IgnoreIfServiceEnviromentVariableSet();
        }

        [Test]
        public void CheckNestedNetworkManager()
        {
            var parent = new GameObject("ParentObject");
            var networkManagerObject = new GameObject(nameof(CheckNestedNetworkManager));

            var unityTransport = networkManagerObject.AddComponent<UnityTransport>();
            var networkManager = networkManagerObject.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig() { NetworkTransport = unityTransport };

            // Make our NetworkManager's GameObject nested
            networkManagerObject.transform.parent = parent.transform;

            // Trap for the nested NetworkManager exception
            LogAssert.Expect(LogType.Error, new Regex("NetworkManager cannot be nested"));

            // Clean up
            Object.Destroy(parent);
        }
    }
}
