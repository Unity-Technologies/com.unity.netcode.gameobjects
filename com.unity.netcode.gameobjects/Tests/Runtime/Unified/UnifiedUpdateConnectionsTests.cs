#if UNIFIED_NETCODE
using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class UnifiedUpdateConnectionsTests
    {
        private GameObject m_NetworkManagerObject;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            NetcodeIntegrationTestHelpers.IgnoreIfServiceEnviromentVariableSet();
        }

        /// <summary>
        /// Validates that a session without ghost prefabs leaves no world with a <see cref="UnifiedUpdateConnections"/>
        /// system.
        /// </summary>
        [UnityTest]
        public IEnumerator NoWorldUpdatesConnectionsWithoutGhostPrefabs()
        {
            m_NetworkManagerObject = new GameObject("NoGhostPrefabs");
            var networkManager = m_NetworkManagerObject.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig() { NetworkTransport = m_NetworkManagerObject.AddComponent<UnityTransport>() };
            Assert.IsTrue(networkManager.StartServer(), $"The {nameof(NetworkManager)} failed to start!");
            yield return null;

            Assert.IsNull(networkManager.NetcodeWorld, $"A session without ghost prefabs created a world!");
            Assert.IsTrue(World.All.Count > 0, "There is no world to check!");
            foreach (var world in World.All)
            {
                Assert.IsNull(world.GetExistingSystemManaged<UnifiedUpdateConnections>(), $"[{world.Name}] Has a {nameof(UnifiedUpdateConnections)} system!");
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_NetworkManagerObject != null)
            {
                Object.DestroyImmediate(m_NetworkManagerObject);
                m_NetworkManagerObject = null;
            }
            yield return null;
        }
    }
}
#endif
