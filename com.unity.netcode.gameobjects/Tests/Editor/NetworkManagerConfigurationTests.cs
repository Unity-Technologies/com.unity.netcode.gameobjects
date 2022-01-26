using NUnit.Framework;
using UnityEngine;
using Unity.Netcode.Editor;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    public class NetworkManagerConfigurationTests
    {
        /// <summary>
        /// Does a simple check to make sure the nested network manager will
        /// notify the user when in the editor.  This is just a unit test to
        /// validate this is functioning
        /// </summary>
        [Test]
        public void NestedNetworkManagerCheck()
        {
            var parent = new GameObject("ParentObject");
            var networkManagerObject = new GameObject(nameof(NestedNetworkManagerCheck));
            var networkManager = networkManagerObject.AddComponent<NetworkManager>();

            // Make our NetworkManager's GameObject nested
            networkManagerObject.transform.parent = parent.transform;

            // Pre-generate the error message we are expecting to see
            var messageToCheck = NetworkManager.GenerateNestedNetworkManagerMessage(networkManagerObject.transform);

            // Trap for the nested NetworkManager exception
            LogAssert.Expect(LogType.Error, messageToCheck);

            // Since this is an in-editor test, we must force this invocation
            NetworkManagerHelper.Singleton.NotifyUserOfNestedNetworkManager(networkManager, false, true);

            // Clean up
            Object.DestroyImmediate(parent);
        }
    }
}
