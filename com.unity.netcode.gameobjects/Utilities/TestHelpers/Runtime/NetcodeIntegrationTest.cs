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
        protected bool m_BypassStartAndWaitForClients = false;
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

        protected virtual NetworkManagerInstatiationMode OnSetIntegrationTestMode()
        {
            return NetworkManagerInstatiationMode.PerTest;
        }

        /// <summary>
        /// Called from
        /// </summary>
        protected virtual void OnOneTimeSetup()
        {
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_NetworkManagerInstatiationMode = OnSetIntegrationTestMode();
            OnOneTimeSetup();
        }

        protected virtual IEnumerator OnPreSetup()
        {
            yield return null;
        }

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

            // Make sure any NetworkObject with a GlobalObjectIdHash value of 0 is destroyed
            // If we are tearing down, we don't want to leave NetworkObjects hanging around
            var networkObjects = Object.FindObjectsOfType<NetworkObject>().ToList();
            foreach (var networkObject in networkObjects)
            {
                Object.DestroyImmediate(networkObject);
            }

            // reset the m_ServerWaitForTick for the next test to initialize
            s_DefaultWaitForTick = new WaitForSeconds(1.0f / k_DefaultTickRate);
        }

        protected virtual IEnumerator OnPreTearDown()
        {
            // wait for 1 tick interval so everything is destroyed and any following tests
            // can execute from clean environment
            yield return s_DefaultWaitForTick;
        }

        protected virtual IEnumerator OnPostTearDown()
        {
            // wait for 1 tick interval so everything is destroyed and any following tests
            // can execute from clean environment
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
        /// Utility to spawn some clients and a server and set them up
        /// </summary>
        /// <param name="nbClients"></param>
        /// <param name="updatePlayerPrefab">Update the prefab with whatever is needed before players spawn</param>
        /// <param name="targetFrameRate">The targetFrameRate of the Unity engine to use while this multi instance test is running. Will be reset on teardown.</param>
        /// <returns></returns>
        public IEnumerator StartSomeClientsAndServerWithPlayers(bool useHost, int nbClients, Action<GameObject> updatePlayerPrefab = null, int targetFrameRate = 60)
        {
            // Make sure any NetworkObject with a GlobalObjectIdHash value of 0 is destroyed
            // If we are tearing down, we don't want to leave NetworkObjects hanging around
            var networkObjects = Object.FindObjectsOfType<NetworkObject>().ToList();
            var networkObjectsList = networkObjects.Where(c => c.GlobalObjectIdHash == 0);
            foreach (var netObject in networkObjects)
            {
                Object.DestroyImmediate(netObject);
            }

            // Create multiple NetworkManager instances
            if (!NetcodeIntegrationTestHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients, targetFrameRate))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ClientNetworkManagers = clients;
            m_ServerNetworkManager = server;

            CreatePlayerPrefab();

            // NSS-TODO: Remove this
            if (updatePlayerPrefab != null)
            {
                updatePlayerPrefab(m_PlayerPrefab); // update player prefab with whatever is needed before players are spawned
            }

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            if (!m_BypassStartAndWaitForClients)
            {

                if (m_ServerNetworkManager != null)
                {
                    s_DefaultWaitForTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
                }

                // Start the instances and pass in our SceneManagerInitialization action that is invoked immediately after host-server
                // is started and after each client is started.
                if (!NetcodeIntegrationTestHelpers.Start(useHost, server, clients))
                {
                    Debug.LogError("Failed to start instances");
                    Assert.Fail("Failed to start instances");
                }

                RegisterSceneManagerHandler();

                // Wait for connection on client side
                yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients);

                // Wait for connection on server side
                yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(server, useHost ? nbClients + 1 : nbClients);
            }
        }
    }

    /// <summary>
    /// Can be used independently or assigned to <see cref="NetcodeIntegrationTest.WaitForConditionOrTimeOut"></see> in the
    /// event the default timeout period needs to be adjusted
    /// </summary>
    public class TimeOutHelper
    {
        private const float k_DefaultTimeOutWaitPeriod = 2.0f;

        private float m_MaximumTimeBeforeTimeOut;
        private float m_TimeOutPeriod;

        private bool m_IsStarted;
        public bool TimedOut { get; internal set; }

        public void Start()
        {
            m_MaximumTimeBeforeTimeOut = Time.realtimeSinceStartup + m_TimeOutPeriod;
            m_IsStarted = true;
            TimedOut = false;
        }

        public void Stop()
        {
            TimedOut = HasTimedOut();
            m_IsStarted = false;
        }

        public bool HasTimedOut()
        {
            return m_IsStarted ? m_MaximumTimeBeforeTimeOut < Time.realtimeSinceStartup : TimedOut;
        }

        public TimeOutHelper(float timeOutPeriod = k_DefaultTimeOutWaitPeriod)
        {
            m_TimeOutPeriod = timeOutPeriod;
        }
    }

    /// <summary>
    /// Derive from this class to create your own conditional handling for your <see cref="NetcodeIntegrationTest"/>
    /// integration tests when dealing with more complicated scenarios where initializing values, storing state to be
    /// used across several integration tests.
    /// </summary>
    public class ConditionalPredicateBase : IConditionalPredicate
    {
        private bool m_TimedOut;

        public bool TimedOut { get { return m_TimedOut; } }

        protected virtual bool OnHasConditionBeenReached()
        {
            return true;
        }

        public bool HasConditionBeenReached()
        {
            return OnHasConditionBeenReached();
        }

        protected virtual void OnStarted() { }

        public void Started()
        {
            OnStarted();
        }

        protected virtual void OnFinished() { }

        public void Finished(bool timedOut)
        {
            m_TimedOut = timedOut;
            OnFinished();
        }
    }

    public interface IConditionalPredicate
    {
        /// <summary>
        /// Test the conditions of the test to be reached
        /// </summary>
        bool HasConditionBeenReached();

        /// <summary>
        /// Wait for condition has started
        /// </summary>
        void Started();

        /// <summary>
        /// Wait for condition has finished:
        /// Condition(s) met or timed out
        /// </summary>
        void Finished(bool timedOut);

    }
}
