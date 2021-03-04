using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;

namespace MLAPI.RuntimeTests
{
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
        public IEnumerator RpcQueueUnitTest()
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
            var rpcPipelineTestComponent = playerObject.AddComponent<RpcPipelineTestComponent>();

            instantiatedNetworkManager = true;
            Debug.Log("NetworkManager Instantiated.");

            //Start as host mode as loopback only works in hostmode
            NetworkManager.Singleton.StartHost();
            Debug.Log("Host Started.");

            //Enable the simple ping test
            rpcPipelineTestComponent.PingSelfEnabled = true;

            Debug.Log("Running RPC Queue Tests...");

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

            //Stop pinging
            rpcPipelineTestComponent.PingSelfEnabled = false;
            Debug.Log("RPC Queue Testing completed.");

            //Stop the host
            NetworkManager.Singleton.StopHost();

            //Shutdown the NetworkManager
            NetworkManager.Singleton.Shutdown();

            Debug.Log($"Exiting status => {nameof(testsAreComplete)}: {testsAreComplete} - {nameof(testsAreValidated)}: {testsAreValidated} - {nameof(instantiatedNetworkManager)}: {instantiatedNetworkManager} - {nameof(exceededMaximumStageIterations)}: {exceededMaximumStageIterations}");

            Assert.IsTrue(testsAreComplete && testsAreValidated && instantiatedNetworkManager);

            GameObject.DestroyImmediate(playerObject);
            GameObject.DestroyImmediate(networkManagerObject);
#else
            yield return null;
#endif
        }
    }
}