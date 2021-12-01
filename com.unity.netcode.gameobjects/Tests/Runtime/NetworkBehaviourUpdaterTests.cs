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
            try
            {
                // Create multiple NetworkManager instances
                if (!MultiInstanceHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients))
                {
                    Assert.Fail("Setup: Failed to create instances");
                }
                m_ClientNetworkManagers = clients;
                m_ServerNetworkManager = server;
                Assert.That(m_ClientNetworkManagers.Length, Is.EqualTo(nbClients), "Setup: Did not create the expected number of network managers.");
                Assert.That(m_ServerNetworkManager, Is.Not.Null, "Setup: Did not create server network manager");

                // setup prefab to spawn
                void AddNetworkBehaviour(Type type, GameObject prefab)
                {
                    if (type != null)
                    {
                        var info = prefab.AddComponent(type) as INetVarInfo;
                    }
                }
                var prefabToSpawn = new GameObject();
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

                yield return new WaitForSeconds(0); // wait a frame to make sure spawn is done
                // todo: with Snapshot spawns enabled and the current race condition, the following line is needed:
                // yield return new WaitForSeconds(0.2f); // wait a bit to fix the spawn/update race condition

                foreach (var netVar in serverNetVarsToUpdate)
                {
                    Assert.That(netVar.Value, Is.EqualTo(0), "Setup: Variables should start with 0 value"); // sanity check
                }

                setupSucceeded = true;
            }
            finally
            {
                Debug.Log(setupSucceeded ? "Test Setup Completed." : "Test Setup Failed.");
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

            foreach (var client in m_ClientNetworkManagers)
            {
                var nbVarsCheckedClientSide = 0;
                var countSpawnObjectResult = new MultiInstanceHelpers.CoroutineResultWrapper<bool>();
                yield return MultiInstanceHelpers.WaitForCondition(() => client.SpawnManager.SpawnedObjects.Count == nbSpawnedObjects, countSpawnObjectResult);
                Assert.That(countSpawnObjectResult.Result, Is.True, "Client should have spawned expected number of objects (MultiInstanceHelper)");

                foreach (var spawnedObject in client.SpawnManager.SpawnedObjects)
                {
                    foreach (var behaviour in spawnedObject.Value.GetComponentsInChildren<NetworkBehaviour>())
                    {
                        foreach (var networkVariable in behaviour.NetworkVariableFields)
                        {
                            var varInt = networkVariable as NetworkVariable<int>;
                            var varUpdateResult = new MultiInstanceHelpers.CoroutineResultWrapper<bool>();
                            yield return MultiInstanceHelpers.WaitForCondition(() => varInt.Value == updatedValue, varUpdateResult);
                            Assert.That(varUpdateResult.Result, Is.True, "Variable should be updated on the clients");

                            nbVarsCheckedClientSide++;
                        }
                    }
                }

                Assert.That(nbVarsCheckedClientSide, Is.EqualTo(serverNetVarsToUpdate.Count), $"Client expected to have received {serverNetVarsToUpdate.Count} updates");
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
