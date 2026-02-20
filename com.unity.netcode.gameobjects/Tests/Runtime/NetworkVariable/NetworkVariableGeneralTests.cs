using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// General <see cref="NetworkVariable{T}"/> integration tests.
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkVariableGeneralTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private GameObject m_PrefabToSpawn;
        private GeneralNetVarTest m_AuthorityNetVarTest;

        internal class GeneralNetVarTest : NetworkBehaviour
        {
            /// <summary>
            /// NetworkVariable that is set by the authority during spawn.
            /// </summary>
            public NetworkVariable<float> TestValueOnSpawn = new NetworkVariable<float>(default);
            /// <summary>
            /// NetworkVariable that is set by the authority during post spawn.
            /// </summary>
            public NetworkVariable<float> TestValueOnPostSpawn = new NetworkVariable<float>(default);

            /// <summary>
            /// Field value set to <see cref="TestValueOnSpawn.Value"/> during <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
            /// </summary>
            public float OnNetworkSpawnValue;

            /// <summary>
            /// Field value set to <see cref="TestValueOnPostSpawn.Value"/> during <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
            /// </summary>
            public float OnNetworkPostSpawnValue;

            public override void OnNetworkSpawn()
            {
                if (HasAuthority)
                {
                    TestValueOnSpawn.Value = Random.Range(0.01f, 100.0f);
                }
                else
                {
                    // Only set these values during OnNetworkSpawn to
                    // verify this value is valid for non-authority instances.
                    OnNetworkSpawnValue = TestValueOnSpawn.Value;
                    OnNetworkPostSpawnValue = TestValueOnPostSpawn.Value;
                }
                base.OnNetworkSpawn();
            }

            protected override void OnNetworkPostSpawn()
            {
                if (HasAuthority)
                {
                    TestValueOnPostSpawn.Value = Random.Range(0.01f, 100.0f);
                }
                base.OnNetworkPostSpawn();
            }
        }

        public NetworkVariableGeneralTests(HostOrServer host) : base(host) { }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("TestNetVar");
            m_PrefabToSpawn.AddComponent<GeneralNetVarTest>();
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Verifies that upon spawn or post spawn the value is set within OnNetworkSpawn on the
        /// non-authority instances.
        /// </summary>
        private bool SpawnValuesMatch(StringBuilder errorLog)
        {
            var authority = GetAuthorityNetworkManager();
            var authorityOnSpawnValue = m_AuthorityNetVarTest.TestValueOnSpawn.Value;
            var authorityOnPostSpawnValue = m_AuthorityNetVarTest.TestValueOnPostSpawn.Value;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (authority)
                {
                    continue;
                }
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthorityNetVarTest.NetworkObjectId))
                {
                    errorLog.AppendLine($"[{networkManager.name}] Does not have a spawned instance of {m_AuthorityNetVarTest.name}!");
                    continue;
                }
                var netVarTest = networkManager.SpawnManager.SpawnedObjects[m_AuthorityNetVarTest.NetworkObjectId].GetComponent<GeneralNetVarTest>();
                if (netVarTest.OnNetworkSpawnValue != authorityOnSpawnValue)
                {
                    errorLog.AppendLine($"[{networkManager.name}][OnNetworkSpawn Value] Non-authority value: {netVarTest.OnNetworkSpawnValue} does not match " +
                        $"the authority value: {authorityOnSpawnValue}!");
                }
                if (netVarTest.OnNetworkPostSpawnValue != authorityOnPostSpawnValue)
                {
                    errorLog.AppendLine($"[{networkManager.name}][OnNetworkPostSpawn Value] Non-authority value: {netVarTest.OnNetworkPostSpawnValue} does not match " +
                        $"the authority value: {authorityOnPostSpawnValue}!");
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Verifies that changing the value synchronizes properly.
        /// </summary>
        private bool ChangedValueMatches(StringBuilder errorLog)
        {
            var authority = GetAuthorityNetworkManager();
            var authorityValue = m_AuthorityNetVarTest.TestValueOnSpawn.Value;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (authority)
                {
                    continue;
                }
                var netVarTest = networkManager.SpawnManager.SpawnedObjects[m_AuthorityNetVarTest.NetworkObjectId].GetComponent<GeneralNetVarTest>();
                if (netVarTest.TestValueOnSpawn.Value != authorityValue)
                {
                    errorLog.AppendLine($"[{networkManager.name}][Changed] Non-auhoroty value: {netVarTest.TestValueOnSpawn.Value} does not match " +
                        $"the authority value: {authorityValue}!");
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Validates when the authority applies a <see cref="NetworkVariable{T}"/> value during spawn or
        /// post spawn of a newly instantiated and spawned object the value is set by the time non-authority
        /// instances invoke <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
        /// </summary>
        [UnityTest]
        public IEnumerator ApplyValueDuringSpawnSequence()
        {
            var authority = GetAuthorityNetworkManager();
            m_AuthorityNetVarTest = SpawnObject(m_PrefabToSpawn, authority).GetComponent<GeneralNetVarTest>();
            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthorityNetVarTest.gameObject);
            AssertOnTimeout($"Not all clients spawned {m_AuthorityNetVarTest.name}!");

            yield return WaitForConditionOrTimeOut(SpawnValuesMatch);
            AssertOnTimeout($"Values did not match for {m_AuthorityNetVarTest.name}!");

            // Verify late joined clients synchronize correctly
            yield return CreateAndStartNewClient();

            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthorityNetVarTest.gameObject);
            AssertOnTimeout($"Not all clients spawned {m_AuthorityNetVarTest.name}!");

            yield return WaitForConditionOrTimeOut(SpawnValuesMatch);
            AssertOnTimeout($"Values did not match for {m_AuthorityNetVarTest.name}!");

            // Verify changing the value synchronizes properly
            m_AuthorityNetVarTest.TestValueOnSpawn.Value += Random.Range(0.01f, 100.0f);
            yield return WaitForConditionOrTimeOut(ChangedValueMatches);
            AssertOnTimeout($"Values did not match for {m_AuthorityNetVarTest.name}!");
        }
    }
}
