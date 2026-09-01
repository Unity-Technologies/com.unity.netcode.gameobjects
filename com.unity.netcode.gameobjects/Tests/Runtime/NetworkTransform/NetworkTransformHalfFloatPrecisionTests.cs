using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that <see cref="NetworkTransform.UseHalfFloatPrecision"/> does not introduce motion of its own.
    /// </summary>
    /// <remarks>
    /// Both tests move the authority in one direction only and require the non-authority instance to follow
    /// without ever moving backwards. Interpolation cannot overshoot, so any movement opposite to the direction
    /// the authority moved came from how the position was encoded.
    /// </remarks>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkTransformHalfFloatPrecisionTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 1;

        // Half float resolution gets coarser the further the object is from the base position established when it
        // spawned, so it has to travel away from that base for the resolution to be worth testing.
        private const float k_TravelDistance = 30.0f;

        private const float k_TravelStep = 1.5f;

        // Moves the object off a position that a half float can represent exactly, which leaves no rounding loss
        // behind and so cannot show the problem being tested for.
        private const float k_UnrepresentableOffset = 0.0007f;

        // Small enough per update that the encoding cannot represent the change on its own.
        private const float k_CreepStep = 0.0005f;

        private const int k_CreepTicks = 60;

        // Frames sampled after the creep, while the last states sent are still being interpolated.
        private const int k_SettleFrames = 30;

        // Frames sampled with nothing moving at all.
        private const int k_StationaryFrames = 120;

        // Tolerated backwards movement, which is float noise only. Well below the roughly 1mm resolution.
        private const float k_MonotonicEpsilon = 1e-5f;

        private GameObject m_TestPrefab;
        private NetworkManager m_AuthorityNetworkManager;
        private NetworkTransform m_AuthorityInstance;
        private NetworkTransform m_NonAuthorityInstance;

        private float m_WorstRegression;
        private float m_LastObserved;

        private int m_TicksApplied;
        private float m_StepThisPhase;

        public NetworkTransformHalfFloatPrecisionTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("HalfFloatObj");
            var networkTransform = m_TestPrefab.AddComponent<NetworkTransform>();

            networkTransform.UseHalfFloatPrecision = true;
            networkTransform.Interpolate = true;

            // Lerp smoothing would filter out the movement being tested for.
            networkTransform.PositionInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
            networkTransform.PositionLerpSmoothing = false;

            // No threshold, so the very small movements used below are actually sent.
            networkTransform.PositionThreshold = 0.0f;

            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleY = false;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;

            base.OnServerAndClientsCreated();
        }

        private bool NonAuthorityCaughtUp()
        {
            return Approximately(m_NonAuthorityInstance.transform.position, m_AuthorityInstance.transform.position);
        }

        /// <summary>
        /// Records any movement opposite to the direction the authority is moving.
        /// </summary>
        /// <remarks>
        /// Sampled once per frame rather than once per tick, since the position applied to the transform is what
        /// needs to be checked.
        /// </remarks>
        private void SampleForRegression()
        {
            var current = m_NonAuthorityInstance.transform.position.x;
            m_WorstRegression = Mathf.Max(m_WorstRegression, m_LastObserved - current);
            m_LastObserved = current;
        }

        private void BeginSampling()
        {
            m_WorstRegression = 0.0f;
            m_LastObserved = m_NonAuthorityInstance.transform.position.x;
        }

        private void AssertNoRegression(string phase)
        {
            Assert.LessOrEqual(m_WorstRegression, k_MonotonicEpsilon,
                $"[{phase}] {m_NonAuthorityInstance.NetworkManager.name} moved {m_WorstRegression} backwards along " +
                $"X while the authority only ever moved forwards. Interpolation cannot overshoot, so this motion " +
                $"was introduced by the half float position encoding rather than reproduced from the authority.");
        }

        /// <summary>
        /// Advances the authority one step per tick along +X.
        /// </summary>
        /// <remarks>
        /// Driven from the tick event so the position written is the one captured for that same tick.
        /// </remarks>
        private void OnNetworkTick()
        {
            m_TicksApplied++;
            var position = m_AuthorityInstance.transform.position;
            position.x += m_StepThisPhase;
            m_AuthorityInstance.transform.position = position;
        }

        private IEnumerator DriveAuthority(float stepPerTick, int ticks)
        {
            m_TicksApplied = 0;
            m_StepThisPhase = stepPerTick;
            m_AuthorityNetworkManager.NetworkTickSystem.Tick += OnNetworkTick;
            yield return WaitForConditionOrTimeOut(() => m_TicksApplied >= ticks);
            m_AuthorityNetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;
            AssertOnTimeout($"Timed out waiting for {ticks} authority updates (applied {m_TicksApplied}).");
        }

        /// <summary>
        /// Spawns the test object and travels it away from the base position established when it spawned.
        /// </summary>
        private IEnumerator SpawnAndTravel()
        {
            m_AuthorityNetworkManager = GetAuthorityNetworkManager();
            m_AuthorityInstance = SpawnObject(m_TestPrefab, m_AuthorityNetworkManager).GetComponent<NetworkTransform>();

            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthorityInstance.NetworkObject);
            AssertOnTimeout($"Not all clients spawned {m_AuthorityInstance.name}!");

            var nonAuthority = GetNonAuthorityNetworkManager();
            m_NonAuthorityInstance = nonAuthority.SpawnManager.SpawnedObjects[m_AuthorityInstance.NetworkObjectId].GetComponent<NetworkTransform>();

            yield return DriveAuthority(k_TravelStep, (int)(k_TravelDistance / k_TravelStep));
        }

        /// <summary>
        /// Moves an object away from its base position and then moves it forward in very small steps, requiring
        /// the non-authority instance to follow without ever moving backwards.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the test coroutine.</returns>
        [UnityTest]
        public IEnumerator HalfFloatPrecisionDoesNotInvertMotion()
        {
            yield return SpawnAndTravel();

            yield return WaitForConditionOrTimeOut(NonAuthorityCaughtUp);
            AssertOnTimeout("The non-authority instance did not catch up to the authority after the travel phase.");

            BeginSampling();
            m_TicksApplied = 0;
            m_StepThisPhase = k_CreepStep;
            m_AuthorityNetworkManager.NetworkTickSystem.Tick += OnNetworkTick;
            while (m_TicksApplied < k_CreepTicks)
            {
                SampleForRegression();
                yield return null;
            }
            m_AuthorityNetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;

            // Keep sampling while the last sent states are still being interpolated.
            for (var i = 0; i < k_SettleFrames; i++)
            {
                SampleForRegression();
                yield return null;
            }

            AssertNoRegression("creep");

            // Small movements still have to arrive rather than be discarded.
            yield return WaitForConditionOrTimeOut(NonAuthorityCaughtUp);
            AssertOnTimeout($"The non-authority instance did not converge on the authority position " +
                $"{m_AuthorityInstance.transform.position} after creeping, which means slow motion is being " +
                $"discarded rather than transmitted.");
        }

        /// <summary>
        /// Requires a stationary authority to produce a stationary non-authority.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the test coroutine.</returns>
        [UnityTest]
        public IEnumerator HalfFloatPrecisionHoldsStillWhenStationary()
        {
            yield return SpawnAndTravel();

            // A position that a half float happens to represent exactly leaves no rounding loss behind, and with
            // no rounding loss there is nothing that could move the object. Offsetting by less than the encoding
            // can represent guarantees there is some, which is the state a settling object is normally left in.
            yield return DriveAuthority(k_UnrepresentableOffset, 1);

            yield return WaitForConditionOrTimeOut(NonAuthorityCaughtUp);
            AssertOnTimeout("The non-authority instance did not catch up to the authority after the travel phase.");

            // Nothing moves for the rest of the test, so the last direction the authority moved was forwards.
            // Checking for backwards movement rather than for drift from a starting point means the instance is
            // still free to finish interpolating towards the authority without that counting against it.
            BeginSampling();
            for (var i = 0; i < k_StationaryFrames; i++)
            {
                SampleForRegression();
                yield return null;
            }

            AssertNoRegression("stationary");
        }
    }
}
