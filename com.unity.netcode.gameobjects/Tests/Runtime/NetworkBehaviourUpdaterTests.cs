using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public class NetVarContainer : NetworkBehaviour
    {
        public static GameObject CreatePrefabGameObject(List<NetVarsToCheck> netVarsToCheck)
        {
            var gameObject = new GameObject();
            gameObject.name = "NetVarContainerObject";
            var networkObject = gameObject.AddComponent<NetworkObject>();

            foreach (var netVarContainerType in netVarsToCheck)
            {
                var netVarContainer = gameObject.AddComponent<NetVarContainer>();
                netVarContainer.NumberOfNetVarsToCheck = netVarContainerType;
                netVarContainer.ValueToSetNetVarTo = NetworkBehaviourUpdaterTests.NetVarValueToSet;
            }

            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            return gameObject;
        }

        public enum NetVarsToCheck
        {
            One,
            Two
        }

        public NetVarsToCheck NumberOfNetVarsToCheck;
        public int ValueToSetNetVarTo = 0;

        public bool HaveAllValuesChanged(int valueToCheck)
        {
            var allValuesChanged = false;
            switch (NumberOfNetVarsToCheck)
            {
                case NetVarsToCheck.Two:
                    {
                        allValuesChanged = (m_FirstValue.Value == valueToCheck && m_SeconValue.Value == valueToCheck);
                        break;
                    }
                case NetVarsToCheck.One:
                    {
                        allValuesChanged = (m_FirstValue.Value == valueToCheck);
                        break;
                    }
            }
            return allValuesChanged;
        }

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

        private NetworkVariable<int> m_FirstValue = new NetworkVariable<int>();
        private NetworkVariable<int> m_SeconValue = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
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
            else
            {
                NetworkBehaviourUpdaterTests.ClientSideNotifyObjectSpawned(gameObject);
            }
        }
    }


    public class NetworkBehaviourUpdaterTests : BaseMultiInstanceTest
    {
        // Go ahead and create maximum number of clients (not all tests will use them)
        protected override int NbClients => 2;
        private const float k_TimeOutWaitPeriod = 2.0f;
        public const int NetVarValueToSet = 1;
        private static float s_TimeOutPeriod;
        private static List<GameObject> s_ClientSpawnedNetworkObjects = new List<GameObject>();
        public static void ClientSideNotifyObjectSpawned(GameObject objectSpaned)
        {
            if (!s_ClientSpawnedNetworkObjects.Contains(objectSpaned))
            {
                s_ClientSpawnedNetworkObjects.Add(objectSpaned);
                // As long as we are getting notified the clients are spawning objects
                // then bump up the timeout period
                BumpTimeOut();
            }
        }

        private static void BumpTimeOut()
        {
            s_TimeOutPeriod = Time.realtimeSinceStartup + k_TimeOutWaitPeriod;
        }

        private static bool HasTimedOut()
        {
            return s_TimeOutPeriod <= Time.realtimeSinceStartup;
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // This prevents clients and server from being started during Setup;
            m_BypassStartAndWaitForClients = true;
        }

        private List<NetworkManager> m_ActiveClientsForCurrentTest;

        private IEnumerator StartClientsAndServer(bool useHost, int numberOfClients, GameObject prefabObject)
        {
            Assert.True(numberOfClients <= m_ClientNetworkManagers.Length);
            m_ActiveClientsForCurrentTest = new List<NetworkManager>();
            for (int i = 0; i < numberOfClients; i++)
            {
                m_ActiveClientsForCurrentTest.Add(m_ClientNetworkManagers[i]);
            }

            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabObject });
            foreach (var clientManager in m_ActiveClientsForCurrentTest)
            {
                clientManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabObject });
            }

            // Now spin everything up normally
            var clientsAsArry = m_ActiveClientsForCurrentTest.ToArray();
            Assert.True(MultiInstanceHelpers.Start(useHost, m_ServerNetworkManager, clientsAsArry), "Failed to start server and client instances");

            if (numberOfClients > 0)
            {
                RegisterSceneManagerHandler();
            }

            // Wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clientsAsArry));

            // Wait for connection on server side
            var clientsToWaitFor = useHost ? numberOfClients + 1 : numberOfClients;
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(m_ServerNetworkManager, clientsToWaitFor));
        }

        [UnityTest]
        public IEnumerator BehaviourUpdaterAllTests([Values(0, 1, 2)] int nbClients, [Values] bool useHost, [Values(1, 2)] int numToSpawn,
    [Values(NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)] NetVarContainer.NetVarsToCheck firstType,
    [Values(NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)] NetVarContainer.NetVarsToCheck secondType)
        {
            s_ClientSpawnedNetworkObjects.Clear();
            var netVarTypeList = new List<NetVarContainer.NetVarsToCheck>() { firstType, secondType };
            var prefabToSpawn = NetVarContainer.CreatePrefabGameObject(netVarTypeList);

            yield return StartClientsAndServer(useHost, nbClients, prefabToSpawn);
            var spawnedPrefabs = new List<GameObject>();

            // spawn the objects
            for (int i = 0; i < numToSpawn; i++)
            {
                var spawnedObject = Object.Instantiate(prefabToSpawn);
                spawnedPrefabs.Add(spawnedObject);
                var networkSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
                networkSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;
                networkSpawnedObject.Spawn();
            }

            // Update the NetworkBehaviours to make sure all network variables are no longer marked as dirty
            m_ServerNetworkManager.BehaviourUpdater.NetworkBehaviourUpdate(m_ServerNetworkManager);

            // Verify that all network variables are no longer dirty on server side
            // TODO: Check with Jeffrey and Matt to make sure this is correct but it appears server only will never update
            // the dirty status if no clients are connected
            if (nbClients > 0 || useHost)
            {
                foreach (var serverSpawnedObject in spawnedPrefabs)
                {
                    var netVarContainers = serverSpawnedObject.GetComponents<NetVarContainer>();
                    foreach (var netVarContainer in netVarContainers)
                    {
                        Assert.False(netVarContainer.AreNetVarsDirty(), "Some NetworkVariables were still marked dirty after NetworkBehaviourUpdate!");
                    }
                }
            }

            var tickInterval = 1.0f / m_ServerNetworkManager.NetworkConfig.TickRate;
            // Now wait for all clients to spawn the objects in question
            var numberOfObjectsToSpawnOnClients = numToSpawn * nbClients;

            // When there are no clients (excluding when server is in host mode), we can skip all of this
            if (numberOfObjectsToSpawnOnClients > 0)
            {
                var allClientsSpawnedObjects = false;
                BumpTimeOut();
                while (!allClientsSpawnedObjects && !HasTimedOut())
                {
                    allClientsSpawnedObjects = numberOfObjectsToSpawnOnClients == s_ClientSpawnedNetworkObjects.Count;
                    yield return new WaitForSeconds(tickInterval);
                }

                Assert.True(!HasTimedOut(), $"Timed out waiting for clients to report spawning objects! " +
                    $"Total reported client-side spawned objects {s_ClientSpawnedNetworkObjects.Count}");

                // This really should never fail as it should timeout first
                Assert.True(allClientsSpawnedObjects, "Not all clients spawned their objects!");

                var clientSideNetVarContainers = new List<NetVarContainer>();
                foreach (var clientSpawnedObjects in s_ClientSpawnedNetworkObjects)
                {
                    var netVarContainers = clientSpawnedObjects.GetComponents<NetVarContainer>();
                    foreach (var netvarContiner in netVarContainers)
                    {
                        clientSideNetVarContainers.Add(netvarContiner);
                    }
                }

                var allClientsCompleted = false;
                BumpTimeOut();
                while (!allClientsCompleted && !HasTimedOut())
                {
                    yield return new WaitForSeconds(tickInterval);

                    var completedCount = 0;
                    foreach (var clientNetVarContainer in clientSideNetVarContainers)
                    {
                        if (clientNetVarContainer.HaveAllValuesChanged(NetVarValueToSet))
                        {
                            completedCount++;
                        }
                    }

                    allClientsCompleted = completedCount == clientSideNetVarContainers.Count;
                }

                Assert.True(!HasTimedOut(), $"Timed out waiting for client side NetVarContainers to report all NetworkVariables have been updated!");

                // This really should never fail as it should timeout first
                Assert.True(allClientsCompleted, "Not all client side NetworkVariables reported they were updated with target value!");
            }

            Object.DestroyImmediate(prefabToSpawn);
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            foreach (var spawnedObject in s_ClientSpawnedNetworkObjects)
            {
                Object.DestroyImmediate(spawnedObject);
            }
            return base.Teardown();
        }
    }
}
