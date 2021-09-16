using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Serialization
{
    /// <summary>
    /// Unit tests to test:
    /// - Serializing NetworkObject to NetworkObjectReference
    /// - Deserializing NetworkObjectReference to NetworkObject
    /// - Implicit operators of NetworkObjectReference
    /// </summary>
    public class NetworkBehaviourReferenceTests : IDisposable
    {
        private class TestNetworkBehaviour : NetworkBehaviour
        {
            public NetworkVariable<NetworkBehaviourReference> TestVariable = new NetworkVariable<NetworkBehaviourReference>();

            public TestNetworkBehaviour RpcReceivedBehaviour;

            [ServerRpc]
            public void SendReferenceServerRpc(NetworkBehaviourReference value)
            {
                RpcReceivedBehaviour = (TestNetworkBehaviour)value;
            }
        }

        [UnityTest]
        public IEnumerator TestRpc()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            var testNetworkBehaviour = networkObjectContext.Object.gameObject.AddComponent<TestNetworkBehaviour>();
            networkObjectContext.Object.Spawn();

            using var otherObjectContext = UnityObjectContext.CreateNetworkObject();
            otherObjectContext.Object.Spawn();

            testNetworkBehaviour.SendReferenceServerRpc(new NetworkBehaviourReference(testNetworkBehaviour));

            // wait for rpc completion
            float t = 0;
            while (testNetworkBehaviour.RpcReceivedBehaviour == null)
            {
                t += Time.deltaTime;
                if (t > 5f)
                {
                    new AssertionException("RPC with NetworkBehaviour reference hasn't been received");
                }

                yield return null;
            }

            // validate
            Assert.AreEqual(testNetworkBehaviour, testNetworkBehaviour.RpcReceivedBehaviour);
        }

        [UnityTest]
        public IEnumerator TestRpcImplicitNetworkBehaviour()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            var testNetworkBehaviour = networkObjectContext.Object.gameObject.AddComponent<TestNetworkBehaviour>();
            networkObjectContext.Object.Spawn();

            using var otherObjectContext = UnityObjectContext.CreateNetworkObject();
            otherObjectContext.Object.Spawn();

            testNetworkBehaviour.SendReferenceServerRpc(testNetworkBehaviour);

            // wait for rpc completion
            float t = 0;
            while (testNetworkBehaviour.RpcReceivedBehaviour == null)
            {
                t += Time.deltaTime;
                if (t > 5f)
                {
                    new AssertionException("RPC with NetworkBehaviour reference hasn't been received");
                }

                yield return null;
            }

            // validate
            Assert.AreEqual(testNetworkBehaviour, testNetworkBehaviour.RpcReceivedBehaviour);
        }

        [Test]
        public void TestNetworkVariable()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            var testNetworkBehaviour = networkObjectContext.Object.gameObject.AddComponent<TestNetworkBehaviour>();
            networkObjectContext.Object.Spawn();

            using var otherObjectContext = UnityObjectContext.CreateNetworkObject();
            otherObjectContext.Object.Spawn();

            // check default value is null
            Assert.IsNull((NetworkBehaviour)testNetworkBehaviour.TestVariable.Value);

            testNetworkBehaviour.TestVariable.Value = testNetworkBehaviour;

            Assert.AreEqual((NetworkBehaviour)testNetworkBehaviour.TestVariable.Value, testNetworkBehaviour);
        }

        [Test]
        public void FailSerializeNonSpawnedNetworkObject()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            var component = networkObjectContext.Object.gameObject.AddComponent<TestNetworkBehaviour>();

            Assert.Throws<ArgumentException>(() =>
            {
                NetworkBehaviourReference outReference = component;
            });
        }

        [Test]
        public void FailSerializeGameObjectWithoutNetworkObject()
        {
            using var gameObjectContext = UnityObjectContext.CreateGameObject();
            var component = gameObjectContext.Object.gameObject.AddComponent<TestNetworkBehaviour>();

            Assert.Throws<ArgumentException>(() =>
            {
                NetworkBehaviourReference outReference = component;
            });
        }

        [Test]
        public void FailSerializeNullBehaviour()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                NetworkBehaviourReference outReference = null;
            });
        }

        public void Dispose()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        public NetworkBehaviourReferenceTests()
        {
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out _);
        }
    }
}
