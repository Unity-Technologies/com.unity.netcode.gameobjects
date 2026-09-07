using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that the render time a non-authority instance interpolates towards is derived from the same
    /// clock that the state updates it is interpolating between are stamped on.
    /// </summary>
    /// <remarks>
    /// Measures how far behind ServerTime the state being interpolated towards was sent. The render time is
    /// ServerTime minus the tick latency and only states sent at or before it are eligible, so that measurement
    /// can never be less than the tick latency. Deriving the render time from LocalTime eats into that margin by
    /// however far the two clocks are apart, and can push the target past ServerTime entirely.
    /// </remarks>
    [TestFixture(NetworkTransform.InterpolationTypes.Lerp)]
    [TestFixture(NetworkTransform.InterpolationTypes.SmoothDampening)]
    internal class NetworkTransformInterpolationRenderTimeTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 1;

        // How far LocalTime is pushed ahead of ServerTime, in ticks. An in-process test has no round trip time
        // to separate the two clocks, and this is large enough to exceed NetworkTimeSystem's hard reset
        // threshold so the offset snaps instead of converging at its default adjustment ratio.
        private const int k_LocalBufferTicks = 12;

        // The separation the clocks must actually reach before any measurement is taken.
        private const double k_RequiredLeadTicks = 8.0d;

        // Ticks of authority motion after the clocks have separated, so the interpolator reaches steady state.
        private const int k_WarmUpTicks = 20;

        private const int k_SampledFrames = 90;

        // Far enough each tick that every tick produces a state update rather than being filtered out by the
        // position threshold.
        private const float k_DistancePerTick = 1.37f;

        private readonly NetworkTransform.InterpolationTypes m_InterpolationType;

        private GameObject m_TestPrefab;
        private NetworkManager m_AuthorityNetworkManager;
        private NetworkTransform m_AuthorityInstance;
        private Vector3 m_Direction;

        public NetworkTransformInterpolationRenderTimeTests(NetworkTransform.InterpolationTypes interpolationType)
        {
            m_InterpolationType = interpolationType;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("RenderTimeTestObj");
            var networkTransform = m_TestPrefab.AddComponent<NetworkTransform>();
            networkTransform.PositionInterpolationType = m_InterpolationType;
            base.OnServerAndClientsCreated();
        }

        private static double GetTickInterval(NetworkManager networkManager)
        {
            return 1.0d / networkManager.NetworkTickSystem.TickRate;
        }

        /// <summary>
        /// How far LocalTime currently leads ServerTime, expressed in ticks.
        /// </summary>
        private static double GetClockLeadInTicks(NetworkManager networkManager)
        {
            return (networkManager.LocalTime.Time - networkManager.ServerTime.Time) / GetTickInterval(networkManager);
        }

        /// <summary>
        /// Moves the authority instance once per tick so that a state update is generated every tick.
        /// </summary>
        private void OnNetworkTick()
        {
            m_AuthorityInstance.transform.position += m_Direction * k_DistancePerTick;
        }

        [UnityTest]
        public IEnumerator RenderTimeTrailsTheServerClock()
        {
            m_AuthorityNetworkManager = GetAuthorityNetworkManager();
            m_AuthorityInstance = SpawnObject(m_TestPrefab, m_AuthorityNetworkManager).GetComponent<NetworkTransform>();

            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthorityInstance.NetworkObject);
            AssertOnTimeout($"Not all clients spawned {m_AuthorityInstance.name}!");

            var nonAuthority = GetNonAuthorityNetworkManager();
            var nonAuthorityInstance = nonAuthority.SpawnManager.SpawnedObjects[m_AuthorityInstance.NetworkObject.NetworkObjectId].GetComponent<NetworkTransform>();

            // Separate the two clocks by a known amount so that which one the render time is derived from is
            // actually distinguishable.
            nonAuthority.NetworkTimeSystem.LocalBufferSec = k_LocalBufferTicks * GetTickInterval(nonAuthority);

            // Start continuous motion on the authority.
            m_Direction = GetRandomVector3(-10, 10).normalized;
            m_AuthorityNetworkManager.NetworkTickSystem.Tick += OnNetworkTick;

            // The offset only moves when the client next receives a time sync, so wait for the separation to
            // actually take hold rather than assuming it has.
            yield return WaitForConditionOrTimeOut(() => GetClockLeadInTicks(nonAuthority) >= k_RequiredLeadTicks);
            AssertOnTimeout($"The nonAuthority clock never fell {k_RequiredLeadTicks} ticks behind, so this test " +
                "cannot tell the two clocks apart and would pass regardless of which one is used.");

            // Let the interpolator settle at the new separation before measuring.
            yield return WaitForTicks(m_AuthorityNetworkManager, k_WarmUpTicks);

            // Sample how far behind ServerTime the state being interpolated towards was sent.
            var interpolator = nonAuthorityInstance.GetPositionInterpolator();
            var totalTargetLagTicks = 0.0d;
            var totalBuffered = 0;
            var samples = 0;
            for (int frame = 0; frame < k_SampledFrames; frame++)
            {
                if (interpolator.InterpolateState.Target.HasValue)
                {
                    var targetLag = nonAuthority.ServerTime.Time - interpolator.InterpolateState.Target.Value.TimeSent;
                    totalTargetLagTicks += targetLag / GetTickInterval(nonAuthority);
                    totalBuffered += interpolator.m_BufferQueue.Count;
                    samples++;
                }
                yield return null;
            }

            m_AuthorityNetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;

            Assert.Greater(samples, 0, $"{nonAuthorityInstance.name} never had a state to interpolate towards!");

            var meanTargetLagTicks = totalTargetLagTicks / samples;
            var meanBuffered = totalBuffered / (float)samples;
            var tickLatency = nonAuthority.NetworkTimeSystem.TickLatency;

            // Anything less than the tick latency means the render time came from a clock that leads the one
            // the states are stamped on.
            Assert.GreaterOrEqual(meanTargetLagTicks, tickLatency,
                $"[{m_InterpolationType}] {nonAuthorityInstance.name} was interpolating towards a state sent " +
                $"{meanTargetLagTicks:F3} ticks behind the server clock, but the render time is the server " +
                $"clock minus a tick latency of {tickLatency}, so it should never be less than that. " +
                $"(clock lead {GetClockLeadInTicks(nonAuthority):F3} ticks, mean buffered {meanBuffered:F3})");
        }
    }
}
