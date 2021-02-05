using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MLAPI.Messaging;

namespace MLAPI.Tests
{
    public class RPCQueueTests
    {
        private NetworkedObject m_NetworkObject;
        private GameObject m_RpcTestObject;
        private RpcPipelineTest m_RpcPipelineTest;
        private GameObject m_NetManObject;
        private NetworkingManager m_NetMan;
        private RpcQueueContainer m_RpcQueueContainer;


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
            if(NetworkingManager.Singleton == null)
            {
                var NetManVar = Resources.Load("NetworkingManagerTests");
                if(NetManVar != null)
                {
                    m_NetManObject = GameObject.Instantiate(NetManVar as GameObject);
                    m_NetMan = m_NetManObject.GetComponent<NetworkingManager>();
                    if(m_NetMan != null)
                    {
                        Assert.IsNotNull(m_NetMan.NetworkConfig);
                        if(m_NetMan.NetworkConfig == null)
                        {
                            yield return null;
                        }
                    }
                }
            }

            //Allow for systems to run for a second
            yield return new WaitForSeconds(1.0f);
            if(NetworkingManager.Singleton != null)
            {
                m_RpcQueueContainer = NetworkingManager.Singleton.rpcQueueContainer;

                m_RpcQueueContainer.SetTestingState(true);

                //Start as host mode as loopback only works in hostmode
                NetworkingManager.Singleton.StartHost();

                //Load the testing (resources directory) Player object
                var playerObjectPrefab = Resources.Load("Player") as GameObject;
                //Instantiate the player object
                GameObject playerObject = GameObject.Instantiate(playerObjectPrefab);

                if(m_RpcTestObject == null)
                {
                    //Load the generic loopback object for testing purposes.
                    var go = Resources.Load("RpcTestingObject") as GameObject;
                    //Instantiate it
                    m_RpcTestObject = GameObject.Instantiate(go);

                    //Get the loopback network object
                    m_NetworkObject = m_RpcTestObject.GetComponent<NetworkedObject>();
                    //This should be false
                    if (!m_NetworkObject.IsSpawned)
                    {
                        //Spawn the object to give it a valid network id (required)
                        m_NetworkObject.Spawn();
                    }
                    m_RpcPipelineTest = m_RpcTestObject.GetComponent<RpcPipelineTest>();
                }

                if(m_RpcPipelineTest != null)
                {
                    //Enable the simple ping test
                    m_RpcPipelineTest.pingSelfEnabled = true;
                    var TimeStarted = Time.realtimeSinceStartup + 15;
                    //Wait for the rpc pipeline test to complete
                    while(!m_RpcPipelineTest.IsTestComplete() || TimeStarted <= Time.realtimeSinceStartup)
                    {
                        //Wait half a second
                        yield return new WaitForSeconds(0.5f);
                    }

                    //Stop pinging
                    m_RpcPipelineTest.pingSelfEnabled = false;

                    //If we did not complete all of our pings and we timed out, then assert (otherwise don't assert and return with no errors)
                    Assert.IsFalse(!m_RpcPipelineTest.IsTestComplete() && TimeStarted <= Time.realtimeSinceStartup);
                }
            }

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
