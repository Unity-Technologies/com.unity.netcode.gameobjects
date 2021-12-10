using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    public class NestedNetworkManagerTests
    {
        [UnityTest]
        public IEnumerator CheckNestedNetworkManager()
        {
            var parent = new GameObject("ParentObject");
            var networkManagerObject = new GameObject(nameof(CheckNestedNetworkManager));

            // Make our NetworkManager's GameObject nested
            networkManagerObject.transform.parent = parent.transform;

            // Pre-generate the error message we are expecting to see
            var messageToCheck = NetworkManager.GenerateNestedNetworkManagerMessage(networkManagerObject.transform);
            var transport = networkManagerObject.AddComponent<SIPTransport>();
            var networkManager = networkManagerObject.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig() { NetworkTransport = transport };
            // Trap for the nested NetworkManager exception
            LogAssert.Expect(LogType.Error, messageToCheck);

            yield return new WaitForSeconds(0.02f);

            // Clean up
            Object.Destroy(parent);

            yield return null;
        }
    }
}
