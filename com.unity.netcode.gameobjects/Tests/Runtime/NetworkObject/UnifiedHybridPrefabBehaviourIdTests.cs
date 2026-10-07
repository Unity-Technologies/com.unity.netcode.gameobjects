#if UNIFIED_NETCODE && COM_UNITY_MODULES_PHYSICS
using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Added to the hybrid prefab last, so its <see cref="NetworkBehaviour.NetworkBehaviourId"/> would change if the
    /// <see cref="NetworkTransform"/> or <see cref="NetworkRigidbody"/> were removed from the instance.
    /// </summary>
    internal class HybridTrailingBehaviour : NetworkBehaviour
    {
    }

    /// <summary>
    /// A hybrid prefab keeps its <see cref="NetworkTransform"/> and <see cref="NetworkRigidbodyBase"/> components inert on the instance.<br />
    /// Validates that every peer assigns the same <see cref="NetworkBehaviour.NetworkBehaviourId"/> values and that the body stays kinematic on every peer except the server.<br />
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    internal class UnifiedHybridPrefabBehaviourIdTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        /// <summary>
        /// The <see cref="NetworkBehaviour"/> types in component order, read from the prefab because the test helpers add their own.<br />
        /// Each index is the expected <see cref="NetworkBehaviour.NetworkBehaviourId"/>.<br />
        /// </summary>
        private Type[] m_AuthoredBehaviourOrder;

        private GameObject m_Prefab;
        private NetworkObject m_Instance;

        public UnifiedHybridPrefabBehaviourIdTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_Prefab = CreateNetworkObjectPrefab("HybridOrdering");
            // Owner authority makes an ungated NetworkRigidbody change the kinematic state on an ownership change.
            m_Prefab.AddComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            m_Prefab.AddComponent<Rigidbody>();
            m_Prefab.AddComponent<NetworkRigidbody>();
            m_Prefab.AddComponent<HybridTrailingBehaviour>();

            var authoredBehaviours = m_Prefab.GetComponents<NetworkBehaviour>();
            m_AuthoredBehaviourOrder = new Type[authoredBehaviours.Length];
            for (int i = 0; i < authoredBehaviours.Length; i++)
            {
                m_AuthoredBehaviourOrder[i] = authoredBehaviours[i].GetType();
            }

            base.OnServerAndClientsCreated();
        }

        [HideInCallstack]
        private IEnumerator SpawnHybridInstance()
        {
            m_Instance = SpawnObject(m_Prefab, GetAuthorityNetworkManager()).GetComponent<NetworkObject>();

            yield return WaitForSpawnedOnAllOrTimeOut(m_Instance);
            AssertOnTimeout($"Failed to spawn {m_Instance.name} on all clients!");
        }

        /// <summary>
        /// Every peer has to hold the same behaviour table for the instance: the same number of entries, the
        /// same type at each index, and the same <see cref="NetworkBehaviour.NetworkBehaviourId"/> on each.
        /// </summary>
        private bool ValidateBehaviourTable(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var instance = networkManager.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId];
                var childBehaviours = instance.ChildNetworkBehaviours;
                if (childBehaviours.Count != m_AuthoredBehaviourOrder.Length)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {nameof(NetworkObject.ChildNetworkBehaviours)} holds " +
                        $"{childBehaviours.Count} entries but {m_AuthoredBehaviourOrder.Length} were authored!");
                    continue;
                }

                for (ushort index = 0; index < m_AuthoredBehaviourOrder.Length; index++)
                {
                    if (!childBehaviours.TryGetValue(index, out var behaviour))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] No {nameof(NetworkBehaviour)} at index {index}!");
                        continue;
                    }

                    if (behaviour.GetType() != m_AuthoredBehaviourOrder[index])
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Index {index} holds a {behaviour.GetType().Name} " +
                            $"but a {m_AuthoredBehaviourOrder[index].Name} was authored there!");
                    }

                    if (behaviour.NetworkBehaviourId != index)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {behaviour.GetType().Name} has a " +
                            $"{nameof(NetworkBehaviour.NetworkBehaviourId)} of {behaviour.NetworkBehaviourId} but is at index {index}!");
                    }
                }

                ValidateGatedComponents(networkManager, instance, errorLog);
            }

            return errorLog.Length == 0;
        }

        /// <summary>
        /// The gated components are still present and still inert.<br />
        /// The body is kinematic on every peer except the server.<br />
        /// </summary>
        private void ValidateGatedComponents(NetworkManager networkManager, NetworkObject instance, StringBuilder errorLog)
        {
            if (instance.GetComponent<NetworkRigidbodyBase>() == null)
            {
                errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The {nameof(NetworkRigidbody)} was removed from the instance!");
            }

            var isKinematic = instance.GetComponent<Rigidbody>().isKinematic;
            if (isKinematic == networkManager.IsServer)
            {
                errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {nameof(Rigidbody.isKinematic)} is {isKinematic} but {!networkManager.IsServer} was expected!");
            }

            var networkTransform = instance.GetComponent<NetworkTransform>();
            if (networkTransform == null)
            {
                errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The {nameof(NetworkTransform)} was removed from the instance!");
                return;
            }

            // A NetworkTransform that initialized takes authority on the server and registers its NetworkObject
            // for the per frame update pass everywhere else. A gated one does neither.
            if (networkTransform.CanCommitToTransform)
            {
                errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The {nameof(NetworkTransform)} took authority over a hybrid prefab!");
            }

            if (networkManager.NetworkTransformUpdate.ContainsKey(instance.NetworkObjectId))
            {
                errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The {nameof(NetworkTransform)} registered a hybrid prefab for updates!");
            }
        }

        private bool ValidateOwner(StringBuilder errorLog, ulong ownerClientId)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var ownerOnPeer = networkManager.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].OwnerClientId;
                if (ownerOnPeer != ownerClientId)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Owner is Client-{ownerOnPeer} but Client-{ownerClientId} was expected!");
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Validates that an ownership change leaves the body kinematic on every peer except the server.
        /// </summary>
        [UnityTest]
        public IEnumerator KinematicStateSurvivesOwnershipChange()
        {
            yield return SpawnHybridInstance();

            yield return WaitForConditionOrTimeOut(ValidateBehaviourTable);
            AssertOnTimeout("A peer disagreed about the hybrid prefab's behaviour table!");

            var newOwnerClientId = m_ClientNetworkManagers[0].LocalClientId;
            m_Instance.ChangeOwnership(newOwnerClientId);

            yield return WaitForConditionOrTimeOut(errorLog => ValidateOwner(errorLog, newOwnerClientId));
            AssertOnTimeout($"Ownership did not change to Client-{newOwnerClientId} on every peer!");

            yield return WaitForConditionOrTimeOut(ValidateBehaviourTable);
            AssertOnTimeout("The ownership change altered the hybrid prefab's gated components!");
        }
    }
}
#endif
