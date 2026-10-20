#if UNIFIED_NETCODE
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// NGO parenting of hybrid prefab instances: the worldPositionStays rejection and the mixed timelines warning when NGO handles it, and NGO standing down when
    /// <see cref="GhostObject"/> parent replication handles it.
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridParentingTests : NetcodeIntegrationTest
    {
        private const string k_WorldPositionStaysError = "can only be parented with worldPositionStays disabled";
        private const string k_MixedTimelinesWarning = "only one of them is a hybrid instance";

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
        /// Validates that NGO parenting of a hybrid instance is rejected with worldPositionStays enabled, since clients would apply the
        /// new parent before the ghost's rebased local transform and the object would pop, and that it works with worldPositionStays disabled.
        /// </summary>
        [UnityTest]
        public IEnumerator NgoParenting_WorldPositionStays_RejectedWhenEnabled([Values] bool worldPositionStays)
        {
            yield return SpawnParentAndChild(m_HybridPrefab);
            if (worldPositionStays)
            {
                LogAssert.Expect(LogType.Error, new Regex(k_WorldPositionStaysError));
                Assert.IsFalse(m_Child.TrySetParent(m_Parent, true), $"{nameof(NetworkObject.TrySetParent)} should be rejected!");
                yield return WaitForTicks(GetAuthorityNetworkManager(), 5);
                var errors = new StringBuilder();
                Assert.IsTrue(ChildParentedOnAllInstances(errors, false), errors.ToString());
                yield break;
            }

            Assert.IsTrue(m_Child.TrySetParent(m_Parent, false), $"{nameof(NetworkObject.TrySetParent)} failed!");
            yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, true));
            AssertOnTimeout("Timed out waiting for the child to be parented on all instances!");

            Assert.IsTrue(m_Child.TryRemoveParent(false), $"{nameof(NetworkObject.TryRemoveParent)} failed!");
            yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, false));
            AssertOnTimeout("Timed out waiting for the child to be moved to the root on all instances!");
        }

        /// <summary>
        /// Validates that a direct parent change of a hybrid instance on the authority (<see cref="Transform.parent"/> or
        /// <see cref="Transform.SetParent(Transform, bool)"/>) is replicated when it keeps the local transform, and is otherwise
        /// reverted to the previous parent and local transform.
        /// </summary>
        [UnityTest]
        public IEnumerator NgoParenting_DirectTransformParentChange([Values] bool worldPositionStays)
        {
            yield return SpawnParentAndChild(m_HybridPrefab);
            // Off the parent's position, so that a worldPositionStays reparent changes the local position.
            m_Parent.transform.position = new Vector3(0f, 1.25f, 0f);
            m_Child.transform.position = new Vector3(0f, 1.25f, 3f);
            var childTransform = m_Child.transform;
            var localPosition = childTransform.localPosition;
            if (worldPositionStays)
            {
                LogAssert.Expect(LogType.Error, new Regex(k_WorldPositionStaysError));
                childTransform.parent = m_Parent.transform;
                Assert.IsNull(childTransform.parent, "The parent change should have been reverted!");
                Assert.AreEqual(localPosition, childTransform.localPosition, "The revert should restore the local position!");
                yield return WaitForTicks(GetAuthorityNetworkManager(), 5);
                var errors = new StringBuilder();
                Assert.IsTrue(ChildParentedOnAllInstances(errors, false), errors.ToString());
                yield break;
            }

            childTransform.SetParent(m_Parent.transform, false);
            Assert.IsFalse(m_Child.WorldPositionStays(), "A parent change that kept the local transform should be sent without worldPositionStays!");
            yield return WaitForConditionOrTimeOut(errorLog => ChildParentedOnAllInstances(errorLog, true));
            AssertOnTimeout("Timed out waiting for the child to be parented on all instances!");
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
