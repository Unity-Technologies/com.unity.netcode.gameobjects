using System;
using MLAPI.SceneManagement;
using NUnit.Framework;
namespace MLAPI.RuntimeTests
{
    public class NetworkSceneManagerTests
    {
        [Test]
        public void SwitchSceneWithoutSceneManagement()
        {
            //Only used to create a network object based game asset
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager());
            var threwException = false;
            try
            {
                NetworkSceneManager.SwitchScene("SomeSceneNane");
            }
            catch(Exception ex)
            {
                threwException = true;
            }

            Assert.IsTrue(threwException);
        }

        [SetUp]
        public void Setup()
        {
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager();
        }

        [TearDown]
        public void TearDown()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }
    }
}
