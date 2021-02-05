using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MLAPI.Messaging;

namespace MLAPI.Tests.PlayMode
{
    public class MLAPIPlayModeTestExample
    {
        private GameObject m_NetManObject;
        private NetworkingManager m_NetMan;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return new EnterPlayMode();
        }

        /// <summary>
        /// YieldPlayModeUnitTestExample
        /// Example playmode unit test with yield
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator YieldTestExample()
        {
            if(NetworkingManager.Singleton == null)
            {
                var NetManVar = Resources.Load("NetworkingManagerTests");
                if(NetManVar != null)
                {
                    m_NetManObject = GameObject.Instantiate(NetManVar as GameObject);
                    m_NetMan = m_NetManObject.GetComponent<NetworkingManager>();
                    //Fail if the network manager is null
                    Assert.IsNotNull(m_NetMan);
                    if(m_NetMan != null)
                    {
                        //Fail if the network config is null
                        Assert.IsNotNull(m_NetMan.NetworkConfig);
                        if(m_NetMan.NetworkConfig == null)
                        {
                            yield return null;
                        }
                    }
                }
            }

            //Allow for systems to run for 1/4 of a second (optional)
            yield return new WaitForSeconds(0.25f);

            //Fail if the network manager Singleton is null
            Assert.IsNotNull(NetworkingManager.Singleton);

            if(NetworkingManager.Singleton != null)
            {
                //Example to start as host mode
                //NetworkingManager.Singleton.StartHost();

                //Example Load a prefab for testing (must be in resources directory) Player object
                //var playerObjectPrefab = Resources.Load("Player") as GameObject;

                //Example Instantiate the player object
                //GameObject playerObject = GameObject.Instantiate(playerObjectPrefab);
            }
        }


        /// <summary>
        /// TearDown
        /// Exits playmode and any additional code clean up can occur here
        /// </summary>
        /// <returns></returns>
        [UnitySetUp]
        public IEnumerator TearDown()
        {
            yield return new ExitPlayMode();
        }
    }
}
