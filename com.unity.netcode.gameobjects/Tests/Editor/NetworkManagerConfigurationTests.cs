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

        public enum NetworkObjectPlacement
        {
            Root,   // Added to the same root GameObject
            Child   // Added to a child GameObject
        }

        [Test]
        public void NetworkObjectNotAllowed([Values] NetworkObjectPlacement networkObjectPlacement)
        {
            var gameObject = new GameObject(nameof(NetworkManager));
            var targetforNetworkObject = gameObject;

            if (networkObjectPlacement == NetworkObjectPlacement.Child)
            {
                var childGameObject = new GameObject($"{nameof(NetworkManager)}-Child");
                childGameObject.transform.parent = targetforNetworkObject.transform;
                targetforNetworkObject = childGameObject;
            }

            var networkManager = gameObject.AddComponent<NetworkManager>();

            // The error message we should expect
            var messageToCheck = $"A {nameof(GameObject)} cannot have both a {nameof(NetworkManager)} and {nameof(NetworkObject)} assigned to it.";

            // Trap for the nested NetworkManager exception
            LogAssert.Expect(LogType.Error, messageToCheck);

            var networkObject = targetforNetworkObject.AddComponent<NetworkObject>();

            // Since this is an in-editor test, we must force this invocation
            NetworkManagerHelper.Singleton.CheckAndNotifyUserNetworkObjectRemoved(networkManager, true);

            // Clean up
            Object.DestroyImmediate(gameObject);
        }
    }
}
