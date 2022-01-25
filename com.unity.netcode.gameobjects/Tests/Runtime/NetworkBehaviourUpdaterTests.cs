using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkBehaviourUpdaterTests : BaseMultiInstanceTest
    {
        protected override int NbClients => throw new NotSupportedException("handled per test");

        private static Type[] s_TypesToTest = new[] { null, typeof(ZeroNetVar), typeof(OneNetVar), typeof(TwoNetVar) };

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield break;
        }

        /// <summary>
        /// This runs test combinations for the following
        ///    test with 0, 1, 2 clients
        ///    test with host and server mode
        ///    test with 0, 1, 2 spawned objects
        ///    test with 0, 1, 2 network behaviour per prefab
        ///    test with 0, 1, 2 network variable per network behaviour
        ///    for each, update netvar
        ///    for each check value changed
        ///    check that all network variables are no longer dirty after update
        /// </summary>
        /// <param name="nbClients"></param>
        /// <param name="useHost"></param>
        /// <param name="nbSpawnedObjects"></param>
        /// <param name="firstNetworkBehaviour"></param>
        /// <param name="secondNetworkBehaviour"></param>
        /// <param name="thirdNetworkBehaviour"></param>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator BehaviourUpdaterAllTests([Values(0, 1, 2)] int nbClients, [Values] bool useHost, [Values(0, 1, 2)] int nbSpawnedObjects,
            [ValueSource(nameof(s_TypesToTest))] Type firstNetworkBehaviour, [ValueSource(nameof(s_TypesToTest))] Type secondNetworkBehaviour)
        {
            bool setupSucceeded = false;
            var serverNetVarsToUpdate = new List<NetworkVariable<int>>();
            var prefabToSpawn = new GameObject();
            var spawnedPrefabs = new List<GameObject>();
            try
            {
                // Create multiple NetworkManager instances
                if (!MultiInstanceHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients))
                {
                    Assert.Fail("Setup: Failed to create instances");
                }
                m_ClientNetworkManagers = clients;
                m_ServerNetworkManager = server;
                Assert.That(m_ClientNetworkManagers.Length, Is.EqualTo(nbClients), "Setup: Did not create the expected number of client network managers.");
                Assert.That(m_ServerNetworkManager, Is.Not.Null, "Setup: Did not create server network manager");

                // setup prefab to spawn
                void AddNetworkBehaviour(Type type, GameObject prefab)
                {
                    if (type != null)
                    {
                        var info = prefab.AddComponent(type) as INetVarInfo;
                    }
                }

                var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
                AddNetworkBehaviour(firstNetworkBehaviour, prefabToSpawn);
                AddNetworkBehaviour(secondNetworkBehaviour, prefabToSpawn);
                MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObjectPrefab);
                m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
                foreach (var clientNetworkManager in m_ClientNetworkManagers)
                {
                    clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
                }

                // Start the instances
                if (!MultiInstanceHelpers.Start(useHost, server, clients))
                {
                    Assert.Fail("Setup: Failed to start instances");
                }
                // Wait for connection on client side
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

                // Wait for connection on server side
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clientCount: useHost ? nbClients + 1 : nbClients));

                // gathering netvars to test on
                for (int i = 0; i < nbSpawnedObjects; i++)
                {
                    var spawnedObject = Object.Instantiate(prefabToSpawn);
                    spawnedPrefabs.Add(spawnedObject);
                    var networkSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
                    networkSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;
                    networkSpawnedObject.Spawn();
                    int nbBehaviours = 0;
                    foreach (var networkBehaviour in spawnedObject.GetComponents<NetworkBehaviour>())
                    {
                        serverNetVarsToUpdate.AddRange(((INetVarInfo)networkBehaviour).AllNetVars);
                        nbBehaviours++;
                    }
                    Assert.That(nbBehaviours, Is.EqualTo((firstNetworkBehaviour == null ? 0 : 1) + (secondNetworkBehaviour == null ? 0 : 1)), "Setup: Did not find expected number of NetworkBehaviours");
                }

                yield return null; // wait a frame to make sure spawn is done
                // todo: with Snapshot spawns enabled and the current race condition, the following line is needed:
                // yield return new WaitForSeconds(0.2f); // wait a bit to fix the spawn/update race condition

                foreach (var netVar in serverNetVarsToUpdate)
                {
                    Assert.That(netVar.Value, Is.EqualTo(0), "Setup: Inspected netvars should all be initialized at 0 value"); // sanity check
                }

                setupSucceeded = true;
            }
            finally
            {
                Assert.True(setupSucceeded, "Test Setup Failed.");
            }

            // test updating all netvars
            const int updatedValue = 1;
            foreach (var netVar in serverNetVarsToUpdate)
            {
                netVar.Value = updatedValue;
                // TODO: Why isn't this just a unit test?
                Assert.That(netVar.IsDirty, Is.True, "Variables should be marked dirty on server once changed.");
            }

            m_ServerNetworkManager.BehaviourUpdater.NetworkBehaviourUpdate(m_ServerNetworkManager);

            // make sure we're not dirty anymore and that clients will receive that new value
            foreach (var netVar in serverNetVarsToUpdate)
            {
                if (nbClients > 0 || useHost)
                {
                    Assert.That(netVar.IsDirty, Is.False, "Dirty flags should be reset after update.");
                }
                else
                {
                    Assert.That(netVar.IsDirty, Is.True, "Vars should still be dirty since we have no clients");
                }
            }

            var clientStatesToCheck = new List<StateOfClientCheck>();
            foreach (var client in m_ClientNetworkManagers)
            {
                clientStatesToCheck.Add(new StateOfClientCheck(client, nbSpawnedObjects, updatedValue));
            }


            var allClientsCompleted = false;
            var testTimedOut = false;
            var timeOutAfter = Time.realtimeSinceStartup + 15;
            while (!allClientsCompleted && !testTimedOut)
            {
                if ( timeOutAfter < Time.realtimeSinceStartup)
                {
                    testTimedOut = true;
                    continue;
                }
                var clientsCompleted = 0;
                foreach (var clientStateCheck in clientStatesToCheck)
                {
                    if (clientStateCheck.IsClientCheckComplete())
                    {
                        clientsCompleted++;
                    }
                }

                if (clientsCompleted == clientStatesToCheck.Count)
                {
                    allClientsCompleted = true;
                    continue;
                }
                yield return null;
            }

            Assert.False(testTimedOut, "The client check test timed out!");
            Assert.True(allClientsCompleted, "Not all client state checks completed!");

            clientStatesToCheck.Clear();
            Object.Destroy(prefabToSpawn);

            foreach(var spawnedObject in spawnedPrefabs)
            {
                Object.Destroy(spawnedObject);
            }

            spawnedPrefabs.Clear();
        }

        private class StateOfClientCheck
        {
            private NetworkManager m_ClientNetworkManager;
            private CheckClientState m_CheckClientState;
            private List<NetworkVariable<int>> m_NetworkVariablesToCheck = new List<NetworkVariable<int>>();
            private int m_ExpectedSpawnedObjects;
            private int m_ExpectedNetVarValue;
            private enum CheckClientState
            {
                SpawnCheck,
                NetworkVarCheck,
                CheckComplete
            }

            public bool IsClientCheckComplete()
            {
                var isComplete = false;
                switch(m_CheckClientState)
                {
                    case CheckClientState.SpawnCheck:
                        {
                            if (m_ClientNetworkManager.SpawnManager.SpawnedObjects.Count == m_ExpectedSpawnedObjects)
                            {
                                m_CheckClientState = CheckClientState.NetworkVarCheck;
                                foreach (var spawnedObject in m_ClientNetworkManager.SpawnManager.SpawnedObjects)
                                {
                                    foreach (var behaviour in spawnedObject.Value.GetComponentsInChildren<NetworkBehaviour>())
                                    {
                                        foreach (var networkVariable in behaviour.NetworkVariableFields)
                                        {
                                            m_NetworkVariablesToCheck.Add(networkVariable as NetworkVariable<int>);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case CheckClientState.NetworkVarCheck:
                        {
                            var allNetworkVariablesPassed = true;
                            foreach (var networkVariable in m_NetworkVariablesToCheck)
                            {
                                if (networkVariable.Value != m_ExpectedNetVarValue)
                                {
                                    allNetworkVariablesPassed = false;
                                    break;
                                }
                            }

                            if (allNetworkVariablesPassed)
                            {
                                isComplete = true;
                                m_CheckClientState = CheckClientState.CheckComplete;
                            }

                            break;
                        }
                    case CheckClientState.CheckComplete:
                        {
                            isComplete = true;
                            break;
                        }
                }

                return isComplete;
            }

            public StateOfClientCheck(NetworkManager networkManager, int expectedSpawnedObjects, int expectedNetvarValue)
            {
                m_ClientNetworkManager = networkManager;
                m_CheckClientState = CheckClientState.SpawnCheck;
                m_ExpectedSpawnedObjects = expectedSpawnedObjects;
                m_ExpectedNetVarValue = expectedNetvarValue;
            }
        }
    }


    public interface INetVarInfo
    {
        public List<NetworkVariable<int>> AllNetVars { get; }
    }

    public class ZeroNetVar : NetworkBehaviour, INetVarInfo
    {
        public List<NetworkVariable<int>> AllNetVars => new List<NetworkVariable<int>>(); // Needed to be independant from NetworkBehaviour's list of fields. This way, if that changes, we can still do this validation in this test
    }

    public class OneNetVar : NetworkBehaviour, INetVarInfo
    {
        private NetworkVariable<int> m_SomeValue = new NetworkVariable<int>();
        public List<NetworkVariable<int>> AllNetVars => new List<NetworkVariable<int>>() { m_SomeValue };
    }

    public class TwoNetVar : NetworkBehaviour, INetVarInfo
    {
        private NetworkVariable<int> m_SomeValue = new NetworkVariable<int>();
        private NetworkVariable<int> m_SomeOtherValue = new NetworkVariable<int>();
        public List<NetworkVariable<int>> AllNetVars => new List<NetworkVariable<int>>() { m_SomeValue, m_SomeOtherValue };
    }
}
