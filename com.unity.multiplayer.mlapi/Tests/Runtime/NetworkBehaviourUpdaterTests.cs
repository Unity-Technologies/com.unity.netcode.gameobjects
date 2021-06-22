using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.NetworkVariable;
using MLAPI.RuntimeTests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MLAPI.RuntimeTests
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

        [UnityTest]
        public IEnumerator BehaviourUpdaterAllTests([Values(0, 1, 2)] int nbClients, [Values] bool useHost, [Values(0, 1, 2)] int nbSpawnedObjects,
            [ValueSource(nameof(s_TypesToTest)) ] Type firstNetworkBehaviour, [ValueSource(nameof(s_TypesToTest)) ] Type secondNetworkBehaviour, [ValueSource(nameof(s_TypesToTest)) ] Type thirdNetworkBehaviour)
        // public IEnumerator BehaviourUpdaterAllTests([Values(1)] int nbClients, [Values(true)] bool useHost, [Values(1)] int nbSpawnedObjects,
        // [Values(typeof(OneNetVar)) ] Type firstNetworkBehaviour, [Values(typeof(OneNetVar)) ] Type secondNetworkBehaviour, [Values(typeof(OneNetVar)) ] Type thirdNetworkBehaviour)
        {
            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ClientNetworkManagers = clients;
            m_ServerNetworkManager = server;

            void AddNetworkBehaviour(Type type, GameObject prefab)
            {
                if (type != null)
                {
                    var info = prefab.AddComponent(type) as INetVarInfo;
                    // return info.allNetVars;
                }
                // return new List<NetworkVariableInt>();
            }
            var prefabsToSpawn = new List<GameObject>();

            for (int i = 0; i < nbSpawnedObjects; i++)
            {
                var prefabToSpawn = new GameObject();
                var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
                AddNetworkBehaviour(firstNetworkBehaviour, prefabToSpawn);
                AddNetworkBehaviour(secondNetworkBehaviour, prefabToSpawn);
                AddNetworkBehaviour(thirdNetworkBehaviour, prefabToSpawn);
                MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObjectPrefab);
                m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new Configuration.NetworkPrefab() { Prefab = prefabToSpawn });
                foreach (var clientNetworkManager in m_ClientNetworkManagers)
                {
                    clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new Configuration.NetworkPrefab() { Prefab = prefabToSpawn });
                }
                // networkObjectPrefab.NetworkManagerOwner = m_ServerNetworkManager;
                prefabsToSpawn.Add(prefabToSpawn);
            }

            // Tests with a varying number of network behaviours, each with a varying number of network variables
            // yield return StartSomeClientsAndServerWithPlayers(useHost: useHost, nbClients: nbClients, playerPrefab => { }, hasPlayer: false);
            // Start the instances
            if (!MultiInstanceHelpers.Start(useHost, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clientCount: useHost ? nbClients + 1 : nbClients));

            var serverNetVarsToUpdate = new List<NetworkVariableInt>();
            foreach (var prefab in prefabsToSpawn)
            {
                var spawnedObject = Object.Instantiate(prefab);
                var networkSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
                networkSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;
                networkSpawnedObject.Spawn();
                foreach (var networkBehaviour in spawnedObject.GetComponents<NetworkBehaviour>())
                {
                    serverNetVarsToUpdate.AddRange(((INetVarInfo)networkBehaviour).allNetVars);
                }
            }

            var serverNetVarCount = serverNetVarsToUpdate.Count;

            yield return new WaitForSeconds(0); // wait a frame

            foreach (var netVar in serverNetVarsToUpdate)
            {
                Assert.That(netVar.Value, Is.EqualTo(0)); // sanity check
            }

            foreach (var netVar in serverNetVarsToUpdate)
            {
                netVar.Value = 1;
                Assert.That(netVar.IsDirty, Is.True);
            }

            m_ServerNetworkManager.BehaviourUpdater.CurrentTick = 0; // reset to zero so we can call it here, else need to do a wait for update

            m_ServerNetworkManager.BehaviourUpdater.NetworkBehaviourUpdate(m_ServerNetworkManager);

            // yield return new WaitForSeconds(0); // wait a frame so NetworkBehaviourUpdater has time to update values

            foreach (var netVar in serverNetVarsToUpdate)
            {
                Assert.That(netVar.IsDirty, Is.Not.True);
            }




            // yield return new WaitForSeconds(1); // wait for clients to update

            var nbVarsCheckedClientSide = 0;
            foreach (var client in m_ClientNetworkManagers)
            {
                foreach (var spawnedObject in client.SpawnManager.SpawnedObjects)
                {
                    foreach (var behaviour in spawnedObject.Value.GetComponentsInChildren<NetworkBehaviour>())
                    {
                        foreach (var networkVariable in behaviour.NetworkVariableFields)
                        {

                            var varInt = networkVariable as NetworkVariableInt;
                            yield return MultiInstanceHelpers.WaitForCondition(() => varInt.Value == 1);
                            nbVarsCheckedClientSide++;
                            Assert.That(varInt.Value, Is.EqualTo(1));
                        }
                    }
                }
            }

            Assert.That(nbVarsCheckedClientSide, Is.EqualTo(m_ClientNetworkManagers.Length > 0 ? serverNetVarCount : 0));

            if (serverNetVarCount > 0)
            {
                m_ServerNetworkManager.BehaviourUpdater.NetworkBehaviourUpdate(m_ServerNetworkManager);
                serverNetVarsToUpdate[0].Value = -1;
                m_ServerNetworkManager.BehaviourUpdater.NetworkBehaviourUpdate(m_ServerNetworkManager);
                Assert.That(serverNetVarsToUpdate[0].IsDirty, Is.True); // check that network behaviour updater can only be called once per frame
            }

            // test with 0, 1, 2 clients
            // test with host and server
            // test with 0, 1, 2 spawned objects
            // test with 0, 1, 2 network behaviour per object
            // test with 0, 1, 2 network variable per network behaviour
            // for each, update netvar
            // for each check value changed
            // check that all network variables are no longer dirty after update
            // test execute network behaviour more than once per tick (should fail)
        }
    }

    public interface INetVarInfo
    {
        public List<NetworkVariableInt> allNetVars { get; }
    }

    public class ZeroNetVar : NetworkBehaviour, INetVarInfo
    {
        public List<NetworkVariableInt> allNetVars => new List<NetworkVariableInt>();
    }

    public class OneNetVar : NetworkBehaviour, INetVarInfo
    {
        private NetworkVariableInt m_SomeValue = new NetworkVariableInt();
        public List<NetworkVariableInt> allNetVars => new List<NetworkVariableInt>() { m_SomeValue };
    }

    public class TwoNetVar : NetworkBehaviour, INetVarInfo
    {
        private NetworkVariableInt m_SomeValue = new NetworkVariableInt();
        private NetworkVariableInt m_SomeOtherValue = new NetworkVariableInt();
        public List<NetworkVariableInt> allNetVars => new List<NetworkVariableInt>() { m_SomeValue, m_SomeOtherValue };
    }
}
