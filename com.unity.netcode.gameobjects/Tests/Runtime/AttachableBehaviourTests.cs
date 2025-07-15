using System.Collections;
using System.Collections.Generic;
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

        private GameObject m_SourcePrefab;
        private GameObject m_TargetPrefabA;
        private GameObject m_TargetPrefabB;

        /// <summary>
        /// All of the below instances belong to the authority
        /// </summary>
        private ulong m_TargetInstanceId;
        private NetworkObject m_SourceInstance;
        private NetworkObject m_TargetInstance;
        private NetworkObject m_TargetInstanceB;
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
            m_SourcePrefab = CreateNetworkObjectPrefab("Source");
            m_SourcePrefab.GetComponent<NetworkObject>().DontDestroyWithOwner = true;
            // The target prefab that the source prefab will attach
            // will be parented under the target prefab.
            m_TargetPrefabA = CreateNetworkObjectPrefab("TargetA");
            m_TargetPrefabB = CreateNetworkObjectPrefab("TargetB");
            var sourceChild = new GameObject("SourceChild");
            var targetChildA = new GameObject("TargetChildA");
            var targetChildB = new GameObject("TargetChildB");
            sourceChild.transform.parent = m_SourcePrefab.transform;
            targetChildA.transform.parent = m_TargetPrefabA.transform;
            targetChildB.transform.parent = m_TargetPrefabB.transform;

            sourceChild.AddComponent<TestAttachable>();
            targetChildA.AddComponent<TestNode>();
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

                if (!attachable.CheckStateChangedOverride(checkAttached, false, node))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Did not have its override invoked!");
                }
                if (!attachable.CheckStateChangedOverride(checkAttached, true, node))
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
            m_SourceInstance = SpawnObject(m_SourcePrefab, authority).GetComponent<NetworkObject>();
            m_TargetInstance = SpawnObject(m_TargetPrefabA, authority).GetComponent<NetworkObject>();
            m_TargetInstanceB = SpawnObject(m_TargetPrefabB, authority).GetComponent<NetworkObject>();
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

        /// <summary>
        /// Helps to validate that the overrides and events are invoked when an attachable attaches or detaches from the instance.
        /// This also helps to validate that the appropriate <see cref="AttachableNode"/> instance is passed in as a parameter.
        /// </summary>
        public class TestAttachable : AttachableBehaviour
        {
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
                AttachStateChange -= OnAttachStateChangeEvent;
                base.OnNetworkDespawn();
            }

            private void OnAttachStateChangeEvent(AttachState attachState, AttachableNode attachableNode)
            {
                m_StateUpdateEvents.Add(attachState, attachableNode);
            }

            protected override void OnAttachStateChanged(AttachState attachState, AttachableNode attachableNode)
            {
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
                Debug.Log($"[{name}] {message}");
            }

            public bool CheckStateChangedOverride(bool checkAttached, bool checkEvent, AttachableNode attachableNode)
            {
                var tableToCheck = checkEvent ? m_StateUpdateEvents : m_StateUpdates;
                var checkStatus = checkAttached ? (tableToCheck.ContainsKey(AttachState.Attaching) && tableToCheck.ContainsKey(AttachState.Attached)) :
                    (tableToCheck.ContainsKey(AttachState.Detaching) && tableToCheck.ContainsKey(AttachState.Detached));
                if (checkStatus)
                {
                    foreach (var entry in tableToCheck)
                    {
                        // Ignore any states that don't match what is being checked
                        if ((checkStatus && (entry.Key == AttachState.Detaching || entry.Key == AttachState.Detached)) ||
                            (!checkStatus && (entry.Key == AttachState.Attaching || entry.Key == AttachState.Attached)))
                        {
                            continue;
                        }

                        // Special case for completely detached
                        if (entry.Key == AttachState.Detached)
                        {
                            if (entry.Value != null)
                            {
                                Log($"[Value] The value {entry.Value.name} is not null!");
                                checkStatus = false;
                                break;
                            }
                        }
                        else if (entry.Value != attachableNode)
                        {
                            var attachableName = attachableNode == null ? "null" : attachableNode.name;
                            var entryName = entry.Value == null ? "null" : entry.Value.name;
                            Log($"[{entry.Key}][Value] The value {entryName} is not the same as {attachableName}!");
                            checkStatus = false;
                            break;
                        }
                    }
                }
                return checkStatus;
            }
        }

        /// <summary>
        /// Helps to validate that the overrides are invoked when an attachable attaches or detaches from the instance.
        /// </summary>
        public class TestNode : AttachableNode
        {
            public bool OnAttachedInvoked { get; private set; }
            public bool OnDetachedInvoked { get; private set; }

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
    }
}
