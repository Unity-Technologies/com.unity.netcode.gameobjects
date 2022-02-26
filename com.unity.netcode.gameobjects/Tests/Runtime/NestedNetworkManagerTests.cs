using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public class NestedNetworkManagerTests
    {
        [Test]
        public void CheckNestedNetworkManager()
        {
            var parent = new GameObject("ParentObject");
            var networkManagerObject = new GameObject(nameof(CheckNestedNetworkManager));

            var transport = networkManagerObject.AddComponent<SIPTransport>();
            var networkManager = networkManagerObject.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig() { NetworkTransport = transport };

            // Make our NetworkManager's GameObject nested
            networkManagerObject.transform.parent = parent.transform;

            // Generate the error message we are expecting to see
            var messageToCheck = NetworkManager.GenerateNestedNetworkManagerMessage(networkManagerObject.transform);

            // Trap for the nested NetworkManager exception
#if UNITY_EDITOR
            LogAssert.Expect(LogType.Error, messageToCheck);
#else
            LogAssert.Expect(LogType.Exception, $"Exception: {messageToCheck}");
#endif

            // Clean up
            Object.Destroy(parent);
        }
    }
}
