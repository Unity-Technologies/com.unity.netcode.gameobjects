using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.EditorTests
{
    public class NetworkObjectTests
    {
        [Test]
        public void GetBehaviourIndexNone()
        {
            var gameObject = new GameObject(nameof(NetworkObject));
            var networkObject = gameObject.AddComponent<NetworkObject>();
            
            // TODO: Maybe not hardcode message?
            LogAssert.Expect(LogType.Error, $"[MLAPI] Behaviour index was out of bounds. Did you mess up the order of your {nameof(NetworkBehaviour)}s?");
            LogAssert.Expect(LogType.Error, $"[MLAPI] Behaviour index was out of bounds. Did you mess up the order of your {nameof(NetworkBehaviour)}s?");
            LogAssert.Expect(LogType.Error, $"[MLAPI] Behaviour index was out of bounds. Did you mess up the order of your {nameof(NetworkBehaviour)}s?");
            
            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(0), Is.Null);
            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(1), Is.Null);
            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(2), Is.Null);
        }

        [Test]
        public void GetBehaviourIndexOne()
        {
            var gameObject = new GameObject(nameof(NetworkObject));
            var networkObject = gameObject.AddComponent<NetworkObject>();
            var networkBehaviour = gameObject.AddComponent<EmptyNetworkBehaviour>();
            
            // TODO: Maybe not hardcode message?
            LogAssert.Expect(LogType.Error, $"[MLAPI] Behaviour index was out of bounds. Did you mess up the order of your {nameof(NetworkBehaviour)}s?");
            LogAssert.Expect(LogType.Error, $"[MLAPI] Behaviour index was out of bounds. Did you mess up the order of your {nameof(NetworkBehaviour)}s?");
            
            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(0), Is.EqualTo(networkBehaviour));
            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(1), Is.Null);
            Assert.That(networkObject.GetNetworkBehaviourAtOrderIndex(2), Is.Null);
        }
        
        public class EmptyNetworkBehaviour : NetworkBehaviour
        {
            
        }
    }
}
