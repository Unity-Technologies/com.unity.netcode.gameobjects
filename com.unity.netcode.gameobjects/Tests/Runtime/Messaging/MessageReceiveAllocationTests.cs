using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace Unity.Netcode.RuntimeTests
{
    internal class AllocationTestBehaviour : NetworkBehaviour
    {
        internal int RpcReceivedCount;

        public NetworkVariable<int> TestVariable = new();

        [Rpc(SendTo.NotMe)]
        public void NotMeRpc()
        {
            RpcReceivedCount++;
        }
    }

    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class MessageReceiveAllocationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public MessageReceiveAllocationTests(HostOrServer hostOrServer) : base(hostOrServer) {}

        private GameObject m_Prefab;

        protected override void OnServerAndClientsCreated()
        {
            m_Prefab = CreateNetworkObjectPrefab(nameof(AllocationTestBehaviour));
            m_Prefab.AddComponent<AllocationTestBehaviour>();
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator NoAllocationsOnMessageReceive()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            var authorityInstance = SpawnObject(m_Prefab, authority);
            var authorityComponent = authorityInstance.GetComponent<AllocationTestBehaviour>();
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout("Timed out waiting for objects to spawn");

            var nonAuthorityComponent = nonAuthority.SpawnManager.SpawnedObjects[authorityComponent.NetworkObjectId].GetComponent<AllocationTestBehaviour>();

            /*
             * RpcMessage
             */
            var rpcCatcher = new MessageCatcher<RpcMessage>(nonAuthority);
            nonAuthority.ConnectionManager.MessageManager.Hook(rpcCatcher);

            // Send the same message twice: the first is replayed as a warm-up.
            // The second will follow the identical code path and is checked for allocations.
            authorityComponent.NotMeRpc();
            authorityComponent.NotMeRpc();
            yield return WaitForConditionOrTimeOut(() => rpcCatcher.CaughtMessageCount == 2);
            AssertOnTimeout($"Timed out waiting to catch all expected {nameof(RpcMessage)} messages. Expected: 2, Actual: {rpcCatcher.CaughtMessageCount}");

            // Unhook first so the replayed messages are handled instead of being caught again
            nonAuthority.ConnectionManager.MessageManager.Unhook(rpcCatcher);

            rpcCatcher.HandleCaughtMessage(0);
            Assert.AreEqual(1, nonAuthorityComponent.RpcReceivedCount);

            Assert.That(() =>
            {
                rpcCatcher.HandleCaughtMessage(1);
            }, Is.Not.AllocatingGCMemory());
            Assert.AreEqual(2, nonAuthorityComponent.RpcReceivedCount);

            /*
             * NetworkVariableDeltaMessage
             */
            var deltaCatcher = new MessageCatcher<NetworkVariableDeltaMessage>(nonAuthority);
            nonAuthority.ConnectionManager.MessageManager.Hook(deltaCatcher);

            authorityComponent.TestVariable.Value = 1;
            // Wait for the first change to be received client-side before sending the second change
            yield return WaitForConditionOrTimeOut(() => deltaCatcher.CaughtMessageCount == 1);
            authorityComponent.TestVariable.Value = 2;
            yield return WaitForConditionOrTimeOut(() => deltaCatcher.CaughtMessageCount == 2);
            AssertOnTimeout($"Timed out waiting to catch all expected {nameof(NetworkVariableDeltaMessage)} messages. Expected: 2, Actual: {deltaCatcher.CaughtMessageCount}");

            // Unhook first so the replayed messages are handled instead of being caught again
            nonAuthority.ConnectionManager.MessageManager.Unhook(deltaCatcher);

            deltaCatcher.HandleCaughtMessage(0);
            Assert.AreEqual(1, nonAuthorityComponent.TestVariable.Value);

            Assert.That(() =>
            {
                deltaCatcher.HandleCaughtMessage(1);
            }, Is.Not.AllocatingGCMemory());
            Assert.AreEqual(2, nonAuthorityComponent.TestVariable.Value);
        }

    }
}
