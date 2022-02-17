using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Object = UnityEngine.Object;

namespace Unity.Netcode.TestHelpers.Runtime
{
    public abstract class NetcodeIntegrationTest
    {
        static protected TimeOutHelper s_GloabalTimeOutHelper = new TimeOutHelper(4.0f);
        static protected WaitForSeconds s_DefaultWaitForTick = new WaitForSeconds(1.0f / k_DefaultTickRate);
        public enum NetworkManagerInstatiationMode
        {
            PerTest,        // This will create and destroy new NetworkManagers for each test within a child derived class
            AllTests,       // This will create one set of NetworkManagers used for all tests within a child derived class (destroyed once all tests are finished)
            DoNotCreate     // This will not create any NetworkManagers, it is up to the derived class to manage.
        }

        protected GameObject m_PlayerPrefab;
        protected NetworkManager m_ServerNetworkManager;
        protected NetworkManager[] m_ClientNetworkManagers;
        protected abstract int NbClients { get; }
        protected const uint k_DefaultTickRate = 30;

        private NetworkManagerInstatiationMode m_NetworkManagerInstatiationMode;

        /// <summary>
        /// An update to the original NetcodeIntegrationTestHelpers.WaitForCondition that:
        ///     -operates at the current tick rate
        ///     -allows for a unique TimeOutHelper handler (if none then it uses the default)
        ///     -adjusts its yield period to the settings of the m_ServerNetworkManager.NetworkConfig.TickRate
        /// Notes: This method provides more stability when running integration tests that could
        /// be impacted by:
        ///     -how the integration test is being executed (i.e. in editor or in a stand alone build)
        ///     -potential platform performance issues (i.e. VM is throttled or maxed)
        /// Note: For more complex tests, <see cref="ConditionalPredicateBase"/> and the overloaded version of this method
        /// </summary>
        public static IEnumerator WaitForConditionOrTimeOut(Func<bool> checkForCondition, TimeOutHelper timeOutHelper = null)
        {
            if (checkForCondition == null)
            {
                throw new ArgumentNullException($"checkForCondition cannot be null!");
            }

            // If none is provided we use the default global time out helper
            if (timeOutHelper == null)
            {
                timeOutHelper = s_GloabalTimeOutHelper;
            }

            // Start checking for a timeout
            timeOutHelper.Start();
            while (!timeOutHelper.HasTimedOut())
            {
                // Update and check to see if the condition has been met
                if (checkForCondition.Invoke())
                {
                    break;
                }

                // Otherwise wait for 1 tick interval
                yield return s_DefaultWaitForTick;
            }
            // Stop checking for a timeout
            timeOutHelper.Stop();
        }

        /// <summary>
        /// This version accepts an IConditionalPredicate implementation to provide
        /// more flexibility when the condition to be reached involves more than one
        /// value to be checked.
        /// Note: For simplicity, you can derive from the <see cref="ConditionalPredicateBase"/>
        /// and accomplish most tests.
        /// </summary>
        public static IEnumerator WaitForConditionOrTimeOut(IConditionalPredicate conditionalPredicate, TimeOutHelper timeOutHelper = null)
        {
            if (conditionalPredicate == null)
            {
                throw new ArgumentNullException($"checkForCondition cannot be null!");
            }

            // If none is provided we use the default global time out helper
            if (timeOutHelper == null)
            {
                timeOutHelper = s_GloabalTimeOutHelper;
            }

            conditionalPredicate.Started();
            yield return WaitForConditionOrTimeOut(conditionalPredicate.HasConditionBeenReached, timeOutHelper);
            conditionalPredicate.Finished(timeOutHelper.TimedOut);
        }


        /// <summary>
        /// Validates that all remote clients (i.e. non-server) detect their are connected
        /// to the server and that the server reflects the appropriate number of clients
        /// are connected on its side.
        /// </summary>
        /// <param name="clientsToCheck">An array of clients to be checked</param>
        protected IEnumerator WaitForClientsConnectedOrTimeOut(NetworkManager[] clientsToCheck)
        {
            var remoteClientCount = clientsToCheck.Length;
            var serverClientCount = m_ServerNetworkManager.IsHost ? remoteClientCount + 1 : remoteClientCount;

            yield return WaitForConditionOrTimeOut(() => clientsToCheck.Where((c) => c.IsConnectedClient).Count() == remoteClientCount &&
            m_ServerNetworkManager.ConnectedClients.Count == serverClientCount);
        }

        /// <summary>
        /// Overloaded method that just passes in all clients to
        /// <see cref="WaitForClientsConnectedOrTimeOut(NetworkManager[])"/>
        /// </summary>
        protected IEnumerator WaitForClientsConnectedOrTimeOut()
        {
            yield return WaitForClientsConnectedOrTimeOut(m_ClientNetworkManagers);
        }

        /// <summary>
        /// The very first thing invoked during the <see cref="OneTimeSetup"/> that
        /// determines how this integration test handles NetworkManager instantiation
        /// and destruction.  <see cref="NetworkManagerInstatiationMode"/>
        /// Override this method to change the default mode:
        /// <see cref="NetworkManagerInstatiationMode.AllTests"/>
        /// </summary>
        protected virtual NetworkManagerInstatiationMode OnSetIntegrationTestMode()
        {
            return NetworkManagerInstatiationMode.PerTest;
        }

        protected virtual void OnOneTimeSetup()
        {
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_NetworkManagerInstatiationMode = OnSetIntegrationTestMode();
            OnOneTimeSetup();
        }

        /// <summary>
        /// Called before creating and starting the server and clients
        /// Note: Integration tests configured as <see cref="NetworkManagerInstatiationMode.DoNotCreate"/>
        /// mode can use one or both of the Pre-Post Setup methods.
        /// </summary>
        protected virtual IEnumerator OnPreSetup()
        {
            yield return null;
        }

        /// <summary>
        /// Called after creating and starting the server and clients
        /// Note: Integration tests configured as <see cref="NetworkManagerInstatiationMode.DoNotCreate"/>
        /// mode can use one or both of the Pre-Post Setup methods.
        /// </summary>
        protected virtual IEnumerator OnPostSetup()
        {
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return OnPreSetup();

            if (m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.AllTests)
            {
                if (m_ServerNetworkManager == null)
                {
                    yield return StartSomeClientsAndServerWithPlayers(true, NbClients);
                }
            }
            else
            if (m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.PerTest)
            {
                yield return StartSomeClientsAndServerWithPlayers(true, NbClients);
            }

            yield return OnPostSetup();
        }

        protected void ShutdownAndCleanUp()
        {
            // Shutdown and clean up both of our NetworkManager instances
            try
            {
                if (NetcodeIntegrationTestHelpers.ClientSceneHandler != null)
                {
                    NetcodeIntegrationTestHelpers.ClientSceneHandler.CanClientsLoad -= ClientSceneHandler_CanClientsLoad;
                    NetcodeIntegrationTestHelpers.ClientSceneHandler.CanClientsUnload -= ClientSceneHandler_CanClientsUnload;
                }

                NetcodeIntegrationTestHelpers.Destroy();
            }
            catch (Exception e) { throw e; }
            finally
            {
                if (m_PlayerPrefab != null)
                {
                    Object.Destroy(m_PlayerPrefab);
                    m_PlayerPrefab = null;
                }
            }

            // Cleanup any remaining NetworkObjects
            DestroySceneNetworkObjects();

            // reset the m_ServerWaitForTick for the next test to initialize
            s_DefaultWaitForTick = new WaitForSeconds(1.0f / k_DefaultTickRate);
        }

        /// <summary>
        /// Only valid for integration test mode:
        /// <see cref="NetworkManagerInstatiationMode.PerTest"/>
        /// </summary>
        protected virtual IEnumerator OnPreTearDown()
        {
            yield return s_DefaultWaitForTick;
        }

        /// <summary>
        /// Only valid for integration test mode:
        /// <see cref="NetworkManagerInstatiationMode.PerTest"/>
        /// </summary>
        protected virtual IEnumerator OnPostTearDown()
        {
            yield return s_DefaultWaitForTick;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.PerTest)
            {
                yield return OnPreTearDown();

                ShutdownAndCleanUp();

                yield return OnPostTearDown();
            }
        }

        protected virtual void OnOneTimeTearDown()
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            OnOneTimeTearDown();

            if (m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.AllTests)
            {
                ShutdownAndCleanUp();
            }
        }

        /// <summary>
        /// Override this method to control when clients
        /// fake-load a scene.
        /// </summary>
        protected virtual bool CanClientsLoad()
        {
            return true;
        }

        /// <summary>
        /// Override this method to control when clients
        /// fake-unload a scene.
        /// </summary>
        protected virtual bool CanClientsUnload()
        {
            return true;
        }

        /// <summary>
        /// De-Registers from the CanClientsLoad and CanClientsUnload events of the
        /// ClientSceneHandler (default is IntegrationTestSceneHandler).
        /// </summary>
        protected void DeRegisterSceneManagerHandler()
        {
            if (NetcodeIntegrationTestHelpers.ClientSceneHandler != null)
            {
                NetcodeIntegrationTestHelpers.ClientSceneHandler.CanClientsLoad -= ClientSceneHandler_CanClientsLoad;
                NetcodeIntegrationTestHelpers.ClientSceneHandler.CanClientsUnload -= ClientSceneHandler_CanClientsUnload;
            }
        }

        /// <summary>
        /// Registers the CanClientsLoad and CanClientsUnload events of the
        /// ClientSceneHandler (default is IntegrationTestSceneHandler).
        /// </summary>
        protected void RegisterSceneManagerHandler()
        {
            if (NetcodeIntegrationTestHelpers.ClientSceneHandler != null)
            {
                NetcodeIntegrationTestHelpers.ClientSceneHandler.CanClientsLoad += ClientSceneHandler_CanClientsLoad;
                NetcodeIntegrationTestHelpers.ClientSceneHandler.CanClientsUnload += ClientSceneHandler_CanClientsUnload;
            }
        }

        private bool ClientSceneHandler_CanClientsUnload()
        {
            return CanClientsUnload();
        }

        private bool ClientSceneHandler_CanClientsLoad()
        {
            return CanClientsLoad();
        }

        /// <summary>
        /// Override this to add components (or the like) to the default player prefab
        /// </summary>
        protected virtual void OnCreatePlayerPrefab()
        {
        }

        private void CreatePlayerPrefab()
        {
            // Create playerPrefab
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            OnCreatePlayerPrefab();
        }

        /// <summary>
        /// This is invoked before the server and clients are started.
        /// Override this method if you want to make any adjustments to their
        /// NetworkManager instance(s).
        /// </summary>
        protected virtual void OnServerAndClientsCreated()
        {
        }

        protected void CreateServerAndClients(int nbClients, int targetFrameRate = 60)
        {
            // Create multiple NetworkManager instances
            if (!NetcodeIntegrationTestHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients, targetFrameRate))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ClientNetworkManagers = clients;
            m_ServerNetworkManager = server;

            if (m_ServerNetworkManager != null)
            {
                s_DefaultWaitForTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }

            OnServerAndClientsCreated();
        }

        /// <summary>
        /// Override this method and return false in order to be able
        /// to manually control when the server and clients are started.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanStartServerAndClients()
        {
            return true;
        }

        /// <summary>
        /// Utility to spawn some clients and a server and set them up
        /// </summary>
        /// <param name="nbClients"></param>
        /// <param name="updatePlayerPrefab">Update the prefab with whatever is needed before players spawn</param>
        /// <param name="targetFrameRate">The targetFrameRate of the Unity engine to use while this multi instance test is running. Will be reset on teardown.</param>
        /// <returns></returns>
        public IEnumerator StartSomeClientsAndServerWithPlayers(bool useHost, int nbClients, Action<GameObject> updatePlayerPrefab = null, int targetFrameRate = 60)
        {

            CreateServerAndClients(nbClients);

            CreatePlayerPrefab();

            // NSS-TODO: Remove this
            if (updatePlayerPrefab != null)
            {
                updatePlayerPrefab(m_PlayerPrefab); // update player prefab with whatever is needed before players are spawned
            }

            // Set the player prefab
            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            for (int i = 0; i < m_ClientNetworkManagers.Length; i++)
            {
                m_ClientNetworkManagers[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            if (CanStartServerAndClients())
            {
                // Start the instances and pass in our SceneManagerInitialization action that is invoked immediately after host-server
                // is started and after each client is started.
                if (!NetcodeIntegrationTestHelpers.Start(useHost, m_ServerNetworkManager, m_ClientNetworkManagers))
                {
                    Debug.LogError("Failed to start instances");
                    Assert.Fail("Failed to start instances");
                }

                RegisterSceneManagerHandler();

                // Wait for all clients to connect
                yield return WaitForClientsConnectedOrTimeOut();
            }
        }

        /// <summary>
        /// Override this to filter out the <see cref="NetworkObject"/>s that you
        /// want to allow to persist between integration tests
        /// </summary>
        /// <param name="networkObject"></param>
        protected virtual bool CanDestroyNetworkObject(NetworkObject networkObject)
        {
            return true;
        }

        protected void DestroySceneNetworkObjects()
        {
            // Make sure any NetworkObject with a GlobalObjectIdHash value of 0 is destroyed
            // If we are tearing down, we don't want to leave NetworkObjects hanging around
            var networkObjects = Object.FindObjectsOfType<NetworkObject>().ToList();
            foreach (var networkObject in networkObjects)
            {
                if (CanDestroyNetworkObject(networkObject))
                {
                    Object.DestroyImmediate(networkObject);
                }
            }
        }
    }
}
