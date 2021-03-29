using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MLAPI.EditorTests
{
    public class NetworkBehaviourTests
    {
        [Test]
        public void HasNetworkObjectTest()
        {
            var gameObject = new GameObject(nameof(HasNetworkObjectTest));
            var networkBehaviour = gameObject.AddComponent<EmptyNetworkBehaviour>();

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

            // TODO: Do we really want to throw here?
            // Future API change: return null
            Assert.Throws<NullReferenceException>(() =>
            {
                var x = networkBehaviour.NetworkObject;
            });

            var networkObject = gameObject.AddComponent<NetworkObject>();

            Assert.That(networkBehaviour.NetworkObject, Is.EqualTo(networkObject));

            Object.DestroyImmediate(networkObject);

            // TODO: Do we really want to throw here?
            // Future API change: return null
            Assert.Throws<NullReferenceException>(() =>
            {
                var x = networkBehaviour.NetworkObject;
            });

            // Cleanup
            Object.DestroyImmediate(gameObject);
        }

        public class EmptyNetworkBehaviour : NetworkBehaviour
        {

        }
    }
}
