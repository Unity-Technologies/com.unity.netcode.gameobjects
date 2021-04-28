using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Spawning;
using MLAPI.Serialization;
using MLAPI.NetworkVariable;
using MLAPI.Serialization.Pooled;
using NUnit.Framework;


namespace MLAPI.RuntimeTests
{
    public class NetworkObjectSceneSerializationTests
    {
        /// <summary>
        /// The purpose behind this test is to assure that in-scene NetworkObjects
        /// that are serialized into a single stream (approval or switch scene this happens)
        /// will continue to be processed even if one of the NetworkObjects is invalid.
        /// </summary>
        [Test]
        public void NetworkObjectSceneSerializationFailureTest()
        {
            var networkObjectsToTest = new List<GameObject>();

            var pooledBuffer = PooledNetworkBuffer.Get();
            var writer = PooledNetworkWriter.Get(pooledBuffer);
            var reader = PooledNetworkReader.Get(pooledBuffer);
            long positionExpect = 0;

            //Construct 10 NetworkObjects, the one in the middle of the stream is going to be invalid
            for (int i = 0; i < 9; i++)
            {
                // Inject an invalid NetworkObject
                if (i == 5)
                {
                    // Create the invalid NetworkObject
                    var gameObject = new GameObject($"InvalidTestObject{i}");

                    Assert.IsNotNull(gameObject);

                    var networkObject = gameObject.AddComponent<NetworkObject>();

                    Assert.IsNotNull(networkObject);

                    var networkVariableComponent = gameObject.AddComponent<NetworkBehaviourWithNetworkVariables>();
                    Assert.IsNotNull(networkVariableComponent);

                    positionExpect = pooledBuffer.Position;
                    networkObject.SerializeSceneObject(writer, 0);
                }
                else
                {
                    //Create a valid NetworkObject
                    var gameObject = new GameObject($"TestObject{i}");

                    Assert.IsNotNull(gameObject);

                    var networkObject = gameObject.AddComponent<NetworkObject>();

                    var networkVariableComponent = gameObject.AddComponent<NetworkBehaviourWithNetworkVariables>();
                    Assert.IsNotNull(networkVariableComponent);

                    Assert.IsNotNull(networkObject);
                    
                    networkObject.GlobalObjectIdHash = (uint)(i + 10);

                    networkObjectsToTest.Add(gameObject);

                    networkObject.SerializeSceneObject(writer, 0);

                    NetworkManagerHelper.NetworkManagerObject.SpawnManager.PendingSoftSyncObjects.Add(networkObject.GlobalObjectIdHash, networkObject);
                }
            }

            var totalBufferSize = pooledBuffer.Position;
            //Reset the position for reading this information
            pooledBuffer.Position = 0;

            var networkObjectsDeSerialized = new List<NetworkObject>();
            var currentLogLevel = NetworkManager.Singleton.LogLevel;

            while (pooledBuffer.Position != totalBufferSize)
            {
                // If we reach the point where we expect it to fail, then make sure we let TestRunner know it should expect this log error message
                if (pooledBuffer.Position == positionExpect)
                {
                    // Turn off Network Logging to avoid other errors that we know will happen when the below LogAssert.Expect message occurs.
                    NetworkManager.Singleton.LogLevel = Logging.LogLevel.Nothing;
                    UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "Failed to spawn NetworkObject for Hash 0.");
                }
                var deserializedNetworkObject = NetworkObject.DeserializeSceneObject(pooledBuffer, reader, NetworkManagerHelper.NetworkManagerObject);
                if (deserializedNetworkObject != null)
                {
                    networkObjectsDeSerialized.Add(deserializedNetworkObject);
                }
                else
                {
                    // We are expecting null and will set our log level back to the original value
                    NetworkManager.Singleton.LogLevel = currentLogLevel;
                }
            }
            reader.Dispose();
            writer.Dispose();
            NetworkBufferPool.PutBackInPool(pooledBuffer);

            // Now validate all NetworkObjects returned against the original NetworkObjects we created
            foreach (var entry in networkObjectsToTest)
            {
                var entryNetworkObject = entry.GetComponent<NetworkObject>();
                Assert.IsTrue(networkObjectsDeSerialized.Contains(entryNetworkObject));
            }

        }


        [SetUp]
        public void Setup()
        {
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out NetworkManager networkManager,NetworkManagerHelper.NetworkManagerOperatingMode.None);
            networkManager.NetworkConfig.EnableSceneManagement = true;
            networkManager.StartHost();
        }


        [TearDown]
        public void TearDown()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

    }


    public class NetworkBehaviourWithNetworkVariables:NetworkBehaviour
    {
        public NetworkVariableVector4 Vector4ValueOne;
        public NetworkVariableVector4 Vector4ValueTwo;
        public NetworkVariableVector4 Vector4ValueThree;
        public NetworkVariableVector4 Vector4ValueFour;

        private void Start()
        {
            Vector4ValueOne = new NetworkVariableVector4(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
            Vector4ValueTwo = new NetworkVariableVector4(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
            Vector4ValueThree = new NetworkVariableVector4(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
            Vector4ValueFour = new NetworkVariableVector4(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
        }

    }
}
