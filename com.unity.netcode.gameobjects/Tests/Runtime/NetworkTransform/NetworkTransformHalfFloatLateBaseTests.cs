using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that a half float base sync arriving after the unreliable deltas that depend on it still updates
    /// the non-authority's base position, unless a newer base has already been applied.
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkTransformHalfFloatLateBaseTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 1;

        private const float k_DeltaStep = 0.5f;

        private GameObject m_TestPrefab;
        private NetworkTransform m_NonAuthority;
        private int m_NetworkTick;

        public NetworkTransformHalfFloatLateBaseTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("HalfFloatLateBase");
            var networkTransform = m_TestPrefab.AddComponent<NetworkTransform>();
            networkTransform.UseHalfFloatPrecision = true;
            networkTransform.UseUnreliableDeltas = true;
            networkTransform.Interpolate = false;

            base.OnServerAndClientsCreated();
        }

        private IEnumerator SpawnTestObject()
        {
            var authority = GetAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_TestPrefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"Timed out waiting for {authorityInstance.name} to spawn on all clients!");

            m_NonAuthority = GetNonAuthorityNetworkManager().SpawnManager.SpawnedObjects[authorityInstance.NetworkObjectId].GetComponent<NetworkTransform>();
            m_NetworkTick = authority.ServerTime.Tick + 10;
        }

        private static NetworkTransform.NetworkTransformState CreateState(NetworkDeltaPosition networkDeltaPosition, int networkTick)
        {
            return new NetworkTransform.NetworkTransformState()
            {
                NetworkTick = networkTick,
                NetworkDeltaPosition = networkDeltaPosition,
                FlagStates =
                {
                    HasPositionX = true,
                    HasPositionChange = true,
                    UseHalfFloatPrecision = true,
                    UseUnreliableDeltas = true,
                    SynchronizeBaseHalfFloat = networkDeltaPosition.CollapsedDeltaIntoBase,
                    ReliableSequenced = networkDeltaPosition.CollapsedDeltaIntoBase,
                },
            };
        }

        /// <summary>
        /// Encodes the way the authority does: crossing MaxDeltaBeforeAdjustment collapses the delta into a new base.
        /// </summary>
        private NetworkTransform.NetworkTransformState CreateBaseSync(ref NetworkDeltaPosition networkDeltaPosition, ref Vector3 position)
        {
            position = new Vector3(NetworkDeltaPosition.MaxDeltaBeforeAdjustment + k_DeltaStep, 0.0f, 0.0f);
            networkDeltaPosition.UpdateFrom(ref position, ++m_NetworkTick);
            Assert.IsTrue(networkDeltaPosition.CollapsedDeltaIntoBase, "The update did not collapse into a new base!");
            return CreateState(networkDeltaPosition, m_NetworkTick);
        }

        private void Receive(NetworkTransform.NetworkTransformState state)
        {
            m_NonAuthority.InboundState = state;
            m_NonAuthority.TransformStateUpdate();
        }

        private void AssertNonAuthorityPosition(Vector3 position, string reason)
        {
            Assert.IsTrue(Approximately(m_NonAuthority.transform.position, position),
                $"{m_NonAuthority.NetworkManager.name} is at {m_NonAuthority.transform.position} but the authority is at {position}. {reason}");
        }

        [UnityTest]
        public IEnumerator LateBaseSyncStillUpdatesBase()
        {
            yield return SpawnTestObject();

            var authorityDeltaPosition = new NetworkDeltaPosition(Vector3.zero, m_NetworkTick);
            var position = Vector3.zero;
            var baseSync = CreateBaseSync(ref authorityDeltaPosition, ref position);

            position.x += k_DeltaStep;
            authorityDeltaPosition.UpdateFrom(ref position, ++m_NetworkTick);
            var firstDelta = CreateState(authorityDeltaPosition, m_NetworkTick);

            position.x += k_DeltaStep;
            authorityDeltaPosition.UpdateFrom(ref position, ++m_NetworkTick);
            var secondDelta = CreateState(authorityDeltaPosition, m_NetworkTick);

            // The unreliable delta overtakes the reliable base sync it depends on.
            Receive(firstDelta);
            Receive(baseSync);
            Receive(secondDelta);

            AssertNonAuthorityPosition(position, "The late base sync was not applied, so the delta was decoded against the previous base.");
        }

        [UnityTest]
        public IEnumerator BaseSyncOlderThanAppliedBaseIsIgnored()
        {
            yield return SpawnTestObject();

            // The former owner's base sync, delayed past an ownership change.
            var formerOwnerDeltaPosition = new NetworkDeltaPosition(Vector3.zero, m_NetworkTick);
            var position = Vector3.zero;
            var staleBaseSync = CreateBaseSync(ref formerOwnerDeltaPosition, ref position);

            // The new owner teleports to its current position when it gains ownership, which sets a new base.
            m_NetworkTick += 10;
            position = new Vector3(10.0f, 0.0f, 0.0f);
            var teleport = new NetworkTransform.NetworkTransformState()
            {
                NetworkTick = m_NetworkTick,
                CurrentPosition = position,
                FlagStates =
                {
                    HasPositionX = true,
                    HasPositionChange = true,
                    UseHalfFloatPrecision = true,
                    UseUnreliableDeltas = true,
                    IsTeleportingNextFrame = true,
                    ReliableSequenced = true,
                },
            };

            var newOwnerDeltaPosition = new NetworkDeltaPosition(position, m_NetworkTick);
            position.x += k_DeltaStep;
            newOwnerDeltaPosition.UpdateFrom(ref position, ++m_NetworkTick);
            var newOwnerDelta = CreateState(newOwnerDeltaPosition, m_NetworkTick);

            Receive(teleport);
            Receive(staleBaseSync);
            Receive(newOwnerDelta);

            AssertNonAuthorityPosition(position, "The former owner's base sync replaced the new owner's base.");
        }
    }
}
