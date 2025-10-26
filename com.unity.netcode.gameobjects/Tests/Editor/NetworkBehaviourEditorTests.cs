using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.EditorTests
{
    internal class NetworkBehaviourEditorTests
    {
        [Test]
        public void HasNetworkObjectTest()
        {
            var gameObject = new GameObject(nameof(HasNetworkObjectTest));
            var networkBehaviour = gameObject.AddComponent<EmptyNetworkBehaviour>();

            // Ensure GetTypeName returns the correct value
            Assert.AreEqual(nameof(EmptyNetworkBehaviour), networkBehaviour.__getTypeName());

            Assert.That(networkBehaviour.HasNetworkObject, Is.False);

            var networkObject = gameObject.AddComponent<NetworkObject>();

            Assert.That(networkBehaviour.HasNetworkObject, Is.True);

            Object.DestroyImmediate(networkObject);

            Assert.That(networkBehaviour.HasNetworkObject, Is.False);

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AccessNetworkObjectTest()
        {
            var gameObject = new GameObject(nameof(AccessNetworkObjectTest));
            var networkBehaviour = gameObject.AddComponent<EmptyNetworkBehaviour>();

            Assert.That(networkBehaviour.NetworkObject, Is.Null);

            var networkObject = gameObject.AddComponent<NetworkObject>();

            Assert.That(networkBehaviour.NetworkObject, Is.EqualTo(networkObject));

            Object.DestroyImmediate(networkObject);

            Assert.That(networkBehaviour.NetworkObject, Is.Null);

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AccessNetworkObjectTestInDerivedClassWithOverrideFunctions()
        {
            var gameObject = new GameObject(nameof(AccessNetworkObjectTestInDerivedClassWithOverrideFunctions));
            var networkBehaviour = gameObject.AddComponent<DerivedNetworkBehaviour>();

            Assert.AreEqual(nameof(DerivedNetworkBehaviour), networkBehaviour.__getTypeName());

            var networkObject = gameObject.AddComponent<NetworkObject>();

            Assert.That(networkBehaviour.NetworkObject, Is.EqualTo(networkObject));

            Object.DestroyImmediate(networkObject);

            Assert.That(networkBehaviour.NetworkObject, Is.Null);

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator RpcShouldNoopWhenInvokedWithoutANetworkManagerSession()
        {
            var noNetworkError = "Rpc methods can only be invoked after starting the NetworkManager!";
            var gameObject = new GameObject(nameof(AccessNetworkObjectTestInDerivedClassWithOverrideFunctions));
            var networkBehaviour = gameObject.AddComponent<RpcNetworkBehaviour>();

            // No networkManager exists so error should be logged
            LogAssert.Expect(LogType.Error, noNetworkError);
            networkBehaviour.NoNetworkRpc();

            // Ensure RPC was not invoked locally
            yield return null;
            Assert.That(networkBehaviour.RpcWasInvoked, Is.False);

            var networkManager = gameObject.AddComponent<NetworkManager>();
            networkManager.SetSingleton();

            LogAssert.Expect(LogType.Error, noNetworkError);
            networkBehaviour.NoNetworkRpc();

            // Ensure RPC was not invoked locally
            yield return null;
            Assert.That(networkBehaviour.RpcWasInvoked, Is.False);

            Object.DestroyImmediate(gameObject);
        }

        // Note: in order to repro https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1078
        // this child class must be defined before its parent to assure it is processed first by ILPP
        internal class DerivedNetworkBehaviour : EmptyNetworkBehaviour
        {
        }

        internal class EmptyNetworkBehaviour : NetworkBehaviour
        {
        }

        internal class RpcNetworkBehaviour : NetworkBehaviour
        {
            public bool RpcWasInvoked;

            [Rpc(SendTo.Everyone)]
            public void NoNetworkRpc()
            {
                RpcWasInvoked = true;
            }
        }
    }
}
