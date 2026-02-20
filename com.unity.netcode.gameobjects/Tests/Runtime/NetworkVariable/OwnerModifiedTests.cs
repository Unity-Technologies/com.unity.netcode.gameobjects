using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{

    internal class OwnerModifiedObject : NetworkBehaviour, INetworkUpdateSystem
    {
        public NetworkList<int> MyNetworkList;

        internal static int Updates = 0;

        public static bool EnableVerbose;

        private void Awake()
        {
            MyNetworkList = new NetworkList<int>(new List<int>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
            MyNetworkList.OnListChanged += Changed;
        }

        public void Changed(NetworkListEvent<int> listEvent)
        {
            var expected = 0;
            var listString = "";
            foreach (var i in MyNetworkList)
            {
                Assert.AreEqual(i, expected);
                expected++;
                listString += i.ToString();
            }
            if (EnableVerbose)
            {
                Debug.Log($"[{NetworkManager.LocalClientId}] Value changed to {listString}");
            }

            Updates++;
        }

        public bool AddValues;

        public NetworkUpdateStage NetworkUpdateStageToCheck;

        private int m_ValueToUpdate;

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (updateStage == NetworkUpdateStageToCheck)
            {
                if (AddValues)
                {
                    MyNetworkList.Add(m_ValueToUpdate++);
                    AddValues = false;
                }
            }
        }

        public override void OnDestroy()
        {
            NetworkUpdateLoop.UnregisterAllNetworkUpdates(this);
            base.OnDestroy();
        }

        public void InitializeLastCient()
        {
            NetworkUpdateLoop.RegisterAllNetworkUpdates(this);
        }
    }

    internal class ChangeValueOnAuthority : NetworkBehaviour
    {
        public NetworkVariable<int> SomeIntValue = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                SomeIntValue.Value++;
            }
            base.OnNetworkSpawn();
        }

        protected override void OnNetworkPostSpawn()
        {
            if (HasAuthority)
            {
                SomeIntValue.Value++;
            }
            base.OnNetworkPostSpawn();
        }
    }

    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    internal class OwnerModifiedTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private GameObject m_SpawnObject;

        public OwnerModifiedTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<OwnerModifiedObject>();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_SpawnObject = CreateNetworkObjectPrefab("SpawnObj");
            m_SpawnObject.AddComponent<ChangeValueOnAuthority>();
            base.OnServerAndClientsCreated();
        }

        private NetworkManager m_LastClient;

        protected override void OnNewClientStartedAndConnected(NetworkManager networkManager)
        {
            m_LastClient = networkManager;
            base.OnNewClientStartedAndConnected(networkManager);
        }

        /// <summary>
        /// Addresses MTT-4386 #2109
        /// Verify NetworkVariable updates are not repeated on some clients.
        /// TODO: This test needs to be re-written/overhauled.
        /// </summary>
        [UnityTest]
        public IEnumerator VerifyDoesNotRepeatOnSomeClients()
        {
            var authority = GetAuthorityNetworkManager();
            OwnerModifiedObject.EnableVerbose = m_EnableVerboseDebug;
            // We use this to assure we are the "last client" connected.
            yield return CreateAndStartNewClient();
            var ownerModLastClient = m_LastClient.LocalClient.PlayerObject.GetComponent<OwnerModifiedObject>();
            ownerModLastClient.InitializeLastCient();

            // Run through all update loops setting the value once every 5 frames
            foreach (var updateLoopType in System.Enum.GetValues(typeof(NetworkUpdateStage)))
            {
                ownerModLastClient.NetworkUpdateStageToCheck = (NetworkUpdateStage)updateLoopType;
                VerboseDebug($"Testing Update Stage: {ownerModLastClient.NetworkUpdateStageToCheck}");
                ownerModLastClient.AddValues = true;
                yield return WaitForTicks(authority, 5);
            }

            yield return WaitForTicks(authority, 5);

            // We'll have at least one update per stage per client, if all goes well.
            Assert.True(OwnerModifiedObject.Updates > 20);
        }

        private ChangeValueOnAuthority m_InstanceAuthority;

        private bool NetworkVariablesMatch(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var changeValue = networkManager.SpawnManager.SpawnedObjects[m_InstanceAuthority.NetworkObjectId].GetComponent<ChangeValueOnAuthority>();
                if (networkManager == m_InstanceAuthority.NetworkManager)
                {
                    if (m_InstanceAuthority.SomeIntValue.Value != 2)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {changeValue.name} value is {changeValue.SomeIntValue.Value} but was expecting 2!");
                    }
                    continue;
                }
                if (changeValue.SomeIntValue.Value != 2)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {changeValue.name} value is {changeValue.SomeIntValue.Value} but was expecting 2!");
                }

                if (changeValue.SomeIntValue.Value != m_InstanceAuthority.SomeIntValue.Value)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {changeValue.name} value is {changeValue.SomeIntValue.Value} but was expecting {m_InstanceAuthority.SomeIntValue.Value}!");
                }
            }

            return errorLog.Length == 0;
        }

        /// <summary>
        /// Verifies that when running a distributed authority network topology
        /// </summary>
        [UnityTest]
        public IEnumerator OwnershipSpawnedAndUpdatedDuringSpawn()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();
            // If running in distributed authority mode, we use the nonauthority (i.e. not SessionOwner) instance to spawn.
            var spawnAuthority = m_DistributedAuthority ? nonAuthority : authority;
            m_InstanceAuthority = SpawnObject(m_SpawnObject, spawnAuthority).GetComponent<ChangeValueOnAuthority>();

            yield return WaitForSpawnedOnAllOrTimeOut(m_InstanceAuthority.NetworkObjectId);
            AssertOnTimeout($"Failed to spawn {m_InstanceAuthority.name} on all clients!");

            yield return WaitForConditionOrTimeOut(NetworkVariablesMatch);
            AssertOnTimeout($"The {nameof(ChangeValueOnAuthority.SomeIntValue)} failed to synchronize on all clients!");
        }
    }
}
