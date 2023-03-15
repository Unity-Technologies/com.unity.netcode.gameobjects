using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    public class NetworkObjectTests
    {
        [Test]
        public void NetworkManagerOverrideTest()
        {
            // Create "bait"
            var singletonNetworkManager = new GameObject(nameof(NetworkManager)).AddComponent<NetworkManager>();
            singletonNetworkManager.SetSingleton();

            // Create override
            var networkManager = new GameObject(nameof(NetworkManager)).AddComponent<NetworkManager>();

            // NetworkObject
            var gameObject = new GameObject(nameof(NetworkManagerOverrideTest));
            var networkObject = gameObject.AddComponent<NetworkObject>();

            // Set override
            networkObject.NetworkManagerOwner = networkManager;

            Debug.Assert(networkObject.NetworkManager == networkManager);

            Object.DestroyImmediate(singletonNetworkManager.gameObject);
            Object.DestroyImmediate(networkManager.gameObject);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void GetBehaviourIndexNone(int index)
        {
            var gameObject = new GameObject(nameof(GetBehaviourIndexNone));
            var networkObject = gameObject.AddComponent<NetworkObject>();

            LogAssert.Expect(LogType.Error, new Regex(".*out of bounds.*"));

            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex((ushort)index), Is.Null);

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GetBehaviourIndexOne()
        {
            var gameObject = new GameObject(nameof(GetBehaviourIndexOne));
            var networkObject = gameObject.AddComponent<NetworkObject>();
            var networkBehaviour = gameObject.AddComponent<EmptyNetworkBehaviour>();

            LogAssert.Expect(LogType.Error, new Regex(".*out of bounds.*"));

            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(0), Is.EqualTo(networkBehaviour));
            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(1), Is.Null);

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        public class EmptyNetworkBehaviour : NetworkBehaviour
        {

        }
    }
}
