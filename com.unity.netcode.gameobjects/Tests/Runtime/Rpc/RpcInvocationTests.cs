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

                if (!manager.IsServer)
                {
                    Assert.IsTrue(threwException);
                }
            }

            yield return WaitForConditionOrTimeOut(AllExpectedCallsReceived);
            AssertOnTimeout("[InvokePermissions.Server] Rpc invoked an incorrect number of times");

            // [Rpc(SendTo.Everyone, InvokePermission.Owner)]
            foreach (var (_, instance) in m_InvokeInstances)
            {
                instance.ExpectedCallCounts[nameof(InvokePermissionBehaviour.OwnerInvokePermissionRpc)] = 1;

                var threwException = false;
                try
                {
                    instance.OwnerInvokePermissionRpc();
                }
                catch (RpcException)
                {
                    Assert.IsFalse(instance.IsOwner);
                    threwException = true;
                }

                if (!instance.IsOwner)
                {
                    Assert.IsTrue(threwException);
                }
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

                SendUncheckedMessage(manager, instance, nameof(InvokePermissionBehaviour.OwnerInvokePermissionRpc));
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
        public readonly Dictionary<string, int> RpcCallCounts = new();
        public readonly Dictionary<string, int> ExpectedCallCounts = new();

        public bool HasReceivedExpectedRpcs(StringBuilder errorLog)
        {
            var isValid = true;
            var seen = new HashSet<string>();
            foreach (var (expectedMethodCall, expectedCallCount) in ExpectedCallCounts)
            {
                seen.Add(expectedMethodCall);
                if (!RpcCallCounts.TryGetValue(expectedMethodCall, out var actualCallCount))
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
            foreach (var rpcCall in RpcCallCounts.Keys)
            {
                if (!seen.Contains(rpcCall))
                {
                    isValid = false;
                    errorLog.AppendLine($"[Client-{NetworkManager.LocalClientId}] {rpcCall} was invoked when it should not have been.");
                }
            }

            return isValid;
        }

        public void Reset()
        {
            RpcCallCounts.Clear();
            ExpectedCallCounts.Clear();
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void ServerInvokePermissionRpc()
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
            if (!RpcCallCounts.TryAdd(rpcName, 1))
            {
                // If the key already existed, increment it
                RpcCallCounts[rpcName]++;
            }
        }

        private static string GetCaller([CallerMemberName] string caller = null)
        {
            return caller;
        }
    }
}
