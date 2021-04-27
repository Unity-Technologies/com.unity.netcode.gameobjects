using System;
using System.Collections;
using System.Linq;
using MLAPI.SceneManagement;
using MLAPI.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests
{
    public class NetworkSceneManagerTests
    {
        [Test]
        public void SwitchSceneWithoutSceneManagement()
        {
            //Only used to create a network object based game asset
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out NetworkManager networkManager));
            var threwException = false;
            try
            {
                networkManager.SceneManager.SwitchScene("SomeSceneNane");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling this method."))
                {
                    threwException = true;
                }
            }

            Assert.IsTrue(threwException);
        }

        [SetUp]
        public void Setup()
        {
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out _);
        }

        [TearDown]
        public void TearDown()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }
    }
}
