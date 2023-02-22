using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkManagerEventsTests
    {
        [UnityTest]
        public IEnumerator OnServerStoppedCalledWhenServerStops()
        {
            bool callbackInvoked = false;
            var gameObject = new GameObject(nameof(OnServerStoppedCalledWhenServerStops));
            var networkManager = gameObject.AddComponent<NetworkManager>();

            // Set dummy transport that does nothing
            var transport = gameObject.AddComponent<DummyTransport>();
            networkManager.NetworkConfig = new NetworkConfig() { NetworkTransport = transport };

            Action onServerStopped = () =>
            {
                callbackInvoked = true;
                if (networkManager.IsServer)
                {
                    Assert.Fail("OnServerStopped called when the server is still active");
                }
            };

            // Start server to cause initialization process
            Assert.True(networkManager.StartServer());
            Assert.True(networkManager.IsListening);

            networkManager.OnServerStopped += onServerStopped;
            networkManager.Shutdown();
            UnityEngine.Object.DestroyImmediate(gameObject);

            /* Need two updates to actually shut down. First one to see the transport failing, which
             marks the NetworkManager as shutting down. Second one where actual shutdown occurs. */
            yield return null;
            yield return null;

            Assert.False(networkManager.IsListening);
            Assert.True(callbackInvoked, "OnServerStopped wasn't invoked");
        }
    }
}
