using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;

namespace MLAPI.RuntimeTests
{
    public class RPCQueueTests
    {
        private NetworkingManager m_NetMan;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return new EnterPlayMode();
        }

        /// <summary>
        /// RPCQueueUnitTest
        /// Tests the egress and ingress RPC queue functionality
        /// ** This does not include any of the MLAPI to Transport code **
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator RPCQueueUnitTest()
        {
            GameObject NetManObject = new GameObject();
            m_NetMan = NetManObject.AddComponent<NetworkingManager>();
            UnetTransport unetTransport = NetManObject.AddComponent<UnetTransport>();
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
            Scene CurrentActiveScene = SceneManager.GetActiveScene();

            bool InstantiatedNetworkingManager = false;
            bool TestsAreComplete = false;
            var MaximumTimeTaken = 0.0f;

            if(CurrentActiveScene != null)
            {
                //Add our test scene name
                NetworkSceneManager.AddRuntimeSceneName(CurrentActiveScene.name, 0);

                //Create the player object that we will spawn as a host
                GameObject playerObject = new GameObject("RpcTestObject");
                NetworkedObject playerNetworkObject = playerObject.AddComponent<NetworkedObject>();
                RpcPipelineTestComponent RpcPipelineTestComponent = playerObject.AddComponent<RpcPipelineTestComponent>();

                if(NetworkingManager.Singleton != null)
                {
                    Debug.Log("Networking Manager Instantiated.");
                    InstantiatedNetworkingManager = true;
                    //Start as host mode as loopback only works in hostmode
                    NetworkingManager.Singleton.StartHost();

                    Debug.Log("Host Started.");

                    if (RpcPipelineTestComponent != null)
                    {
                        //Enable the simple ping test
                        RpcPipelineTestComponent.PingSelfEnabled = true;
                        Debug.Log("Running RPC Queue Tests...");

                        //We shouldn't (for sure) take longer than 30 seconds
                        MaximumTimeTaken = Time.realtimeSinceStartup + 30.0f;

                        //Wait for the rpc pipeline test to complete or
                        while (!TestsAreComplete && MaximumTimeTaken > Time.realtimeSinceStartup)
                        {
                            //Wait half a second
                            yield return new WaitForSeconds(0.5f);

                            TestsAreComplete = RpcPipelineTestComponent.IsTestComplete();
                        }

                        //Stop pinging
                        RpcPipelineTestComponent.PingSelfEnabled = false;

                        Debug.Log("RPC Queue Testing completed.");
                    }
                }
            }
            Assert.IsTrue(TestsAreComplete && InstantiatedNetworkingManager && MaximumTimeTaken > Time.realtimeSinceStartup);
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }


        [UnitySetUp]
        public IEnumerator TearDown()
        {
            yield return new ExitPlayMode();
        }
    }
}
