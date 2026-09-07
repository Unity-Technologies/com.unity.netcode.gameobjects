#if UNIFIED_NETCODE && COM_UNITY_MODULES_PHYSICS
using System;
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
    /// <summary>
    /// Authored after the <see cref="NetworkRigidbody"/> on the hybrid prefab, which makes this the component
    /// that gets renumbered when anything ahead of it leaves <see cref="NetworkObject.ChildNetworkBehaviours"/>.
    /// </summary>
    internal class HybridTrailingBehaviour : NetworkBehaviour
    {
        public NetworkVariable<int> SynchronizedValue = new NetworkVariable<int>();

        // Only ever written by the Rpc handlers below. An Rpc that resolved to a different component
        // never reaches them, so these double as the "arrived at the right component" assertion.
        public List<ulong> PingSenders = new List<ulong>();
        public int PongCount;

        [Rpc(SendTo.Server)]
        public void PingRpc(ulong senderClientId)
        {
            PingSenders.Add(senderClientId);
            PongRpc();
        }

        [Rpc(SendTo.Everyone)]
        public void PongRpc()
        {
            PongCount++;
        }
    }

    /// <summary>
    /// A hybrid prefab keeps its <see cref="NetworkTransform"/> and <see cref="NetworkRigidbodyBase"/> components
    /// and gates their initialization rather than destroying them at runtime.
    /// </summary>
    /// <remarks>
    /// Destroying them dropped an entry out of <see cref="NetworkObject.ChildNetworkBehaviours"/> while the
    /// component itself lived until the end of the frame, so the next rebuild renumbered every behaviour that
    /// followed. Rpcs address a behaviour by <see cref="NetworkBehaviour.NetworkBehaviourId"/> and NetworkVariable
    /// synchronization is positional within that same collection, so a one entry disagreement misroutes an Rpc
    /// and corrupts the whole synchronization payload for the object.
    /// Hybrid prefab spawning is client-server only, so there is no distributed authority case here.
    /// </remarks>
    [TestFixture(HostOrServer.UnifiedHost)]
    internal class UnifiedHybridPrefabBehaviourIdTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private const int k_SynchronizedValue = 0x5AF3;

        // The authored component order, captured from the prefab. Every peer has to reproduce it, and the index
        // of each entry is the NetworkBehaviourId that peer is expected to have assigned. Read from the prefab
        // rather than written out here because the integration test helpers author components of their own.
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
            m_Prefab.AddComponent<NetworkTransform>();
            // NetworkRigidbody requires the Rigidbody. Adding it explicitly keeps the authored order visible
            // instead of leaving it to RequireComponent.
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

        /// <summary>
        /// The fixture only tests anything if the trailing behaviour really is authored after the components
        /// that used to be destroyed at runtime.
        /// </summary>
        private void AssertAuthoredOrder()
        {
            var transformIndex = Array.IndexOf(m_AuthoredBehaviourOrder, typeof(NetworkTransform));
            var rigidbodyIndex = Array.IndexOf(m_AuthoredBehaviourOrder, typeof(NetworkRigidbody));
            var trailingIndex = Array.IndexOf(m_AuthoredBehaviourOrder, typeof(HybridTrailingBehaviour));

            Assert.Greater(rigidbodyIndex, transformIndex, $"The {nameof(NetworkRigidbody)} is not authored after the {nameof(NetworkTransform)}!");
            Assert.Greater(trailingIndex, rigidbodyIndex, $"The {nameof(HybridTrailingBehaviour)} is not authored after the {nameof(NetworkRigidbody)}!");
        }

        private HybridTrailingBehaviour GetTrailingBehaviour(NetworkManager networkManager)
        {
            return networkManager.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<HybridTrailingBehaviour>();
        }

        private IEnumerator SpawnHybridInstance()
        {
            AssertAuthoredOrder();
            m_Instance = SpawnObject(m_Prefab, m_ServerNetworkManager).GetComponent<NetworkObject>();

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
                if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(m_Instance.NetworkObjectId, out var instance))
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has not spawned the instance!");
                    continue;
                }

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
        /// The gated components are still present and still inert.
        /// </summary>
        private void ValidateGatedComponents(NetworkManager networkManager, NetworkObject instance, StringBuilder errorLog)
        {
            if (instance.GetComponent<NetworkRigidbodyBase>() == null)
            {
                errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] The {nameof(NetworkRigidbody)} was removed from the instance!");
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

        /// <summary>
        /// Every client pings the authority on the trailing behaviour and the authority answers all of them.
        /// </summary>
        private bool ValidateRpcRoundTrip(StringBuilder errorLog)
        {
            var expectedPongs = m_ClientNetworkManagers.Length;
            var authorityBehaviour = GetTrailingBehaviour(GetAuthorityNetworkManager());
            if (authorityBehaviour.PingSenders.Count != m_ClientNetworkManagers.Length)
            {
                errorLog.AppendLine($"[Authority] Received {authorityBehaviour.PingSenders.Count} pings but expected {m_ClientNetworkManagers.Length}!");
            }

            foreach (var client in m_ClientNetworkManagers)
            {
                if (!authorityBehaviour.PingSenders.Contains(client.LocalClientId))
                {
                    errorLog.AppendLine($"[Authority] Received no ping from Client-{client.LocalClientId}!");
                }
            }

            foreach (var networkManager in m_NetworkManagers)
            {
                var pongCount = GetTrailingBehaviour(networkManager).PongCount;
                if (pongCount != expectedPongs)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Received {pongCount} pongs but expected {expectedPongs}!");
                }
            }

            return errorLog.Length == 0;
        }

        private IEnumerator PingFromEveryClient()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                GetTrailingBehaviour(client).PingRpc(client.LocalClientId);
            }

            yield return WaitForConditionOrTimeOut(ValidateRpcRoundTrip);
            AssertOnTimeout($"An Rpc on the {nameof(HybridTrailingBehaviour)} did not complete its round trip!");
        }

        private bool ValidateSynchronizedValue(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(m_Instance.NetworkObjectId, out var instance))
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has not spawned the instance!");
                    continue;
                }

                var value = instance.GetComponent<HybridTrailingBehaviour>().SynchronizedValue.Value;
                if (value != k_SynchronizedValue)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] {nameof(HybridTrailingBehaviour.SynchronizedValue)} is " +
                        $"{value} but {k_SynchronizedValue} was expected!");
                }
            }

            return errorLog.Length == 0;
        }

        [UnityTest]
        public IEnumerator BehaviourIdsMatchOnAllPeers()
        {
            yield return SpawnHybridInstance();

            yield return WaitForConditionOrTimeOut(ValidateBehaviourTable);
            AssertOnTimeout("A peer disagreed about the hybrid prefab's behaviour table!");

            yield return PingFromEveryClient();
        }

        /// <summary>
        /// <see cref="NetworkObject.InitializeChildNetworkBehaviours"/> rebuilds the table from the components
        /// that are on the instance at that moment, and it is reachable after spawn through the public
        /// <see cref="NetworkObject.GetNetworkBehaviourOrderIndex"/> and
        /// <see cref="NetworkObject.GetNetworkBehaviourAtOrderIndex"/>. A rebuild on one peer must therefore
        /// produce the same ids that peer already handed out, or it stops agreeing with everyone else.
        /// </summary>
        [UnityTest]
        public IEnumerator BehaviourIdsSurviveARebuild()
        {
            yield return SpawnHybridInstance();

            yield return WaitForConditionOrTimeOut(ValidateBehaviourTable);
            AssertOnTimeout("A peer disagreed about the hybrid prefab's behaviour table!");

            foreach (var networkManager in m_NetworkManagers)
            {
                networkManager.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].InitializeChildNetworkBehaviours();
            }

            yield return WaitForConditionOrTimeOut(ValidateBehaviourTable);
            AssertOnTimeout("Rebuilding the behaviour table after spawn moved the behaviour ids!");

            // The ids are only worth anything if they still route a message, so exercise the wire too.
            yield return PingFromEveryClient();
        }

        /// <summary>
        /// NetworkVariable synchronization walks <see cref="NetworkObject.ChildNetworkBehaviours"/> positionally
        /// with nothing on the wire to identify a behaviour, so a late joiner reading a table the authority does
        /// not share desynchronizes every value after the first disagreement.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkVariableSynchronizesToALateJoiner()
        {
            yield return SpawnHybridInstance();

            GetTrailingBehaviour(GetAuthorityNetworkManager()).SynchronizedValue.Value = k_SynchronizedValue;

            yield return WaitForConditionOrTimeOut(ValidateSynchronizedValue);
            AssertOnTimeout($"A peer did not receive the {nameof(HybridTrailingBehaviour.SynchronizedValue)} update!");

            yield return CreateAndStartNewClient();

            yield return WaitForConditionOrTimeOut(ValidateSynchronizedValue);
            AssertOnTimeout($"The late joining client did not synchronize the {nameof(HybridTrailingBehaviour.SynchronizedValue)}!");

            yield return WaitForConditionOrTimeOut(ValidateBehaviourTable);
            AssertOnTimeout("The late joining client disagreed about the hybrid prefab's behaviour table!");
        }
    }
}
#endif
