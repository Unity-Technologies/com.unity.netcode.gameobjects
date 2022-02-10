using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public class NestedNetworkManagerTests
    {
        [UnityTest]
        public IEnumerator CheckNestedNetworkManager()
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

            yield return new WaitForSeconds(0.02f);

            // Clean up
            Object.Destroy(parent);

            yield return null;
        }
    }
}
