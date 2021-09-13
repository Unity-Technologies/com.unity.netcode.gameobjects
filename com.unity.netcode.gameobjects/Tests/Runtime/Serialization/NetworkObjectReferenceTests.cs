using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests.Serialization
{
    /// <summary>
    /// Unit tests to test:
    /// - Serializing NetworkObject to NetworkObjectReference
    /// - Deserializing NetworkObjectReference to NetworkObject
    /// - Implicit operators of NetworkObjectReference
    /// </summary>
    public class NetworkObjectReferenceTests : IDisposable
    {
        private class TestNetworkBehaviour : NetworkBehaviour
        {
            public NetworkVariable<NetworkObjectReference> TestVariable = new NetworkVariable<NetworkObjectReference>();

            public NetworkObject RpcReceivedNetworkObject;

            public GameObject RpcReceivedGameObject;

            [ServerRpc]
            public void SendReferenceServerRpc(NetworkObjectReference value)
            {
                RpcReceivedGameObject = value;
                RpcReceivedNetworkObject = value;
            }
        }

        [Test]
        public void TestSerializeNetworkObject()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            networkObjectContext.Object.Spawn();
            using var outStream = PooledNetworkBuffer.Get();
            using var outWriter = PooledNetworkWriter.Get(outStream);
            using var inStream = PooledNetworkBuffer.Get();
            using var inReader = PooledNetworkReader.Get(inStream);

            // serialize
            var outSerializer = new NetworkSerializer(outWriter);
            NetworkObjectReference outReference = networkObjectContext.Object;
            outReference.NetworkSerialize(outSerializer);

            // deserialize
            NetworkObjectReference inReference = default;
            inStream.Write(outStream.ToArray());
            inStream.Position = 0;
            var inSerializer = new NetworkSerializer(inReader);
            inReference.NetworkSerialize(inSerializer);

            // validate
            Assert.NotNull((NetworkObject)inReference);
            Assert.AreEqual(inReference.NetworkObjectId, networkObjectContext.Object.NetworkObjectId);
            Assert.AreEqual(outReference, inReference);
            Assert.AreEqual(networkObjectContext.Object, (NetworkObject)inReference);
        }

        [Test]
        public void TestSerializeGameObject()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            networkObjectContext.Object.Spawn();
            using var outStream = PooledNetworkBuffer.Get();
            using var outWriter = PooledNetworkWriter.Get(outStream);
            using var inStream = PooledNetworkBuffer.Get();
            using var inReader = PooledNetworkReader.Get(inStream);

            // serialize
            var outSerializer = new NetworkSerializer(outWriter);
            NetworkObjectReference outReference = networkObjectContext.Object.gameObject;
            outReference.NetworkSerialize(outSerializer);

            // deserialize
            NetworkObjectReference inReference = default;
            inStream.Write(outStream.ToArray());
            inStream.Position = 0;
            var inSerializer = new NetworkSerializer(inReader);
            inReference.NetworkSerialize(inSerializer);
            GameObject gameObject = inReference;

            // validate
            Assert.AreEqual(outReference, inReference);
            Assert.AreEqual(networkObjectContext.Object.gameObject, gameObject);
        }

        [Test]
        public void TestTryGet()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            networkObjectContext.Object.Spawn();

            NetworkObjectReference networkObjectReference = networkObjectContext.Object;

            Assert.True(networkObjectReference.TryGet(out NetworkObject networkObject));
            Assert.NotNull(networkObject);
            networkObjectReference.TryGet(out NetworkObject result);
            Assert.AreEqual(networkObject, result);
        }

        [UnityTest]
        public IEnumerator TestRpc()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            var testNetworkBehaviour = networkObjectContext.Object.gameObject.AddComponent<TestNetworkBehaviour>();
            networkObjectContext.Object.Spawn();

            using var otherObjectContext = UnityObjectContext.CreateNetworkObject();
            otherObjectContext.Object.Spawn();

            testNetworkBehaviour.SendReferenceServerRpc(otherObjectContext.Object);

            // wait for rpc completion
            float t = 0;
            while (testNetworkBehaviour.RpcReceivedGameObject == null)
            {
                t += Time.deltaTime;
                if (t > 5f)
                {
                    new AssertionException("RPC with NetworkBehaviour reference hasn't been received");
                }

                yield return null;
            }

            // validate
            Assert.AreEqual(otherObjectContext.Object, testNetworkBehaviour.RpcReceivedNetworkObject);
            Assert.AreEqual(otherObjectContext.Object.gameObject, testNetworkBehaviour.RpcReceivedGameObject);
        }

        [Test]
        public void TestDespawn()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();
            networkObjectContext.Object.Spawn();
            var originalId = networkObjectContext.Object.NetworkObjectId;

            NetworkObjectReference networkObjectReference = networkObjectContext.Object;
            Assert.AreEqual(networkObjectContext.Object, (NetworkObject)networkObjectReference);

            networkObjectContext.Object.Despawn();
            Assert.IsFalse(networkObjectReference.TryGet(out NetworkObject _));

            networkObjectContext.Object.Spawn();

            // After spawning again the reference will still no longer work as it still points to the old object
            Assert.AreNotEqual(originalId, networkObjectContext.Object.NetworkObjectId);
            Assert.IsFalse(networkObjectReference.TryGet(out NetworkObject _));

            // creating a new reference will make it work again
            networkObjectReference = networkObjectContext.Object;
            Assert.AreEqual(networkObjectContext.Object, (NetworkObject)networkObjectReference);
        }

        [Test]
        public void FailSerializeNonSpawnedNetworkObject()
        {
            using var networkObjectContext = UnityObjectContext.CreateNetworkObject();

            Assert.Throws<ArgumentException>(() =>
            {
                NetworkObjectReference outReference = networkObjectContext.Object;
            });
        }

        [Test]
        public void FailSerializeGameObjectWithoutNetworkObject()
        {
            using var gameObjectContext = UnityObjectContext.CreateGameObject();

            Assert.Throws<ArgumentException>(() =>
            {
                NetworkObjectReference outReference = gameObjectContext.Object;
            });
        }

        [Test]
        public void FailSerializeNullNetworkObject()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                NetworkObjectReference outReference = (NetworkObject)null;
            });
        }

        [Test]
        public void FailSerializeNullGameObject()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                NetworkObjectReference outReference = (GameObject)null;
            });
        }

        public void Dispose()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        public NetworkObjectReferenceTests()
        {
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out _);
        }
    }

    /// <summary>
    /// Helper method for tests to create and destroy Unity Objects.
    /// </summary>
    /// <typeparam name="T">The type of Object this context incorporates.</typeparam>
    public class UnityObjectContext<T> : UnityObjectContext where T : Object
    {
        private T m_Object;

        internal UnityObjectContext(T unityObject, Object root)
            : base(root)
        {
            m_Object = unityObject;
        }

        public T Object => m_Object;
    }

    public class UnityObjectContext : IDisposable
    {
        private Object m_Root;

        protected UnityObjectContext(Object root)
        {
            m_Root = root;
        }

        public static UnityObjectContext<GameObject> CreateGameObject(string name = "")
        {
            var gameObject = new GameObject(name);
            return new UnityObjectContext<GameObject>(gameObject, gameObject);
        }

        public static UnityObjectContext<NetworkObject> CreateNetworkObject(string name = "")
        {
            var gameObject = new GameObject(name);
            var networkObject = gameObject.AddComponent<NetworkObject>();
            return new UnityObjectContext<NetworkObject>(networkObject, gameObject);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(m_Root);
        }
    }
}
