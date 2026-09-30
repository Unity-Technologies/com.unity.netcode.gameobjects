using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkTransformMixedAuthorityTests : IntegrationTestWithApproximation
    {
        private const float k_MotionMagnitude = 5.5f;
        private const int k_Iterations = 4;

        protected override int NumberOfClients => 2;

        /// <summary>
        /// The root's authority mode for each case. The nested child uses the inverse.
        /// </summary>
        private static readonly NetworkTransform.AuthorityModes[] k_RootAuthorityModes =
        {
            NetworkTransform.AuthorityModes.Server,
            NetworkTransform.AuthorityModes.Owner,
        };

        private GameObject[] m_MixedAuthorityPrefabs;

        private StringBuilder m_ErrorMsg = new StringBuilder();

        public NetworkTransformMixedAuthorityTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            m_MixedAuthorityPrefabs = new GameObject[k_RootAuthorityModes.Length];
            for (int i = 0; i < k_RootAuthorityModes.Length; i++)
            {
                var rootAuthorityMode = k_RootAuthorityModes[i];
                var prefab = CreateNetworkObjectPrefab($"MixedAuthority-{rootAuthorityMode}Root");
                prefab.AddComponent<NetworkTransform>().AuthorityMode = rootAuthorityMode;

                var childGameObject = new GameObject();
                childGameObject.transform.parent = prefab.transform;
                var childNetworkTransform = childGameObject.AddComponent<NetworkTransform>();
                childNetworkTransform.AuthorityMode = InverseOf(rootAuthorityMode);
                childNetworkTransform.InLocalSpace = true;

                m_MixedAuthorityPrefabs[i] = prefab;
            }

            base.OnServerAndClientsCreated();
        }

        private static NetworkTransform.AuthorityModes InverseOf(NetworkTransform.AuthorityModes authorityMode)
        {
            return authorityMode == NetworkTransform.AuthorityModes.Server ? NetworkTransform.AuthorityModes.Owner : NetworkTransform.AuthorityModes.Server;
        }

        /// <summary>
        /// Returns the instance with authority over a <see cref="NetworkTransform"/> set to the given authority mode.
        /// </summary>
        private NetworkObject GetAuthorityInstance(NetworkObject instance, NetworkManager owner, NetworkTransform.AuthorityModes authorityMode)
        {
            return GetManagersInstance(authorityMode == NetworkTransform.AuthorityModes.Server ? m_ServerNetworkManager : owner, instance);
        }

        private bool AllInstancePositionsMatch(NetworkObject instance, NetworkManager owner, NetworkTransform.AuthorityModes rootAuthorityMode)
        {
            m_ErrorMsg.Clear();
            var authorityRootPosition = GetAuthorityInstance(instance, owner, rootAuthorityMode).transform.position;
            var authorityChildPosition = GetAuthorityInstance(instance, owner, InverseOf(rootAuthorityMode)).transform.GetChild(0).localPosition;

            // The authority instances are compared too. An instance with authority over one nested
            // NetworkTransform is still non-authority for the other.
            foreach (var networkManager in m_NetworkManagers)
            {
                var clone = GetManagersInstance(networkManager, instance);
                var cloneRootPosition = clone.transform.position;
                var cloneChildPosition = clone.transform.GetChild(0).localPosition;

                if (!Approximately(authorityRootPosition, cloneRootPosition))
                {
                    m_ErrorMsg.AppendLine($"[{rootAuthorityMode}Root][Client-{networkManager.LocalClientId}] Root mismatch ({GetVector3Values(authorityRootPosition)})({GetVector3Values(cloneRootPosition)})!");
                }

                if (!Approximately(authorityChildPosition, cloneChildPosition))
                {
                    m_ErrorMsg.AppendLine($"[{rootAuthorityMode}Root][Client-{networkManager.LocalClientId}] Child mismatch ({GetVector3Values(authorityChildPosition)})({GetVector3Values(cloneChildPosition)})!");
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        /// <summary>
        /// Client-Server Only
        /// Validates that mixed authority is working properly for both arrangements:
        /// Root -- Server or Owner authoritative
        /// |--Child -- The inverse of the root's authority mode
        /// </summary>
        [UnityTest]
        public IEnumerator MixedAuthorityTest()
        {
            // A client owns the instance so the owner authoritative half is never also the server.
            var owner = m_ClientNetworkManagers[0];
            for (int i = 0; i < k_RootAuthorityModes.Length; i++)
            {
                var rootAuthorityMode = k_RootAuthorityModes[i];
                var instance = SpawnObject(m_MixedAuthorityPrefabs[i], owner).GetComponent<NetworkObject>();
                yield return WaitForSpawnedOnAllOrTimeOut(instance);
                AssertOnTimeout($"[{rootAuthorityMode}Root] Failed to spawn {instance.name} on all clients!");

                // An instance stays registered for updates while any of its nested NetworkTransform components is non-authority.
                foreach (var networkManager in m_NetworkManagers)
                {
                    var clone = GetManagersInstance(networkManager, instance);
                    var hasNonAuthority = false;
                    foreach (var networkTransform in clone.NetworkTransforms)
                    {
                        hasNonAuthority |= !networkTransform.CanCommitToTransform;
                    }
                    Assert.AreEqual(hasNonAuthority, networkManager.NetworkTransformUpdate.ContainsKey(instance.NetworkObjectId), $"[{rootAuthorityMode}Root][Client-{networkManager.LocalClientId}] Unexpected update registration!");
                }

                for (int iteration = 0; iteration < k_Iterations; iteration++)
                {
                    var direction = GetRandomVector3(-1.0f, 1.0f);
                    GetAuthorityInstance(instance, owner, rootAuthorityMode).transform.position += direction * k_MotionMagnitude;
                    GetAuthorityInstance(instance, owner, InverseOf(rootAuthorityMode)).transform.GetChild(0).localPosition += direction * k_MotionMagnitude;

                    yield return WaitForConditionOrTimeOut(() => AllInstancePositionsMatch(instance, owner, rootAuthorityMode));
                    AssertOnTimeout($"[{rootAuthorityMode}Root] Transforms failed to synchronize!\n{m_ErrorMsg}");
                }

                instance.Despawn();
            }
        }
    }
}
