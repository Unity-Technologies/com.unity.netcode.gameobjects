using System;
using System.Collections;
using System.Collections.Generic;


namespace MLAPI.RuntimeTests
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using NUnit.Framework;
    using MLAPI.Messaging;
    using MLAPI.SceneManagement;
    using MLAPI.Transports.UNET;
    using MLAPI.Serialization;


    /// <summary>
    /// The RpcQueue unit tests to validate:
    /// - Sending and Receiving pipeline to validate that both sending and receiving pipelines are functioning properly.
    /// - Usage of the ServerRpcParams.Send.UpdateStage and ClientRpcParams.Send.UpdateStage functionality.
    /// - Rpcs receive will be invoked at the appropriate NetworkUpdateStage.
    /// Requires: RpcPipelineTestComponent
    /// </summary>
    public class RpcQueueTests
    {
        private NetworkManager m_NetworkManager;

        /// <summary>
        /// Tests the egress and ingress RPC queue functionality
        /// ** This does not include any of the MLAPI to Transport code **
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator TestNetworkUpdateStages()
        {
#if UNITY_2020_2_OR_NEWER // Disabling this test on 2019.4 due to ILPP issues on Yamato CI/CD runs
            var networkManagerObject = new GameObject(nameof(NetworkManager));
            m_NetworkManager = networkManagerObject.AddComponent<NetworkManager>();
            var unetTransport = networkManagerObject.AddComponent<UNetTransport>();
            m_NetworkManager.NetworkConfig = new Configuration.NetworkConfig
            {
                CreatePlayerPrefab = false,
                AllowRuntimeSceneChanges = true,
                EnableSceneManagement = false
            };
            unetTransport.ConnectAddress = "127.0.0.1";
            unetTransport.ConnectPort = 7777;
            unetTransport.ServerListenPort = 7777;
            unetTransport.MessageBufferSize = 65535;
            unetTransport.MaxConnections = 100;
            unetTransport.MessageSendMode = UNetTransport.SendMode.Immediately;
            m_NetworkManager.NetworkConfig.NetworkTransport = unetTransport;

            var currentActiveScene = SceneManager.GetActiveScene();
            var instantiatedNetworkManager = false;
            var testsAreComplete = false;
            var testsAreValidated = false;
            var exceededMaximumStageIterations = false;

            //Add our test scene name
            NetworkSceneManager.AddRuntimeSceneName(currentActiveScene.name, 0);

            //Create the player object that we will spawn as a host
            var playerObject = new GameObject("RpcTestObject");
            playerObject.AddComponent<NetworkObject>();
            var rpcPipelineTestComponent = playerObject.AddComponent<NetworkUpdateStagesComponent>();

            instantiatedNetworkManager = true;
            Debug.Log("NetworkManager Instantiated.");

            //Start as host mode as loopback only works in hostmode
            NetworkManager.Singleton.StartHost();
            Debug.Log("Host Started.");

            //Start testing
            rpcPipelineTestComponent.EnableTesting = true;

            Debug.Log("Running TestNetworkUpdateStages: ");

            //Wait for the rpc pipeline test to complete or if we exceeded the maximum iterations bail
            while (!testsAreComplete && !exceededMaximumStageIterations)
            {
                //Wait for 20ms
                yield return new WaitForSeconds(0.02f);

                testsAreComplete = rpcPipelineTestComponent.IsTestComplete();
                exceededMaximumStageIterations = rpcPipelineTestComponent.ExceededMaxIterations();
            }

            if (!exceededMaximumStageIterations)
            {
                testsAreValidated = rpcPipelineTestComponent.ValidateUpdateStages();
            }

            //Stop testing
            rpcPipelineTestComponent.EnableTesting = false;
            Debug.Log("RPC Queue Testing completed.");

            //Stop the host
            NetworkManager.Singleton.StopHost();

            //Shutdown the NetworkManager
            NetworkManager.Singleton.Shutdown();

            Debug.Log($"Exiting status => {nameof(testsAreComplete)}: {testsAreComplete} - {nameof(testsAreValidated)}: {testsAreValidated} - {nameof(instantiatedNetworkManager)}: {instantiatedNetworkManager} - {nameof(exceededMaximumStageIterations)}: {exceededMaximumStageIterations}");

            Assert.IsTrue(testsAreComplete && testsAreValidated && instantiatedNetworkManager);

            GameObject.DestroyImmediate(playerObject);
            GameObject.DestroyImmediate(networkManagerObject);
            yield return null;
#else
            yield return null;
#endif
        }

        /// <summary>
        /// This test validates that the RPC Queue can handle the maximum allowed UNet buffer size for a single RPC
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator GrowingRpcBufferSizes()
        {
#if UNITY_2020_2_OR_NEWER // Disabling this test on 2019.4 due to ILPP issues on Yamato CI/CD runs
            var networkManagerObject = new GameObject(nameof(NetworkManager));
            m_NetworkManager = networkManagerObject.AddComponent<NetworkManager>();
            var unetTransport = networkManagerObject.AddComponent<UNetTransport>();
            m_NetworkManager.NetworkConfig = new Configuration.NetworkConfig
            {
                CreatePlayerPrefab = false,
                AllowRuntimeSceneChanges = true,
                EnableSceneManagement = false
            };
            unetTransport.ConnectAddress = "127.0.0.1";
            unetTransport.ConnectPort = 7777;
            unetTransport.ServerListenPort = 7777;
            unetTransport.MessageBufferSize = 65535;
            unetTransport.MaxConnections = 100;
            unetTransport.MessageSendMode = UNetTransport.SendMode.Immediately;
            m_NetworkManager.NetworkConfig.NetworkTransport = unetTransport;

            var currentActiveScene = SceneManager.GetActiveScene();
            var instantiatedNetworkManager = false;
            var testsAreComplete = false;

            //Add our test scene name
            NetworkSceneManager.AddRuntimeSceneName(currentActiveScene.name, 0);

            //Create the player object that we will spawn as a host
            var playerObject = new GameObject("RpcTestObject");
            playerObject.AddComponent<NetworkObject>();
            var rpcPipelineTestComponent = playerObject.AddComponent<GrowingRpcBufferSizesComponent>();

            instantiatedNetworkManager = true;
            Debug.Log("NetworkManager Instantiated.");

            //Start as host mode as loopback only works in hostmode
            NetworkManager.Singleton.StartHost();
            Debug.Log("Host Started.");

            //Start Testing
            rpcPipelineTestComponent.EnableTesting = true;

            Debug.Log("Running RpcQueueTests.GrowingRpcBufferSizes: ");

            //Wait for the rpc pipeline test to complete or if we exceeded the maximum iterations bail
            while (!testsAreComplete)
            {
                //Wait for 20ms
                yield return new WaitForSeconds(0.02f);

                testsAreComplete = rpcPipelineTestComponent.IsTestComplete();
            }

            //Stop Testing
            rpcPipelineTestComponent.EnableTesting = false;
            Debug.Log("RpcQueueTests.GrowingRpcBufferSizes completed.");

            //Stop the host
            NetworkManager.Singleton.StopHost();

            //Shutdown the NetworkManager
            NetworkManager.Singleton.Shutdown();

            Debug.Log($"Exiting status => {nameof(testsAreComplete)}: {testsAreComplete} - {nameof(instantiatedNetworkManager)}: {instantiatedNetworkManager}");

            Assert.IsTrue(testsAreComplete && instantiatedNetworkManager);

            GameObject.DestroyImmediate(playerObject);
            GameObject.DestroyImmediate(networkManagerObject);
            yield return null;
#else
            yield return null;
#endif



        }

        [UnityTest]
        public IEnumerator RpcQueueContainerBaseLineTest()
        {
            bool InitializeNetworkManager = NetowrkManangerHelper.StartNetworkManager();
            Assert.IsTrue(InitializeNetworkManager);
            if (!InitializeNetworkManager)
            {
                yield return null;
            }

            if (NetowrkManangerHelper.StartHostSocketTasks == null)
            {
                Assert.IsNotNull(NetowrkManangerHelper.StartHostSocketTasks);
                yield return null;
            }

            foreach (Transports.Tasks.SocketTask task in NetowrkManangerHelper.StartHostSocketTasks.Tasks)
            {
                while (!task.IsDone)
                {
                    yield return new WaitForSeconds(0.02f);
                }
                if (task.SocketError != System.Net.Sockets.SocketError.Success)
                {
                    Assert.AreSame(task.SocketError, System.Net.Sockets.SocketError.Success);
                    yield return null;
                }
            }

            Debug.Log("Host Started.");

            //Create a testing rpcQueueContainer that doesn't get added to the network update loop so we don't try to send or process during the test
            var rpcQueueContainer = new RpcQueueContainer(true);

            //This test doesn't test the history frames functionality, it just tests the baseline functionality of the RpcQueueContainer
            rpcQueueContainer.Initialize(0);

            var MaxPsuedoRpcMessageSize = 1024;
            var MaxRpcEntries = 8;

            var PreCalculatedBufferValues = new List<byte>(MaxPsuedoRpcMessageSize);
            var MessageSizeChunks = MaxPsuedoRpcMessageSize >> 3;

            for (int i = 0; i <= MessageSizeChunks; i++)
            {
                PreCalculatedBufferValues.AddRange(BitConverter.GetBytes(Random.Range(0, UInt64.MaxValue)));
            }

            var IndexOffset = 0;
            ulong SenderNetworkId = 1;

            //Create ficticious list of clients to send to
            ulong[] PsuedoClients = new ulong[]{0,1,3,4};

            var PsuedoTimeStamp = Time.realtimeSinceStartup;
            var RandomGeneratedDataArray = PreCalculatedBufferValues.ToArray();

            //Testing outbound side of the RpcQueueContainer
            for (int i = 0; i < MaxRpcEntries; i++)
            {
                var writer = rpcQueueContainer.BeginAddQueueItemToFrame(RpcQueueContainer.QueueItemType.ClientRpc, Time.realtimeSinceStartup,Transports.NetworkChannel.DefaultMessage,
                        SenderNetworkId, PsuedoClients, RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

                //Write chunks of the randomly generated data to the "psuedo rpc entry"
                writer.WriteBytes(PreCalculatedBufferValues.ToArray(), MessageSizeChunks, IndexOffset);

                rpcQueueContainer.EndAddQueueItemToFrame(writer, RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

                //Increment our offset into our randomly generated data for next entry;
                IndexOffset = i * MessageSizeChunks;
            }

            //Now verify the data by obtaining the RpcQueueHistoryFrame we just wrote to
            var currentFrame = rpcQueueContainer.GetLoopBackHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

            //Reset our index offset
            IndexOffset = 0;
            int QueueEntryItemCount = 0;
            //Parse through the entries written to the current RpcQueueHistoryFrame
            var currentQueueItem = currentFrame.GetFirstQueueItem();
            while (currentQueueItem.QueueItemType != RpcQueueContainer.QueueItemType.None)
            {
                //Check to make sure the clients list is accurate
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(currentQueueItem.ClientNetworkIds[i], PsuedoClients[i]);
                }
                //Check to make sure the wrapper information is accurate for the entry
                Assert.AreEqual(currentQueueItem.NetworkId, SenderNetworkId);
                Assert.AreEqual(currentQueueItem.QueueItemType, RpcQueueContainer.QueueItemType.ClientRpc);
                Assert.AreEqual(currentQueueItem.UpdateStage, NetworkUpdateStage.PostLateUpdate);
                Assert.AreEqual(currentQueueItem.Timestamp, PsuedoTimeStamp);
                Assert.AreEqual(currentQueueItem.NetworkChannel, Transports.NetworkChannel.DefaultMessage);


                //Increment our offset into our randomly generated data for next entry;
                IndexOffset = QueueEntryItemCount * MessageSizeChunks;

                //Validate the data
                Assert.IsTrue(NetowrkManangerHelper.BuffersMatch(IndexOffset, MessageSizeChunks, currentQueueItem.MessageData.Array, RandomGeneratedDataArray));

                //Prepare for next queue item
                QueueEntryItemCount++;
                currentQueueItem = currentFrame.GetNextQueueItem();
            }
            yield return null;
        }


        public static class NetowrkManangerHelper
        {
            public static Transports.Tasks.SocketTasks StartHostSocketTasks { get; internal set; }
            public static GameObject PlayerObject { get; internal set; }
            public static NetworkObject PlayerNetworkObject { get; internal set; }
            public static bool StartNetworkManager()
            {
                var networkManagerObject = new GameObject(nameof(NetworkManager));
                var NetworkManagerComponent = networkManagerObject.AddComponent<NetworkManager>();
                if (NetworkManagerComponent == null)
                {
                    return false;
                }

                Debug.Log("NetworkManager Instantiated.");

                var unetTransport = networkManagerObject.AddComponent<UNetTransport>();

                NetworkManagerComponent.NetworkConfig = new Configuration.NetworkConfig
                {
                    CreatePlayerPrefab = false,
                    AllowRuntimeSceneChanges = true,
                    EnableSceneManagement = false
                };
                unetTransport.ConnectAddress = "127.0.0.1";
                unetTransport.ConnectPort = 7777;
                unetTransport.ServerListenPort = 7777;
                unetTransport.MessageBufferSize = 65535;
                unetTransport.MaxConnections = 100;
                unetTransport.MessageSendMode = UNetTransport.SendMode.Immediately;
                NetworkManagerComponent.NetworkConfig.NetworkTransport = unetTransport;

                var currentActiveScene = SceneManager.GetActiveScene();

                //Add our test scene name
                NetworkSceneManager.AddRuntimeSceneName(currentActiveScene.name, 0);

                //Create the player object that we will spawn as a host
                PlayerObject = new GameObject("RpcTestObject");
                PlayerNetworkObject = PlayerObject.AddComponent<NetworkObject>();

                if (!PlayerNetworkObject)
                {
                    return false;
                }

                //Start as host mode as loopback only works in hostmode
                StartHostSocketTasks = NetworkManager.Singleton.StartHost();

                return true;
            }


            public static bool BuffersMatch(int IndexOffset, long TargetSize, byte[] SourceArray, byte[] OriginalArry)
            {
                long LargeInt64Blocks = TargetSize >> 3; //Divide by 8

                //process by 8 byte blocks if we can
                for (long i = 0; i < LargeInt64Blocks; i++)
                {
                    if (BitConverter.ToInt64(SourceArray, IndexOffset) != BitConverter.ToInt64(OriginalArry, IndexOffset))
                    {
                        return false;
                    }
                    IndexOffset += 8;
                }

                long Offset = LargeInt64Blocks * 8;
                long Remainder = TargetSize - Offset;

                //4 byte block
                if (Remainder >= 4)
                {
                    if (BitConverter.ToInt32(SourceArray, IndexOffset) != BitConverter.ToInt32(OriginalArry, IndexOffset))
                    {
                        return false;
                    }
                    IndexOffset += 4;
                    Offset += 4;
                }

                //Remainder of bytes < 4
                if (TargetSize - Offset > 0)
                {
                    for (long i = 0; i < (TargetSize - Offset); i++)
                    {
                        if (SourceArray[IndexOffset + i] != OriginalArry[IndexOffset + i])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

    }


}
