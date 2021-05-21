using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.RuntimeTests
{
    public class BaseMultiInstanceTest
    {
        private int m_OriginalTargetFrameRate;

        protected GameObject m_PlayerPrefab;

        protected NetworkManager m_ServerNetworkManager;
        protected NetworkManager[] m_ClientNetworkManagers;

        public virtual void Setup()
        {
            // Just always track the current target frame rate (will be re-applied upon TearDown)
            m_OriginalTargetFrameRate = Application.targetFrameRate;

            // Since we use frame count as a metric, we need to assure it runs at a "common update rate"
            // between platforms (i.e. Ubuntu seems to run at much higher FPS when set to -1)
            if (Application.targetFrameRate < 0 || Application.targetFrameRate > 120)
            {
                Application.targetFrameRate = 120;
            }
        }

        public virtual IEnumerator Teardown()
        {
            // Shutdown and clean up both of our NetworkManager instances
            MultiInstanceHelpers.Destroy();

            // Set the application's target frame rate back to its original value
            Application.targetFrameRate = m_OriginalTargetFrameRate;
            yield return new WaitForSeconds(0); // wait for next frame so everything is destroyed, so following tests can execute from clean environment
        }

        /// <summary>
        /// Utility to spawn some clients and a server and set them up
        /// </summary>
        /// <param name="nbClients"></param>
        /// <param name="updatePlayerPrefab">Update the prefab with whatever is needed before players spawn</param>
        /// <returns></returns>
        public IEnumerator StartSomeClientAndServer(bool useHost, int nbClients, Action<GameObject> updatePlayerPrefab)
        {
            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ClientNetworkManagers = clients;
            m_ServerNetworkManager = server;

            // Create playerPrefab
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            updatePlayerPrefab(m_PlayerPrefab); // update player prefab with whatever is needed before players are spawned

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(useHost, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Wait for connection on client side
            for (int i = 0; i < clients.Length; i++)
            {
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnected(clients[i]));
            }

            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clientCount: useHost ? nbClients+1 : nbClients));
        }
    }
}
