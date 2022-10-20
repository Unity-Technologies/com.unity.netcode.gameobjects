using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Runtime.CompilerServices;
using Unity.Netcode.RuntimeTests;
using Object = UnityEngine.Object;

namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// The default Netcode for GameObjects integration test helper class
    /// </summary>
    public abstract class NetcodeIntegrationTest
    {
        /// <summary>
        /// Used to determine if a NetcodeIntegrationTest is currently running to
        /// determine how clients will load scenes
        /// </summary>
        internal static bool IsRunning { get; private set; }
        protected static TimeoutHelper s_GlobalTimeoutHelper = new TimeoutHelper(8.0f);
        protected static WaitForSecondsRealtime s_DefaultWaitForTick = new WaitForSecondsRealtime(1.0f / k_DefaultTickRate);

        public NetcodeLogAssert NetcodeLogAssert;

        /// <summary>
        /// Registered list of all NetworkObjects spawned.
        /// Format is as follows:
        /// [ClientId-side where this NetworkObject instance resides][NetworkObjectId][NetworkObject]
        /// Where finding the NetworkObject with a NetworkObjectId of 10 on ClientId of 2 would be:
        /// s_GlobalNetworkObjects[2][10]
        /// To find the client or server player objects please see:
        /// <see cref="m_PlayerNetworkObjects"/>
        /// </summary>
        protected static Dictionary<ulong, Dictionary<ulong, NetworkObject>> s_GlobalNetworkObjects = new Dictionary<ulong, Dictionary<ulong, NetworkObject>>();

        public static void RegisterNetworkObject(NetworkObject networkObject)
        {
            if (!s_GlobalNetworkObjects.ContainsKey(networkObject.NetworkManager.LocalClientId))
            {
                s_GlobalNetworkObjects.Add(networkObject.NetworkManager.LocalClientId, new Dictionary<ulong, NetworkObject>());
            }
            if (s_GlobalNetworkObjects[networkObject.NetworkManager.LocalClientId].ContainsKey(networkObject.NetworkObjectId))
            {
                if (s_GlobalNetworkObjects[networkObject.NetworkManager.LocalClientId] == null)
                {
                    Assert.False(s_GlobalNetworkObjects[networkObject.NetworkManager.LocalClientId][networkObject.NetworkObjectId] != null,
                        $"Duplicate NetworkObjectId {networkObject.NetworkObjectId} found in {nameof(s_GlobalNetworkObjects)} for client id {networkObject.NetworkManager.LocalClientId}!");
                }
                else
                {
                    s_GlobalNetworkObjects[networkObject.NetworkManager.LocalClientId][networkObject.NetworkObjectId] = networkObject;
                }
            }
            else
            {
                s_GlobalNetworkObjects[networkObject.NetworkManager.LocalClientId].Add(networkObject.NetworkObjectId, networkObject);
            }
        }

        public static void DeregisterNetworkObject(NetworkObject networkObject)
        {
            if (networkObject.IsSpawned && networkObject.NetworkManager != null)
            {
                DeregisterNetworkObject(networkObject.NetworkManager.LocalClientId, networkObject.NetworkObjectId);
            }
        }

        public static void DeregisterNetworkObject(ulong localClientId, ulong networkObjectId)
        {
            if (s_GlobalNetworkObjects.ContainsKey(localClientId) && s_GlobalNetworkObjects[localClientId].ContainsKey(networkObjectId))
            {
                s_GlobalNetworkObjects[localClientId].Remove(networkObjectId);
                if (s_GlobalNetworkObjects[localClientId].Count == 0)
                {
                    s_GlobalNetworkObjects.Remove(localClientId);
                }
            }
        }

        protected int TotalClients => m_UseHost ? m_NumberOfClients + 1 : m_NumberOfClients;

        protected const uint k_DefaultTickRate = 30;

        private int m_NumberOfClients;
        protected abstract int NumberOfClients { get; }

        /// <summary>
        /// Set this to false to create the clients first.
        /// Note: If you are using scene placed NetworkObjects or doing any form of scene testing and
        /// get prefab hash id "soft synchronization" errors, then set this to false and run your test
        /// again.  This is a work-around until we can resolve some issues with NetworkManagerOwner and
        /// NetworkManager.Singleton.
        /// </summary>
        protected bool m_CreateServerFirst = true;

        public enum NetworkManagerInstatiationMode
        {
            PerTest,        // This will create and destroy new NetworkManagers for each test within a child derived class
            AllTests,       // This will create one set of NetworkManagers used for all tests within a child derived class (destroyed once all tests are finished)
            DoNotCreate     // This will not create any NetworkManagers, it is up to the derived class to manage.
        }

        public enum HostOrServer
        {
            Host,
            Server
        }

        protected GameObject m_PlayerPrefab;
        protected NetworkManager m_ServerNetworkManager;
        protected NetworkManager[] m_ClientNetworkManagers;

        /// <summary>
        /// Contains each client relative set of player NetworkObject instances
        /// [Client Relative set of player instances][The player instance ClientId][The player instance's NetworkObject]
        /// Example:
        /// To get the player instance with a ClientId of 3 that was instantiated (relative) on the player instance with a ClientId of 2
        /// m_PlayerNetworkObjects[2][3]
        /// </summary>
        protected Dictionary<ulong, Dictionary<ulong, NetworkObject>> m_PlayerNetworkObjects = new Dictionary<ulong, Dictionary<ulong, NetworkObject>>();

        protected bool m_UseHost = true;
        protected int m_TargetFrameRate = 60;

        private NetworkManagerInstatiationMode m_NetworkManagerInstatiationMode;

        protected bool m_EnableVerboseDebug { get; set; }

        /// <summary>
        /// When set to true, this will bypass the entire
        /// wait for clients to connect process.
        /// </summary>
        /// <remarks>
        /// CAUTION:
        /// Setting this to true will bypass other helper
        /// identification related code, so this should only
        /// be used for connection failure oriented testing
        /// </remarks>
        protected bool m_BypassConnectionTimeout { get; set; }

        /// <summary>
        /// Used to display the various integration test
        /// stages and can be used to log verbose information
        /// for troubleshooting an integration test.
        /// </summary>
        /// <param name="msg"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void VerboseDebug(string msg)
        {
            if (m_EnableVerboseDebug)
            {
                Debug.Log(msg);
            }
        }

        /// <summary>
        /// Override this and return true if you need
        /// to troubleshoot a hard to track bug within an
        /// integration test.
        /// </summary>
        protected virtual bool OnSetVerboseDebug()
        {
            return false;
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
            Application.runInBackground = true;
            m_NumberOfClients = NumberOfClients;
            IsRunning = true;
            m_EnableVerboseDebug = OnSetVerboseDebug();
            IntegrationTestSceneHandler.VerboseDebugMode = m_EnableVerboseDebug;
            VerboseDebug($"Entering {nameof(OneTimeSetup)}");

            m_NetworkManagerInstatiationMode = OnSetIntegrationTestMode();

            // Enable NetcodeIntegrationTest auto-label feature
            NetcodeIntegrationTestHelpers.RegisterNetcodeIntegrationTest(true);
            OnOneTimeSetup();

            VerboseDebug($"Exiting {nameof(OneTimeSetup)}");
        }

        /// <summary>
        /// Called before creating and starting the server and clients
        /// Note: For <see cref="NetworkManagerInstatiationMode.AllTests"/> and
        /// <see cref="NetworkManagerInstatiationMode.PerTest"/> mode integration tests.
        /// For those two modes, if you want to have access to the server or client
        /// <see cref="NetworkManager"/>s then override <see cref="OnServerAndClientsCreated"/>.
        /// <see cref="m_ServerNetworkManager"/> and <see cref="m_ClientNetworkManagers"/>
        /// </summary>
        protected virtual IEnumerator OnSetup()
        {
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            VerboseDebug($"Entering {nameof(SetUp)}");

            NetcodeLogAssert = new NetcodeLogAssert();
            yield return OnSetup();
            if (m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.AllTests && m_ServerNetworkManager == null ||
                m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.PerTest)
            {
                CreateServerAndClients();

                yield return StartServerAndClients();
            }
            VerboseDebug($"Exiting {nameof(SetUp)}");
        }

        /// <summary>
        /// Override this to add components or adjustments to the default player prefab
        /// <see cref="m_PlayerPrefab"/>
        /// </summary>
        protected virtual void OnCreatePlayerPrefab()
        {
        }

        private void CreatePlayerPrefab()
        {
            VerboseDebug($"Entering {nameof(CreatePlayerPrefab)}");
            // Create playerPrefab
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            OnCreatePlayerPrefab();

            VerboseDebug($"Exiting {nameof(CreatePlayerPrefab)}");
        }

        /// <summary>
        /// This is invoked before the server and client(s) are started.
        /// Override this method if you want to make any adjustments to their
        /// NetworkManager instances.
        /// </summary>
        protected virtual void OnServerAndClientsCreated()
        {
        }

        /// <summary>
        /// Will create <see cref="NumberOfClients"/> number of clients.
        /// To create a specific number of clients <see cref="CreateServerAndClients(int)"/>
        /// </summary>
        protected void CreateServerAndClients()
        {
            CreateServerAndClients(NumberOfClients);
        }

        protected virtual void OnNewClientCreated(NetworkManager networkManager)
        {

        }

        protected virtual void OnNewClientStartedAndConnected(NetworkManager networkManager)
        {

        }

        private void AddRemoveNetworkManager(NetworkManager networkManager, bool addNetworkManager)
        {
            var clientNetworkManagersList = new List<NetworkManager>(m_ClientNetworkManagers);
            if (addNetworkManager)
            {
                clientNetworkManagersList.Add(networkManager);
            }
            else
            {
                clientNetworkManagersList.Remove(networkManager);
            }
            m_ClientNetworkManagers = clientNetworkManagersList.ToArray();
            m_NumberOfClients = clientNetworkManagersList.Count;
        }

        protected IEnumerator CreateAndStartNewClient()
        {
            var networkManager = NetcodeIntegrationTestHelpers.CreateNewClient(m_ClientNetworkManagers.Length);
            networkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            // Notification that the new client (NetworkManager) has been created
            // in the event any modifications need to be made before starting the client
            OnNewClientCreated(networkManager);

            NetcodeIntegrationTestHelpers.StartOneClient(networkManager);
            AddRemoveNetworkManager(networkManager, true);
            // Wait for the new client to connect
            yield return WaitForClientsConnectedOrTimeOut();
            if (s_GlobalTimeoutHelper.TimedOut)
            {
                AddRemoveNetworkManager(networkManager, false);
                Object.Destroy(networkManager.gameObject);
            }
            AssertOnTimeout($"{nameof(CreateAndStartNewClient)} timed out waiting for the new client to be connected!");
            ClientNetworkManagerPostStart(networkManager);
            VerboseDebug($"[{networkManager.name}] Created and connected!");
        }

        protected IEnumerator StopOneClient(NetworkManager networkManager, bool destroy = false)
        {
            NetcodeIntegrationTestHelpers.StopOneClient(networkManager, destroy);
            AddRemoveNetworkManager(networkManager, false);
            yield return WaitForConditionOrTimeOut(() => !networkManager.IsConnectedClient);
        }

        /// <summary>
        /// Creates the server and clients
        /// </summary>
        /// <param name="numberOfClients"></param>
        protected void CreateServerAndClients(int numberOfClients)
        {
            VerboseDebug($"Entering {nameof(CreateServerAndClients)}");

            CreatePlayerPrefab();

            // Create multiple NetworkManager instances
            if (!NetcodeIntegrationTestHelpers.Create(numberOfClients, out NetworkManager server, out NetworkManager[] clients, m_TargetFrameRate, m_CreateServerFirst))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ClientNetworkManagers = clients;
            m_ServerNetworkManager = server;

            if (m_ServerNetworkManager != null)
            {
                s_DefaultWaitForTick = new WaitForSecondsRealtime(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }

            // Set the player prefab for the server and clients
            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            // Provides opportunity to allow child derived classes to
            // modify the NetworkManager's configuration before starting.
            OnServerAndClientsCreated();

            VerboseDebug($"Exiting {nameof(CreateServerAndClients)}");
        }

        /// <summary>
        /// Override this method and return false in order to be able
        /// to manually control when the server and clients are started.
        /// </summary>
        protected virtual bool CanStartServerAndClients()
        {
            return true;
        }

        /// <summary>
        /// Invoked after the server and clients have started.
        /// Note: No connection verification has been done at this point
        /// </summary>
        protected virtual IEnumerator OnStartedServerAndClients()
        {
            yield return null;
        }

        /// <summary>
        /// Invoked after the server and clients have started and verified
        /// their connections with each other.
        /// </summary>
        protected virtual IEnumerator OnServerAndClientsConnected()
        {
            yield return null;
        }

        private void ClientNetworkManagerPostStart(NetworkManager networkManager)
        {
            networkManager.name = $"NetworkManager - Client - {networkManager.LocalClientId}";
            Assert.NotNull(networkManager.LocalClient.PlayerObject, $"{nameof(StartServerAndClients)} detected that client {networkManager.LocalClientId} does not have an assigned player NetworkObject!");

            // Get all player instances for the current client NetworkManager instance
            var clientPlayerClones = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.IsPlayerObject && c.OwnerClientId == networkManager.LocalClientId);
            // Add this player instance to each client player entry
            foreach (var playerNetworkObject in clientPlayerClones)
            {
                // When the server is not the host this needs to be done
                if (!m_PlayerNetworkObjects.ContainsKey(playerNetworkObject.NetworkManager.LocalClientId))
                {
                    m_PlayerNetworkObjects.Add(playerNetworkObject.NetworkManager.LocalClientId, new Dictionary<ulong, NetworkObject>());
                }
                if (!m_PlayerNetworkObjects[playerNetworkObject.NetworkManager.LocalClientId].ContainsKey(networkManager.LocalClientId))
                {
                    m_PlayerNetworkObjects[playerNetworkObject.NetworkManager.LocalClientId].Add(networkManager.LocalClientId, playerNetworkObject);
                }
            }
        }

        protected void ClientNetworkManagerPostStartInit()
        {
            // Creates a dictionary for all player instances client and server relative
            // This provides a simpler way to get a specific player instance relative to a client instance
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                ClientNetworkManagerPostStart(networkManager);
            }
            if (m_UseHost)
            {
                var clientSideServerPlayerClones = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.IsPlayerObject && c.OwnerClientId == m_ServerNetworkManager.LocalClientId);
                foreach (var playerNetworkObject in clientSideServerPlayerClones)
                {
                    // When the server is not the host this needs to be done
                    if (!m_PlayerNetworkObjects.ContainsKey(playerNetworkObject.NetworkManager.LocalClientId))
                    {
                        m_PlayerNetworkObjects.Add(playerNetworkObject.NetworkManager.LocalClientId, new Dictionary<ulong, NetworkObject>());
                    }
                    if (!m_PlayerNetworkObjects[playerNetworkObject.NetworkManager.LocalClientId].ContainsKey(m_ServerNetworkManager.LocalClientId))
                    {
                        m_PlayerNetworkObjects[playerNetworkObject.NetworkManager.LocalClientId].Add(m_ServerNetworkManager.LocalClientId, playerNetworkObject);
                    }
                }
            }
        }

        /// <summary>
        /// This starts the server and clients as long as <see cref="CanStartServerAndClients"/>
        /// returns true.
        /// </summary>
        protected IEnumerator StartServerAndClients()
        {
            if (CanStartServerAndClients())
            {
                VerboseDebug($"Entering {nameof(StartServerAndClients)}");

                // Start the instances and pass in our SceneManagerInitialization action that is invoked immediately after host-server
                // is started and after each client is started.
                if (!NetcodeIntegrationTestHelpers.Start(m_UseHost, m_ServerNetworkManager, m_ClientNetworkManagers))
                {
                    Debug.LogError("Failed to start instances");
                    Assert.Fail("Failed to start instances");
                }

                RegisterSceneManagerHandler();

                // Notification that the server and clients have been started
                yield return OnStartedServerAndClients();

                // When true, we skip everything else (most likely a connection oriented test)
                if (!m_BypassConnectionTimeout)
                {
                    // Wait for all clients to connect
                    yield return WaitForClientsConnectedOrTimeOut();

                    AssertOnTimeout($"{nameof(StartServerAndClients)} timed out waiting for all clients to be connected!");

                    if (m_UseHost || m_ServerNetworkManager.IsHost)
                    {
                        // Add the server player instance to all m_ClientSidePlayerNetworkObjects entries
                        var serverPlayerClones = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.IsPlayerObject && c.OwnerClientId == m_ServerNetworkManager.LocalClientId);
                        foreach (var playerNetworkObject in serverPlayerClones)
                        {
                            if (!m_PlayerNetworkObjects.ContainsKey(playerNetworkObject.NetworkManager.LocalClientId))
                            {
                                m_PlayerNetworkObjects.Add(playerNetworkObject.NetworkManager.LocalClientId, new Dictionary<ulong, NetworkObject>());
                            }
                            m_PlayerNetworkObjects[playerNetworkObject.NetworkManager.LocalClientId].Add(m_ServerNetworkManager.LocalClientId, playerNetworkObject);
                        }
                    }

                    ClientNetworkManagerPostStartInit();

                    // Notification that at this time the server and client(s) are instantiated,
                    // started, and connected on both sides.
                    yield return OnServerAndClientsConnected();

                    VerboseDebug($"Exiting {nameof(StartServerAndClients)}");
                }
            }
        }

        /// <summary>
        /// Override this method to control when clients
        /// can fake-load a scene.
        /// </summary>
        protected virtual bool CanClientsLoad()
        {
            return true;
        }

        /// <summary>
        /// Override this method to control when clients
        /// can fake-unload a scene.
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
            IntegrationTestSceneHandler.CanClientsLoad -= ClientSceneHandler_CanClientsLoad;
            IntegrationTestSceneHandler.CanClientsUnload -= ClientSceneHandler_CanClientsUnload;
            IntegrationTestSceneHandler.NetworkManagers.Clear();
        }

        /// <summary>
        /// Registers the CanClientsLoad and CanClientsUnload events of the
        /// ClientSceneHandler.
        /// The default is: <see cref="IntegrationTestSceneHandler"/>.
        /// </summary>
        protected void RegisterSceneManagerHandler()
        {
            IntegrationTestSceneHandler.CanClientsLoad += ClientSceneHandler_CanClientsLoad;
            IntegrationTestSceneHandler.CanClientsUnload += ClientSceneHandler_CanClientsUnload;
            NetcodeIntegrationTestHelpers.RegisterSceneManagerHandler(m_ServerNetworkManager, true);
        }

        private bool ClientSceneHandler_CanClientsUnload()
        {
            return CanClientsUnload();
        }

        private bool ClientSceneHandler_CanClientsLoad()
        {
            return CanClientsLoad();
        }

        protected bool OnCanSceneCleanUpUnload(Scene scene)
        {
            return true;
        }

        /// <summary>
        /// This shuts down all NetworkManager instances registered via the
        /// <see cref="NetcodeIntegrationTestHelpers"/> class and cleans up
        /// the test runner scene of any left over NetworkObjects.
        /// <see cref="DestroySceneNetworkObjects"/>
        /// </summary>
        protected void ShutdownAndCleanUp()
        {
            VerboseDebug($"Entering {nameof(ShutdownAndCleanUp)}");
            // Shutdown and clean up both of our NetworkManager instances
            try
            {
                DeRegisterSceneManagerHandler();

                NetcodeIntegrationTestHelpers.Destroy();

                m_PlayerNetworkObjects.Clear();
                s_GlobalNetworkObjects.Clear();
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

            UnloadRemainingScenes();

            // reset the m_ServerWaitForTick for the next test to initialize
            s_DefaultWaitForTick = new WaitForSecondsRealtime(1.0f / k_DefaultTickRate);
            VerboseDebug($"Exiting {nameof(ShutdownAndCleanUp)}");
        }

        /// <summary>
        /// Note: For <see cref="NetworkManagerInstatiationMode.PerTest"/> mode
        /// this is called before ShutdownAndCleanUp.
        /// </summary>
        protected virtual IEnumerator OnTearDown()
        {
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            VerboseDebug($"Entering {nameof(TearDown)}");
            yield return OnTearDown();

            if (m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.PerTest)
            {
                ShutdownAndCleanUp();
            }

            VerboseDebug($"Exiting {nameof(TearDown)}");
            NetcodeLogAssert.Dispose();
        }

        /// <summary>
        /// Override this method to do handle cleaning up once the test(s)
        /// within the child derived class have completed
        /// Note: For <see cref="NetworkManagerInstatiationMode.AllTests"/> mode
        /// this is called before ShutdownAndCleanUp.
        /// </summary>
        protected virtual void OnOneTimeTearDown()
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            IntegrationTestSceneHandler.VerboseDebugMode = false;
            VerboseDebug($"Entering {nameof(OneTimeTearDown)}");
            OnOneTimeTearDown();

            if (m_NetworkManagerInstatiationMode == NetworkManagerInstatiationMode.AllTests)
            {
                ShutdownAndCleanUp();
            }

            // Disable NetcodeIntegrationTest auto-label feature
            NetcodeIntegrationTestHelpers.RegisterNetcodeIntegrationTest(false);

            UnloadRemainingScenes();

            VerboseDebug($"Exiting {nameof(OneTimeTearDown)}");

            IsRunning = false;
        }

        /// <summary>
        /// Override this to filter out the <see cref="NetworkObject"/>s that you
        /// want to allow to persist between integration tests.
        /// <see cref="DestroySceneNetworkObjects"/>
        /// <see cref="ShutdownAndCleanUp"/>
        /// </summary>
        /// <param name="networkObject">the network object in question to be destroyed</param>
        protected virtual bool CanDestroyNetworkObject(NetworkObject networkObject)
        {
            return true;
        }

        /// <summary>
        /// Destroys all NetworkObjects at the end of a test cycle.
        /// </summary>
        protected void DestroySceneNetworkObjects()
        {
            var networkObjects = Object.FindObjectsOfType<NetworkObject>();
            foreach (var networkObject in networkObjects)
            {
                // This can sometimes be null depending upon order of operations
                // when dealing with parented NetworkObjects.  If NetworkObjectB
                // is a child of NetworkObjectA and NetworkObjectA comes before
                // NetworkObjectB in the list of NeworkObjects found, then when
                // NetworkObjectA's GameObject is destroyed it will also destroy
                // NetworkObjectB's GameObject which will destroy NetworkObjectB.
                // If there is a null entry in the list, this is the most likely
                // scenario and so we just skip over it.
                if (networkObject == null)
                {
                    continue;
                }
                if (CanDestroyNetworkObject(networkObject))
                {
                    networkObject.NetworkManagerOwner = m_ServerNetworkManager;
                    // Destroy the GameObject that holds the NetworkObject component
                    Object.DestroyImmediate(networkObject.gameObject);
                }
            }
        }

        /// <summary>
        /// Waits for the function condition to return true or it will time out.
        /// This will operate at the current m_ServerNetworkManager.NetworkConfig.TickRate
        /// and allow for a unique TimeoutHelper handler (if none then it uses the default)
        /// Notes: This provides more stability when running integration tests that could be
        /// impacted by:
        ///     -how the integration test is being executed (i.e. in editor or in a stand alone build)
        ///     -potential platform performance issues (i.e. VM is throttled or maxed)
        /// Note: For more complex tests, <see cref="ConditionalPredicateBase"/> and the overloaded
        /// version of this method
        /// </summary>
        public static IEnumerator WaitForConditionOrTimeOut(Func<bool> checkForCondition, TimeoutHelper timeOutHelper = null)
        {
            if (checkForCondition == null)
            {
                throw new ArgumentNullException($"checkForCondition cannot be null!");
            }

            // If none is provided we use the default global time out helper
            if (timeOutHelper == null)
            {
                timeOutHelper = s_GlobalTimeoutHelper;
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
        /// more flexibility for checking complex conditional cases.
        /// </summary>
        public static IEnumerator WaitForConditionOrTimeOut(IConditionalPredicate conditionalPredicate, TimeoutHelper timeOutHelper = null)
        {
            if (conditionalPredicate == null)
            {
                throw new ArgumentNullException($"checkForCondition cannot be null!");
            }

            // If none is provided we use the default global time out helper
            if (timeOutHelper == null)
            {
                timeOutHelper = s_GlobalTimeoutHelper;
            }

            conditionalPredicate.Started();
            yield return WaitForConditionOrTimeOut(conditionalPredicate.HasConditionBeenReached, timeOutHelper);
            conditionalPredicate.Finished(timeOutHelper.TimedOut);
        }

        /// <summary>
        /// Validates that all remote clients (i.e. non-server) detect they are connected
        /// to the server and that the server reflects the appropriate number of clients
        /// have connected or it will time out.
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

        internal IEnumerator WaitForMessageReceived<T>(List<NetworkManager> wiatForReceivedBy, ReceiptType type = ReceiptType.Handled) where T : INetworkMessage
        {
            // Build our message hook entries tables so we can determine if all clients received spawn or ownership messages
            var messageHookEntriesForSpawn = new List<MessageHookEntry>();
            foreach (var clientNetworkManager in wiatForReceivedBy)
            {
                var messageHook = new MessageHookEntry(clientNetworkManager, type);
                messageHook.AssignMessageType<T>();
                messageHookEntriesForSpawn.Add(messageHook);
            }
            // Used to determine if all clients received the CreateObjectMessage
            var hooks = new MessageHooksConditional(messageHookEntriesForSpawn);
            yield return WaitForConditionOrTimeOut(hooks);
            Assert.False(s_GlobalTimeoutHelper.TimedOut);
        }

        internal IEnumerator WaitForMessagesReceived(List<Type> messagesInOrder, List<NetworkManager> wiatForReceivedBy, ReceiptType type = ReceiptType.Handled)
        {
            // Build our message hook entries tables so we can determine if all clients received spawn or ownership messages
            var messageHookEntriesForSpawn = new List<MessageHookEntry>();
            foreach (var clientNetworkManager in wiatForReceivedBy)
            {
                foreach (var message in messagesInOrder)
                {
                    var messageHook = new MessageHookEntry(clientNetworkManager, type);
                    messageHook.AssignMessageType(message);
                    messageHookEntriesForSpawn.Add(messageHook);
                }
            }
            // Used to determine if all clients received the CreateObjectMessage
            var hooks = new MessageHooksConditional(messageHookEntriesForSpawn);
            yield return WaitForConditionOrTimeOut(hooks);
            Assert.False(s_GlobalTimeoutHelper.TimedOut);
        }

        /// <summary>
        /// Creates a basic NetworkObject test prefab, assigns it to a new
        /// NetworkPrefab entry, and then adds it to the server and client(s)
        /// NetworkManagers' NetworkConfig.NetworkPrefab lists.
        /// </summary>
        /// <param name="baseName">the basic name to be used for each instance</param>
        /// <returns>NetworkObject of the GameObject assigned to the new NetworkPrefab entry</returns>
        protected GameObject CreateNetworkObjectPrefab(string baseName)
        {
            var prefabCreateAssertError = $"You can only invoke this method during {nameof(OnServerAndClientsCreated)} " +
                $"but before {nameof(OnStartedServerAndClients)}!";
            Assert.IsNotNull(m_ServerNetworkManager, prefabCreateAssertError);
            Assert.IsFalse(m_ServerNetworkManager.IsListening, prefabCreateAssertError);

            var gameObject = new GameObject();
            gameObject.name = baseName;
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_ServerNetworkManager;
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            var networkPrefab = new NetworkPrefab() { Prefab = gameObject };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }
            return gameObject;
        }

        /// <summary>
        /// Overloaded method <see cref="SpawnObject(NetworkObject, NetworkManager, bool)"/>
        /// </summary>
        protected GameObject SpawnObject(GameObject prefabGameObject, NetworkManager owner, bool destroyWithScene = false)
        {
            var prefabNetworkObject = prefabGameObject.GetComponent<NetworkObject>();
            Assert.IsNotNull(prefabNetworkObject, $"{nameof(GameObject)} {prefabGameObject.name} does not have a {nameof(NetworkObject)} component!");
            return SpawnObject(prefabNetworkObject, owner, destroyWithScene);
        }

        /// <summary>
        /// Spawn a NetworkObject prefab instance
        /// </summary>
        /// <param name="prefabNetworkObject">the prefab NetworkObject to spawn</param>
        /// <param name="owner">the owner of the instance</param>
        /// <param name="destroyWithScene">default is false</param>
        /// <returns>GameObject instance spawned</returns>
        private GameObject SpawnObject(NetworkObject prefabNetworkObject, NetworkManager owner, bool destroyWithScene = false)
        {
            Assert.IsTrue(prefabNetworkObject.GlobalObjectIdHash > 0, $"{nameof(GameObject)} {prefabNetworkObject.name} has a {nameof(NetworkObject.GlobalObjectIdHash)} value of 0! Make sure to make it a valid prefab before trying to spawn!");
            var newInstance = Object.Instantiate(prefabNetworkObject.gameObject);
            var networkObjectToSpawn = newInstance.GetComponent<NetworkObject>();
            networkObjectToSpawn.NetworkManagerOwner = m_ServerNetworkManager; // Required to assure the server does the spawning
            if (owner == m_ServerNetworkManager)
            {
                if (m_UseHost)
                {
                    networkObjectToSpawn.SpawnWithOwnership(owner.LocalClientId, destroyWithScene);
                }
                else
                {
                    networkObjectToSpawn.Spawn(destroyWithScene);
                }
            }
            else
            {
                networkObjectToSpawn.SpawnWithOwnership(owner.LocalClientId, destroyWithScene);
            }

            return newInstance;
        }

        /// <summary>
        /// Overloaded method <see cref="SpawnObjects(NetworkObject, NetworkManager, int, bool)"/>
        /// </summary>
        protected List<GameObject> SpawnObjects(GameObject prefabGameObject, NetworkManager owner, int count, bool destroyWithScene = false)
        {
            var prefabNetworkObject = prefabGameObject.GetComponent<NetworkObject>();
            Assert.IsNotNull(prefabNetworkObject, $"{nameof(GameObject)} {prefabGameObject.name} does not have a {nameof(NetworkObject)} component!");
            return SpawnObjects(prefabNetworkObject, owner, count, destroyWithScene);
        }

        /// <summary>
        /// Will spawn (x) number of prefab NetworkObjects
        /// <see cref="SpawnObject(NetworkObject, NetworkManager, bool)"/>
        /// </summary>
        /// <param name="prefabNetworkObject">the prefab NetworkObject to spawn</param>
        /// <param name="owner">the owner of the instance</param>
        /// <param name="count">number of instances to create and spawn</param>
        /// <param name="destroyWithScene">default is false</param>
        private List<GameObject> SpawnObjects(NetworkObject prefabNetworkObject, NetworkManager owner, int count, bool destroyWithScene = false)
        {
            var gameObjectsSpawned = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                gameObjectsSpawned.Add(SpawnObject(prefabNetworkObject, owner, destroyWithScene));
            }
            return gameObjectsSpawned;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public NetcodeIntegrationTest()
        {

        }

        /// <summary>
        /// Optional Host or Server integration tests
        /// Constructor that allows you To break tests up as a host
        /// and a server.
        /// Example: Decorate your child derived class with TestFixture
        /// and then create a constructor at the child level.
        /// Don't forget to set your constructor public, else Unity will
        /// give you a hard to decipher error
        /// [TestFixture(HostOrServer.Host)]
        /// [TestFixture(HostOrServer.Server)]
        /// public class MyChildClass : NetcodeIntegrationTest
        /// {
        ///     public MyChildClass(HostOrServer hostOrServer) : base(hostOrServer) { }
        /// }
        /// </summary>
        /// <param name="hostOrServer"></param>
        public NetcodeIntegrationTest(HostOrServer hostOrServer)
        {
            m_UseHost = hostOrServer == HostOrServer.Host ? true : false;
        }

        /// <summary>
        /// Just a helper function to avoid having to write the entire assert just to check if you
        /// timed out.
        /// </summary>
        protected void AssertOnTimeout(string timeOutErrorMessage, TimeoutHelper assignedTimeoutHelper = null)
        {
            var timeoutHelper = assignedTimeoutHelper != null ? assignedTimeoutHelper : s_GlobalTimeoutHelper;
            Assert.False(timeoutHelper.TimedOut, timeOutErrorMessage);
        }

        private void UnloadRemainingScenes()
        {
            // Unload any remaining scenes loaded but the test runner scene
            // Note: Some tests only unload the server-side instance, and this
            // just assures no currently loaded scenes will impact the next test
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || scene.name.Contains(NetcodeIntegrationTestHelpers.FirstPartOfTestRunnerSceneName) || !OnCanSceneCleanUpUnload(scene))
                {
                    continue;
                }
                VerboseDebug($"Unloading scene {scene.name}-{scene.handle}");
                var asyncOperation = SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
