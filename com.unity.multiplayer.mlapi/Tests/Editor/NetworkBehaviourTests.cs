using NUnit.Framework;
using UnityEngine;

namespace MLAPI.EditorTests
{
    public class NetworkBehaviourPropertyTests
    {
        [Test]
        public void HasNetworkObjectTest()
        {
            var gameObject = new GameObject(nameof(NetworkObject));
            var networkBehaviour = gameObject.AddComponent<EmptyNetworkBehaviour>();
            
            Assert.That(networkBehaviour.HasNetworkObject, Is.False);

            var networkObject = gameObject.AddComponent<NetworkObject>();
            
            Assert.That(networkBehaviour.HasNetworkObject, Is.True);
            
            Object.DestroyImmediate(networkObject);
            
            Assert.That(networkBehaviour.HasNetworkObject, Is.False);
        }

        public class EmptyNetworkBehaviour : NetworkBehaviour
        {
            
        }
    }
}
