using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Drives the same scenarios through both <see cref="TransformSyncModes"/> and asserts that every
    /// non-authority instance ends up where the authority is.
    /// </summary>
    /// <remarks>
    /// The two modes do not share a wire format, a send path, or an interpolator, so nothing structural keeps
    /// them equivalent. This is the regression net for that: it does not compare bytes (they legitimately
    /// differ) but compares the observable outcome, which is what has to stay the same.<br /><br />
    /// The scenarios were chosen from the places where the batched path diverges from the per instance one
    /// rather than from a general notion of coverage. Each one is documented with what it would catch.
    /// </remarks>
    // These tests do not need to run against the Rust server.
    [IgnoreIfServiceEnvironmentVariableSet]
    [TestFixture(TransformSyncModes.PerInstance, NetworkTransform.AuthorityModes.Server)]
    [TestFixture(TransformSyncModes.PerInstance, NetworkTransform.AuthorityModes.Owner)]
    [TestFixture(TransformSyncModes.Batched, NetworkTransform.AuthorityModes.Server)]
    [TestFixture(TransformSyncModes.Batched, NetworkTransform.AuthorityModes.Owner)]
    internal class NetworkTransformSyncModeParityTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 3;

        private readonly TransformSyncModes m_SyncMode;
        private readonly NetworkTransform.AuthorityModes m_AuthorityMode;

        private GameObject m_MoverPrefab;
        private GameObject m_HalfFloatMoverPrefab;
        private readonly List<NetworkObject> m_SpawnedMovers = new List<NetworkObject>();

        public NetworkTransformSyncModeParityTests(TransformSyncModes syncMode, NetworkTransform.AuthorityModes authorityMode)
        {
            m_SyncMode = syncMode;
            m_AuthorityMode = authorityMode;
        }

        internal override TransformSyncModes OnGetSyncMode()
        {
            return m_SyncMode;
        }

        /// <summary>
        /// Records whether any state update it received carried the teleport flag.
        /// </summary>
        /// <remarks>
        /// Convergence alone does not distinguish a teleport from a delta, because interpolation reaches the
        /// same place either way. The flag is the only observable difference, so the tests that care about a
        /// teleport assert on this rather than on where the object ended up.
        /// </remarks>
        internal class ParityMover : NetworkTransform
        {
            internal bool ReceivedTeleport;

            internal void ClearReceived()
            {
                ReceivedTeleport = false;
            }

            protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
            {
                ReceivedTeleport |= newState.IsTeleportingNextFrame;
                base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            m_MoverPrefab = CreateNetworkObjectPrefab("ParityMover");
            var networkTransform = m_MoverPrefab.AddComponent<ParityMover>();
            networkTransform.AuthorityMode = m_AuthorityMode;
            networkTransform.Interpolate = true;

            // Half float position is a separate prefab rather than a setting flipped after spawning, because
            // the delta position baseline is established during synchronization.
            m_HalfFloatMoverPrefab = CreateNetworkObjectPrefab("HalfFloatParityMover");
            var halfFloatTransform = m_HalfFloatMoverPrefab.AddComponent<ParityMover>();
            halfFloatTransform.AuthorityMode = m_AuthorityMode;
            halfFloatTransform.Interpolate = true;
            halfFloatTransform.UseHalfFloatPrecision = true;

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            m_SpawnedMovers.Clear();
            return base.OnTearDown();
        }

        /// <summary>
        /// Spawns an instance owned by the given client, or by the server when no owner is given.
        /// </summary>
        private NetworkObject SpawnMover(ulong ownerClientId = NetworkManager.ServerClientId, GameObject prefab = null)
        {
            var instance = Object.Instantiate(prefab ?? m_MoverPrefab);
            var networkObject = instance.GetComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_ServerNetworkManager;
            networkObject.SpawnWithOwnership(ownerClientId);
            m_SpawnedMovers.Add(networkObject);
            return networkObject;
        }

        /// <summary>
        /// The instance that is allowed to move the transform, which depends on the authority mode.
        /// </summary>
        private NetworkTransform GetMotionAuthorityInstance(NetworkObject serverSide)
        {
            if (m_AuthorityMode == NetworkTransform.AuthorityModes.Server || serverSide.OwnerClientId == NetworkManager.ServerClientId)
            {
                return serverSide.GetComponent<NetworkTransform>();
            }

            // Owner authoritative and owned by a client, so the owning client's clone drives it. It can be
            // absent while a spawn or an ownership change is still propagating.
            var owner = GetNetworkManagerByClientId(serverSide.OwnerClientId);
            if (owner == null || !owner.SpawnManager.SpawnedObjects.TryGetValue(serverSide.NetworkObjectId, out var clone))
            {
                return null;
            }
            return clone.GetComponent<NetworkTransform>();
        }

        /// <summary>
        /// Whether the instance that should be driving the transform has actually been told it has authority.
        /// </summary>
        /// <remarks>
        /// Ownership is applied to the server side <see cref="NetworkObject"/> synchronously, but the owning
        /// client only learns of it a round trip later. Moving the transform before then is a non-authority
        /// write: it is discarded by interpolation and nothing is ever sent, which looks exactly like a
        /// replication failure.
        /// </remarks>
        private bool MotionAuthorityIsEstablished(NetworkObject serverSide)
        {
            var authority = GetMotionAuthorityInstance(serverSide);
            return authority != null && authority.CanCommitToTransform;
        }

        private NetworkManager GetNetworkManagerByClientId(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                return m_ServerNetworkManager;
            }
            foreach (var client in m_ClientNetworkManagers)
            {
                if (client.LocalClientId == clientId)
                {
                    return client;
                }
            }
            Assert.Fail($"No {nameof(NetworkManager)} for client {clientId}!");
            return null;
        }

        /// <summary>
        /// Every manager that should be able to see the object agrees with the authority's transform.
        /// </summary>
        private bool AllObserversMatch(NetworkObject serverSide, Vector3 expectedPosition, IReadOnlyList<NetworkManager> expectedObservers)
        {
            foreach (var manager in expectedObservers)
            {
                if (!manager.SpawnManager.SpawnedObjects.TryGetValue(serverSide.NetworkObjectId, out var clone))
                {
                    return false;
                }
                if (!Approximately(clone.transform.position, expectedPosition))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Every instance, not just the server side one, reports the expected owner.
        /// </summary>
        private bool AllInstancesAgreeOnOwner(NetworkObject serverSide, ulong expectedOwner)
        {
            foreach (var manager in m_NetworkManagers)
            {
                if (!manager.SpawnManager.SpawnedObjects.TryGetValue(serverSide.NetworkObjectId, out var clone))
                {
                    return false;
                }
                if (clone.OwnerClientId != expectedOwner)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Moves the authority instance and waits for everyone to agree.
        /// </summary>
        private IEnumerator MoveAndConverge(NetworkObject serverSide, Vector3 target, IReadOnlyList<NetworkManager> observers)
        {
            yield return WaitForConditionOrTimeOut(() => MotionAuthorityIsEstablished(serverSide));
            AssertOnTimeout($"[{m_SyncMode}][{m_AuthorityMode}] The motion authority instance never gained authority!");

            var authority = GetMotionAuthorityInstance(serverSide);
            authority.transform.position = target;

            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(serverSide, target, observers));
            AssertOnTimeout($"[{m_SyncMode}][{m_AuthorityMode}] Not every observer reached {target}!\n{DescribeObservers(serverSide, target, observers)}");
        }

        /// <summary>
        /// Reports where each instance actually is, so a convergence failure identifies which instance was
        /// left behind rather than only that one was.
        /// </summary>
        private string DescribeObservers(NetworkObject serverSide, Vector3 expectedPosition, IReadOnlyList<NetworkManager> observers)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"  expected {expectedPosition}, owner is client {serverSide.OwnerClientId}");
            foreach (var manager in observers)
            {
                var role = manager.IsServer ? "server" : $"client-{manager.LocalClientId}";
                if (!manager.SpawnManager.SpawnedObjects.TryGetValue(serverSide.NetworkObjectId, out var clone))
                {
                    builder.AppendLine($"  {role}: object not spawned");
                    continue;
                }

                var networkTransform = clone.GetComponent<NetworkTransform>();
                var matches = Approximately(clone.transform.position, expectedPosition) ? "OK " : "BAD";
                // The two indices discriminate between "never registered for the batched interpolation" and
                // "registered but the results are not being applied".
                builder.AppendLine($"  {role}: {matches} pos={clone.transform.position} owner={clone.OwnerClientId} " +
                    $"canCommit={networkTransform.CanCommitToTransform} isOwner={clone.IsOwner} " +
                    $"stateIdx={networkTransform.StateManagerIndex} interpIdx={networkTransform.InterpolatorIndex}");
                if (networkTransform.InterpolatorIndex >= 0)
                {
                    builder.AppendLine($"      interp: {manager.TransformStateManager.DescribePositionInterpolator(networkTransform.InterpolatorIndex)}");
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// The baseline: a moving object reaches every observer in both modes.
        /// </summary>
        [UnityTest]
        public IEnumerator MotionReachesEveryObserver()
        {
            var mover = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(mover, mover.transform.position, m_NetworkManagers));
            AssertOnTimeout("Initial spawn did not reach every client!");

            yield return MoveAndConverge(mover, new Vector3(3.0f, 1.5f, -2.0f), m_NetworkManagers);
            yield return MoveAndConverge(mover, new Vector3(-6.25f, 4.0f, 8.5f), m_NetworkManagers);
        }

        /// <summary>
        /// Owner authoritative instances owned by a client.
        /// </summary>
        /// <remarks>
        /// Batched mode deliberately leaves these on the per instance path, because the batch is assembled per
        /// observing client and sent directly, which only the server can do. This is here because that
        /// exclusion is invisible at runtime: get it wrong and the transform simply stops replicating with no
        /// error, which is exactly what happened before the exclusion was added.
        /// </remarks>
        [UnityTest]
        public IEnumerator ClientOwnedInstanceStillReplicates()
        {
            var mover = SpawnMover(m_ClientNetworkManagers[0].LocalClientId);

            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(mover, mover.transform.position, m_NetworkManagers));
            AssertOnTimeout("Client owned instance did not spawn on every client!");

            yield return MoveAndConverge(mover, new Vector3(5.0f, 2.0f, 1.0f), m_NetworkManagers);
        }

        /// <summary>
        /// Two objects where one is hidden from a single client.
        /// </summary>
        /// <remarks>
        /// The batched message is assembled per client, so this is what proves the observer filtering: the
        /// hidden object's entry must be absent from that one client's batch while still reaching the others.
        /// A filtering mistake shows up either as the hidden object appearing, or as the whole batch failing
        /// to deserialize for that client and every object freezing.
        /// </remarks>
        [UnityTest]
        public IEnumerator MixedObserversReceiveOnlyWhatTheyObserve()
        {
            var visibleToAll = SpawnMover();
            var hiddenFromOne = SpawnMover();

            var hiddenFrom = m_ClientNetworkManagers[2];
            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(hiddenFromOne, hiddenFromOne.transform.position, m_NetworkManagers));
            AssertOnTimeout("Second object did not reach every client before being hidden!");

            hiddenFromOne.NetworkHide(hiddenFrom.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => !hiddenFrom.SpawnManager.SpawnedObjects.ContainsKey(hiddenFromOne.NetworkObjectId));
            AssertOnTimeout("Object was not hidden from the target client!");

            // Everyone still observing the hidden object has to keep receiving it.
            var stillObserving = new List<NetworkManager> { m_ServerNetworkManager, m_ClientNetworkManagers[0], m_ClientNetworkManagers[1] };
            yield return MoveAndConverge(hiddenFromOne, new Vector3(9.0f, 3.0f, -4.0f), stillObserving);

            // And the client it is hidden from has to keep receiving the object it can still see, which is
            // what breaks if a mis-sized batch desynchronizes that client's reader.
            yield return MoveAndConverge(visibleToAll, new Vector3(-2.0f, 6.0f, 7.0f), m_NetworkManagers);

            // Bringing it back has to resume delivery to the client it was hidden from.
            hiddenFromOne.NetworkShow(hiddenFrom.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => hiddenFrom.SpawnManager.SpawnedObjects.ContainsKey(hiddenFromOne.NetworkObjectId));
            AssertOnTimeout("Object was not shown again to the target client!");

            yield return MoveAndConverge(hiddenFromOne, new Vector3(1.0f, 1.0f, 1.0f), m_NetworkManagers);
        }

        /// <summary>
        /// Every non-authority instance that can see the object, other than the authority itself.
        /// </summary>
        private List<ParityMover> GetNonAuthorityMovers(NetworkObject serverSide)
        {
            var authority = GetMotionAuthorityInstance(serverSide);
            var movers = new List<ParityMover>();
            foreach (var manager in m_NetworkManagers)
            {
                if (!manager.SpawnManager.SpawnedObjects.TryGetValue(serverSide.NetworkObjectId, out var clone))
                {
                    continue;
                }
                var mover = clone.GetComponent<ParityMover>();
                if (mover != authority)
                {
                    movers.Add(mover);
                }
            }
            return movers;
        }

        /// <summary>
        /// A teleport has to arrive as a teleport rather than being interpolated towards.
        /// </summary>
        /// <remarks>
        /// Teleports take a different route through both the delta check and the interpolator reset paths,
        /// and the batched path captures the state before the teleport flag is cleared. Capturing it at the
        /// wrong point turns a teleport into an ordinary delta, which converges to the same place and is only
        /// visible as a long glide, so the flag is asserted rather than the destination.
        /// </remarks>
        [UnityTest]
        public IEnumerator TeleportArrivesAsATeleport()
        {
            var mover = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(mover, mover.transform.position, m_NetworkManagers));
            AssertOnTimeout("Initial spawn did not reach every client!");

            // The spawn synchronization is itself a teleport, so the recorders start from the state that
            // follows it rather than from the state at spawn.
            var observers = GetNonAuthorityMovers(mover);
            foreach (var observer in observers)
            {
                observer.ClearReceived();
            }

            var authority = GetMotionAuthorityInstance(mover);
            var target = new Vector3(120.0f, 45.0f, -85.0f);
            authority.SetState(target, null, null, false);

            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(mover, target, m_NetworkManagers));
            AssertOnTimeout($"Teleport to {target} did not reach every client!");

            foreach (var observer in observers)
            {
                Assert.IsTrue(observer.ReceivedTeleport,
                    $"[{m_SyncMode}][{m_AuthorityMode}] Client-{observer.NetworkManager.LocalClientId} reached {target} " +
                    "without ever receiving a state flagged as a teleport, so it glided there instead of snapping!");
            }
        }

        /// <summary>
        /// Re-enabling an axis that drifted while it was off has to arrive as a teleport.
        /// </summary>
        /// <remarks>
        /// While an axis is disabled the authority keeps moving but stops sending that axis, so the half
        /// float delta it would resume from is stale by more than the delta can represent. The check that
        /// catches this ran from the per instance tick only, which a batched instance never reaches, and it
        /// assigned rather than accumulated its per axis result, so an X trigger was discarded whenever Z was
        /// also re-enabled and in range. Both axes are moved and re-enabled here for that reason.
        /// </remarks>
        [UnityTest]
        public IEnumerator ReEnablingADriftedAxisTeleports()
        {
            var mover = SpawnMover(prefab: m_HalfFloatMoverPrefab);
            yield return WaitForConditionOrTimeOut(() => MotionAuthorityIsEstablished(mover));
            AssertOnTimeout("The motion authority instance never gained authority!");

            yield return MoveAndConverge(mover, new Vector3(1.0f, 1.0f, 1.0f), m_NetworkManagers);
            var authority = (ParityMover)GetMotionAuthorityInstance(mover);

            // X and Z stop being sent, and Y keeps moving so that position updates keep flowing. The axis
            // registration the delta position holds is only rewritten on a tick where the position is dirty,
            // so without the Y motion the authority would never record that X and Z went quiet.
            authority.SyncPositionX = false;
            authority.SyncPositionZ = false;
            for (int i = 0; i < 6; i++)
            {
                var position = authority.transform.position;
                authority.transform.position = new Vector3(position.x + 100.0f, position.y + 0.25f, position.z);
                yield return s_DefaultWaitForTick;
            }

            var observers = GetNonAuthorityMovers(mover);
            foreach (var observer in observers)
            {
                observer.ClearReceived();
            }

            // Z is re-enabled alongside X and has not moved, so its in-range result used to overwrite the
            // out-of-range one X produced.
            authority.SyncPositionX = true;
            authority.SyncPositionZ = true;

            var target = authority.transform.position;
            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(mover, target, m_NetworkManagers));
            AssertOnTimeout($"[{m_SyncMode}][{m_AuthorityMode}] Re-enabled axes never converged on {target}!\n{DescribeObservers(mover, target, m_NetworkManagers)}");

            foreach (var observer in observers)
            {
                Assert.IsTrue(observer.ReceivedTeleport,
                    $"[{m_SyncMode}][{m_AuthorityMode}] Client-{observer.NetworkManager.LocalClientId} resumed the drifted axis " +
                    "without a teleport, so it resumed from a half float delta that can no longer represent the distance!");
            }
        }

        /// <summary>
        /// Ownership moving between clients mid session.
        /// </summary>
        /// <remarks>
        /// A change of ownership re-runs initialization, which moves an instance between the delta tracking
        /// and interpolation registrations, and under owner authority it also moves it between the batched
        /// and per instance send paths. The handle has to survive that, since it is allocated once and is not
        /// reassigned on ownership change.
        /// </remarks>
        [UnityTest]
        public IEnumerator OwnershipChangeKeepsReplicating()
        {
            var mover = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(mover, mover.transform.position, m_NetworkManagers));
            AssertOnTimeout("Initial spawn did not reach every client!");

            yield return MoveAndConverge(mover, new Vector3(2.0f, 2.0f, 2.0f), m_NetworkManagers);

            mover.ChangeOwnership(m_ClientNetworkManagers[1].LocalClientId);
            // Waited for on every instance, not just the server side object. The server applies ownership
            // synchronously, so waiting on it would let the test proceed before the new owner knows it owns
            // anything.
            yield return WaitForConditionOrTimeOut(() => AllInstancesAgreeOnOwner(mover, m_ClientNetworkManagers[1].LocalClientId));
            AssertOnTimeout("Ownership did not transfer to every instance!");

            yield return MoveAndConverge(mover, new Vector3(-4.0f, 5.0f, 3.0f), m_NetworkManagers);

            // And back to the server, which under owner authority moves it from the per instance path onto
            // the batched one.
            mover.ChangeOwnership(NetworkManager.ServerClientId);
            yield return WaitForConditionOrTimeOut(() => AllInstancesAgreeOnOwner(mover, NetworkManager.ServerClientId));
            AssertOnTimeout("Ownership did not transfer back to every instance!");

            yield return MoveAndConverge(mover, new Vector3(7.0f, -1.0f, 0.5f), m_NetworkManagers);
        }

        /// <summary>
        /// Despawning and respawning, which is what exercises handle release and reuse.
        /// </summary>
        /// <remarks>
        /// Handles are held for several seconds before being reissued so a state update still in flight cannot
        /// land on whichever instance picks the handle up next. A respawn inside that window therefore has to
        /// receive a different handle; if it did not, the surviving object and the new one would fight over
        /// the same address and one would snap to the other's position.
        /// </remarks>
        [UnityTest]
        public IEnumerator DespawnAndRespawnDoNotShareAHandle()
        {
            var first = SpawnMover();
            var second = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(second, second.transform.position, m_NetworkManagers));
            AssertOnTimeout("Initial spawns did not reach every client!");

            yield return MoveAndConverge(first, new Vector3(10.0f, 0.0f, 0.0f), m_NetworkManagers);

            first.Despawn();
            yield return WaitForConditionOrTimeOut(() => !m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(first.NetworkObjectId));
            AssertOnTimeout("Despawn did not reach the clients!");

            // Respawn immediately, inside the window where the released handle is still being held.
            var third = SpawnMover();
            yield return WaitForConditionOrTimeOut(() => AllObserversMatch(third, third.transform.position, m_NetworkManagers));
            AssertOnTimeout("Respawned object did not reach every client!");

            // If the new object had inherited the despawned one's handle, moving it would drag the survivor
            // with it, so both are checked.
            var secondPosition = second.transform.position;
            yield return MoveAndConverge(third, new Vector3(-15.0f, 2.0f, 6.0f), m_NetworkManagers);

            Assert.IsTrue(AllObserversMatch(second, secondPosition, m_NetworkManagers),
                $"[{m_SyncMode}] Moving the respawned object also moved an unrelated one, which means they share a handle!");
        }
    }
}
