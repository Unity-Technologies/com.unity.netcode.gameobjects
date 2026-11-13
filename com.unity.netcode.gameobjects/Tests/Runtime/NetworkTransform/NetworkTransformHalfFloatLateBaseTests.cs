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
    /// the non-authority's base position.
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkTransformHalfFloatLateBaseTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 1;

        private const float k_DeltaStep = 0.5f;

        private GameObject m_TestPrefab;

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

        private static void Receive(NetworkTransform nonAuthority, NetworkTransform.NetworkTransformState state)
        {
            nonAuthority.InboundState = state;
            nonAuthority.TransformStateUpdate();
        }

        [UnityTest]
        public IEnumerator LateBaseSyncStillUpdatesBase()
        {
            var authority = GetAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_TestPrefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"Timed out waiting for {authorityInstance.name} to spawn on all clients!");

            var nonAuthority = GetNonAuthorityNetworkManager().SpawnManager.SpawnedObjects[authorityInstance.NetworkObjectId].GetComponent<NetworkTransform>();

            // Encode the way the authority does: crossing MaxDeltaBeforeAdjustment collapses the delta into a new base.
            var networkTick = authority.ServerTime.Tick + 10;
            var authorityDeltaPosition = new NetworkDeltaPosition(Vector3.zero, networkTick);
            var position = new Vector3(NetworkDeltaPosition.MaxDeltaBeforeAdjustment + k_DeltaStep, 0.0f, 0.0f);
            authorityDeltaPosition.UpdateFrom(ref position, ++networkTick);
            Assert.IsTrue(authorityDeltaPosition.CollapsedDeltaIntoBase, "The first update did not collapse into a new base!");
            var baseSync = CreateState(authorityDeltaPosition, networkTick);

            position.x += k_DeltaStep;
            authorityDeltaPosition.UpdateFrom(ref position, ++networkTick);
            var firstDelta = CreateState(authorityDeltaPosition, networkTick);

            position.x += k_DeltaStep;
            authorityDeltaPosition.UpdateFrom(ref position, ++networkTick);
            var secondDelta = CreateState(authorityDeltaPosition, networkTick);

            // The unreliable delta overtakes the reliable base sync it depends on.
            Receive(nonAuthority, firstDelta);
            Receive(nonAuthority, baseSync);
            Receive(nonAuthority, secondDelta);

            Assert.IsTrue(Approximately(nonAuthority.transform.position, position),
                $"{nonAuthority.NetworkManager.name} is at {nonAuthority.transform.position} but the authority is at {position}. " +
                $"The late base sync was not applied, so the delta was decoded against the previous base.");
        }
    }
}
