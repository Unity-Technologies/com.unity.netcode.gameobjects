using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that a rejected parent change restores the NetworkObject's parent and its original pose.
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkObjectParentRevertTests : NetcodeIntegrationTest
    {
        // Off the child's position, so that a parent change with worldPositionStays disabled moves the child.
        private static readonly Vector3 k_ParentPosition = new Vector3(5f, 1.25f, 0f);
        private static readonly Vector3 k_ChildPosition = new Vector3(0f, 1.25f, 3f);

        protected override int NumberOfClients => 1;

        private GameObject m_Prefab;

        public NetworkObjectParentRevertTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_Prefab = CreateNetworkObjectPrefab("ParentRevertObject");
            base.OnServerAndClientsCreated();
        }

        private void AssertPoseUnchanged(Transform child, Vector3 worldPosition, Vector3 localPosition)
        {
            Assert.IsNull(child.parent, $"{child.name}'s parent change should have been reverted!");
            Assert.AreEqual(worldPosition, child.position, $"{child.name}'s world position changed!");
            Assert.AreEqual(localPosition, child.localPosition, $"{child.name}'s local position changed!");
        }

        /// <summary>
        /// Validates that a parent change made by a peer without parenting authority is reverted to the original pose.
        /// </summary>
        [UnityTest]
        public IEnumerator NonAuthorityParentChangeRestoresPose([Values] bool worldPositionStays)
        {
            var authority = GetAuthorityNetworkManager();
            var parent = SpawnObject(m_Prefab, authority).GetComponent<NetworkObject>();
            var child = SpawnObject(m_Prefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(parent);
            AssertOnTimeout($"Timed out waiting for {parent.name} to spawn on all instances!");
            yield return WaitForSpawnedOnAllOrTimeOut(child);
            AssertOnTimeout($"Timed out waiting for {child.name} to spawn on all instances!");

            var nonAuthority = GetNonAuthorityNetworkManager();
            var localParent = nonAuthority.SpawnManager.SpawnedObjects[parent.NetworkObjectId].transform;
            var localChild = nonAuthority.SpawnManager.SpawnedObjects[child.NetworkObjectId].transform;
            localParent.position = k_ParentPosition;
            localChild.position = k_ChildPosition;
            var worldPosition = localChild.position;
            var localPosition = localChild.localPosition;

            LogAssert.Expect(LogType.Error, new Regex("re-parent"));
            localChild.SetParent(localParent, worldPositionStays);
            AssertPoseUnchanged(localChild, worldPosition, localPosition);
        }

        /// <summary>
        /// Validates that a parent change under a GameObject without a <see cref="NetworkObject"/> is reverted to the original pose.
        /// </summary>
        [UnityTest]
        public IEnumerator NonNetworkObjectParentRestoresPose([Values] bool worldPositionStays)
        {
            var authority = GetAuthorityNetworkManager();
            var child = SpawnObject(m_Prefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(child);
            AssertOnTimeout($"Timed out waiting for {child.name} to spawn on all instances!");

            var plainParent = new GameObject("PlainParent");
            try
            {
                plainParent.transform.position = k_ParentPosition;
                var childTransform = child.transform;
                childTransform.position = k_ChildPosition;
                var worldPosition = childTransform.position;
                var localPosition = childTransform.localPosition;

                LogAssert.Expect(LogType.Error, new Regex("Invalid parenting"));
                childTransform.SetParent(plainParent.transform, worldPositionStays);
                AssertPoseUnchanged(childTransform, worldPosition, localPosition);
            }
            finally
            {
                Object.Destroy(plainParent);
            }
        }
    }
}
