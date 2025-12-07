using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.Server)]
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.Host)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, HostOrServer.DAHost)]
    internal class NetworkVariableBaseInitializesWhenPersisted : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private static GameObject s_NetworkPrefab;

        public NetworkVariableBaseInitializesWhenPersisted(NetworkTopologyTypes networkTopologyTypes, HostOrServer hostOrServer) : base(networkTopologyTypes, hostOrServer) { }

        protected override void OnOneTimeSetup()
        {
            // Create a prefab to persist for all tests
            s_NetworkPrefab = new GameObject("PresistPrefab");
            var networkObject = s_NetworkPrefab.AddComponent<NetworkObject>();
            networkObject.GlobalObjectIdHash = 8888888;
            networkObject.SetSceneObjectStatus(false);
            s_NetworkPrefab.AddComponent<TestBehaviour>();
            s_NetworkPrefab.AddComponent<ObjectNameIdentifier>();
            // Create enough prefab instance handlers to be re-used for all tests.
            PrefabInstanceHandler.OneTimeSetup(NumberOfClients + 1, s_NetworkPrefab);
            Object.DontDestroyOnLoad(s_NetworkPrefab);
            base.OnOneTimeSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            // Repeat these steps for clients
            foreach (var manager in m_NetworkManagers)
            {
                PrefabInstanceHandler.AssignHandler(manager);
                manager.AddNetworkPrefab(s_NetworkPrefab);
            }

            // !!! IMPORTANT !!!
            // Disable the persisted network prefab so it isn't spawned nor destroyed
            s_NetworkPrefab.SetActive(false);
            base.OnServerAndClientsCreated();
        }

        private List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();

        [UnityTest]
        public IEnumerator PrefabSessionIstantiationPass([Values(4, 3, 2, 1)] int iterationsLeft)
        {
            // Start out waiting for a long duration before updating the NetworkVariable so each
            // next iteration we change it earlier than the previous. This validates that the
            // NetworkVariable's last update time is being reset each time a persisted NetworkObject
            // is being spawned.
            var baseWaitTime = 0.35f;
            var waitPeriod = baseWaitTime * iterationsLeft;

            foreach (var networkManager in m_NetworkManagers)
            {
                // Validate Pooled Objects Persisted Between Tests
                // We start with having 4 iterations (including the first of the 4), which means that when we have 4
                // iterations remaining we haven't spawned any instances so it will test that there are no instances.
                // When we there are 3 iterations left, every persisted instance of the network prefab should have been
                // spawned at least once...when 2 then all should have been spawned twice...when 1 then all should been
                // spawned three times.
                Assert.True(PrefabInstanceHandler.ValidatePersistedInstances(networkManager, 4 - iterationsLeft), "Failed validation of persisted pooled objects!");

                // Spawn 1 NetworkObject per NetworkManager with the NetworkManager's client being the owner
                var networkManagerToSpawn = m_DistributedAuthority ? networkManager : m_ServerNetworkManager;
                var objectToSpawn = PrefabInstanceHandler.GetInstanceToSpawn(networkManagerToSpawn);
                objectToSpawn.NetworkManagerOwner = networkManagerToSpawn;
                objectToSpawn.SpawnWithOwnership(networkManager.LocalClientId);
                m_SpawnedObjects.Add(objectToSpawn);
            }

            // Conditional wait for all clients to spawn all objects
            bool AllInstancesSpawnedOnAllCLients()
            {
                foreach (var spawnedObject in m_SpawnedObjects)
                {
                    foreach (var networkManager in m_NetworkManagers)
                    {
                        if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(spawnedObject.NetworkObjectId))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            // Wait for all clients to locally clone and spawn all spawned NetworkObjects
            yield return WaitForConditionOrTimeOut(AllInstancesSpawnedOnAllCLients);
            AssertOnTimeout($"Failed to spawn all instances on all clients!");

            // Wait for the continually decreasing waitPeriod to validate that
            // NetworkVariableBase has reset the last update time.
            yield return new WaitForSeconds(waitPeriod);

            // Have the owner of each spawned NetworkObject assign a new NetworkVariable
            foreach (var spawnedObject in m_SpawnedObjects)
            {
                var testBehaviour = spawnedObject.GetComponent<TestBehaviour>();
                if (!m_DistributedAuthority)
                {
                    var client = m_NetworkManagers.Where((c) => c.LocalClientId == testBehaviour.OwnerClientId).First();
                    testBehaviour = client.SpawnManager.SpawnedObjects[testBehaviour.NetworkObjectId].GetComponent<TestBehaviour>();
                }
                testBehaviour.TestNetworkVariable.Value = Random.Range(0, 1000);
            }

            // Wait for half of the base time before checking to see if the time delta is within the acceptable range.
            // The time delta is the time from spawn to when the value changes. Each iteration of this test decreases
            // the total wait period, and so the delta should always be less than the wait period plus the base wait time.
            yield return new WaitForSeconds(baseWaitTime * 0.5f);

            // Conditional to verify the time delta between spawn and the NetworkVariable being updated is within the
            // expected range
            bool AllSpawnedObjectsUpdatedWithinTimeSpan()
            {
                foreach (var spawnedObject in m_SpawnedObjects)
                {
                    foreach (var networkManager in m_NetworkManagers)
                    {
                        if (spawnedObject.OwnerClientId == networkManager.LocalClientId)
                        {
                            continue;
                        }
                        if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(spawnedObject.NetworkObjectId))
                        {
                            return false;
                        }
                        var instance = networkManager.SpawnManager.SpawnedObjects[spawnedObject.NetworkObjectId].GetComponent<TestBehaviour>();
                        // Check to make sure all clients' delta between updates is not greater than the wait period plus the baseWaitTime.
                        // Ignore the first iteration as that becomes our baseline.
                        if (iterationsLeft < 4 && instance.LastUpdateDelta >= (waitPeriod + baseWaitTime))
                        {
                            VerboseDebug($"Last Spawn Delta = {instance.LastUpdateDelta} is greater or equal to {waitPeriod + baseWaitTime}");
                            return false;
                        }
                    }
                }
                return true;
            }

            yield return WaitForConditionOrTimeOut(AllSpawnedObjectsUpdatedWithinTimeSpan);
            AssertOnTimeout($"Failed to reset NetworkVariableBase instances!");
        }


        protected override IEnumerator OnTearDown()
        {
            m_SpawnedObjects.Clear();
            PrefabInstanceHandler.ReleaseAll();
            yield return base.OnTearDown();
        }

        protected override void OnOneTimeTearDown()
        {
            Object.DestroyImmediate(s_NetworkPrefab);
            s_NetworkPrefab = null;
            PrefabInstanceHandler.ReleaseAll(true);
            base.OnOneTimeTearDown();
        }

        /// <summary>
        /// Test NetworkBehaviour that updates a NetworkVariable and tracks the time between
        /// spawn and when the NetworkVariable is updated.
        /// </summary>
        public class TestBehaviour : NetworkBehaviour
        {
            public NetworkVariable<int> TestNetworkVariable = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

            public float LastUpdateDelta { get; private set; }
            private float m_TimeSinceLastUpdate = 0.0f;

            // How many times has this instance been spawned.
            public int SpawnedCount { get; private set; }

            public override void OnNetworkSpawn()
            {
                SpawnedCount++;
                if (IsOwner)
                {
                    TestNetworkVariable.Value = 0;
                }
                base.OnNetworkSpawn();
            }

            protected override void OnNetworkPostSpawn()
            {
                if (!IsOwner)
                {
                    TestNetworkVariable.OnValueChanged += OnTestValueChanged;
                }
                m_TimeSinceLastUpdate = Time.realtimeSinceStartup;
                base.OnNetworkPostSpawn();
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
            }

            public override void OnNetworkDespawn()
            {
                TestNetworkVariable.OnValueChanged -= OnTestValueChanged;
                LastUpdateDelta = 0.0f;
                base.OnNetworkDespawn();
            }

            private void OnTestValueChanged(int previous, int current)
            {
                LastUpdateDelta = Time.realtimeSinceStartup - m_TimeSinceLastUpdate;
            }
        }

        /// <summary>
        /// Creates a specified number of instances that persist throughout the entire test session
        /// and will only be destroyed/released during the OneTimeTeardown
        /// </summary>
        public class PrefabInstanceHandler : INetworkPrefabInstanceHandler
        {
            private static Dictionary<NetworkManager, PrefabInstanceHandler> s_AssignedInstances = new Dictionary<NetworkManager, PrefabInstanceHandler>();
            private static Queue<PrefabInstanceHandler> s_PrefabInstanceHandlers = new Queue<PrefabInstanceHandler>();
            public Queue<GameObject> PrefabInstances = new Queue<GameObject>();
            private GameObject m_NetworkPrefab;
            private NetworkManager m_NetworkManager;
            private ulong m_AssignedClientId;

            public static void OneTimeSetup(int numberOfInstances, GameObject prefabInstance)
            {
                for (int i = 0; i < numberOfInstances; i++)
                {
                    s_PrefabInstanceHandlers.Enqueue(new PrefabInstanceHandler(prefabInstance));
                }
            }

            /// <summary>
            /// Invoke when <see cref="NetworkManager"/>s are created but not started.
            /// </summary>
            /// <param name="networkManager">The <see cref="NetworkManager"/> instance to assign a handler to. Must not be null and must not already have a handler assigned.</param>
            public static void AssignHandler(NetworkManager networkManager)
            {
                if (s_PrefabInstanceHandlers.Count > 0)
                {
                    var instance = s_PrefabInstanceHandlers.Dequeue();
                    instance.Initialize(networkManager);
                    s_AssignedInstances.Add(networkManager, instance);
                }
                else
                {
                    Debug.LogError($"[{nameof(PrefabInstanceHandler)}] Exhausted total number of instances available!");
                }
            }

            public static NetworkObject GetInstanceToSpawn(NetworkManager networkManager)
            {
                if (s_AssignedInstances.ContainsKey(networkManager))
                {
                    return s_AssignedInstances[networkManager].GetInstance();
                }
                return null;
            }


            public static bool ValidatePersistedInstances(NetworkManager networkManager, int minCount)
            {
                if (s_AssignedInstances.ContainsKey(networkManager))
                {
                    var prefabInstanceHandler = s_AssignedInstances[networkManager];
                    return prefabInstanceHandler.ValidateInstanceSpawnCount(minCount);
                }
                return false;
            }

            /// <summary>
            /// Releases back to the queue and if destroy is true it will completely
            /// remove all references so they are cleaned up when
            /// </summary>
            /// <param name="destroy">If true, completely removes all references and cleans up instances. If false, returns handlers to the queue for reuse.</param>
            public static void ReleaseAll(bool destroy = false)
            {
                foreach (var entry in s_AssignedInstances)
                {
                    entry.Value.DeregisterHandler();
                    if (!destroy)
                    {
                        s_PrefabInstanceHandlers.Enqueue(entry.Value);
                    }
                    else if (entry.Value.PrefabInstances.Count > 0)
                    {
                        entry.Value.CleanInstances();
                    }
                }
                s_AssignedInstances.Clear();

                if (destroy)
                {
                    while (s_PrefabInstanceHandlers.Count > 0)
                    {
                        s_PrefabInstanceHandlers.Dequeue();
                    }
                }
            }

            public PrefabInstanceHandler(GameObject gameObject)
            {
                m_NetworkPrefab = gameObject;
            }


            public void Initialize(NetworkManager networkManager)
            {
                m_NetworkManager = networkManager;
                networkManager.PrefabHandler.AddHandler(m_NetworkPrefab, this);
            }

            /// <summary>
            /// This validates that the instances persisted to the next test set and persisted
            /// between network sessions
            /// </summary>
            public bool ValidateInstanceSpawnCount(int minCount)
            {
                // First pass we should have no instances
                if (minCount == 0)
                {
                    return PrefabInstances.Count == 0;
                }
                else
                {
                    foreach (var instance in PrefabInstances)
                    {
                        var testBehaviour = instance.GetComponent<TestBehaviour>();
                        if (testBehaviour.SpawnedCount < minCount)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            /// <summary>
            /// When we are done with all tests, we finally destroy the persisted objects
            /// </summary>
            public void CleanInstances()
            {
                while (PrefabInstances.Count > 0)
                {
                    var instance = PrefabInstances.Dequeue();
                    Object.DestroyImmediate(instance);
                }
            }

            public void DeregisterHandler()
            {
                if (m_NetworkManager != null && m_NetworkManager.PrefabHandler != null)
                {
                    m_NetworkManager.PrefabHandler.RemoveHandler(m_NetworkPrefab);
                }
            }

            public NetworkObject GetInstance()
            {
                var instanceToReturn = (NetworkObject)null;
                if (PrefabInstances.Count == 0)
                {
                    instanceToReturn = Object.Instantiate(m_NetworkPrefab).GetComponent<NetworkObject>();
                    instanceToReturn.SetSceneObjectStatus(false);
                    instanceToReturn.gameObject.SetActive(true);
                }
                else
                {
                    instanceToReturn = PrefabInstances.Dequeue().GetComponent<NetworkObject>();
                    instanceToReturn.gameObject.SetActive(true);
                    SceneManager.MoveGameObjectToScene(instanceToReturn.gameObject, SceneManager.GetActiveScene());
                }
                return instanceToReturn;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                return GetInstance();
            }

            public void Destroy(NetworkObject networkObject)
            {
                if (m_NetworkPrefab != null && m_NetworkPrefab.gameObject == networkObject.gameObject)
                {
                    return;
                }
                Object.DontDestroyOnLoad(networkObject.gameObject);
                networkObject.gameObject.SetActive(false);
                PrefabInstances.Enqueue(networkObject.gameObject);
            }
        }
    }
}
