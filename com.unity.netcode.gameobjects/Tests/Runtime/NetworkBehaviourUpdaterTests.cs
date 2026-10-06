using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// This is a refactor of the original test's NetworkBehaviour INetVarInfo derived NetworkBehaviours
    /// </summary>
    internal class NetVarContainer : NetworkBehaviour
    {
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

        public void SetOwnerWrite()
        {
            m_FirstValue = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
            m_SeconValue = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        }

        /// <summary>
        /// Server side only, sets the NetworkVariables being used to the ValueToSetNetVarTo
        /// that is pre-configured when the Network Prefab is created.
        /// </summary>
        public void SetNetworkVariableValues()
        {
            if ((NetworkManager.DistributedAuthorityMode && IsOwner) || (!NetworkManager.DistributedAuthorityMode && IsServer))
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
    internal struct NetVarCombinationTypes
    {
        public NetVarContainer.NetVarsToCheck FirstType;
        public NetVarContainer.NetVarsToCheck SecondType;
    }

    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
#if UNIFIED_NETCODE
    [TestFixture(HostOrServer.UnifiedServer)]
    [TestFixture(HostOrServer.UnifiedHost)]
#endif
    internal class NetworkBehaviourUpdaterTests : NetcodeIntegrationTest
    {
        public const int NetVarValueToSet = 1;
        private const int k_MaxClients = 2;

        private static readonly NetVarCombinationTypes[] k_NetVarCombinations =
        {
            new NetVarCombinationTypes { FirstType = NetVarContainer.NetVarsToCheck.One, SecondType = NetVarContainer.NetVarsToCheck.One },
            new NetVarCombinationTypes { FirstType = NetVarContainer.NetVarsToCheck.One, SecondType = NetVarContainer.NetVarsToCheck.Two },
            new NetVarCombinationTypes { FirstType = NetVarContainer.NetVarsToCheck.Two, SecondType = NetVarContainer.NetVarsToCheck.Two },
        };

        private static readonly int[] k_NumberToSpawn = { 1, 2 };

        // The starting client count. Clients are added during the test.
        protected override int NumberOfClients => m_MinimumClients;

#if UNIFIED_NETCODE
        protected override bool UseUnifiedTests()
        {
            return true;
        }
#endif

        private readonly int m_MinimumClients;
        private readonly GameObject[] m_Prefabs = new GameObject[k_NetVarCombinations.Length];
        private readonly List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();
        private readonly List<NetVarContainer> m_ClientSideNetVarContainers = new List<NetVarContainer>();
        private readonly StringBuilder m_ErrorLog = new StringBuilder();

        public NetworkBehaviourUpdaterTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
            // Server and distributed authority modes require at least 1 client while the host does not.
            m_MinimumClients = hostOrServer == HostOrServer.DAHost || !m_UseHost ? 1 : 0;
        }

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < k_NetVarCombinations.Length; i++)
            {
                var combination = k_NetVarCombinations[i];
                m_Prefabs[i] = CreateNetworkObjectPrefab($"NetVarCont-{combination.FirstType}-{combination.SecondType}");
                AddNetVarContainer(m_Prefabs[i], combination.FirstType);
                AddNetVarContainer(m_Prefabs[i], combination.SecondType);
            }
            base.OnServerAndClientsCreated();
        }

        private void AddNetVarContainer(GameObject prefab, NetVarContainer.NetVarsToCheck netVarsToCheck)
        {
            var netVarContainer = prefab.AddComponent<NetVarContainer>();
            if (m_NetworkTopologyType == NetworkTopologyTypes.DistributedAuthority)
            {
                netVarContainer.SetOwnerWrite();
            }
            netVarContainer.NumberOfNetVarsToCheck = netVarsToCheck;
            netVarContainer.ValueToSetNetVarTo = NetVarValueToSet;
        }

        private bool AllClientsSpawnedObjects()
        {
            m_ErrorLog.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                foreach (var spawnedObject in m_SpawnedObjects)
                {
                    if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(spawnedObject.NetworkObjectId))
                    {
                        m_ErrorLog.AppendLine($"[{networkManager.name}] Has not spawned {nameof(NetworkObject)}-{spawnedObject.NetworkObjectId}.");
                    }
                }
            }
            return m_ErrorLog.Length == 0;
        }

        private bool AllClientSideValuesChanged()
        {
            foreach (var netVarContainer in m_ClientSideNetVarContainers)
            {
                if (!netVarContainer.HaveAllValuesChanged(NetVarValueToSet))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Runs each NetVar combination and spawn count for each client count in one session.<br />
        /// A client is added between client count passes.<br />
        /// Each case despawns its NetworkObjects before the next case starts.
        /// </summary>
        [UnityTest]
        public IEnumerator BehaviourUpdaterAllTests()
        {
            var authority = GetAuthorityNetworkManager();
            for (int clientCount = m_MinimumClients; clientCount <= k_MaxClients; clientCount++)
            {
                if (clientCount > m_MinimumClients)
                {
                    yield return CreateAndStartNewClient();
                }

                for (int i = 0; i < k_NetVarCombinations.Length; i++)
                {
                    foreach (var numToSpawn in k_NumberToSpawn)
                    {
                        var combination = k_NetVarCombinations[i];
                        var testCase = $"[Clients: {clientCount}][NetVars: {combination.FirstType}, {combination.SecondType}][Spawned: {numToSpawn}]";
                        yield return RunCase(authority, m_Prefabs[i], numToSpawn, testCase);
                    }
                }
            }
        }

        private IEnumerator RunCase(NetworkManager authority, GameObject prefab, int numToSpawn, string testCase)
        {
            m_SpawnedObjects.Clear();
            for (int i = 0; i < numToSpawn; i++)
            {
                m_SpawnedObjects.Add(SpawnObject(prefab, authority).GetComponent<NetworkObject>());
            }

            yield return WaitForConditionOrTimeOut(AllClientsSpawnedObjects);
            AssertOnTimeout($"{testCase} Timed out waiting for clients to report spawning objects!\n {m_ErrorLog}");

            // Once all clients have spawned the NetworkObjects, set the network variables on the authority side.
            foreach (var spawnedObject in m_SpawnedObjects)
            {
                foreach (var netVarContainer in spawnedObject.GetComponents<NetVarContainer>())
                {
                    netVarContainer.SetNetworkVariableValues();
                }
            }

            // Update the NetworkBehaviours to make sure all network variables are no longer marked as dirty
            authority.BehaviourUpdater.NetworkBehaviourUpdate();

            foreach (var spawnedObject in m_SpawnedObjects)
            {
                foreach (var netVarContainer in spawnedObject.GetComponents<NetVarContainer>())
                {
                    Assert.False(netVarContainer.AreNetVarsDirty(), $"{testCase} Some NetworkVariables were still marked dirty after NetworkBehaviourUpdate!");
                }
            }

            m_ClientSideNetVarContainers.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager == authority)
                {
                    continue;
                }
                foreach (var spawnedObject in m_SpawnedObjects)
                {
                    m_ClientSideNetVarContainers.AddRange(networkManager.SpawnManager.SpawnedObjects[spawnedObject.NetworkObjectId].GetComponents<NetVarContainer>());
                }
            }

            yield return WaitForConditionOrTimeOut(AllClientSideValuesChanged);
            AssertOnTimeout($"{testCase} Timed out waiting for client side NetVarContainers to report all NetworkVariables have been updated!");

            foreach (var spawnedObject in m_SpawnedObjects)
            {
                spawnedObject.Despawn();
            }
            yield return WaitForDespawnedOnAllOrTimeOut(m_SpawnedObjects);
            AssertOnTimeout($"{testCase} Timed out waiting for all clients to despawn the objects!");
        }
    }
}
