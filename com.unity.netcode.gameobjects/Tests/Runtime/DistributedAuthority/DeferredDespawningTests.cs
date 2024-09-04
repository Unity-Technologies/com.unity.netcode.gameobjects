using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;


namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    internal class DeferredDespawningTests : IntegrationTestWithApproximation
    {
        private const int k_DaisyChainedCount = 5;
        protected override int NumberOfClients => 2;
        private List<GameObject> m_DaisyChainedDespawnObjects = new List<GameObject>();
        private List<ulong> m_HasReachedEnd = new List<ulong>();
        private NetworkManager m_SessionOwner;

        public DeferredDespawningTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            var daisyChainPrevious = (DeferredDespawnDaisyChained)null;
            for (int i = 0; i < k_DaisyChainedCount; i++)
            {
                var daisyChainNode = CreateNetworkObjectPrefab($"Daisy-{i}");
                var daisyChainBehaviour = daisyChainNode.AddComponent<DeferredDespawnDaisyChained>();
                daisyChainBehaviour.IsRoot = i == 0;
                if (daisyChainPrevious != null)
                {
                    daisyChainPrevious.PrefabToSpawnWhenDespawned = daisyChainBehaviour.gameObject;
                }
                m_DaisyChainedDespawnObjects.Add(daisyChainNode);
                daisyChainNode.SetActive(false);

                daisyChainPrevious = daisyChainBehaviour;
            }

            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator DeferredDespawning()
        {
            DeferredDespawnDaisyChained.TestFailed.Clear();
            m_SessionOwner = GetSessionOwner();
            foreach (var daisyChaingedObject in m_DaisyChainedDespawnObjects)
            {
                daisyChaingedObject.SetActive(true);
            }
            var rootInstance = SpawnObject(m_DaisyChainedDespawnObjects[0], m_SessionOwner);
            DeferredDespawnDaisyChained.ReachedLastChainInstance = ReachedLastChainObject;
            var timeoutHelper = new TimeoutHelper(30);
            yield return WaitForConditionOrTimeOut(HaveAllClientsReachedEndOfChain, timeoutHelper);
            AssertOnTimeout($"Timed out waiting for all children to reach the end of their chained deferred despawns! {DeferredDespawnDaisyChained.TestFailed.ToString()}", timeoutHelper);
        }

        private bool HaveAllClientsReachedEndOfChain()
        {
            if (!m_HasReachedEnd.Contains(m_SessionOwner.LocalClientId))
            {
                return false;
            }

            foreach (var client in m_ClientNetworkManagers)
            {
                if (!m_HasReachedEnd.Contains(client.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }

        private void ReachedLastChainObject(ulong clientId)
        {
            m_HasReachedEnd.Add(clientId);
        }
    }

    /// <summary>
    /// This helper behaviour handles the majority of the validation for deferred despawning.
    /// Each instance triggers a series of deferred despawns where the owner validates the
    /// NetworkVariables are updated and spawns another prefab prior to despawning locally
    /// and the non-owners validate receiving the NetworkVariable change notification which
    /// contains a reference to a DeferredDespawnDaisyChained component on the newly spawned
    /// prefab driven by the authority. This repeats for the number specified in the integration
    /// test.
    /// </summary>
    internal class DeferredDespawnDaisyChained : NetworkBehaviour
    {
        public static bool EnableVerbose;
        public static Action<ulong> ReachedLastChainInstance;
        private const int k_StartingDeferTick = 4;
        public static Dictionary<ulong, Dictionary<ulong, DeferredDespawnDaisyChained>> ClientRelativeInstances = new Dictionary<ulong, Dictionary<ulong, DeferredDespawnDaisyChained>>();
        public bool IsRoot;
        public GameObject PrefabToSpawnWhenDespawned;
        public bool WasContactedByPeviousChainMember { get; private set; }
        public int DeferDespawnTick { get; private set; }

        private void PingInstance()
        {
            WasContactedByPeviousChainMember = true;
        }

        /// <summary>
        /// This hits two birds with one NetworkVariable:
        /// - Validates that NetworkVariables modified while the authority is in the middle of deferring a despawn are serialized and received by non-authority instances.
        /// - Validates that the non-authority instances receive the updates within the deferred tick period of time and can use them to handle other visual synchronization
        /// realted tasks (or the like).
        /// </summary>
        private NetworkVariable<NetworkBehaviourReference> m_ValidateDirtyNetworkVarUpdate = new NetworkVariable<NetworkBehaviourReference>();

        private DeferredDespawnDaisyChained m_NextNodeSpawned = null;

        public static StringBuilder TestFailed = new StringBuilder();

        private void FailTest(string msg)
        {
            TestFailed.AppendLine($"[{nameof(DeferredDespawnDaisyChained)}][Client-{NetworkManager.LocalClientId}] {msg}");
        }

        public override void OnNetworkSpawn()
        {
            var localId = NetworkManager.LocalClientId;
            if (!ClientRelativeInstances.ContainsKey(localId))
            {
                ClientRelativeInstances.Add(localId, new Dictionary<ulong, DeferredDespawnDaisyChained>());
            }

            if (ClientRelativeInstances[localId].ContainsKey(NetworkObject.NetworkObjectId))
            {
                FailTest($"[{nameof(OnNetworkSpawn)}] Client already has a table entry for NetworkObject-{NetworkObject.NetworkObjectId} | {name}!");
            }

            ClientRelativeInstances[localId].Add(NetworkObject.NetworkObjectId, this);

            if (!HasAuthority)
            {
                m_ValidateDirtyNetworkVarUpdate.OnValueChanged += OnValidateDirtyChanged;
                m_ValidateDirtyNetworkVarUpdate.Value.TryGet(out m_NextNodeSpawned, NetworkManager);
            }

            if (HasAuthority && IsRoot)
            {
                DeferDespawnTick = k_StartingDeferTick;
            }

            base.OnNetworkSpawn();
        }

        private void OnValidateDirtyChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            Log($"[OnValidateDirtyChanged] Getting NetworkBehaviour component....");
            if (!HasAuthority)
            {
                if (!current.TryGet(out m_NextNodeSpawned, NetworkManager))
                {
                    FailTest($"[{nameof(OnValidateDirtyChanged)}][{nameof(NetworkBehaviourReference)}] Failed to get the {nameof(DeferredDespawnDaisyChained)} behaviour from the {nameof(NetworkBehaviourReference)}!");
                }

                if (m_NextNodeSpawned.NetworkManager != NetworkManager)
                {
                    FailTest($"[{nameof(NetworkManager)}][{nameof(NetworkBehaviourReference.TryGet)}] The {nameof(NetworkManager)} of {nameof(m_NextNodeSpawned)} does not match the local relative {nameof(NetworkManager)} instance!");
                }

                if (m_NextNodeSpawned != null)
                {
                    Log($"[OnValidateDirtyChanged] m_NextNodeSpawned set -- {m_NextNodeSpawned.name}!");
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!HasAuthority && !NetworkManager.ShutdownInProgress)
            {
                Log($"[OnNetworkDespawn] despawning... ");
                if (PrefabToSpawnWhenDespawned != null)
                {
                    if (m_NextNodeSpawned == null)
                    {
                        Log($"[OnNetworkDespawn] No m_NextNodeSpawned set!!!");
                        if (!m_ValidateDirtyNetworkVarUpdate.Value.TryGet(out m_NextNodeSpawned, NetworkManager))
                        {
                            FailTest($"[{nameof(OnValidateDirtyChanged)}][{nameof(NetworkBehaviourReference)}] Failed to get the {nameof(DeferredDespawnDaisyChained)} behaviour from the {nameof(NetworkBehaviourReference)}!");
                            return;
                        }
                    }
                    Log($"[OnNetworkDespawn] Pinging {m_NextNodeSpawned.name}...");
                    m_NextNodeSpawned.PingInstance();
                }
                else
                {
                    ReachedLastChainInstance?.Invoke(NetworkManager.LocalClientId);
                }
            }
            base.OnNetworkDespawn();
        }

        private void InvokeDespawn()
        {
            Log($"Despawn with defer started! DeferDespawnTick = {DeferDespawnTick}");
            if (!HasAuthority)
            {
                FailTest($"[{nameof(InvokeDespawn)}] Client is not the authority but this was invoked (integration test logic issue)!");
            }
            NetworkObject.DeferDespawn(DeferDespawnTick);
            Log($"Should despawn on tick {DeferDespawnTick + NetworkManager.ServerTime.Tick}...");
        }

        public override void OnDeferringDespawn(int despawnTick)
        {
            if (!HasAuthority)
            {
                FailTest($"[{nameof(OnDeferringDespawn)}] Client is not the authority but this was invoked (integration test logic issue)!");
            }

            if (despawnTick != (DeferDespawnTick + NetworkManager.ServerTime.Tick))
            {
                FailTest($"[{nameof(OnDeferringDespawn)}] The passed in {despawnTick} parameter ({despawnTick}) does not equal the expected value of ({DeferDespawnTick + NetworkManager.ServerTime.Tick})!");
            }
            Log($"[OnDeferringDespawn] Deferring despawn...");
            if (PrefabToSpawnWhenDespawned != null)
            {
                var deferNetworkObject = PrefabToSpawnWhenDespawned.GetComponent<NetworkObject>().InstantiateAndSpawn(NetworkManager);
                var deferComponent = deferNetworkObject.GetComponent<DeferredDespawnDaisyChained>();
                // Slowly increment the despawn tick count as we process the chain of deferred despawns
                deferComponent.DeferDespawnTick = k_StartingDeferTick + 1;
                // This should get updated on all non-authority instances before they despawn
                m_ValidateDirtyNetworkVarUpdate.Value = new NetworkBehaviourReference(deferComponent);
            }
            else
            {
                ReachedLastChainInstance?.Invoke(NetworkManager.LocalClientId);
            }
            base.OnDeferringDespawn(despawnTick);
        }

        private bool m_DeferredDespawn;
        private bool m_InstantiatedObject;
        private float m_TickDelay;

        private DeferredDespawnDaisyChained m_DeferredComponent;
        private NetworkObject m_DeferredObject;
        private void Update()
        {
            if (!IsSpawned || !HasAuthority || m_DeferredDespawn)
            {
                return;
            }
            // Wait until all clients have this instance
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                if (!ClientRelativeInstances.ContainsKey(clientId))
                {
                    // exit early if the client doesn't exist yet
                    return;
                }

                if (!ClientRelativeInstances[clientId].ContainsKey(NetworkObjectId))
                {
                    // exit early if the client hasn't spawned a clone of this instance yet
                    return;
                }

                if (clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }

                // This should happen shortly afte the instances spawns (based on the deferred despawn count)
                if (!IsRoot && !ClientRelativeInstances[clientId][NetworkObjectId].WasContactedByPeviousChainMember)
                {
                    // exit early if the non-authority instance has not been contacted yet
                    return;
                }
            }
            InvokeDespawn();
            m_DeferredDespawn = true;
        }

        private void Log(string message)
        {
            if (!EnableVerbose)
            {
                return;
            }
            Debug.Log($"[{name}][Client-{NetworkManager.LocalClientId}][{NetworkObjectId}][{NetworkManager.ServerTime.Tick}][DD: {NetworkManager.ServerTime.Tick + DeferDespawnTick}] {message}");
        }
    }
}
