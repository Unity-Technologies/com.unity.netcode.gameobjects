#if COM_UNITY_MODULES_PHYSICS
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(RigidbodyInterpolation.Interpolate, true, true)] // This should be allowed under all condistions when using Rigidbody motion
    [TestFixture(RigidbodyInterpolation.Extrapolate, true, true)] // This should not allow extrapolation on non-auth instances when using Rigidbody motion & NT interpolation
    [TestFixture(RigidbodyInterpolation.Extrapolate, false, true)] // This should allow extrapolation on non-auth instances when using Rigidbody & NT has no interpolation
    [TestFixture(RigidbodyInterpolation.Interpolate, true, false)] // This should not allow kinematic instances to have Rigidbody interpolation enabled
    [TestFixture(RigidbodyInterpolation.Interpolate, false, false)] // Testing that rigid body interpolation remains the same if NT interpolate is disabled
    public class NetworkRigidbodyTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private bool m_NetworkTransformInterpolate;
        private bool m_UseRigidBodyForMotion;
        private RigidbodyInterpolation m_RigidbodyInterpolation;

        public NetworkRigidbodyTest(RigidbodyInterpolation rigidbodyInterpolation, bool networkTransformInterpolate, bool useRigidbodyForMotion)
        {
            m_RigidbodyInterpolation = rigidbodyInterpolation;
            m_NetworkTransformInterpolate = networkTransformInterpolate;
            m_UseRigidBodyForMotion = useRigidbodyForMotion;
        }

        protected override void OnCreatePlayerPrefab()
        {
            var networkTransform = m_PlayerPrefab.AddComponent<NetworkTransform>();
            networkTransform.Interpolate = m_NetworkTransformInterpolate;
            var rigidbody = m_PlayerPrefab.AddComponent<Rigidbody>();
            rigidbody.interpolation = m_RigidbodyInterpolation;
            var networkRigidbody = m_PlayerPrefab.AddComponent<NetworkRigidbody>();
            networkRigidbody.UseRigidBodyForMotion = m_UseRigidBodyForMotion;
        }

        /// <summary>
        /// Tests that a server can destroy a NetworkObject and that it gets despawned correctly.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestRigidbodyKinematicEnableDisable()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerInstance = m_ServerNetworkManager.ConnectedClients[m_ClientNetworkManagers[0].LocalClientId].PlayerObject;

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientPlayerInstance = m_ClientNetworkManagers[0].LocalClient.PlayerObject;

            Assert.IsNotNull(serverClientPlayerInstance, $"{nameof(serverClientPlayerInstance)} is null!");
            Assert.IsNotNull(clientPlayerInstance, $"{nameof(clientPlayerInstance)} is null!");

            var serverClientInstanceRigidBody = serverClientPlayerInstance.GetComponent<Rigidbody>();
            var clientRigidBody = clientPlayerInstance.GetComponent<Rigidbody>();

            if (m_UseRigidBodyForMotion)
            {
                var interpolateCompareNonAuthoritative = m_NetworkTransformInterpolate ? RigidbodyInterpolation.Interpolate : m_RigidbodyInterpolation;

                // Server authoritative NT should yield non-kinematic mode for the server-side player instance
                Assert.False(serverClientInstanceRigidBody.isKinematic, $"[Server-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} player's {nameof(Rigidbody)} is kinematic!");

                // The authoritative instance can be None, Interpolate, or Extrapolate for the Rigidbody interpolation settings.
                Assert.AreEqual(m_RigidbodyInterpolation, serverClientInstanceRigidBody.interpolation, $"[Server-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} " +
                    $"player's {nameof(Rigidbody)}'s interpolation is {serverClientInstanceRigidBody.interpolation} and not {m_RigidbodyInterpolation}!");

                // Server authoritative NT should yield kinematic mode for the client-side player instance
                Assert.True(clientRigidBody.isKinematic, $"[Client-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} player's {nameof(Rigidbody)} is not kinematic!");

                // When using Rigidbody motion, authoritative and non-authoritative Rigidbody interpolation settings should be preserved (except when extrapolation is used
                Assert.AreEqual(interpolateCompareNonAuthoritative, clientRigidBody.interpolation, $"[Client-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} " +
                    $"player's {nameof(Rigidbody)}'s interpolation is {clientRigidBody.interpolation} and not {interpolateCompareNonAuthoritative}!");
            }
            else
            {
                // server rigidbody has authority and should not be kinematic
                Assert.False(serverClientInstanceRigidBody.isKinematic, $"[Server-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} player's {nameof(Rigidbody)} is kinematic!");
                Assert.AreEqual(RigidbodyInterpolation.Interpolate, serverClientInstanceRigidBody.interpolation, $"[Server-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} " +
                    $"player's {nameof(Rigidbody)}'s interpolation is {serverClientInstanceRigidBody.interpolation} and not {nameof(RigidbodyInterpolation.Interpolate)}!");

                // Server authoritative NT should yield kinematic mode for the client-side player instance
                Assert.True(clientRigidBody.isKinematic, $"[Client-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} player's {nameof(Rigidbody)} is not kinematic!");

                // client rigidbody has no authority with NT interpolation disabled should allow Rigidbody interpolation
                if (!m_NetworkTransformInterpolate)
                {
                    Assert.AreEqual(RigidbodyInterpolation.Interpolate, clientRigidBody.interpolation, $"[Client-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} " +
                        $"player's {nameof(Rigidbody)}'s interpolation is {clientRigidBody.interpolation} and not {nameof(RigidbodyInterpolation.Interpolate)}!");
                }
                else
                {
                    Assert.AreEqual(RigidbodyInterpolation.None, clientRigidBody.interpolation, $"[Client-Side] Client-{m_ClientNetworkManagers[0].LocalClientId} " +
                        $"player's {nameof(Rigidbody)}'s interpolation is {clientRigidBody.interpolation} and not {nameof(RigidbodyInterpolation.None)}!");
                }
            }

            // despawn the server player (but keep it around on the server)
            serverClientPlayerInstance.Despawn(false);

            yield return WaitForConditionOrTimeOut(() => !serverClientPlayerInstance.IsSpawned && !clientPlayerInstance.IsSpawned);
            AssertOnTimeout("Timed out waiting for client player to despawn on both server and client!");

            // When despawned, we should always be kinematic (i.e. don't apply physics when despawned)
            Assert.True(serverClientInstanceRigidBody.isKinematic, $"[Server-Side][Despawned] Client-{m_ClientNetworkManagers[0].LocalClientId} player's {nameof(Rigidbody)} is not kinematic when despawned!");
            Assert.IsTrue(clientPlayerInstance == null, $"[Client-Side] Player {nameof(NetworkObject)} is not null!");
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS
