using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;

namespace MLAPI.RuntimeTests
{
    /// <summary>
    /// The RPC Queue unit test to validate:
    /// - Sending and Receiving pipeline to validate that both sending and receiving pipelines are functioning properly.
    /// - Usage of the ServerRpcParams.Send.UpdateStage and ClientRpcParams.Send.UpdateStage functionality.
    /// - Rpcs receive will be invoked at the appropriate NetworkUpdateStage.
    /// Requires: RpcPipelineTestComponent
    /// </summary>
    public class RpcQueueTests
    {
        private NetworkingManager m_NetMan;

#if UNITY_EDITOR
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return new EnterPlayMode();
        }
#endif

        /// <summary>
        /// Tests the egress and ingress RPC queue functionality
        /// ** This does not include any of the MLAPI to Transport code **
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator RpcQueueUnitTest()
        {
            var netManObject = new GameObject();
            m_NetMan = netManObject.AddComponent<NetworkingManager>();
            UnetTransport unetTransport = netManObject.AddComponent<UnetTransport>();
            m_NetMan.NetworkConfig = new Configuration.NetworkConfig();
            m_NetMan.NetworkConfig.CreatePlayerPrefab = false;
            m_NetMan.NetworkConfig.AllowRuntimeSceneChanges = true;
            m_NetMan.NetworkConfig.EnableSceneManagement = false;
            unetTransport.ConnectAddress = "127.0.0.1";
            unetTransport.ConnectPort = 7777;
            unetTransport.ServerListenPort = 7777;
            unetTransport.MessageBufferSize = 65535;
            unetTransport.MaxConnections = 100;
            unetTransport.MessageSendMode = UnetTransport.SendMode.Immediately;
            m_NetMan.NetworkConfig.NetworkTransport = unetTransport;

            var currentActiveScene = SceneManager.GetActiveScene();
            var instantiatedNetworkingManager = false;
            var testsAreComplete = false;
            var testsAreValidated = false;
            var maximumTimeTaken = 0.0f;

            if (currentActiveScene != null)
            {
                //Add our test scene name
                NetworkSceneManager.AddRuntimeSceneName(currentActiveScene.name, 0);

                //Create the player object that we will spawn as a host
                var playerObject = new GameObject("RpcTestObject");
                playerObject.AddComponent<NetworkedObject>();
                var RpcPipelineTestComponent = playerObject.AddComponent<RpcPipelineTestComponent>();

                if (NetworkingManager.Singleton != null)
                {
                    Debug.Log("Networking Manager Instantiated.");
                    instantiatedNetworkingManager = true;
                    //Start as host mode as loopback only works in hostmode
                    NetworkingManager.Singleton.StartHost();

                    Debug.Log("Host Started.");

                    if (RpcPipelineTestComponent != null)
                    {
                        //Enable the simple ping test
                        RpcPipelineTestComponent.PingSelfEnabled = true;
                        Debug.Log("Running RPC Queue Tests...");

                        //We shouldn't (for sure) take longer than 30 seconds
                        maximumTimeTaken = Time.realtimeSinceStartup + 30.0f;

                        //Wait for the rpc pipeline test to complete or
                        while (!testsAreComplete && maximumTimeTaken > Time.realtimeSinceStartup)
                        {
                            //Wait for 100ms
                            yield return new WaitForSeconds(0.1f);

                            testsAreComplete = RpcPipelineTestComponent.IsTestComplete();
                        }

                        testsAreValidated = RpcPipelineTestComponent.ValidateUpdateStages();
                        //Stop pinging
                        RpcPipelineTestComponent.PingSelfEnabled = false;

                        Debug.Log("RPC Queue Testing completed.");
                    }

                    //Stop the host
                    NetworkingManager.Singleton.StopHost();


                }
            }

            //Shutdown the networking manager
            NetworkingManager.Singleton.Shutdown();

            Assert.IsTrue(testsAreComplete && testsAreValidated && instantiatedNetworkingManager && maximumTimeTaken > Time.realtimeSinceStartup);
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }

#if UNITY_EDITOR
        [UnitySetUp]
        public IEnumerator TearDown()
        {
            yield return new ExitPlayMode();
        }
#endif
    }
}
