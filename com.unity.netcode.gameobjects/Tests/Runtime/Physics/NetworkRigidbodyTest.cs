#if COM_UNITY_MODULES_PHYSICS || COM_UNITY_MODULES_PHYSICS2D
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkRigidbodyTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private List<(RigidbodyInterpolation interpolationType, bool enableInterpolation, bool useRigidbodyForMotion)> m_TestConfigurations =
            new List<(RigidbodyInterpolation interpolationType, bool enableInterpolation, bool useRigidbodyForMotion)>()
            {
                (RigidbodyInterpolation.Interpolate, true, true), // This should be allowed under all condistions when using Rigidbody motion
                (RigidbodyInterpolation.Extrapolate, true, true), // This should not allow extrapolation on non-auth instances when using Rigidbody motion & NT interpolation
                (RigidbodyInterpolation.Extrapolate, false, true), // This should allow extrapolation on non-auth instances when using Rigidbody & NT has no interpolation
                (RigidbodyInterpolation.Interpolate, true, false), // This should not allow kinematic instances to have Rigidbody interpolation enabled
                (RigidbodyInterpolation.Interpolate, false, false) // Testing that rigidbody interpolation remains the same if NT interpolate is disabled
            };

        /// <summary>
        /// The current test configuration applied to the current test running.
        /// </summary>
        private (RigidbodyInterpolation interpolationType, bool enableInterpolation, bool useRigidbodyForMotion) m_CurrentConfiguration;

        public NetworkRigidbodyTest(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        /// <summary>
        /// Base prefab for <see cref="Rigidbody"/> and <see cref="NetworkRigidbody"/>
        /// </summary>
        private GameObject m_RigidbodyPrefab;
        private NetworkTransform m_3DNetworkTransform;
        private Rigidbody m_PrefabRigidbody;
        private NetworkRigidbody m_PrefabNetworkRigidbody;
        private NetworkObject m_3DAuthorityInstance;

        /// <summary>
        /// Base prefab for <see cref="Rigidbody2D"/> and <see cref="NetworkRigidbody2D"/>
        /// </summary>
        private GameObject m_Rigidbody2DPrefab;
        private NetworkTransform m_2DNetworkTransform;
        private Rigidbody2D m_PrefabRigidbody2D;
        private NetworkRigidbody2D m_PrefabNetworkRigidbody2D;
        private NetworkObject m_2DAuthorityInstance;

        protected override void OnServerAndClientsCreated()
        {
            m_RigidbodyPrefab = CreateNetworkObjectPrefab("RBTest");
            m_3DNetworkTransform = m_RigidbodyPrefab.AddComponent<NetworkTransform>();
            m_PrefabRigidbody = m_RigidbodyPrefab.AddComponent<Rigidbody>();
            m_PrefabNetworkRigidbody = m_RigidbodyPrefab.AddComponent<NetworkRigidbody>();

            m_Rigidbody2DPrefab = CreateNetworkObjectPrefab("RB2DTest");
            m_2DNetworkTransform = m_Rigidbody2DPrefab.AddComponent<NetworkTransform>();
            m_PrefabRigidbody2D = m_Rigidbody2DPrefab.AddComponent<Rigidbody2D>();
            m_PrefabNetworkRigidbody2D = m_Rigidbody2DPrefab.AddComponent<NetworkRigidbody2D>();

            base.OnServerAndClientsCreated();
        }

        private string m_ConfigHeader;
        private void ApplyCurrentTestConfiguration()
        {
            // Configure both 3D and 2D versions based on the current test configuration
            m_3DNetworkTransform.Interpolate = m_CurrentConfiguration.enableInterpolation;
            m_PrefabRigidbody.interpolation = m_CurrentConfiguration.interpolationType;
            m_PrefabNetworkRigidbody.UseRigidBodyForMotion = m_CurrentConfiguration.useRigidbodyForMotion;
            m_2DNetworkTransform.Interpolate = m_CurrentConfiguration.enableInterpolation;
            m_PrefabRigidbody2D.interpolation = m_CurrentConfiguration.interpolationType == RigidbodyInterpolation.Interpolate ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.Extrapolate;
            m_PrefabNetworkRigidbody2D.UseRigidBodyForMotion = m_CurrentConfiguration.useRigidbodyForMotion;

            // Build a header used in assert messages
            m_ConfigHeader = $"[{m_CurrentConfiguration.interpolationType}][Interpolate: {m_CurrentConfiguration.enableInterpolation}][RB-Motion: {m_CurrentConfiguration.useRigidbodyForMotion}]";
        }

        /// <summary>
        /// Iterates through the <see cref="m_TestConfigurations"/> to validate various
        /// Rigidbody interpolation settings and kinematic states for authority and non-authority
        /// instances.
        /// </summary>
        [UnityTest]
        public IEnumerator TestRigidbodyKinematicEnableDisable()
        {
            foreach (var configuration in m_TestConfigurations)
            {
                m_CurrentConfiguration = configuration;
                ApplyCurrentTestConfiguration();

                // Host, Server, DAHost/Session-owner are spawn authority
                yield return RunTestConfiguration();

                // When using distributed authority, swap the session owner with
                // the non-session owner client as being the spawn authority.
                if (m_DistributedAuthority)
                {
                    yield return RunTestConfiguration(true);
                }
            }
        }

        /// <summary>
        /// Validates the current applied test configuration.
        /// </summary>
        private IEnumerator RunTestConfiguration(bool swapAuthority = false)
        {
            // The authority is the "spawn authority".
            // Distributed authority runs this a second time with a non-session owner client being the
            // spawn authority to validate that scenario works correctly.
            var authority = !swapAuthority ? GetAuthorityNetworkManager() : GetNonAuthorityNetworkManager();
            var nonAuthority = !swapAuthority ? GetNonAuthorityNetworkManager() : GetAuthorityNetworkManager();

            // Spawn instances of both the 3D and 2D prefabs configured for the current test.
            m_3DAuthorityInstance = SpawnObject(m_RigidbodyPrefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(m_3DAuthorityInstance);
            AssertOnTimeout($"Failed to spawn {m_3DAuthorityInstance.name} on all clients!");

            m_2DAuthorityInstance = SpawnObject(m_Rigidbody2DPrefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(m_2DAuthorityInstance);
            AssertOnTimeout($"Failed to spawn {m_2DAuthorityInstance.name} on all clients!");

            // Test 3D Rigidbody
            #region 3D Rigidbody validation
            var authorityRigidbody = m_3DAuthorityInstance.GetComponent<Rigidbody>();
            var nonAuthorityInstance = nonAuthority.SpawnManager.SpawnedObjects[m_3DAuthorityInstance.NetworkObjectId];
            var nonAuthorityRigidbody = nonAuthorityInstance.GetComponent<Rigidbody>();
            var authorityHeader = $"{m_ConfigHeader}[Authority] Client-{authority.LocalClientId}'s instance of {m_3DAuthorityInstance.name}";
            // The authority instance should always be non-kinematic
            Assert.False(authorityRigidbody.isKinematic, $"{authorityHeader} is kinematic!");

            var nonAuthorityHeader = $"{m_ConfigHeader}[Non-Authority] Client-{nonAuthority.LocalClientId}'s instance of {nonAuthorityInstance.name}";
            // Non-authority instances should always be kinematic
            Assert.True(nonAuthorityRigidbody.isKinematic, $"{nonAuthorityHeader} is not kinematic!");
            var interpolateCompareNonAuthoritative = RigidbodyInterpolation.None;

            if (m_CurrentConfiguration.useRigidbodyForMotion)
            {
                // The authoritative instance can be None, Interpolate, or Extrapolate for the Rigidbody interpolation settings.
                Assert.AreEqual(m_CurrentConfiguration.interpolationType, authorityRigidbody.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody.interpolation} " +
                    $"and not {m_CurrentConfiguration.interpolationType}!");

                // When using Rigidbody motion, authoritative and non-authoritative Rigidbody interpolation settings should be preserved (except when extrapolation is used
                interpolateCompareNonAuthoritative = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation.Interpolate : m_CurrentConfiguration.interpolationType;

            }
            else
            {
                Assert.AreEqual(RigidbodyInterpolation.Interpolate, authorityRigidbody.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody.interpolation} " +
                    $"and not {RigidbodyInterpolation.Interpolate}!");

                // client rigidbody has no authority with NT interpolation disabled should allow Rigidbody interpolation
                interpolateCompareNonAuthoritative = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
            }

            Assert.AreEqual(interpolateCompareNonAuthoritative, nonAuthorityRigidbody.interpolation, $"{nonAuthorityHeader} interpolation is {nonAuthorityRigidbody.interpolation} " +
                $"and not {interpolateCompareNonAuthoritative}!");
            #endregion

            // Test 2D Rigidbody
            #region 2D Rigidbody validation
            var authorityRigidbody2D = m_2DAuthorityInstance.GetComponent<Rigidbody2D>();
            var nonAuthorityInstance2D = nonAuthority.SpawnManager.SpawnedObjects[m_2DAuthorityInstance.NetworkObjectId];
            var nonAuthorityRigidbody2D = nonAuthorityInstance2D.GetComponent<Rigidbody2D>();

            authorityHeader = $"{m_ConfigHeader}[Authority] Client-{authority.LocalClientId}'s instance of {m_2DAuthorityInstance.name}";
            // The authority instance should always be non-kinematic
            Assert.False(authorityRigidbody2D.bodyType == RigidbodyType2D.Kinematic, $"{authorityHeader} is kinematic!");

            nonAuthorityHeader = $"{m_ConfigHeader}[Non-Authority] Client-{nonAuthority.LocalClientId}'s instance of {nonAuthorityInstance.name}";
            // Non-authority instances should always be kinematic
            Assert.True(nonAuthorityRigidbody2D.bodyType == RigidbodyType2D.Kinematic, $"{nonAuthorityHeader} is not kinematic!");
            var interpolateCompareNonAuthoritative2D = RigidbodyInterpolation2D.None;
            var configInterpolation2D = m_CurrentConfiguration.interpolationType == RigidbodyInterpolation.Interpolate ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.Extrapolate;
            if (m_CurrentConfiguration.useRigidbodyForMotion)
            {
                // The authoritative instance can be None, Interpolate, or Extrapolate for the Rigidbody interpolation settings.
                Assert.AreEqual(configInterpolation2D, authorityRigidbody2D.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody2D.interpolation} " +
                    $"and not {m_CurrentConfiguration.interpolationType}!");

                // When using Rigidbody motion, authoritative and non-authoritative Rigidbody interpolation settings should be preserved (except when extrapolation is used
                interpolateCompareNonAuthoritative2D = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation2D.Interpolate : configInterpolation2D;
            }
            else
            {
                Assert.AreEqual(RigidbodyInterpolation2D.Interpolate, authorityRigidbody2D.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody2D.interpolation} " +
                    $"and not {RigidbodyInterpolation2D.Interpolate}!");

                // client rigidbody has no authority with NT interpolation disabled should allow Rigidbody interpolation
                interpolateCompareNonAuthoritative2D = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation2D.None : RigidbodyInterpolation2D.Interpolate;
            }

            Assert.AreEqual(interpolateCompareNonAuthoritative2D, nonAuthorityRigidbody2D.interpolation, $"{nonAuthorityHeader} interpolation is {nonAuthorityRigidbody2D.interpolation} " +
                $"and not {interpolateCompareNonAuthoritative}!");
            #endregion

            var spawnedInstances = new List<NetworkObject>() { m_3DAuthorityInstance, m_2DAuthorityInstance };
            m_3DAuthorityInstance.Despawn();
            m_2DAuthorityInstance.Despawn();
            yield return WaitForDespawnedOnAllOrTimeOut(spawnedInstances);
            AssertOnTimeout($"Failed to de-spawn instances on all clients!");
            m_3DAuthorityInstance = null;
            m_2DAuthorityInstance = null;
        }

        /// <summary>
        /// Handle clean up in case of a failed test
        /// </summary>
        protected override IEnumerator OnTearDown()
        {
            // If either of these are not null then we most likely failed and didn't cleanup.

            // Clean-up m_3DAuthorityInstance
            if (m_3DAuthorityInstance)
            {
                Object.Destroy(m_3DAuthorityInstance);
                m_3DAuthorityInstance = null;
            }

            // Clean-up m_2DAuthorityInstance
            if (m_2DAuthorityInstance)
            {
                Object.Destroy(m_2DAuthorityInstance);
                m_2DAuthorityInstance = null;
            }

            return base.OnTearDown();
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS
