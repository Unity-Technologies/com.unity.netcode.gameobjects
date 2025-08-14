using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.DAHost)]
    internal class AttachableBehaviourTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        public AttachableBehaviourTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        private GameObject m_AttachablePrefab;
        private GameObject m_TargetNodePrefabA;
        private GameObject m_TargetNodePrefabB;

        /// <summary>
        /// All of the below instances belong to the authority
        /// </summary>
        private ulong m_TargetInstanceId;
        private NetworkObject m_SourceInstance;
        private NetworkObject m_TargetInstance;
        private NetworkObject m_TargetInstanceB;
        private TestAttachable m_PrefabTestAttachable;
        private TestAttachable m_AttachableBehaviourInstance;
        private TestNode m_AttachableNodeInstance;
        private TestNode m_AttachableNodeInstanceB;

        private bool m_UseTargetB;

        private StringBuilder m_ErrorLog = new StringBuilder();

        protected override IEnumerator OnSetup()
        {
            m_ErrorLog.Clear();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            // The source prefab contains the nested NetworkBehaviour that
            // will be parented under the target prefab.
            m_AttachablePrefab = CreateNetworkObjectPrefab("Source");
            var attachableNetworkObject = m_AttachablePrefab.GetComponent<NetworkObject>();
            attachableNetworkObject.DontDestroyWithOwner = true;
            attachableNetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);

            // The target prefab that the source prefab will attach
            // will be parented under the target prefab.
            m_TargetNodePrefabA = CreateNetworkObjectPrefab("TargetA");
            m_TargetNodePrefabB = CreateNetworkObjectPrefab("TargetB");
            var sourceChild = new GameObject("SourceChild");
            var targetChildA = new GameObject("TargetChildA");
            var targetChildB = new GameObject("TargetChildB");
            sourceChild.transform.parent = m_AttachablePrefab.transform;
            targetChildA.transform.parent = m_TargetNodePrefabA.transform;
            targetChildB.transform.parent = m_TargetNodePrefabB.transform;

            m_PrefabTestAttachable = sourceChild.AddComponent<TestAttachable>();
            var targetChildATestNode = targetChildA.AddComponent<TestNode>();
            targetChildATestNode.DetachOnDespawn = true;
            targetChildB.AddComponent<TestNode>();
            base.OnServerAndClientsCreated();
        }

        private NetworkObject GetTargetInstance()
        {
            return m_UseTargetB ? m_TargetInstanceB : m_TargetInstance;
        }

        private bool AllClientsSpawnedInstances()
        {
            m_ErrorLog.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_SourceInstance.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has not spawned {m_SourceInstance.name} yet!");
                }
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_TargetInstance.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has not spawned {m_TargetInstance.name} yet!");
                }
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_TargetInstanceB.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has not spawned {m_TargetInstanceB.name} yet!");
                }
            }
            return m_ErrorLog.Length == 0;
        }

        private bool ResetAllStates()
        {
            m_ErrorLog.Clear();
            var target = GetTargetInstance();

            TestAttachable.LastKnownEventStates.Clear();
            TestAttachable.LastKnownOverrideStates.Clear();
            // The attachable can move between the two spawned instances.
            var currentAttachableRoot = m_AttachableBehaviourInstance.State == AttachableBehaviour.AttachState.Attached ? target : m_SourceInstance;

            foreach (var networkManager in m_NetworkManagers)
            {
                // Source
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_SourceInstance.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {currentAttachableRoot.name}!");
                }
                else
                {
                    var attachable = networkManager.SpawnManager.SpawnedObjects[currentAttachableRoot.NetworkObjectId].GetComponentInChildren<TestAttachable>();
                    attachable.ResetStates();
                }

                // Target
                if (m_TargetInstance && !networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_TargetInstance.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {m_TargetInstance.name}!");
                }
                else
                {
                    var node = networkManager.SpawnManager.SpawnedObjects[m_TargetInstance.NetworkObjectId].GetComponentInChildren<TestNode>();
                    node.ResetStates();
                }

                // Target B
                if (m_TargetInstanceB && !networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_TargetInstanceB.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {m_TargetInstanceB.name}!");
                }
                else
                {
                    var node = networkManager.SpawnManager.SpawnedObjects[m_TargetInstanceB.NetworkObjectId].GetComponentInChildren<TestNode>();
                    node.ResetStates();
                }
            }
            return m_ErrorLog.Length == 0;
        }

        private bool AllInstancesAttachedStateChanged(bool checkAttached, bool ignoreIfDespawned = false)
        {
            m_ErrorLog.Clear();
            var target = GetTargetInstance();
            var targetId = target == null ? m_TargetInstanceId : target.NetworkObjectId;
            // The attachable can move between the two spawned instances so we have to use the appropriate one depending upon the authority's current state.
            var currentAttachableRoot = m_AttachableBehaviourInstance.State == AttachableBehaviour.AttachState.Attached ? target : m_SourceInstance;
            var attachable = (TestAttachable)null;
            var node = (TestNode)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(currentAttachableRoot.NetworkObjectId))
                {
                    if (!ignoreIfDespawned)
                    {
                        m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {currentAttachableRoot.name}!");
                    }
                    continue;
                }
                else
                {
                    attachable = networkManager.SpawnManager.SpawnedObjects[currentAttachableRoot.NetworkObjectId].GetComponentInChildren<TestAttachable>();
                }

                if (!attachable)
                {
                    attachable = networkManager.SpawnManager.SpawnedObjects[m_TargetInstanceId].GetComponentInChildren<TestAttachable>();
                    if (!attachable)
                    {
                        attachable = networkManager.SpawnManager.SpawnedObjects[m_TargetInstanceB.NetworkObjectId].GetComponentInChildren<TestAttachable>();
                        if (!attachable)
                        {
                            m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][Attachable] Attachable was not found!");
                        }
                    }
                    continue;
                }

                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(targetId))
                {
                    if (!ignoreIfDespawned)
                    {
                        m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {target.name}!");
                    }
                    continue;
                }
                else
                {
                    node = networkManager.SpawnManager.SpawnedObjects[targetId].GetComponentInChildren<TestNode>();
                }

                if (!node && ignoreIfDespawned)
                {
                    VerboseDebug("Skipping check during despawn.");
                    continue;
                }

                if (!attachable.CheckForState(checkAttached, false))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Did not have its override invoked!");
                }
                if (!attachable.CheckForState(checkAttached, true))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Did not have its event invoked!");
                }
                if ((checkAttached && !node.OnAttachedInvoked) || (!checkAttached && !node.OnDetachedInvoked))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{node.name}] Did not have its override invoked!");
                }
                if (checkAttached && attachable.transform.parent != node.transform)
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] {node.name} is not the parent of {attachable.name}!");
                }
                else if (!checkAttached && attachable.transform.parent != attachable.DefaultParent.transform)
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] {attachable.DefaultParent.name} is not the parent of {attachable.name}!");
                }
            }
            return m_ErrorLog.Length == 0;
        }

        private bool AllInstancesDespawned()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager != null && networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_TargetInstanceId))
                {
                    return false;
                }
            }
            return true;
        }


        [UnityTest]
        public IEnumerator AttachAndDetachTests()
        {
            var authority = GetAuthorityNetworkManager();

            m_SourceInstance = SpawnObject(m_AttachablePrefab, authority).GetComponent<NetworkObject>();
            m_TargetInstance = SpawnObject(m_TargetNodePrefabA, authority).GetComponent<NetworkObject>();
            m_TargetInstanceB = SpawnObject(m_TargetNodePrefabB, authority).GetComponent<NetworkObject>();
            m_TargetInstanceId = m_TargetInstance.NetworkObjectId;
            yield return WaitForConditionOrTimeOut(AllClientsSpawnedInstances);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {m_SourceInstance.name} and {m_TargetInstance.name}!\n {m_ErrorLog}");

            m_AttachableBehaviourInstance = m_SourceInstance.GetComponentInChildren<TestAttachable>();
            Assert.NotNull(m_AttachableBehaviourInstance, $"{m_SourceInstance.name} does not have a nested child {nameof(AttachableBehaviour)}!");

            m_AttachableNodeInstance = m_TargetInstance.GetComponentInChildren<TestNode>();
            Assert.NotNull(m_AttachableNodeInstance, $"{m_TargetInstance.name} does not have a nested child {nameof(AttachableNode)}!");

            m_AttachableNodeInstanceB = m_TargetInstanceB.GetComponentInChildren<TestNode>();
            Assert.NotNull(m_AttachableNodeInstanceB, $"{m_TargetInstanceB.name} does not have a nested child {nameof(AttachableNode)}!");

            Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
            m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstance);

            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
            AssertOnTimeout($"Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

            // Wait a brief period of time
            yield return s_DefaultWaitForTick;

            // Now late join a client to make sure it synchronizes properly
            yield return CreateAndStartNewClient();
            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
            AssertOnTimeout($"Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

            // Wait a brief period of time
            yield return s_DefaultWaitForTick;

            // Reset all states and prepare for 2nd attach test
            Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
            Debug.Log("Attaching Node-B");

            // Now, while attached, attach to another attachable node which should detach from the current and attach to the new.
            m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstanceB);

            // The attachable should detach from the current AttachableNode first
            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(false));
            AssertOnTimeout($"Timed out waiting for all clients to detach {m_AttachableBehaviourInstance.name} from {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

            // Switch the conditional to check the target B attachable node
            m_UseTargetB = true;

            // Then the attachable should attach to the target B attachable node
            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
            AssertOnTimeout($"Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstanceB.name}!\n {m_ErrorLog}");

            // Reset all states and prepare for final detach test
            Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");

            // Now verify complete detaching works
            m_AttachableBehaviourInstance.Detach();
            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(false));
            AssertOnTimeout($"Timed out waiting for all clients to detach {m_AttachableBehaviourInstance.name} from {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

            // Finally, re-attach to the original spawned instance
            Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
            m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstance);

            // Switch back to using the first target attachable node
            m_UseTargetB = false;

            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
            AssertOnTimeout($"[Despawn Detach Phase] Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

            var targetInstanceName = m_TargetInstance.name;
            VerboseDebug("======== DESPAWN & DETACH ========");
            m_TargetInstance.Despawn();
            m_TargetInstance = null;
            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(false, true));
            AssertOnTimeout($"[Despawn Detach Phase] Timed out waiting for all clients to detach {m_AttachableBehaviourInstance.name} from {targetInstanceName}!\n {m_ErrorLog}");

            yield return WaitForConditionOrTimeOut(AllInstancesDespawned);
            AssertOnTimeout($"[Despawn Detach Phase] Timed out waiting for all clients to despawn {targetInstanceName}!");
        }


        private bool OwnershipChangedOnAllInstances()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager != null && !networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AttachableBehaviourInstance.NetworkObjectId))
                {
                    return false;
                }
                if (m_NonAuthority.LocalClientId != networkManager.SpawnManager.SpawnedObjects[m_AttachableBehaviourInstance.NetworkObjectId].OwnerClientId)
                {
                    return false;
                }
            }
            return true;
        }


        private bool ObjectDespawnedOnAllInstances(ulong networkObjectId)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager != null && networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AllInstancesDetached()
        {
            m_ErrorLog.Clear();
            // The attachable can move between the two spawned instances so we have to use the appropriate one depending upon the authority's current state.
            var attachable = (TestAttachable)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AttachableBehaviourInstance.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {m_AttachableBehaviourInstance.name}!");
                    continue;
                }
                else
                {
                    attachable = networkManager.SpawnManager.SpawnedObjects[m_AttachableBehaviourInstance.NetworkObjectId].GetComponentInChildren<TestAttachable>();
                }

                if (!attachable)
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][Attachable] Attachable was not found!");
                    continue;
                }

                if (!attachable.CheckForState(false, false))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Did not have its override invoked!");
                }
                if (!attachable.CheckForState(false, true))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Did not have its event invoked!");
                }

                if (attachable.InternalAttachableNode != null)
                {
                    var nodeHasAttachments = attachable.InternalAttachableNode.HasAttachments ? $" {attachable.InternalAttachableNode.name} still has attachments!" : "";
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Still refers to {attachable.InternalAttachableNode.name}!{nodeHasAttachments}");
                }
            }
            return m_ErrorLog.Length == 0;
        }

        private bool AllInstancesDetachedWhenAttachableDespawned()
        {
            m_ErrorLog.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                var localClientId = networkManager.LocalClientId;
                if (!TestAttachable.LastKnownOverrideStates.ContainsKey(localClientId))
                {
                    m_ErrorLog.AppendLine($"[Client-{localClientId}] Has no override states!");
                    continue;
                }
                if (!TestAttachable.LastKnownEventStates.ContainsKey(localClientId))
                {
                    m_ErrorLog.AppendLine($"[Client-{localClientId}] Has no event states!");
                    continue;
                }
                if (!TestAttachable.LastKnownOverrideStates[localClientId].ContainsKey(AttachableBehaviour.AttachState.Detached))
                {
                    m_ErrorLog.AppendLine($"[Client-{localClientId}] Does not contain the {AttachableBehaviour.AttachState.Detached} override state!");
                    continue;
                }
                if (!TestAttachable.LastKnownEventStates[localClientId].ContainsKey(AttachableBehaviour.AttachState.Detached))
                {
                    m_ErrorLog.AppendLine($"[Client-{localClientId}] Does not contain the {AttachableBehaviour.AttachState.Detached} event state!");
                    continue;
                }

                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_TargetInstanceId))
                {
                    m_ErrorLog.AppendLine($"[Client-{localClientId}] Does not have a spawned target node ({m_TargetInstanceId})!");
                    continue;
                }
                var targetNode = networkManager.SpawnManager.SpawnedObjects[m_TargetInstanceId].GetComponentInChildren<TestNode>();
                if (targetNode == null)
                {
                    m_ErrorLog.AppendLine($"[Client-{localClientId}] Does not have a target node component (null)!");
                    continue;
                }

                if (targetNode.HasAttachments)
                {
                    m_ErrorLog.AppendLine($"[Client-{localClientId}] {targetNode.name} still has attachments!");
                }
            }
            return m_ErrorLog.Length == 0;
        }

        private NetworkManager m_NonAuthority;
        private NetworkManager m_Authority;

        public enum DetachCombinations
        {
            AllFlags,
            OwnerDespawn,
            OwnerDestroy,
            DespawnDestroy,
            Owner,
            Despawn,
            Destroy
        }

        private AttachableBehaviour.AutoDetachTypes GetDetachType(DetachCombinations detachCombination)
        {
            var autoDetachTypeFlags = AttachableBehaviour.AutoDetachTypes.OnDespawn | AttachableBehaviour.AutoDetachTypes.OnOwnershipChange | AttachableBehaviour.AutoDetachTypes.OnAttachNodeDestroy;
            switch (detachCombination)
            {
                case DetachCombinations.AllFlags:
                    {
                        break;
                    }
                case DetachCombinations.OwnerDespawn:
                    {
                        autoDetachTypeFlags = AttachableBehaviour.AutoDetachTypes.OnDespawn | AttachableBehaviour.AutoDetachTypes.OnOwnershipChange;
                        break;
                    }
                case DetachCombinations.OwnerDestroy:
                    {
                        autoDetachTypeFlags = AttachableBehaviour.AutoDetachTypes.OnOwnershipChange | AttachableBehaviour.AutoDetachTypes.OnAttachNodeDestroy;
                        break;
                    }
                case DetachCombinations.DespawnDestroy:
                    {
                        autoDetachTypeFlags = AttachableBehaviour.AutoDetachTypes.OnDespawn | AttachableBehaviour.AutoDetachTypes.OnAttachNodeDestroy;
                        break;
                    }
                case DetachCombinations.Owner:
                    {
                        autoDetachTypeFlags = AttachableBehaviour.AutoDetachTypes.OnOwnershipChange;
                        break;
                    }
                case DetachCombinations.Despawn:
                    {
                        autoDetachTypeFlags = AttachableBehaviour.AutoDetachTypes.OnDespawn;
                        break;
                    }
                case DetachCombinations.Destroy:
                    {
                        autoDetachTypeFlags = AttachableBehaviour.AutoDetachTypes.OnAttachNodeDestroy;
                        break;
                    }
            }

            return autoDetachTypeFlags;
        }

        [UnityTest]
        public IEnumerator AutoDetachTests([Values] DetachCombinations detachCombination)
        {
            var autoDetachTypeFlags = GetDetachType(detachCombination);
            m_UseTargetB = false;
            m_Authority = GetAuthorityNetworkManager();
            m_NonAuthority = GetNonAuthorityNetworkManager();

            m_PrefabTestAttachable.AutoDetach = autoDetachTypeFlags;

            m_SourceInstance = SpawnObject(m_AttachablePrefab, m_Authority).GetComponent<NetworkObject>();
            m_TargetInstance = SpawnObject(m_TargetNodePrefabA, m_Authority).GetComponent<NetworkObject>();
            m_TargetInstanceB = SpawnObject(m_TargetNodePrefabB, m_Authority).GetComponent<NetworkObject>();
            m_TargetInstanceId = m_TargetInstance.NetworkObjectId;
            yield return WaitForConditionOrTimeOut(AllClientsSpawnedInstances);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {m_SourceInstance.name} and {m_TargetInstance.name}!\n {m_ErrorLog}");

            m_AttachableBehaviourInstance = m_SourceInstance.GetComponentInChildren<TestAttachable>();
            Assert.NotNull(m_AttachableBehaviourInstance, $"{m_SourceInstance.name} does not have a nested child {nameof(AttachableBehaviour)}!");

            m_AttachableNodeInstance = m_TargetInstance.GetComponentInChildren<TestNode>();
            Assert.NotNull(m_AttachableNodeInstance, $"{m_TargetInstance.name} does not have a nested child {nameof(AttachableNode)}!");

            m_AttachableNodeInstanceB = m_TargetInstanceB.GetComponentInChildren<TestNode>();
            Assert.NotNull(m_AttachableNodeInstanceB, $"{m_TargetInstanceB.name} does not have a nested child {nameof(AttachableNode)}!");

            Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
            m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstance);

            yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
            AssertOnTimeout($"Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

            if (autoDetachTypeFlags.HasFlag(AttachableBehaviour.AutoDetachTypes.OnOwnershipChange))
            {
                Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
                if (m_DistributedAuthority)
                {
                    m_Authority.SpawnManager.SpawnedObjects[m_SourceInstance.NetworkObjectId].ChangeOwnership(m_NonAuthority.LocalClientId);
                }
                else
                {
                    m_SourceInstance.ChangeOwnership(m_NonAuthority.LocalClientId);
                }

                yield return WaitForConditionOrTimeOut(OwnershipChangedOnAllInstances);
                AssertOnTimeout($"[OnOwnershipChange] Timed out waiting for all clients to change the ownership from {m_Authority.name} to {m_NonAuthority.name}!");

                yield return WaitForConditionOrTimeOut(AllInstancesDetached);
                AssertOnTimeout($"[OnOwnershipChange] Timed out waiting for all clients to detach {m_AttachableBehaviourInstance.name} from {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

                if (m_DistributedAuthority)
                {
                    var nonAuthority = m_Authority;
                    m_Authority = m_NonAuthority;
                    m_NonAuthority = nonAuthority;

                    m_SourceInstance = m_Authority.SpawnManager.SpawnedObjects[m_SourceInstance.NetworkObjectId];
                    m_AttachableBehaviourInstance = m_SourceInstance.GetComponentInChildren<TestAttachable>();
                    m_AttachableNodeInstance = m_Authority.SpawnManager.SpawnedObjects[m_AttachableNodeInstance.NetworkObjectId].GetComponentInChildren<TestNode>();
                    Assert.NotNull(m_AttachableBehaviourInstance, $"{m_SourceInstance.name} does not have a nested child {nameof(AttachableBehaviour)}!");
                }
                Assert.False(m_AttachableNodeInstance.IsAttached(m_AttachableBehaviourInstance), $"{m_AttachableNodeInstance.name} still thinks it is attached to {m_AttachableBehaviourInstance.name}!");
                Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");

                m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstance);

                yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
                AssertOnTimeout($"[OnOwnershipChange][End] Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

                // Reset context of the AttachableNode instance to the owner of the m_TargetInstance for other below tests
                if (m_DistributedAuthority)
                {
                    var attachableNodeInstanceName = m_AttachableNodeInstance.name;
                    var ownerNetworkManager = m_NetworkManagers.Where((c) => c.LocalClientId == m_AttachableNodeInstance.OwnerClientId).First();
                    Assert.True(ownerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AttachableNodeInstance.NetworkObjectId), $"{ownerNetworkManager.name} does not have a spawned instance of {m_AttachableNodeInstance.name}!");
                    m_AttachableNodeInstance = ownerNetworkManager.SpawnManager.SpawnedObjects[m_AttachableNodeInstance.NetworkObjectId].GetComponentInChildren<TestNode>();
                    Assert.NotNull(m_AttachableNodeInstance, $"{attachableNodeInstanceName} does not exist on {ownerNetworkManager.name}!");
                }
            }

            // Detach on despawn validation
            if (autoDetachTypeFlags.HasFlag(AttachableBehaviour.AutoDetachTypes.OnDespawn))
            {
                // Validates AttachableNode detaches AttachableBehaviours when despawned
                Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
                m_TargetInstance.Despawn(false);

                yield return WaitForConditionOrTimeOut(() => ObjectDespawnedOnAllInstances(m_TargetInstanceId));
                AssertOnTimeout($"[OnDespawn] Timed out waiting for all clients to despawn {m_TargetInstance.name}!");

                yield return WaitForConditionOrTimeOut(AllInstancesDetached);
                AssertOnTimeout($"[OnDespawn] Timed out waiting for all clients to detach {m_AttachableBehaviourInstance.name} from {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

                m_TargetInstance.Spawn();

                yield return WaitForConditionOrTimeOut(AllClientsSpawnedInstances);
                AssertOnTimeout($"Timed out waiting for all clients to spawn {m_TargetInstance.name}!\n {m_ErrorLog}");

                m_TargetInstanceId = m_TargetInstance.NetworkObjectId;

                Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
                m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstance);

                yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
                AssertOnTimeout($"[OnDespawn][End] Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

                // Validates when the AttachableBehaviour is despawned it will detach from the AttachableNode
                Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
                var sourceInstanceId = m_SourceInstance.NetworkObjectId;
                var sourceName = m_SourceInstance.name;
                var attachableName = m_AttachableBehaviourInstance.name;
                m_SourceInstance.Despawn(false);

                yield return WaitForConditionOrTimeOut(() => ObjectDespawnedOnAllInstances(sourceInstanceId));
                AssertOnTimeout($"[OnDespawn] Timed out waiting for all clients to despawn {sourceName}!");

                yield return WaitForConditionOrTimeOut(AllInstancesDetachedWhenAttachableDespawned);
                AssertOnTimeout($"[OnDespawn] Timed out waiting for all clients to detach {attachableName} from {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");

                m_SourceInstance.Spawn();

                yield return WaitForConditionOrTimeOut(AllClientsSpawnedInstances);
                AssertOnTimeout($"Timed out waiting for all clients to spawn {m_TargetInstance.name}!\n {m_ErrorLog}");

                Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
                if (m_DistributedAuthority)
                {
                    m_AttachableBehaviourInstance = m_SourceInstance.GetComponentInChildren<TestAttachable>();
                    Assert.NotNull(m_AttachableBehaviourInstance, $"{m_SourceInstance.name} does not have a nested child {nameof(AttachableBehaviour)}!");
                }

                m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstance);

                yield return WaitForConditionOrTimeOut(() => AllInstancesAttachedStateChanged(true));
                AssertOnTimeout($"[OnDespawn][End] Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!\n {m_ErrorLog}");
            }

            // Detach on destroy validation
            if (autoDetachTypeFlags.HasFlag(AttachableBehaviour.AutoDetachTypes.OnAttachNodeDestroy))
            {
                Assert.True(ResetAllStates(), $"Failed to reset all states!\n {m_ErrorLog}");
                // Mock the edge case scenario where the AttachableNode could be destroyed when an AttachableBehaviour is attached.
                // Remove all other flags but the OnAttachNodeDestroy to assure this is what is invoked when the spawned AttachableNode (TargetInstance)
                // is destroyed.
                foreach (var networkManager in m_NetworkManagers)
                {
                    var targetInstance = networkManager.SpawnManager.SpawnedObjects[m_TargetInstance.NetworkObjectId];
                    var attachable = targetInstance.GetComponentInChildren<TestAttachable>();
                    // Directly assign the value to assure this is the only thing that will trigger a detach
                    attachable.AutoDetach = AttachableBehaviour.AutoDetachTypes.OnAttachNodeDestroy;
                }
                var attachableNodeName = m_AttachableNodeInstance.name;
                Object.Destroy(m_TargetInstance.gameObject);
                yield return WaitForConditionOrTimeOut(AllInstancesDetached);
                AssertOnTimeout($"[OnAttachNodeDestroy] Timed out waiting for all clients to detach {m_AttachableBehaviourInstance.name} from {attachableNodeName}!\n {m_ErrorLog}");
            }
        }

        /// <summary>
        /// Helps to validate that the overrides and events are invoked when an attachable attaches or detaches from the instance.
        /// This also helps to validate that the appropriate <see cref="AttachableNode"/> instance is passed in as a parameter.
        /// </summary>
        internal class TestAttachable : AttachableBehaviour
        {
            public static bool VerboseMode;

            public static readonly Dictionary<ulong, Dictionary<AttachState, AttachableNode>> LastKnownOverrideStates = new Dictionary<ulong, Dictionary<AttachState, AttachableNode>>();
            public static readonly Dictionary<ulong, Dictionary<AttachState, AttachableNode>> LastKnownEventStates = new Dictionary<ulong, Dictionary<AttachState, AttachableNode>>();

            private Dictionary<AttachState, AttachableNode> m_StateUpdates = new Dictionary<AttachState, AttachableNode>();
            private Dictionary<AttachState, AttachableNode> m_StateUpdateEvents = new Dictionary<AttachState, AttachableNode>();

            public GameObject DefaultParent => m_DefaultParent;
            public AttachState State => m_AttachState;

            public override void OnNetworkSpawn()
            {
                AttachStateChange += OnAttachStateChangeEvent;
                name = $"{name}-{NetworkManager.LocalClientId}";
                base.OnNetworkSpawn();
            }

            public override void OnNetworkDespawn()
            {
                if (!LastKnownOverrideStates.ContainsKey(NetworkManager.LocalClientId))
                {
                    var localClientId = NetworkManager.LocalClientId;
                    LastKnownOverrideStates.Add(localClientId, new Dictionary<AttachState, AttachableNode>());
                    LastKnownEventStates.Add(localClientId, new Dictionary<AttachState, AttachableNode>());

                    foreach (var overrideEntry in m_StateUpdates)
                    {
                        LastKnownOverrideStates[localClientId].Add(overrideEntry.Key, overrideEntry.Value);
                    }

                    foreach (var eventEntry in m_StateUpdateEvents)
                    {
                        LastKnownEventStates[localClientId].Add(eventEntry.Key, eventEntry.Value);
                    }
                }
                AttachStateChange -= OnAttachStateChangeEvent;
                base.OnNetworkDespawn();
            }

            private void OnAttachStateChangeEvent(AttachState attachState, AttachableNode attachableNode)
            {
                Log($"[Event][{name}][AttachState Changed] State: {attachState}");
                // When attaching to a new target node while attached to an existing one, just overwrite
                // to get the most current attach state.
                if (m_StateUpdateEvents.ContainsKey(attachState))
                {
                    m_StateUpdateEvents.Remove(attachState);
                }
                m_StateUpdateEvents.Add(attachState, attachableNode);
            }

            protected override void OnAttachStateChanged(AttachState attachState, AttachableNode attachableNode)
            {
                Log($"[Override][{name}][AttachState Changed] State: {attachState}");
                // When attaching to a new target node while attached to an existing one, just overwrite
                // to get the most current attach state.
                if (m_StateUpdates.ContainsKey(attachState))
                {
                    m_StateUpdates.Remove(attachState);
                }
                m_StateUpdates.Add(attachState, attachableNode);
                base.OnAttachStateChanged(attachState, attachableNode);
            }

            public void ResetStates()
            {
                m_StateUpdates.Clear();
                m_StateUpdateEvents.Clear();
            }

            private void Log(string message)
            {
                if (!VerboseMode)
                {
                    return;
                }
                Debug.Log($"[{name}] {message}");
            }

            public bool CheckForState(bool checkAttached, bool checkEvent)
            {
                var tableToCheck = checkEvent ? m_StateUpdateEvents : m_StateUpdates;
                var expectedState = checkAttached ? AttachState.Attached : AttachState.Detached;
                var checkStatus = tableToCheck.ContainsKey(expectedState);

                if (checkStatus)
                {
                    if ((checkAttached && transform.parent == DefaultParent.transform) || (!checkAttached && transform.parent != DefaultParent.transform))
                    {
                        if (!checkAttached)
                        {
                            Log($"[CheckState][Fail][Wrong Parent] checkAttached = {checkAttached} | parent = {transform.parent?.name} | Expected {DefaultParent.name}");
                        }
                        else
                        {
                            Log($"[CheckState][Fail][Wrong Parent] checkAttached = {checkAttached} | parent = {transform.parent?.name}");
                        }
                        return false;
                    }
                }
                else
                {
                    var checkType = checkEvent ? "m_StateUpdateEvents" : "m_StateUpdates";
                    Log($"[CheckState][Fail][No Event Logged] checkAttached = {checkAttached} | table {checkType} does not contain the expected state {expectedState} log.");
                }
                return checkStatus;
            }
        }

        /// <summary>
        /// Helps to validate that the overrides are invoked when an attachable attaches or detaches from the instance.
        /// </summary>
        internal class TestNode : AttachableNode
        {
            public bool OnAttachedInvoked { get; private set; }
            public bool OnDetachedInvoked { get; private set; }

            public bool IsAttached(AttachableBehaviour attachableBehaviour)
            {
                return m_AttachedBehaviours.Contains(attachableBehaviour);
            }

            public void ResetStates()
            {
                OnAttachedInvoked = false;
                OnDetachedInvoked = false;
            }

            protected override void OnAttached(AttachableBehaviour attachableBehaviour)
            {
                OnAttachedInvoked = true;
                base.OnAttached(attachableBehaviour);
            }

            protected override void OnDetached(AttachableBehaviour attachableBehaviour)
            {
                OnDetachedInvoked = true;
                base.OnDetached(attachableBehaviour);
            }
        }

        internal class TestController : ComponentController
        {

        }
    }
}
