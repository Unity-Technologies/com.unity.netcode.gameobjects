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

        [UnityTest]
        public IEnumerator TestOnClientLoadedScene()
        {
            SetupNetworkManagerForClientLoadedCallbackTests();
            var networkManager = NetworkManagerHelper.NetworkManagerObject;
            
            bool callbackReceived = false;
            
            networkManager.SceneManager.OnClientLoadedScene += (progress, clientId) =>
            {
                Debug.Log("OnClientLoadedScene invoked on the host, all good.");
                callbackReceived = true;
            }; 
            
            networkManager.SceneManager.SwitchScene("SceneWeAreSwitchingTo");

            var startTime = DateTime.UtcNow;
            do
            {
                yield return null;
            }
            while ((DateTime.UtcNow - startTime).TotalSeconds < networkManager.NetworkConfig.LoadSceneTimeOut && !callbackReceived);
            
            Assert.IsTrue(callbackReceived);
        }
        
        [UnityTest]
        public IEnumerator TestOnAllClientsLoadedScene()
        {
            SetupNetworkManagerForClientLoadedCallbackTests();
            var networkManager = NetworkManagerHelper.NetworkManagerObject;
            
            bool callbackReceived = false;
            
            networkManager.SceneManager.OnAllClientsLoadedScene += (progress, timedOut) =>
            {
                Debug.Log("OnAllClientsLoadedScene invoked on the host, all good.");
                callbackReceived = true;
            }; 
            
            networkManager.SceneManager.SwitchScene("SceneWeAreSwitchingTo");

            var startTime = DateTime.UtcNow;
            do
            {
                yield return null;
            }
            while ((DateTime.UtcNow - startTime).TotalSeconds < networkManager.NetworkConfig.LoadSceneTimeOut && !callbackReceived);
            
            Assert.IsTrue(callbackReceived);
        }

        private void SetupNetworkManagerForClientLoadedCallbackTests()
        {
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out NetworkManager networkManager, NetworkManagerHelper.NetworkManagerOperatingMode.None));
            Debug.Log("We have stopped the NetworkManager to do changes to NetworkConfig.");

            networkManager.NetworkConfig.EnableSceneManagement = true;
            networkManager.NetworkConfig.RegisteredScenes.Add("SceneWeAreSwitchingTo");
            networkManager.NetworkConfig.LoadSceneTimeOut = 5;

            networkManager.StartHost();
            Debug.Log("Host started");
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
