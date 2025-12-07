using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Server and Distributed Authority modes require at least 1 client while the host does not.
    /// </summary>
    /// [Host or Server mode][Number of Clients][First NetVar Type][Second NetVar Type]
    [TestFixture(HostOrServer.DAHost, 1, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.One)]
    [TestFixture(HostOrServer.DAHost, 1, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.DAHost, 1, NetVarContainer.NetVarsToCheck.Two, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.DAHost, 2, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.One)]
    [TestFixture(HostOrServer.DAHost, 2, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.DAHost, 2, NetVarContainer.NetVarsToCheck.Two, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Server, 1, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.One)]
    [TestFixture(HostOrServer.Server, 1, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Server, 1, NetVarContainer.NetVarsToCheck.Two, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Server, 2, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.One)]
    [TestFixture(HostOrServer.Server, 2, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Server, 2, NetVarContainer.NetVarsToCheck.Two, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Host, 0, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.One)]
    [TestFixture(HostOrServer.Host, 0, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Host, 0, NetVarContainer.NetVarsToCheck.Two, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Host, 1, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.One)]
    [TestFixture(HostOrServer.Host, 1, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Host, 1, NetVarContainer.NetVarsToCheck.Two, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Host, 2, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.One)]
    [TestFixture(HostOrServer.Host, 2, NetVarContainer.NetVarsToCheck.One, NetVarContainer.NetVarsToCheck.Two)]
    [TestFixture(HostOrServer.Host, 2, NetVarContainer.NetVarsToCheck.Two, NetVarContainer.NetVarsToCheck.Two)]
    internal class NetworkBehaviourUpdaterTests : NetcodeIntegrationTest
    {
        // Go ahead and create maximum number of clients (not all tests will use them)
        protected override int NumberOfClients => m_ClientCount;
        public const int NetVarValueToSet = 1;
        private List<ulong> m_SpawnedObjects = new List<ulong>();
        private GameObject m_PrefabToSpawn;
        private NetVarCombinationTypes m_NetVarCombinationTypes;
        private int m_ClientCount = 0;

        private StringBuilder m_ErrorLog = new StringBuilder();

        public NetworkBehaviourUpdaterTests(HostOrServer hostOrServer, int numberOfClients, NetVarContainer.NetVarsToCheck first, NetVarContainer.NetVarsToCheck second) : base(hostOrServer)
        {
            m_NetVarCombinationTypes = new NetVarCombinationTypes()
            {
                FirstType = first,
                SecondType = second
            };
            // Adjust the client count if connecting to the service.
            m_ClientCount = numberOfClients;
        }

        protected override IEnumerator OnSetup()
        {
            m_SpawnedObjects.Clear();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("NetVarCont");
            // Create the two instances of the NetVarContainer components and add them to the
            // GameObject of this prefab
            var netVarContainer = m_PrefabToSpawn.AddComponent<NetVarContainer>();
            netVarContainer.NumberOfNetVarsToCheck = m_NetVarCombinationTypes.FirstType;
            if (m_NetworkTopologyType == NetworkTopologyTypes.DistributedAuthority)
            {
                netVarContainer.SetOwnerWrite();
            }

            netVarContainer.ValueToSetNetVarTo = NetVarValueToSet;
            netVarContainer = m_PrefabToSpawn.AddComponent<NetVarContainer>();

            if (m_NetworkTopologyType == NetworkTopologyTypes.DistributedAuthority)
            {
                netVarContainer.SetOwnerWrite();
            }

            netVarContainer.NumberOfNetVarsToCheck = m_NetVarCombinationTypes.SecondType;
            netVarContainer.ValueToSetNetVarTo = NetVarValueToSet;

            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Determines if all clients have spawned clone instances.
        /// </summary>
        /// <remarks>
        /// <see cref="m_ErrorLog"/> will contain log entries of the
        /// <see cref="NetworkManager"/> instances and NetworkObjects
        /// that did not get spawned.
        /// </remarks>
        /// <returns>true(success) or false (failure)</returns>
        private bool AllClientsSpawnedObjects()
        {
            m_ErrorLog.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                foreach (var networkObjectId in m_SpawnedObjects)
                {
                    if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                    {
                        m_ErrorLog.AppendLine($"[{networkManager.name}] Has not spawned {nameof(NetworkObject)}-{networkObjectId}.");
                    }
                }
            }
            return m_ErrorLog.Length == 0;
        }

        /// <summary>
        /// The updated BehaviourUpdaterAllTests was re-designed to replicate the same functionality being tested in the
        /// original version of this test with additional time out handling and a re-organization in the order of operations.
        /// Things like making sure all clients have spawned the NetworkObjects in question prior to testing for the
        /// NetworkVariable value changes helped to eliminate the timing issues that were happening when this test was run
        /// in a stand alone test runner build (i.e. all consoles run the stand alone version as opposed to the in-editor
        /// version like the desktop tests use).
        /// This update also updated how the server and clients were being constructed to help reduce the execution time.
        /// </summary>
        /// <param name="hostOrServer"> whether to run the server as a host or not</param>
        /// <param name="numToSpawn"> number of NetworkObjects to be spawned</param>
        [UnityTest]
        public IEnumerator BehaviourUpdaterAllTests([Values(1, 2)] int numToSpawn)
        {
            // Tracks the server-side spawned prefab instances
            var spawnedPrefabs = new List<GameObject>();

            // Used to determine if the client-side checks of this test should be
            // executed or not as well is used to make sure all clients have spawned
            // the appropriate number of NetworkObjects with the NetVarContainer behaviour
            var numberOfObjectsToSpawn = numToSpawn * NumberOfClients;

            var authority = GetAuthorityNetworkManager();

            // spawn the objects
            for (int i = 0; i < numToSpawn; i++)
            {
                var spawnedObject = SpawnObject(m_PrefabToSpawn, authority);
                spawnedPrefabs.Add(spawnedObject);
                m_SpawnedObjects.Add(spawnedObject.GetComponent<NetworkObject>().NetworkObjectId);
            }

            // Waits for all clients to spawn the NetworkObjects
            yield return WaitForConditionOrTimeOut(AllClientsSpawnedObjects);
            AssertOnTimeout($"Timed out waiting for clients to report spawning objects!\n {m_ErrorLog}");


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
            authority.BehaviourUpdater.NetworkBehaviourUpdate();

            // Verify that all network variables are no longer dirty on server side only if we have clients (including host)
            foreach (var spawnedPrefab in spawnedPrefabs)
            {
                var netVarContainers = spawnedPrefab.GetComponents<NetVarContainer>();
                foreach (var netVarContainer in netVarContainers)
                {
                    Assert.False(netVarContainer.AreNetVarsDirty(), "Some NetworkVariables were still marked dirty after NetworkBehaviourUpdate!");
                }
            }

            // Get a list of all NetVarContainer components on the client-side spawned NetworkObjects
            var clientSideNetVarContainers = new List<NetVarContainer>();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager == authority)
                {
                    continue;
                }
                foreach (var networkObjectId in m_SpawnedObjects)
                {
                    var netVarContainers = networkManager.SpawnManager.SpawnedObjects[networkObjectId].GetComponents<NetVarContainer>();
                    foreach (var netvarContiner in netVarContainers)
                    {
                        clientSideNetVarContainers.Add(netvarContiner);
                    }
                }
            }

            yield return WaitForConditionOrTimeOut(() =>
            clientSideNetVarContainers.Where(d =>
            d.HaveAllValuesChanged(NetVarValueToSet)).Count() == clientSideNetVarContainers.Count);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client side NetVarContainers to report all NetworkVariables have been updated!");
        }
    }
}
