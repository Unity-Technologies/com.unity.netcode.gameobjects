using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// This is where all of the SceneEventData specific tests should reside.
    /// </summary>
    public class SceneEventDataTests
    {
        /// <summary>
        /// This verifies that change from Allocator.TmpJob to Allocator.Persistent
        /// will not cause memory leak warning notifications if the scene event takes
        /// longer than 4 frames to complete.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator FastReaderAllocationTest()
        {
            var fastBufferWriter = new FastBufferWriter(1024, Unity.Collections.Allocator.Persistent);
            var networkManagerGameObject = new GameObject("NetworkManager - Host");

            var networkManager = networkManagerGameObject.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig()
            {
                ConnectionApproval = false,
                NetworkPrefabs = new List<NetworkPrefab>(),
                NetworkTransport = networkManagerGameObject.AddComponent<SIPTransport>(),
            };

            networkManager.StartHost();

            var sceneEventData = new SceneEventData(networkManager);
            sceneEventData.SceneEventType = SceneEventType.Load;
            sceneEventData.SceneHash = XXHash.Hash32("SomeRandomSceneName");
            sceneEventData.SceneEventProgressId = Guid.NewGuid();
            sceneEventData.LoadSceneMode = LoadSceneMode.Single;
            sceneEventData.SceneHandle = 32768;

            sceneEventData.Serialize(fastBufferWriter);
            var nativeArray = new Unity.Collections.NativeArray<byte>(fastBufferWriter.ToArray(), Unity.Collections.Allocator.Persistent);
            var fastBufferReader = new FastBufferReader(nativeArray, Unity.Collections.Allocator.Persistent, fastBufferWriter.ToArray().Length);

            var incomingSceneEventData = new SceneEventData(networkManager);
            incomingSceneEventData.Deserialize(fastBufferReader);

            // Wait for 30 frames
            var framesToWait = Time.frameCount + 30;
            yield return new WaitUntil(() => Time.frameCount > framesToWait);

            // As long as no errors occurred, the test verifies that
            incomingSceneEventData.Dispose();
            fastBufferReader.Dispose();
            nativeArray.Dispose();
            fastBufferWriter.Dispose();
            networkManager.Shutdown();
            Object.Destroy(networkManagerGameObject);
        }
    }
}
