using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using MLAPI.Messaging;

namespace MLAPI.RuntimeTests
{
    /// <summary>
    /// The RpcQueue unit tests validate:
    /// Maximum buffer size that can be sent (currently 1MB is the default maximum RpcQueueHistoryFrame size)
    /// That all RPCs invoke at the appropriate NetworkUpdateStage (Client and Server)
    /// A lower level RpcQueueContainer test that validates RpcQueueFrameItems after they have been put into the queue
    /// </summary>
    public class RpcQueueTests : IDisposable
    {
        /// <summary>
        /// Tests to make sure providing differen
        /// ** This does not include any of the MLAPI to Transport code **
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator UpdateStagesInvocation()
        {
 // Disabling this test on 2019.4 due to ILPP issues on Yamato CI/CD runs (UNITY_2020_2_OR_NEWER)
 // Adding the ability to test if we are running in editor local (UNIT_MANUAL_TESTING)
#if UNITY_2020_2_OR_NEWER || UNITY_MANUAL_TESTING
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager());

            Guid updateStagesTestId = NetworkManagerHelper.AddGameNetworkObject("UpdateStagesTest");
            var rpcPipelineTestComponent = NetworkManagerHelper.AddComponentToObject<NetworkUpdateStagesComponent>(updateStagesTestId);
            NetworkManagerHelper.SpawnNetworkObject(updateStagesTestId);
            var testsAreComplete = rpcPipelineTestComponent.IsTestComplete();
            var exceededMaximumStageIterations = rpcPipelineTestComponent.ExceededMaxIterations();

            //Start testing
            rpcPipelineTestComponent.EnableTesting = true;

            Debug.Log("Running TestNetworkUpdateStages: ");

            //Wait for the rpc pipeline test to complete or if we exceeded the maximum iterations bail
            while (!testsAreComplete && !exceededMaximumStageIterations)
            {
                //Wait for 20ms
                yield return new WaitForSeconds(0.02f);

                testsAreComplete = rpcPipelineTestComponent.IsTestComplete();
                Assert.IsFalse(rpcPipelineTestComponent.ExceededMaxIterations());
            }
            var testsAreValidated = rpcPipelineTestComponent.ValidateUpdateStages();

            //Stop testing
            rpcPipelineTestComponent.EnableTesting = false;

            Debug.Log($"Exiting status => {nameof(testsAreComplete)}: {testsAreComplete} - {nameof(testsAreValidated)}: {testsAreValidated} -{nameof(exceededMaximumStageIterations)}: {exceededMaximumStageIterations}");

            Assert.IsTrue(testsAreComplete && testsAreValidated);

            //Disable this so it isn't running any longer.
            rpcPipelineTestComponent.gameObject.SetActive(false);

            yield return null;
#else
            yield return null;
#endif
        }

        /// <summary>
        /// This tests the RPC Queue outbound and inbound buffer capabilities.
        /// It will send
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator BufferDataValidation()
        {
 // Disabling this test on 2019.4 due to ILPP issues on Yamato CI/CD runs (UNITY_2020_2_OR_NEWER)
 // Adding the ability to test if we are running in editor local (UNIT_MANUAL_TESTING)
#if UNITY_2020_2_OR_NEWER || UNITY_MANUAL_TESTING
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager());

            Guid gameObjectId = NetworkManagerHelper.AddGameNetworkObject("GrowingBufferObject");

            var growingRpcBufferSizeComponent = NetworkManagerHelper.AddComponentToObject<BufferDataValidationComponent>(gameObjectId);
            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);

            //Start Testing
            growingRpcBufferSizeComponent.EnableTesting = true;

            var testsAreComplete = growingRpcBufferSizeComponent.IsTestComplete();

            //Wait for the rpc pipeline test to complete or if we exceeded the maximum iterations bail
            while (!testsAreComplete)
            {
                //Wait for 20ms
                yield return new WaitForSeconds(0.02f);

                testsAreComplete = growingRpcBufferSizeComponent.IsTestComplete();
            }

            //Stop Testing
            growingRpcBufferSizeComponent.EnableTesting = false;

            //Just disable this once we are done.
            growingRpcBufferSizeComponent.gameObject.SetActive(false);

            Assert.IsTrue(testsAreComplete);

            yield return null;
#else
            yield return null;
#endif

        }


        [UnityTest]
        public IEnumerator RpcQueueContainerClass()
        {
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager());

            //Create a testing rpcQueueContainer that doesn't get added to the network update loop so we don't try to send or process during the test
            var rpcQueueContainer = new RpcQueueContainer(NetworkManager.Singleton, 0, true);

            //Make sure we set testing mode so we don't try to invoke rpcs
            rpcQueueContainer.SetTestingState(true);

            var maxRpcEntries = 8;
            var messageChunkSize = 2048;
            var preCalculatedBufferValues = new List<byte>(messageChunkSize);


            for (int i = 0; i < messageChunkSize; i++)
            {
                preCalculatedBufferValues.AddRange(BitConverter.GetBytes(UnityEngine.Random.Range(0, ulong.MaxValue)));
            }

            var indexOffset = 0;
            ulong senderNetworkId = 1;

            //Create ficticious list of clients to send to
            ulong[] psuedoClients = new ulong[]{0};

            var randomGeneratedDataArray = preCalculatedBufferValues.ToArray();
            var maximumOffsetValue = preCalculatedBufferValues.Count;

            //Testing outbound side of the RpcQueueContainer
            for (int i = 0; i < maxRpcEntries; i++)
            {
                //Increment our offset into our randomly generated data for next entry;
                indexOffset = (i * messageChunkSize) % maximumOffsetValue;

                var writer = rpcQueueContainer.BeginAddQueueItemToFrame(RpcQueueContainer.QueueItemType.ServerRpc, Time.realtimeSinceStartup,Transports.NetworkChannel.DefaultMessage,
                        senderNetworkId, psuedoClients, RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);


                writer.WriteByteArray(randomGeneratedDataArray, messageChunkSize);


                rpcQueueContainer.EndAddQueueItemToFrame(writer, RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
            }

            //Now verify the data by obtaining the RpcQueueHistoryFrame we just wrote to
            var currentFrame = rpcQueueContainer.GetLoopBackHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

            //Reset our index offset
            indexOffset = 0;
            int queueEntryItemCount = 0;
            //Parse through the entries written to the current RpcQueueHistoryFrame
            var currentQueueItem = currentFrame.GetFirstQueueItem();
            while (currentQueueItem.QueueItemType != RpcQueueContainer.QueueItemType.None)
            {
                //Check to make sure the wrapper information is accurate for the entry
                Assert.AreEqual(currentQueueItem.NetworkId, senderNetworkId);
                Assert.AreEqual(currentQueueItem.QueueItemType, RpcQueueContainer.QueueItemType.ServerRpc);
                Assert.AreEqual(currentQueueItem.UpdateStage, NetworkUpdateStage.PostLateUpdate);
                Assert.AreEqual(currentQueueItem.NetworkChannel, Transports.NetworkChannel.DefaultMessage);

                //Validate the data in the queue
                Assert.IsTrue(NetworkManagerHelper.BuffersMatch(currentQueueItem.MessageData.Offset, messageChunkSize, currentQueueItem.MessageData.Array, randomGeneratedDataArray));

                //Prepare for next queue item
                queueEntryItemCount++;
                currentQueueItem = currentFrame.GetNextQueueItem();
            }

            rpcQueueContainer.Dispose();
            rpcQueueContainer = null;
            //If we made it to here we are all done and success!
            yield return null;
        }

        public void Dispose()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        public RpcQueueTests()
        {
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager();
        }
    }
}
