using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public abstract class BaseMultiInstanceTest
    {
        protected GameObject m_PlayerPrefab;
        protected NetworkManager m_ServerNetworkManager;
        protected NetworkManager[] m_ClientNetworkManagers;

        protected abstract int NbClients { get; }

        protected bool m_BypassStartAndWaitForClients = false;

        static protected TimeOutHelper s_GloabalTimeOutHelper = new TimeOutHelper(4.0f);

        protected const uint k_DefaultTickRate = 30;
        protected WaitForSeconds m_DefaultWaitForTick = new WaitForSeconds(1.0f / k_DefaultTickRate);

        /// <summary>
        /// An update to the original MultiInstanceHelpers.WaitForCondition that:
        ///     -operates at the current tick rate
        ///     -allows for a unique TimeOutHelper handler (if none then it uses the default)
        ///     -adjusts its yield period to the settings of the m_ServerNetworkManager.NetworkConfig.TickRate
        /// Notes: This method provides more stability when running integration tests that could
        /// be impacted by:
        ///     -how the integration test is being executed (i.e. in editor or in a stand alone build)
        ///     -potential platform performance issues (i.e. VM is throttled or maxed)
        /// Note: For more complex tests, <see cref="ConditionalPredicateBase"/> and the overloaded version of this method
        /// </summary>
        protected IEnumerator WaitForConditionOrTimeOut(Func<bool> checkForCondition, TimeOutHelper timeOutHelper = null)
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
                yield return m_DefaultWaitForTick;
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
        protected IEnumerator WaitForConditionOrTimeOut(IConditionalPredicate conditionalPredicate, TimeOutHelper timeOutHelper = null)
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

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, _ => { });
            if (m_ServerNetworkManager != null)
            {
                m_DefaultWaitForTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }
        }

        [UnityTearDown]
        public virtual IEnumerator Teardown()
        {
            // Shutdown and clean up both of our NetworkManager instances
            try
            {
                if (MultiInstanceHelpers.ClientSceneHandler != null)
                {
                    MultiInstanceHelpers.ClientSceneHandler.CanClientsLoad -= ClientSceneHandler_CanClientsLoad;
                    MultiInstanceHelpers.ClientSceneHandler.CanClientsUnload -= ClientSceneHandler_CanClientsUnload;
                }

                MultiInstanceHelpers.Destroy();
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

            // Make sure we clean up after ourselves and destroy any remaining NetworkObjects
            // before we exit our test
            var networkObjects = Object.FindObjectsOfType<NetworkObject>().ToList();
            foreach (var networkObject in networkObjects)
            {
                Object.DestroyImmediate(networkObject);
            }

            // wait for 1 tick interval so everything is destroyed and any following tests
            // can execute from clean environment
            yield return m_DefaultWaitForTick;

            // reset the m_ServerWaitForTick for the next test to initialize
            m_DefaultWaitForTick = new WaitForSeconds(1.0f / k_DefaultTickRate);
        }

        /// <summary>
        /// NSS-TODO: See RegisterSceneManagerHandler
        /// Override this method to control when clients
        /// fake-load a scene.
        /// </summary>
        protected virtual bool CanClientsLoad()
        {
            return true;
        }

        /// <summary>
        /// NSS-TODO: See RegisterSceneManagerHandler
        /// Override this method to control when clients
        /// fake-unload a scene.
        /// </summary>
        protected virtual bool CanClientsUnload()
        {
            return true;
        }

        /// <summary>
        /// NSS-TODO: Back port PR-1405 to get this functionality
        /// Registers the CanClientsLoad and CanClientsUnload events of the
        /// ClientSceneHandler (default is IntegrationTestSceneHandler).
        /// </summary>
        protected void RegisterSceneManagerHandler()
        {
            MultiInstanceHelpers.ClientSceneHandler.CanClientsLoad += ClientSceneHandler_CanClientsLoad;
            MultiInstanceHelpers.ClientSceneHandler.CanClientsUnload += ClientSceneHandler_CanClientsUnload;
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
        /// Utility to spawn some clients and a server and set them up
        /// </summary>
        /// <param name="nbClients"></param>
        /// <param name="updatePlayerPrefab">Update the prefab with whatever is needed before players spawn</param>
        /// <param name="targetFrameRate">The targetFrameRate of the Unity engine to use while this multi instance test is running. Will be reset on teardown.</param>
        /// <returns></returns>
        public IEnumerator StartSomeClientsAndServerWithPlayers(bool useHost, int nbClients, Action<GameObject> updatePlayerPrefab = null, int targetFrameRate = 60)
        {
            // Make sure there are no remaining NetworkObjects from a previous test
            // before we start our new test
            var networkObjects = Object.FindObjectsOfType<NetworkObject>().ToList();
            foreach (var netObject in networkObjects)
            {
                Object.DestroyImmediate(netObject);
            }

            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients, targetFrameRate))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ClientNetworkManagers = clients;
            m_ServerNetworkManager = server;

            // Create playerPrefab
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();
            /*
             * Normally we would only allow player prefabs to be set to a prefab. Not runtime created objects.
             * In order to prevent having a Resource folder full of a TON of prefabs that we have to maintain,
             * MultiInstanceHelper has a helper function that lets you mark a runtime created object to be
             * treated as a prefab by the Netcode. That's how we can get away with creating the player prefab
             * at runtime without it being treated as a SceneObject or causing other conflicts with the Netcode.
             */
            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

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
                // Start the instances and pass in our SceneManagerInitialization action that is invoked immediately after host-server
                // is started and after each client is started.
                if (!MultiInstanceHelpers.Start(useHost, server, clients))
                {
                    Debug.LogError("Failed to start instances");
                    Assert.Fail("Failed to start instances");
                }

                RegisterSceneManagerHandler();

                // Wait for connection on client side
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

                // Wait for connection on server side
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, useHost ? nbClients + 1 : nbClients));
            }
        }
    }

    /// <summary>
    /// Can be used independently or assigned to <see cref="BaseMultiInstanceTest.WaitForConditionOrTimeOut"></see> in the
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
    /// Derive from this class to create your own conditional handling for your <see cref="BaseMultiInstanceTest"/>
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
