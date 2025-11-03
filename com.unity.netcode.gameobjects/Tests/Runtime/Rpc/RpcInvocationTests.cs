using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    internal class RpcInvocationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 3;

        public RpcInvocationTests(NetworkTopologyTypes topologyType) : base(topologyType) { }

        private GameObject m_Prefab;

        private Dictionary<NetworkManager, InvokePermissionBehaviour> m_InvokeInstances = new();

        // TODO: [CmbServiceTests] Enable once the CMB service fixes the client spoofing issue.
        protected override bool UseCMBService() => false;

        protected override void OnServerAndClientsCreated()
        {
            m_Prefab = CreateNetworkObjectPrefab("RpcInvokePermissionTest");
            m_Prefab.AddComponent<InvokePermissionBehaviour>();

            base.OnServerAndClientsCreated();
        }

        private void BuildInvokeInstancesMap(ulong networkObjectId)
        {
            m_InvokeInstances.Clear();
            foreach (var manager in m_NetworkManagers)
            {
                Assert.IsTrue(manager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var instance));
                m_InvokeInstances.Add(manager, instance.GetComponent<InvokePermissionBehaviour>());
            }
        }

        private bool AllExpectedCallsReceived(StringBuilder errorLog)
        {
            var allInstancesValid = true;
            foreach (var instance in m_InvokeInstances.Values)
            {
                if (!instance.HasReceivedExpectedRpcs(errorLog))
                {
                    allInstancesValid = false;
                }
            }
            return allInstancesValid;
        }

        [UnityTest]
        public IEnumerator RpcInvokePermissionSendingTests()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_Prefab, nonAuthority).GetComponent<NetworkObject>();

            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout("Failed to spawn InvokePermissions test object");

            BuildInvokeInstancesMap(authorityInstance.NetworkObjectId);

            // [Rpc(SendTo.Everyone, InvokePermission.Server)]
            foreach (var (manager, instance) in m_InvokeInstances)
            {
                // When using the Cmb service there is no server so no calls should be made.
                if (!m_UseCmbService)
                {
                    instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.ServerInvokePermissionRpc)] = 1;
                }

                var threwException = false;
                try
                {
                    instance.ServerInvokePermissionRpc();
                }
                catch (RpcException)
                {
                    Assert.IsFalse(manager.IsServer);
                    threwException = true;
                }

                // Server should not throw, everyone else should throw
                Assert.AreEqual(!manager.IsServer, threwException, $"[Client-{manager.LocalClientId}] had an unexpected exception behaviour. Expected {(manager.IsServer ? "no exception" : "exception")} but was {(threwException ? "exception" : "no exception")}");
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[InvokePermissions.Server] Rpc invoked an incorrect number of times");

            // [Rpc(SendTo.Everyone, InvokePermission.Owner)]
            foreach (var (manager, instance) in m_InvokeInstances)
            {
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.OwnerInvokePermissionRpc)] = 1;
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.OwnerRequireOwnershipRpc)] = 1;

                var threwException = false;
                try
                {
                    instance.OwnerInvokePermissionRpc();
                    instance.OwnerRequireOwnershipRpc();
                }
                catch (RpcException)
                {
                    Assert.IsFalse(instance.IsOwner);
                    threwException = true;
                }

                Assert.AreEqual(!instance.IsOwner, threwException, $"[Client-{manager.LocalClientId}] had an unexpected exception behaviour. Expected {(instance.IsOwner ? "no exception" : "exception")} but was {(threwException ? "exception" : "no exception")}");
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[InvokePermissions.Owner] Rpc invoked an incorrect number of times");

            // [Rpc(SendTo.Everyone, InvokePermission.Everyone)]
            foreach (var (_, instance) in m_InvokeInstances)
            {
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc)] = NumberOfClients + 1;

                try
                {
                    instance.EveryoneInvokePermissionRpc();
                }
                catch (RpcException e)
                {
                    Assert.Fail($"Unexpected RpcException was thrown! Exception: {e}");
                }
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[InvokePermissions.Everyone] Rpc invoked an incorrect number of times");
        }


        [UnityTest]
        public IEnumerator RpcInvokePermissionReceivingTests()
        {
            var firstClient = GetNonAuthorityNetworkManager(0);

            var spawnedObject = SpawnObject(m_Prefab, firstClient).GetComponent<NetworkObject>();

            yield return WaitForSpawnedOnAllOrTimeOut(spawnedObject);
            AssertOnTimeout("Failed to spawn InvokePermissions test object");

            BuildInvokeInstancesMap(spawnedObject.NetworkObjectId);

            // [Rpc(SendTo.Everyone, InvokePermission.Server)]
            foreach (var (manager, instance) in m_InvokeInstances)
            {
                // When using the Cmb service there is no server so no calls should be made.
                if (!m_UseCmbService)
                {
                    instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.ServerInvokePermissionRpc)] = 1;
                }

                SendUncheckedMessage(manager, instance, nameof(InvokePermissionBehaviour.ServerInvokePermissionRpc));
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[InvokePermissions.Server] Incorrect Rpc calls received");

            // [Rpc(SendTo.Everyone, InvokePermission.Owner)]
            foreach (var (manager, instance) in m_InvokeInstances)
            {
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.OwnerInvokePermissionRpc)] = 1;
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.OwnerRequireOwnershipRpc)] = 1;

                SendUncheckedMessage(manager, instance, nameof(InvokePermissionBehaviour.OwnerInvokePermissionRpc));
                SendUncheckedMessage(manager, instance, nameof(InvokePermissionBehaviour.OwnerRequireOwnershipRpc));
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[InvokePermissions.Owner] Incorrect Rpc calls received");

            // [Rpc(SendTo.Everyone, InvokePermission.Everyone)]
            foreach (var (manager, instance) in m_InvokeInstances)
            {
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc)] = NumberOfClients + 1;

                SendUncheckedMessage(manager, instance, nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc));
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[InvokePermissions.Everyone] Incorrect Rpc calls received");

            var firstClientInstance = m_InvokeInstances[firstClient];
            var secondClient = GetNonAuthorityNetworkManager(1);
            var thirdClient = GetNonAuthorityNetworkManager(2);

            firstClientInstance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.TrackSenderIdRpc)] = 1;

            // Manually set the senderId to an incorrect value
            var secondClientInstance = m_InvokeInstances[secondClient];
            var bufferWriter = new FastBufferWriter(1024, Allocator.Temp);
            using (bufferWriter)
            {
                var rpcMessage = new RpcMessage
                {
                    Metadata = new RpcMetadata
                    {
                        NetworkObjectId = secondClientInstance.NetworkObjectId,
                        NetworkBehaviourId = secondClientInstance.NetworkBehaviourId,
                        NetworkRpcMethodId = GetMethodIdFromMethodName(nameof(InvokePermissionBehaviour.TrackSenderIdRpc)),
                    },
                    // Set the sender to the third client
                    SenderClientId = thirdClient.LocalClientId,
                    WriteBuffer = bufferWriter
                };

                // Send the message on the second client
                secondClientInstance.RpcTarget.Owner.Send(secondClientInstance, ref rpcMessage, NetworkDelivery.Reliable, new RpcParams());
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[SpoofedSenderId] Incorrect Rpc calls received");

            Assert.That(firstClientInstance.SenderIdReceived, Is.EqualTo(secondClient.LocalClientId), "Received spoofed sender id!");
        }

        private bool ValidateInvocationOrder(StringBuilder errorLog)
        {
            var allInstancesValid = true;
            foreach (var instance in m_InvokeInstances.Values)
            {
                if (!instance.RpcsWereInvokedInExpectedOrder(errorLog))
                {
                    allInstancesValid = false;
                }
            }
            return allInstancesValid;
        }

        [UnityTest]
        public IEnumerator RpcInvocationOrderTests()
        {
            var authority = GetAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_Prefab, authority).GetComponent<InvokePermissionBehaviour>();
            var errorLog = new StringBuilder();

            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance.NetworkObjectId);
            AssertOnTimeout("Failed to spawn InvokePermissions test object");

            Assert.IsTrue(authorityInstance.IsOwner);

            BuildInvokeInstancesMap(authorityInstance.NetworkObjectId);

            var expectedOrder = new List<string>()
            {
                nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc),
                nameof(InvokePermissionBehaviour.OwnerInvokePermissionRpc),
                nameof(InvokePermissionBehaviour.AnotherEveryoneInvokePermissionRpc),
            };
            foreach (var instance in m_InvokeInstances.Values)
            {
                instance.ExpectedInvocationOrder = expectedOrder;
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc)] = 1;
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.OwnerInvokePermissionRpc)] = 1;
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.AnotherEveryoneInvokePermissionRpc)] = 1;
            }

            authorityInstance.EveryoneInvokePermissionRpc();
            authorityInstance.OwnerInvokePermissionRpc();
            authorityInstance.AnotherEveryoneInvokePermissionRpc();

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[Simple ordering][authority] Incorrect number of rpcs were invoked");
            Assert.IsTrue(ValidateInvocationOrder(errorLog), $"[Simple ordering][authority] Rpcs were invoked in an incorrect order\n {errorLog}");
            errorLog.Clear();

            ResetAllExpectedInvocations();

            var nonAuthority = GetNonAuthorityNetworkManager();
            var nonAuthorityInstance = m_InvokeInstances[nonAuthority];

            expectedOrder = new List<string>()
            {
                nameof(InvokePermissionBehaviour.AnotherEveryoneInvokePermissionRpc),
                nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc),
                nameof(InvokePermissionBehaviour.AnotherEveryoneInvokePermissionRpc),
            };
            foreach (var instance in m_InvokeInstances.Values)
            {
                instance.ExpectedInvocationOrder = expectedOrder;
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc)] = 1;
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.AnotherEveryoneInvokePermissionRpc)] = 2;
            }

            nonAuthorityInstance.AnotherEveryoneInvokePermissionRpc();
            nonAuthorityInstance.EveryoneInvokePermissionRpc();
            nonAuthorityInstance.AnotherEveryoneInvokePermissionRpc();

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[Simple ordering][nonAuthority] Incorrect number of rpcs were invoked");
            Assert.IsTrue(ValidateInvocationOrder(errorLog), $"[Simple ordering][nonAuthority] Rpcs were invoked in an incorrect order\n {errorLog}");
            errorLog.Clear();

            for (var i = 0; i < 3; i++)
            {
                var testType = (LocalDeferMode)i;

                ResetAllExpectedInvocations();

                expectedOrder = new List<string>()
                {
                    nameof(InvokePermissionBehaviour.NestedInvocationRpc),
                    nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc),
                };
                var reversedOrder = new List<string>() { expectedOrder[1], expectedOrder[0] };
                foreach (var (manager, instance) in m_InvokeInstances)
                {
                    // Invocation order will be reversed when not the invoking instance if not using defer mode
                    var isReversed = testType != LocalDeferMode.Defer && manager != nonAuthority;
                    instance.ExpectedInvocationOrder = isReversed ? reversedOrder : expectedOrder;
                    instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.NestedInvocationRpc)] = 1;
                    instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.EveryoneInvokePermissionRpc)] = 1;
                }

                nonAuthorityInstance.NestedInvocationRpc(testType);

                yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
                AssertOnTimeout($"[Has nested][nonAuthority][{testType}] Incorrect number of rpcs were invoked");
                Assert.IsTrue(ValidateInvocationOrder(errorLog), $"[Has nested][nonAuthority][{testType}] Rpcs were invoked in an incorrect order\n {errorLog}");
                errorLog.Clear();
            }
        }

        private void ResetAllExpectedInvocations()
        {
            foreach (var instance in m_InvokeInstances.Values)
            {
                instance.Reset();
            }
        }


        private void SendUncheckedMessage(NetworkManager manager, InvokePermissionBehaviour invokePermissionsObject, string rpcMethodName)
        {
            using var bufferWriter = new FastBufferWriter(1024, Allocator.Temp);
            var rpcMessage = new RpcMessage
            {
                Metadata = new RpcMetadata
                {
                    NetworkObjectId = invokePermissionsObject.NetworkObjectId,
                    NetworkBehaviourId = invokePermissionsObject.NetworkBehaviourId,
                    NetworkRpcMethodId = GetMethodIdFromMethodName(rpcMethodName),
                },
                SenderClientId = manager.LocalClientId,
                WriteBuffer = bufferWriter
            };

            invokePermissionsObject.RpcTarget.Everyone.Send(invokePermissionsObject, ref rpcMessage, NetworkDelivery.Reliable, new RpcParams());
        }

        private static readonly Dictionary<string, uint> k_MethodIdLookups = new();

        private uint GetMethodIdFromMethodName(string methodName)
        {
            if (k_MethodIdLookups.TryGetValue(methodName, out var id))
            {
                return id;
            }

            var nameLookup = NetworkBehaviour.__rpc_name_table.GetValueOrDefault(typeof(InvokePermissionBehaviour));

            foreach (var (rpcMethodId, rpcMethodName) in nameLookup)
            {
                if (rpcMethodName == methodName)
                {
                    k_MethodIdLookups.Add(rpcMethodName, rpcMethodId);
                    return rpcMethodId;
                }
            }

            Assert.Fail($"Method \"{methodName}\" was not found in rpc method id lookups.");
            return default;
        }
    }

    internal class InvokePermissionBehaviour : NetworkBehaviour
    {
        private readonly Dictionary<string, int> m_RpcCallCounts = new();
        public readonly Dictionary<string, int> ExpectedCallCounts = new();
        private readonly List<string> m_RpcInvocationOrder = new();
        public List<string> ExpectedInvocationOrder = new();

        public bool HasReceivedExpectedRpcs(StringBuilder errorLog)
        {
            var isValid = true;
            var seen = new HashSet<string>();
            foreach (var (expectedMethodCall, expectedCallCount) in ExpectedCallCounts)
            {
                seen.Add(expectedMethodCall);
                if (!m_RpcCallCounts.TryGetValue(expectedMethodCall, out var actualCallCount))
                {
                    errorLog.AppendLine($"[Client-{NetworkManager.LocalClientId}] Expected {expectedMethodCall} to have been invoked!");
                }

                if (expectedCallCount != actualCallCount)
                {
                    isValid = false;
                    errorLog.AppendLine($"[Client-{NetworkManager.LocalClientId}] {expectedMethodCall} was invoked an incorrect number of times! Expected: {expectedCallCount}, Received: {actualCallCount}");
                }
            }

            // Ensure no other rpcs were called when they weren't expected
            foreach (var rpcCall in m_RpcCallCounts.Keys)
            {
                if (!seen.Contains(rpcCall))
                {
                    isValid = false;
                    errorLog.AppendLine($"[Client-{NetworkManager.LocalClientId}] {rpcCall} was invoked when it should not have been.");
                }
            }

            return isValid;
        }

        public bool RpcsWereInvokedInExpectedOrder(StringBuilder errorLog)
        {
            var isValid = true;
            for (var i = 0; i < m_RpcInvocationOrder.Count; i++)
            {
                if (!ExpectedInvocationOrder[i].Equals(m_RpcInvocationOrder[i]))
                {
                    errorLog.AppendLine($"[Client-{NetworkManager.LocalClientId}][Invocation-{i}] Rpc invoked in incorrect order. Expected {ExpectedInvocationOrder[i]}, but was {m_RpcInvocationOrder[i]}");
                    isValid = false;
                }
            }
            return isValid;
        }

        public void Reset()
        {
            m_RpcCallCounts.Clear();
            ExpectedCallCounts.Clear();
            m_RpcInvocationOrder.Clear();
            ExpectedInvocationOrder.Clear();
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void ServerInvokePermissionRpc()
        {
            TrackRpcCalled(GetCaller());
        }


#pragma warning disable CS0618 // Type or member is obsolete
        [Rpc(SendTo.Everyone, RequireOwnership = true)]
#pragma warning restore CS0618 // Type or member is obsolete
        public void OwnerRequireOwnershipRpc()
        {
            TrackRpcCalled(GetCaller());
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
        public void OwnerInvokePermissionRpc()
        {
            TrackRpcCalled(GetCaller());
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
        public void EveryoneInvokePermissionRpc()
        {
            TrackRpcCalled(GetCaller());
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
        public void AnotherEveryoneInvokePermissionRpc()
        {
            TrackRpcCalled(GetCaller());
        }

        [Rpc(SendTo.Everyone)]
        public void NestedInvocationRpc(RpcParams rpcParams = default)
        {
            TrackRpcCalled(GetCaller());

            if (rpcParams.Receive.SenderClientId == NetworkManager.LocalClientId)
            {
                EveryoneInvokePermissionRpc();
            }
        }

        internal ulong SenderIdReceived;
        [Rpc(SendTo.Owner)]
        public void TrackSenderIdRpc(RpcParams rpcParams)
        {
            TrackRpcCalled(GetCaller());
            SenderIdReceived = rpcParams.Receive.SenderClientId;
        }

        private void TrackRpcCalled(string rpcName)
        {
            // TryAdd returns false and will not add anything if the key already existed.
            if (!m_RpcCallCounts.TryAdd(rpcName, 1))
            {
                // If the key already existed, increment it
                m_RpcCallCounts[rpcName]++;
            }

            m_RpcInvocationOrder.Add(rpcName);
        }

        private static string GetCaller([CallerMemberName] string caller = null)
        {
            return caller;
        }
    }
}
