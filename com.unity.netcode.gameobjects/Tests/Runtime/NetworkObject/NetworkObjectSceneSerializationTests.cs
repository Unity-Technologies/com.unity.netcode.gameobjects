using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectSceneSerializationTests
    {

        /// <summary>
        /// The purpose behind this test is to assure that in-scene NetworkObjects
        /// that are serialized into a single stream (approval or switch scene this happens)
        /// will continue to be processed even if one of the NetworkObjects is invalid.
        /// </summary>
        [Test]
        public void NetworkObjectSceneSerializationFailure()
        {
            var networkObjectsToTest = new List<GameObject>();

            var writer = new FastBufferWriter(1300, Allocator.Temp, 4096000);
            var invalidNetworkObjectOffsets = new List<long>();
            var invalidNetworkObjectIdCount = new List<int>();
            var invalidNetworkObjects = new List<GameObject>();
            var invalidNetworkObjectFrequency = 3;
            using (writer)
            {
                // Construct 50 NetworkObjects
                for (int i = 0; i < 50; i++)
                {
                    // Inject an invalid NetworkObject every [invalidNetworkObjectFrequency] entry
                    if ((i % invalidNetworkObjectFrequency) == 0)
                    {
                        // Create the invalid NetworkObject
                        var gameObject = new GameObject($"InvalidTestObject{i}");

                        Assert.IsNotNull(gameObject);

                        var networkObject = gameObject.AddComponent<NetworkObject>();

                        Assert.IsNotNull(networkObject);

                        var networkVariableComponent = gameObject.AddComponent<NetworkBehaviourWithNetworkVariables>();
                        Assert.IsNotNull(networkVariableComponent);

                        // Add invalid NetworkObject's starting position before serialization to handle trapping for the Debug.LogError message
                        // that we know will be thrown
                        invalidNetworkObjectOffsets.Add(writer.Position);

                        networkObject.GlobalObjectIdHash = (uint)(i);
                        invalidNetworkObjectIdCount.Add(i);

                        invalidNetworkObjects.Add(gameObject);

                        writer.WriteValueSafe((int)networkObject.gameObject.scene.handle);
                        // Serialize the invalid NetworkObject
                        var sceneObject = networkObject.GetMessageSceneObject(0);
                        var prePosition = writer.Position;
                        sceneObject.Serialize(writer);

                        Debug.Log(
                            $"Invalid {nameof(NetworkObject)} Size {writer.Position - prePosition}");

                        // Now adjust how frequent we will inject invalid NetworkObjects
                        invalidNetworkObjectFrequency = Random.Range(2, 5);

                    }
                    else
                    {
                        // Create a valid NetworkObject
                        var gameObject = new GameObject($"TestObject{i}");

                        Assert.IsNotNull(gameObject);

                        var networkObject = gameObject.AddComponent<NetworkObject>();

                        var networkVariableComponent = gameObject.AddComponent<NetworkBehaviourWithNetworkVariables>();
                        Assert.IsNotNull(networkVariableComponent);

                        Assert.IsNotNull(networkObject);

                        networkObject.GlobalObjectIdHash = (uint)(i + 4096);

                        networkObjectsToTest.Add(gameObject);

                        writer.WriteValueSafe((int)networkObject.gameObject.scene.handle);

                        // Handle populating the scenes loaded list
                        var scene = networkObject.gameObject.scene;

                        if (!NetworkManagerHelper.NetworkManagerObject.SceneManager.ScenesLoaded.ContainsKey(
                            scene.handle))
                        {
                            NetworkManagerHelper.NetworkManagerObject.SceneManager.ScenesLoaded
                                .Add(scene.handle, scene);
                        }

                        // Since this is a unit test, we will fake the server to client handle lookup by just adding the same handle key and value
                        if (!NetworkManagerHelper.NetworkManagerObject.SceneManager.ServerSceneHandleToClientSceneHandle
                            .ContainsKey(networkObject.gameObject.scene.handle))
                        {
                            NetworkManagerHelper.NetworkManagerObject.SceneManager.ServerSceneHandleToClientSceneHandle
                                .Add(networkObject.gameObject.scene.handle, networkObject.gameObject.scene.handle);
                        }

                        // Serialize the valid NetworkObject
                        var sceneObject = networkObject.GetMessageSceneObject(0);
                        sceneObject.Serialize(writer);

                        if (!NetworkManagerHelper.NetworkManagerObject.SceneManager.ScenePlacedObjects.ContainsKey(
                            networkObject.GlobalObjectIdHash))
                        {
                            NetworkManagerHelper.NetworkManagerObject.SceneManager.ScenePlacedObjects.Add(
                                networkObject.GlobalObjectIdHash, new Dictionary<int, NetworkObject>());
                        }

                        // Add this valid NetworkObject into the ScenePlacedObjects list
                        NetworkManagerHelper.NetworkManagerObject.SceneManager
                            .ScenePlacedObjects[networkObject.GlobalObjectIdHash]
                            .Add(SceneManager.GetActiveScene().handle, networkObject);
                    }
                }

                var totalBufferSize = writer.Position;

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {

                    var networkObjectsDeSerialized = new List<NetworkObject>();
                    var currentLogLevel = NetworkManager.Singleton.LogLevel;
                    var invalidNetworkObjectCount = 0;
                    while (reader.Position != totalBufferSize)
                    {
                        // If we reach the point where we expect it to fail, then make sure we let TestRunner know it should expect this log error message
                        if (invalidNetworkObjectOffsets.Count > 0 &&
                            reader.Position == invalidNetworkObjectOffsets[0])
                        {
                            invalidNetworkObjectOffsets.RemoveAt(0);

                            // Turn off Network Logging to avoid other errors that we know will happen after the below LogAssert.Expect message occurs.
                            NetworkManager.Singleton.LogLevel = LogLevel.Nothing;

                            // Trap for this specific error message so we don't make Test Runner think we failed (it will fail on Debug.LogError)
                            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                                $"Failed to spawn {nameof(NetworkObject)} for Hash {invalidNetworkObjectIdCount[invalidNetworkObjectCount]}.");

                            invalidNetworkObjectCount++;
                        }


                        reader.ReadValueSafe(out int handle);
                        NetworkManagerHelper.NetworkManagerObject.SceneManager.SetTheSceneBeingSynchronized(handle);
                        var sceneObject = new NetworkObject.SceneObject();
                        sceneObject.Deserialize(reader);

                        var deserializedNetworkObject = NetworkObject.AddSceneObject(sceneObject, reader,
                            NetworkManagerHelper.NetworkManagerObject);
                        if (deserializedNetworkObject != null)
                        {
                            networkObjectsDeSerialized.Add(deserializedNetworkObject);
                        }
                        else
                        {
                            // Under this condition, we are expecting null (i.e. no NetworkObject instantiated)
                            // and will set our log level back to the original value to assure the valid NetworkObjects
                            // aren't causing any log Errors to occur
                            NetworkManager.Singleton.LogLevel = currentLogLevel;
                        }
                    }

                    // Now validate all NetworkObjects returned against the original NetworkObjects we created
                    // after they validate, destroy the objects
                    foreach (var entry in networkObjectsToTest)
                    {
                        var entryNetworkObject = entry.GetComponent<NetworkObject>();
                        Assert.IsTrue(networkObjectsDeSerialized.Contains(entryNetworkObject));
                        Object.Destroy(entry);
                    }
                }
            }

            // Destroy the invalid network objects
            foreach (var entry in invalidNetworkObjects)
            {
                Object.Destroy(entry);
            }
        }

        [SetUp]
        public void Setup()
        {
            // Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out NetworkManager networkManager, NetworkManagerHelper.NetworkManagerOperatingMode.None);
            networkManager.NetworkConfig.EnableSceneManagement = true;
            networkManager.StartHost();

        }

        [TearDown]
        public void TearDown()
        {
            // Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }
    }

    /// <summary>
    /// A simple test class that will provide varying NetworkBuffer stream sizes
    /// when the NetworkVariable is serialized
    /// </summary>
    public class NetworkBehaviourWithNetworkVariables : NetworkBehaviour
    {
        private const uint k_MinDataBlocks = 1;
        private const uint k_MaxDataBlocks = 64;

        public NetworkList<ulong> NetworkVariableData;

        private void Awake()
        {
            var dataBlocksAssigned = new List<ulong>();
            var numberDataBlocks = Random.Range(k_MinDataBlocks, k_MaxDataBlocks);
            for (var i = 0; i < numberDataBlocks; i++)
            {
                dataBlocksAssigned.Add((ulong)Random.Range(0.0f, float.MaxValue));
            }

            NetworkVariableData = new NetworkList<ulong>(dataBlocksAssigned);
        }
    }
}
