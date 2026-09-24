using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host, NetworkTransform.AuthorityModes.Server)]
    [TestFixture(HostOrServer.Host, NetworkTransform.AuthorityModes.Owner)]
    [TestFixture(HostOrServer.Server, NetworkTransform.AuthorityModes.Server)]
    [TestFixture(HostOrServer.Server, NetworkTransform.AuthorityModes.Owner)]
    internal class NetworkTransformMixedAuthorityTests : IntegrationTestWithApproximation
    {
        private const float k_MotionMagnitude = 5.5f;
        private const int k_Iterations = 4;

        protected override int NumberOfClients => 2;

        private readonly NetworkTransform.AuthorityModes m_RootAuthorityMode;
        private readonly NetworkTransform.AuthorityModes m_ChildAuthorityMode;

        private StringBuilder m_ErrorMsg = new StringBuilder();

        public NetworkTransformMixedAuthorityTests(HostOrServer hostOrServer, NetworkTransform.AuthorityModes rootAuthorityMode) : base(hostOrServer)
        {
            m_RootAuthorityMode = rootAuthorityMode;
            m_ChildAuthorityMode = rootAuthorityMode == NetworkTransform.AuthorityModes.Server ? NetworkTransform.AuthorityModes.Owner : NetworkTransform.AuthorityModes.Server;
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<NetworkTransform>().AuthorityMode = m_RootAuthorityMode;

            var childGameObject = new GameObject();
            childGameObject.transform.parent = m_PlayerPrefab.transform;
            var childNetworkTransform = childGameObject.AddComponent<NetworkTransform>();
            childNetworkTransform.AuthorityMode = m_ChildAuthorityMode;
            childNetworkTransform.InLocalSpace = true;

            base.OnCreatePlayerPrefab();
        }

        /// <summary>
        /// Returns the instance of <paramref name="player"/>'s player object that has authority over a
        /// <see cref="NetworkTransform"/> using the <paramref name="authorityMode"/> authority mode.
        /// </summary>
        private NetworkObject GetAuthorityInstance(NetworkManager player, NetworkTransform.AuthorityModes authorityMode)
        {
            var authority = authorityMode == NetworkTransform.AuthorityModes.Server ? m_ServerNetworkManager : player;
            return authority.SpawnManager.SpawnedObjects[player.LocalClient.PlayerObject.NetworkObjectId];
        }

        private void MovePlayers()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                var direction = GetRandomVector3(-1.0f, 1.0f);
                GetAuthorityInstance(networkManager, m_RootAuthorityMode).transform.position += direction * k_MotionMagnitude;
                GetAuthorityInstance(networkManager, m_ChildAuthorityMode).transform.GetChild(0).localPosition += direction * k_MotionMagnitude;
            }
        }

        private bool AllInstancePositionsMatch()
        {
            m_ErrorMsg.Clear();
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                var playerObjectId = networkManager.LocalClient.PlayerObject.NetworkObjectId;
                var authorityRootPosition = GetAuthorityInstance(networkManager, m_RootAuthorityMode).transform.position;
                var authorityChildPosition = GetAuthorityInstance(networkManager, m_ChildAuthorityMode).transform.GetChild(0).localPosition;

                // The authority instances are compared too, as an instance with authority over one nested
                // NetworkTransform is still non-authority for the other.
                foreach (var client in m_NetworkManagers)
                {
                    var playerClone = client.SpawnManager.SpawnedObjects[playerObjectId];
                    var cloneRootPosition = playerClone.transform.position;
                    var cloneChildPosition = playerClone.transform.GetChild(0).localPosition;

                    if (!Approximately(authorityRootPosition, cloneRootPosition))
                    {
                        m_ErrorMsg.AppendLine($"[Client-{client.LocalClientId}][{playerClone.name}] Root mismatch ({GetVector3Values(authorityRootPosition)})({GetVector3Values(cloneRootPosition)})!");
                    }

                    if (!Approximately(authorityChildPosition, cloneChildPosition))
                    {
                        m_ErrorMsg.AppendLine($"[Client-{client.LocalClientId}][{playerClone.name}] Child mismatch ({GetVector3Values(authorityChildPosition)})({GetVector3Values(cloneChildPosition)})!");
                    }
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        /// <summary>
        /// Client-Server Only
        /// Validates that mixed authority is working properly
        /// Root -- Server or Owner authoritative
        /// |--Child -- The inverse of the root's authority mode
        /// </summary>
        [UnityTest]
        public IEnumerator MixedAuthorityTest()
        {
            for (int i = 0; i < k_Iterations; i++)
            {
                MovePlayers();
                yield return WaitForConditionOrTimeOut(AllInstancePositionsMatch);
                AssertOnTimeout($"Transforms failed to synchronize!\n{m_ErrorMsg}");
            }
        }

        /// <summary>
        /// The update registration is per-NetworkObject while the authority motion model is per-NetworkTransform,
        /// so an instance stays registered for as long as any one of its nested NetworkTransform components is
        /// non-authority.
        /// </summary>
        [Test]
        public void MixedAuthorityUpdateRegistration()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                var playerObjectId = networkManager.LocalClient.PlayerObject.NetworkObjectId;
                foreach (var client in m_NetworkManagers)
                {
                    var playerClone = client.SpawnManager.SpawnedObjects[playerObjectId];
                    var hasNonAuthority = false;
                    foreach (var networkTransform in playerClone.NetworkTransforms)
                    {
                        hasNonAuthority |= !networkTransform.CanCommitToTransform;
                    }
                    Assert.AreEqual(hasNonAuthority, client.NetworkTransformUpdate.ContainsKey(playerObjectId), $"[Client-{client.LocalClientId}][{playerClone.name}] Unexpected update registration!");
                }
            }
        }
    }
}
