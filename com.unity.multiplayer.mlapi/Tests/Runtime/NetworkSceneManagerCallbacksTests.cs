using System;
using System.Collections;
using MLAPI.Configuration;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests
{
    public class NetworkSceneManagerCallbackTests
    {
        
        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/com.unity.multiplayer.mlapi/Tests/Runtime/OnAllClientsReady/SceneWeAreSwitchingFrom.unity", new LoadSceneParameters(LoadSceneMode.Single));
            
            var networkConfig = new NetworkConfig
            {
                CreatePlayerPrefab = false,
                EnableSceneManagement = true,
                LoadSceneTimeOut = 5,
            };

            networkConfig.RegisteredScenes.Add("SceneWeAreSwitchingTo");
            
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out _, NetworkManagerHelper.NetworkManagerOperatingMode.Host, networkConfig);
        }
        
        [UnityTest]
        public IEnumerator TestOnClientLoadedScene()
        {
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
            } while ((DateTime.UtcNow - startTime).TotalSeconds < networkManager.NetworkConfig.LoadSceneTimeOut && !callbackReceived);

            Assert.IsTrue(callbackReceived);
        }
        
        [UnityTest]
        public IEnumerator TestOnAllClientsLoadedScene()
        {
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
            } while ((DateTime.UtcNow - startTime).TotalSeconds < networkManager.NetworkConfig.LoadSceneTimeOut && !callbackReceived);

            Assert.IsTrue(callbackReceived);
        }

        [TearDown]
        public void TearDown()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }
    }
}