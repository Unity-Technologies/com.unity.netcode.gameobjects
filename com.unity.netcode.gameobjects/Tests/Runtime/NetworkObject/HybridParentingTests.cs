#if UNIFIED_NETCODE
using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// NGO parenting of hybrid prefab instances: the developer warnings when NGO handles it, and NGO standing down when
    /// <see cref="GhostObject"/> parent replication handles it.
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridParentingTests : NetcodeIntegrationTest
    {
        private const string k_WorldPositionStaysWarning = "re-parented with worldPositionStays enabled";
        private const string k_MixedTimelinesWarning = "only one of them is a hybrid instance";
        private const float k_PositionTolerance = 0.01f;
        // Off the origin, so that the child's local position changes when it is reparented with worldPositionStays.
        private static readonly Vector3 k_ParentPosition = new Vector3(0f, 1.25f, 0f);
        private static readonly Vector3 k_ChildPosition = new Vector3(0f, 1.25f, 3f);

        protected override int NumberOfClients => 2;

        private GameObject m_HybridPrefab;
        private GameObject m_NonHybridPrefab;
        private NetworkObject m_Parent;
        private NetworkObject m_Child;
        private readonly StringBuilder m_Warnings = new StringBuilder();

        public HybridParentingTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("HybridParentingObject");
            m_NonHybridPrefab = NetcodeIntegrationTestHelpers.CreateNetworkObjectPrefab("NonHybridParentingObject", GetAuthorityNetworkManager(), m_ClientNetworkManagers);
            // N4E reads this when each world registers the prefab, so it is set before the session starts.
            m_HybridPrefab.GetComponent<GhostObject>().ParentReplication = TestContext.CurrentContext.Test.Name.StartsWith("ParentReplication_");
            if (TestContext.CurrentContext.Test.Name.StartsWith("NgoParenting_HostSmoothing_"))
            {
                // What NetworkObjectBridge sets on a hybrid prefab with a Rigidbody.
                m_HybridPrefab.GetComponent<GhostObject>().SingleWorldHostInterpolationSmoothing = SingleWorldHostInterpolationMode.Interpolate;
            }
            foreach (var networkManager in m_NetworkManagers)
            {
                networkManager.LogLevel = LogLevel.Developer;
            }
            base.OnServerAndClientsCreated();
        }

        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
            {
                m_Warnings.AppendLine(message);
            }
        }

        private int WarningCount(string pattern)
        {
            var warnings = m_Warnings.ToString();
            var count = 0;
            for (var index = warnings.IndexOf(pattern); index >= 0; index = warnings.IndexOf(pattern, index + pattern.Length))
            {
                count++;
            }
            return count;
        }

        private IEnumerator SpawnParentAndChild(GameObject parentPrefab)
        {
            var authority = GetAuthorityNetworkManager();
            m_Parent = SpawnObject(parentPrefab, authority).GetComponent<NetworkObject>();
            m_Child = SpawnObject(m_HybridPrefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(m_Parent);
            AssertOnTimeout($"Timed out waiting for {m_Parent.name} to spawn on all instances!");
            yield return WaitForSpawnedOnAllOrTimeOut(m_Child);
            AssertOnTimeout($"Timed out waiting for {m_Child.name} to spawn on all instances!");
        }

        private bool ChildParentedOnAllInstances(StringBuilder errorLog, bool parented)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var child = networkManager.SpawnManager.SpawnedObjects[m_Child.NetworkObjectId].transform;
                var expected = parented ? networkManager.SpawnManager.SpawnedObjects[m_Parent.NetworkObjectId].transform : null;
                if (child.parent != expected)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {child.name}'s parent is {(child.parent ? child.parent.name : "root")}.");
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// With worldPositionStays enabled, clients apply the new parent ahead of the ghost's rebased local transform.
        /// Disabled, the replicated local transform does not change at the reparent, so nothing is logged.
        /// </summary>
        [UnityTest]
        public IEnumerator NgoParenting_WorldPositionStays_WarnsOnlyWhenEnabled([Values] bool worldPositionStays)
        {
            yield return SpawnParentAndChild(m_HybridPrefab);
            m_Warnings.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
            try
            {
                Assert.IsTrue(m_Child.TrySetParent(m_Parent, worldPositionStays), $"{nameof(NetworkObject.TrySetParent)} failed!");
                yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, true));
                AssertOnTimeout("Timed out waiting for the child to be parented on all instances!");

                Assert.IsTrue(m_Child.TryRemoveParent(worldPositionStays), $"{nameof(NetworkObject.TryRemoveParent)} failed!");
                yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, false));
                AssertOnTimeout("Timed out waiting for the child to be moved to the root on all instances!");
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            // Once for the parent and once for its removal, on the authority only.
            Assert.AreEqual(worldPositionStays ? 2 : 0, WarningCount(k_WorldPositionStaysWarning), m_Warnings.ToString());
            Assert.AreEqual(0, WarningCount(k_MixedTimelinesWarning), m_Warnings.ToString());
        }

        /// <summary>
        /// A hybrid child under a non-hybrid parent mixes N4E snapshot and NGO transform timelines.
        /// </summary>
        [UnityTest]
        public IEnumerator NgoParenting_UnderNonHybridParent_WarnsMixedTimelines()
        {
            yield return SpawnParentAndChild(m_NonHybridPrefab);
            m_Warnings.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
            try
            {
                Assert.IsTrue(m_Child.TrySetParent(m_Parent, false), $"{nameof(NetworkObject.TrySetParent)} failed!");
                yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, true));
                AssertOnTimeout("Timed out waiting for the child to be parented on all instances!");
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            Assert.AreEqual(1, WarningCount(k_MixedTimelinesWarning), m_Warnings.ToString());
            Assert.AreEqual(0, WarningCount(k_WorldPositionStaysWarning), m_Warnings.ToString());
        }

        private bool ChildAtWorldPositionOnAllInstances(StringBuilder errorLog, Vector3 expected)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var position = networkManager.SpawnManager.SpawnedObjects[m_Child.NetworkObjectId].transform.position;
                if (Vector3.Distance(expected, position) > k_PositionTolerance)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {m_Child.name} is at {position}, expected {expected}.");
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Under host smoothing N4E restores the entity's local transform onto the GameObject every frame, so a
        /// worldPositionStays reparent made through NGO must still reach the entity, or the child moves by the parent's offset.
        /// </summary>
        [UnityTest]
        [Ignore("Needs the N4E fix that syncs a reparent made without parent replication to the entity: the child settles at the parent's offset.")]
        public IEnumerator NgoParenting_HostSmoothing_WorldPositionStays_KeepsWorldPosition()
        {
            yield return SpawnParentAndChild(m_HybridPrefab);
            // Placed through the entity, since host smoothing reverts a direct transform write.
            var parentGhost = m_Parent.GetComponent<GhostObject>();
            parentGhost.Position = k_ParentPosition;
            parentGhost.PublishTransform();
            var childGhost = m_Child.GetComponent<GhostObject>();
            childGhost.Position = k_ChildPosition;
            childGhost.PublishTransform();
            yield return WaitForConditionOrTimeOut(errorLog => ChildAtWorldPositionOnAllInstances(errorLog, k_ChildPosition));
            AssertOnTimeout("Timed out waiting for the child to reach its start position on all instances!");

            Assert.IsTrue(m_Child.TrySetParent(m_Parent, true), $"{nameof(NetworkObject.TrySetParent)} failed!");
            yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, true));
            AssertOnTimeout("Timed out waiting for the child to be parented on all instances!");
            // The displacement appears on the first restore after the reparent and then holds, so a settled check catches it.
            yield return WaitForTicks(GetAuthorityNetworkManager(), 10);

            var errors = new StringBuilder();
            Assert.IsTrue(ChildAtWorldPositionOnAllInstances(errors, k_ChildPosition), errors.ToString());
        }

        [UnityTest]
        public IEnumerator ParentReplication_NgoEntryPoints_WarnAndExit()
        {
            yield return SpawnParentAndChild(m_HybridPrefab);
            m_Warnings.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
            try
            {
                Assert.IsFalse(m_Child.TrySetParent(m_Parent), $"{nameof(NetworkObject.TrySetParent)}({nameof(NetworkObject)}) should exit early!");
                Assert.IsFalse(m_Child.TrySetParent(m_Parent.transform), $"{nameof(NetworkObject.TrySetParent)}({nameof(Transform)}) should exit early!");
                Assert.IsFalse(m_Child.TrySetParent(m_Parent.gameObject), $"{nameof(NetworkObject.TrySetParent)}({nameof(GameObject)}) should exit early!");
                Assert.IsFalse(m_Child.TryRemoveParent(), $"{nameof(NetworkObject.TryRemoveParent)} should exit early!");
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            Assert.IsNull(m_Child.transform.parent, "NGO parenting should not have changed the parent!");
            Assert.AreEqual(3, WarningCount($"{nameof(NetworkObject.TrySetParent)} was ignored"), m_Warnings.ToString());
            Assert.AreEqual(1, WarningCount($"{nameof(NetworkObject.TryRemoveParent)} was ignored"), m_Warnings.ToString());
        }

        /// <summary>
        /// N4E replicates a <see cref="Transform.SetParent(Transform, bool)"/> made on the authority. Were NGO still handling
        /// the change, clients would revert it and log an error, which fails the test.
        /// </summary>
        [UnityTest]
        public IEnumerator ParentReplication_TransformSetParent_ReplicatesWithoutNgo([Values] bool worldPositionStays)
        {
            yield return SpawnParentAndChild(m_HybridPrefab);
            m_Warnings.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
            try
            {
                m_Child.transform.SetParent(m_Parent.transform, worldPositionStays);
                yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, true));
                AssertOnTimeout("Timed out waiting for N4E to replicate the parent to all instances!");

                m_Child.transform.SetParent(null, worldPositionStays);
                yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, false));
                AssertOnTimeout("Timed out waiting for N4E to replicate the removed parent to all instances!");
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            Assert.AreEqual(0, WarningCount(k_WorldPositionStaysWarning), m_Warnings.ToString());
        }

        /// <summary>
        /// NGO still moves first generation children to the root when their parent is despawned, and N4E replicates that.
        /// </summary>
        [UnityTest]
        [Ignore("Intermittent: a client can apply the parent's despawn before the replicated unparent, destroying the child with it.")]
        public IEnumerator ParentReplication_ParentDespawned_ChildSurvivesAtRoot()
        {
            yield return SpawnParentAndChild(m_HybridPrefab);

            m_Child.transform.SetParent(m_Parent.transform, false);
            yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, true));
            AssertOnTimeout("Timed out waiting for N4E to replicate the parent to all instances!");

            var parentId = m_Parent.NetworkObjectId;
            var childId = m_Child.NetworkObjectId;
            m_Parent.Despawn();
            yield return WaitForConditionOrTimeOut(errorLog =>
            {
                foreach (var networkManager in m_NetworkManagers)
                {
                    if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(parentId))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The parent is still spawned.");
                    }
                    if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(childId, out var child) || child == null)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The child was destroyed with its parent.");
                    }
                    else if (child.transform.parent != null)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The child is still parented under {child.transform.parent.name}.");
                    }
                }
                return errorLog.Length == 0;
            });
            AssertOnTimeout("Timed out waiting for the child to survive at the root on all instances!");
        }
    }
}
#endif
