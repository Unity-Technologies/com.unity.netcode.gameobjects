using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// This is a refactor of the original test's NetworkBehaviour INetVarInfo derived NetworkBehaviours
    /// </summary>
    public class NetVarContainer : NetworkBehaviour
    {
        /// <summary>
        /// Creates a prefab with two instances of this NetworkBehaviour
        /// </summary>
        /// <returns></returns>
        public static GameObject CreatePrefabGameObject(NetVarCombinationTypes netVarsToCheck)
        {
            var gameObject = new GameObject
            {
                // Always a good idea to name the Prefab for easy identification purposes
                name = "NetVarContainerObject"
            };
            var networkObject = gameObject.AddComponent<NetworkObject>();

            // Create the two instances of the NetVarContainer components and add them to the
            // GameObject of this prefab
            var netVarContainer = gameObject.AddComponent<NetVarContainer>();
            netVarContainer.NumberOfNetVarsToCheck = netVarsToCheck.FirstType;
            netVarContainer.ValueToSetNetVarTo = NetworkBehaviourUpdaterTests.NetVarValueToSet;
            netVarContainer = gameObject.AddComponent<NetVarContainer>();
            netVarContainer.NumberOfNetVarsToCheck = netVarsToCheck.SecondType;
            netVarContainer.ValueToSetNetVarTo = NetworkBehaviourUpdaterTests.NetVarValueToSet;

            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            return gameObject;
        }

        public enum NetVarsToCheck
        {
            One,
            Two
        }

        public NetVarsToCheck NumberOfNetVarsToCheck;
        public int ValueToSetNetVarTo = 0;

        /// <summary>
        /// Only used on the client-side for this test, this
        /// is used to see if the network variables have changed.
        /// </summary>
        public bool HaveAllValuesChanged(int valueToCheck)
        {
            var allValuesChanged = false;
            switch (NumberOfNetVarsToCheck)
            {
                case NetVarsToCheck.Two:
                    {
                        allValuesChanged = m_FirstValue.Value == valueToCheck && m_SeconValue.Value == valueToCheck;
                        break;
                    }
                case NetVarsToCheck.One:
                    {
                        allValuesChanged = m_FirstValue.Value == valueToCheck;
                        break;
                    }
            }
            return allValuesChanged;
        }

        /// <summary>
        /// Only used on the server side to check the isDirty flag for the
        /// NetworkVariables being used for each test iteration
        /// </summary>
        public bool AreNetVarsDirty()
        {
            var areDirty = false;
            switch (NumberOfNetVarsToCheck)
            {
                case NetVarsToCheck.Two:
                    {
                        areDirty = m_FirstValue.IsDirty() && m_SeconValue.IsDirty();
                        break;
                    }
                case NetVarsToCheck.One:
                    {
                        areDirty = m_FirstValue.IsDirty();
                        break;
                    }
            }

            return areDirty;
        }

        /// <summary>
        /// The original version of this test only ever had up to 2 NetworkVariables per
        /// NetworkBehaviour.  As opposed to using a List of NetworkVariables, we just
        /// create the maximum number that could be used and then only use what we need
        /// for each test iteration.
        /// </summary>
        private NetworkVariable<int> m_FirstValue = new NetworkVariable<int>();
        private NetworkVariable<int> m_SeconValue = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            // Clients will register each NetworkObject when it is spawned
            if (!IsServer)
            {
                NetworkBehaviourUpdaterTests.ClientSideNotifyObjectSpawned(gameObject);
            }
        }

        /// <summary>
        /// Server side only, sets the NetworkVariables being used to the ValueToSetNetVarTo
        /// that is pre-configured when the Network Prefab is created.
        /// </summary>
        public void SetNetworkVariableValues()
        {
            if (IsServer)
            {
                switch (NumberOfNetVarsToCheck)
                {
                    case NetVarsToCheck.Two:
                        {
                            m_FirstValue.Value = ValueToSetNetVarTo;
                            m_SeconValue.Value = ValueToSetNetVarTo;
                            Assert.True(AreNetVarsDirty(), "Not all NetworkVariables were marked dirty on server after spawned!");
                            break;
                        }
                    case NetVarsToCheck.One:
                        {
                            m_FirstValue.Value = ValueToSetNetVarTo;
                            Assert.True(AreNetVarsDirty(), "Not all NetworkVariables were marked dirty on server after spawned!");
                            break;
                        }
                }
            }
        }
    }

    /// <summary>
    /// Used to define how many NetworkVariables to use per NetVarContainer instance.
    /// There are always two
    /// </summary>
    public struct NetVarCombinationTypes
    {
        public NetVarContainer.NetVarsToCheck FirstType;
        public NetVarContainer.NetVarsToCheck SecondType;
    }

    public class NetworkBehaviourUpdaterTests : NetcodeIntegrationTest
    {
        // Go ahead and create maximum number of clients (not all tests will use them)
        protected override int NumberOfClients => 2;
        public const int NetVarValueToSet = 1;
        private static List<GameObject> s_ClientSpawnedNetworkObjects = new List<GameObject>();
        private List<NetworkManager> m_ActiveClientsForCurrentTest;

        /// <summary>
        /// Clients will call this when NetworkObjects are spawned on their end
        /// </summary>
        /// <param name="objectSpaned">the GameObject of the NetworkObject spawned</param>
        public static void ClientSideNotifyObjectSpawned(GameObject objectSpaned)
        {
            if (!s_ClientSpawnedNetworkObjects.Contains(objectSpaned))
            {
                s_ClientSpawnedNetworkObjects.Add(objectSpaned);
            }
        }

        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        /// <summary>
        /// Creates the server and client(s) required for this particular test iteration
        /// </summary>
        private IEnumerator StartClientsAndServer(bool useHost, int numberOfClients, GameObject prefabObject)
        {
            void AddNetworkPrefab(NetworkConfig config, NetworkPrefab prefab)
            {
                config.Prefabs.Add(prefab);
            }

            // Sanity check to make sure we are not trying to create more clients than we have available to use
            Assert.True(numberOfClients <= m_ClientNetworkManagers.Length);
            m_ActiveClientsForCurrentTest = new List<NetworkManager>();

            // Create a list of the clients to be used in this test from the available clients
            for (int i = 0; i < numberOfClients; i++)
            {
                m_ActiveClientsForCurrentTest.Add(m_ClientNetworkManagers[i]);
            }

            // Add the prefab to be used for this particular test iteration
            var np = new NetworkPrefab { Prefab = prefabObject };
            AddNetworkPrefab(m_ServerNetworkManager.NetworkConfig, np);
            m_ServerNetworkManager.NetworkConfig.TickRate = 30;
            foreach (var clientManager in m_ActiveClientsForCurrentTest)
            {
                m_ServerNetworkManager.NetworkConfig.TickRate = 30;
                AddNetworkPrefab(clientManager.NetworkConfig, np);
            }

            // Now spin everything up normally
            var clientsAsArry = m_ActiveClientsForCurrentTest.ToArray();
            Assert.True(NetcodeIntegrationTestHelpers.Start(useHost, m_ServerNetworkManager, clientsAsArry), "Failed to start server and client instances");

            // Only if we have clients (not host)
            if (numberOfClients > 0)
            {
                RegisterSceneManagerHandler();
            }

            // Wait for connection on client and server side
            yield return WaitForClientsConnectedOrTimeOut(clientsAsArry);
        }

        /// <summary>
        /// This list replaces the original NetworkVariable types to be checked.
        /// Both NetworkVariables are of type int and the original version of this test was testing
        /// the NetworkBehaviour Update when there were 1 or more (i.e two) on the same NetworkBehaviour.
        /// After reviewing, we really only needed to test a much smaller combination of types and so
        /// this pre-generated array represents the reduced set of combinations to test.
        /// Note:
        /// The original test was also testing for no NetworkVariables of type int, which there ended up
        /// being no reason to do that and only added to the length of the execution time for this test.
        /// </summary>
        public static NetVarCombinationTypes[] NetVarCombinationTypeValues = new[]{
            new NetVarCombinationTypes() { FirstType = NetVarContainer.NetVarsToCheck.One, SecondType = NetVarContainer.NetVarsToCheck.One },
            new NetVarCombinationTypes() { FirstType = NetVarContainer.NetVarsToCheck.One, SecondType = NetVarContainer.NetVarsToCheck.Two },
            new NetVarCombinationTypes() { FirstType = NetVarContainer.NetVarsToCheck.Two, SecondType = NetVarContainer.NetVarsToCheck.Two }};

        /// <summary>
        /// The updated BehaviourUpdaterAllTests was re-designed to replicate the same functionality being tested in the
        /// original version of this test with additional time out handling and a re-organization in the order of operations.
        /// Things like making sure all clients have spawned the NetworkObjects in question prior to testing for the
        /// NetworkVariable value changes helped to eliminate the timing issues that were happening when this test was run
        /// in a stand alone test runner build (i.e. all consoles run the stand alone version as opposed to the in-editor
        /// version like the desktop tests use).
        /// This update also updated how the server and clients were being constructed to help reduce the execution time.
        /// </summary>
        /// <param name="useHost"> whether to run the server as a host or not</param>
        /// <param name="varCombinationTypes">the NetworkVariable combination types</param>
        /// <param name="nbClients"> number of clients to use for the test</param>
        /// <param name="numToSpawn"> number of NetworkObjects to be spawned</param>
        [UnityTest]
        public IEnumerator BehaviourUpdaterAllTests([Values] bool useHost,
            [ValueSource(nameof(NetVarCombinationTypeValues))] NetVarCombinationTypes varCombinationTypes,
            [Values(0, 1, 2)] int nbClients, [Values(1, 2)] int numToSpawn)
        {
            s_ClientSpawnedNetworkObjects.Clear();

            // The edge case scenario where we can exit early is when we are running
            // just the server (i.e. non-host) and there are zero clients.  Under this
            // edge case scenario of the various combinations we do not need to run
            // this test as the IsDirty flag is never cleared when no clients exist at all.
            if (nbClients == 0 && !useHost)
            {
                yield break;
            }

            // Create our prefab based on the NetVarCombinationTypes
            var prefabToSpawn = NetVarContainer.CreatePrefabGameObject(varCombinationTypes);

            yield return StartClientsAndServer(useHost, nbClients, prefabToSpawn);

            // Tracks the server-side spawned prefab instances
            var spawnedPrefabs = new List<GameObject>();
            var tickInterval = 1.0f / m_ServerNetworkManager.NetworkConfig.TickRate;

            // Used to determine if the client-side checks of this test should be
            // executed or not as well is used to make sure all clients have spawned
            // the appropriate number of NetworkObjects with the NetVarContainer behaviour
            var numberOfObjectsToSpawnOnClients = numToSpawn * nbClients;

            // spawn the objects
            for (int i = 0; i < numToSpawn; i++)
            {
                var spawnedObject = Object.Instantiate(prefabToSpawn);
                spawnedPrefabs.Add(spawnedObject);
                var networkSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
                networkSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;
                networkSpawnedObject.Spawn();
            }

            // When there are no clients (excluding when server is in host mode), we can skip all of this
            // wait until all objects are spawned on the clients
            if (numberOfObjectsToSpawnOnClients > 0)
            {
                // Waits for all clients to spawn the NetworkObjects
                yield return WaitForConditionOrTimeOut(() => numberOfObjectsToSpawnOnClients == s_ClientSpawnedNetworkObjects.Count);
                Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for clients to report spawning objects! " +
                    $"Total reported client-side spawned objects {s_ClientSpawnedNetworkObjects.Count}");
            }

            // Once all clients have spawned the NetworkObjects, set the network variables for
            // those NetworkObjects on the server-side.
            foreach (var spawnedPrefab in spawnedPrefabs)
            {
                var netVarContiners = spawnedPrefab.GetComponents<NetVarContainer>();
                foreach (var netVarContiner in netVarContiners)
                {
                    netVarContiner.SetNetworkVariableValues();
                }
            }

            // Update the NetworkBehaviours to make sure all network variables are no longer marked as dirty
            m_ServerNetworkManager.BehaviourUpdater.NetworkBehaviourUpdate();

            // Verify that all network variables are no longer dirty on server side only if we have clients (including host)
            foreach (var serverSpawnedObject in spawnedPrefabs)
            {
                var netVarContainers = serverSpawnedObject.GetComponents<NetVarContainer>();
                foreach (var netVarContainer in netVarContainers)
                {
                    Assert.False(netVarContainer.AreNetVarsDirty(), "Some NetworkVariables were still marked dirty after NetworkBehaviourUpdate!");
                }
            }

            // When there are no clients (excluding when server is in host mode), we can skip all of this
            if (numberOfObjectsToSpawnOnClients > 0)
            {
                // Get a list of all NetVarContainer components on the client-side spawned NetworkObjects
                var clientSideNetVarContainers = new List<NetVarContainer>();
                foreach (var clientSpawnedObjects in s_ClientSpawnedNetworkObjects)
                {
                    var netVarContainers = clientSpawnedObjects.GetComponents<NetVarContainer>();
                    foreach (var netvarContiner in netVarContainers)
                    {
                        clientSideNetVarContainers.Add(netvarContiner);
                    }
                }

                yield return WaitForConditionOrTimeOut(() =>
                clientSideNetVarContainers.Where(d =>
                d.HaveAllValuesChanged(NetVarValueToSet)).Count() == clientSideNetVarContainers.Count);
                Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client side NetVarContainers to report all NetworkVariables have been updated!");
            }

            Object.DestroyImmediate(prefabToSpawn);
        }
    }
}
