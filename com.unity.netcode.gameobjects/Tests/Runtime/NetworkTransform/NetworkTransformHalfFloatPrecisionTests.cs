using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
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
    /// Both tests move the authority in one direction only and require non-authority instances to follow without
    /// ever moving backwards. Interpolation cannot overshoot, so any movement opposite to the authority's has to
    /// have come from how the position was encoded rather than from the authority.
    /// <br /><br />
    /// These do not use the time travel harness because the behavior only appears over multiple real state update
    /// and interpolation cycles.
    /// </remarks>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkTransformHalfFloatPrecisionTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 1;

        /// <summary>
        /// How far the object travels before the position is checked.
        /// </summary>
        /// <remarks>
        /// Half float resolution gets coarser the further the object is from the base position established when it
        /// spawned, so the object has to travel away from that base for the resolution to be worth testing.
        /// </remarks>
        private const float k_TravelDistance = 30.0f;

        private const float k_TravelStep = 1.5f;

        // Moves the object off a position that a half float can represent exactly, which is a position that leaves
        // no rounding loss behind and so cannot show the problem being tested for.
        private const float k_UnrepresentableOffset = 0.0007f;

        // Small enough per update that the encoding cannot represent the change on its own.
        private const float k_CreepStep = 0.0005f;

        private const int k_CreepTicks = 60;

        // Tolerated backwards movement, which is float noise only. Well below the roughly 1mm resolution.
        private const float k_MonotonicEpsilon = 1e-5f;

        private GameObject m_TestPrefab;
        private NetworkManager m_AuthorityNetworkManager;
        private NetworkTransform m_AuthorityInstance;

        private readonly Dictionary<NetworkTransform, float> m_WorstRegression = new Dictionary<NetworkTransform, float>();
        private readonly Dictionary<NetworkTransform, float> m_LastObserved = new Dictionary<NetworkTransform, float>();

        private int m_TicksApplied;
        private float m_StepThisPhase;

        public NetworkTransformHalfFloatPrecisionTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        // TODO: [CmbServiceTests] Validate this against the service once half float precision is covered there.
        protected override bool UseCMBService()
        {
            return false;
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

        private bool AllInstancesCaughtUp()
        {
            var authority = GetAuthorityNetworkManager();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager == authority)
                {
                    continue;
                }
                var nonAuthority = networkManager.SpawnManager.SpawnedObjects[m_AuthorityInstance.NetworkObjectId];
                if (!Approximately(nonAuthority.transform.position, m_AuthorityInstance.transform.position))
                {
                    return false;
                }
            }
            return true;
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
            var authority = GetAuthorityNetworkManager();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager == authority)
                {
                    continue;
                }
                var nonAuthority = networkManager.SpawnManager.SpawnedObjects[m_AuthorityInstance.NetworkObjectId].GetComponent<NetworkTransform>();
                var current = nonAuthority.transform.position.x;
                if (m_LastObserved.TryGetValue(nonAuthority, out var previous))
                {
                    var regression = previous - current;
                    if (regression > m_WorstRegression[nonAuthority])
                    {
                        m_WorstRegression[nonAuthority] = regression;
                    }
                }
                m_LastObserved[nonAuthority] = current;
            }
        }

        private void BeginSampling()
        {
            m_WorstRegression.Clear();
            m_LastObserved.Clear();
            var authority = GetAuthorityNetworkManager();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager == authority)
                {
                    continue;
                }
                var nonAuthority = networkManager.SpawnManager.SpawnedObjects[m_AuthorityInstance.NetworkObjectId].GetComponent<NetworkTransform>();
                m_WorstRegression.Add(nonAuthority, 0.0f);
                m_LastObserved.Add(nonAuthority, nonAuthority.transform.position.x);
            }
        }

        private void AssertNoRegression(string phase)
        {
            foreach (var entry in m_WorstRegression)
            {
                Assert.LessOrEqual(entry.Value, k_MonotonicEpsilon,
                    $"[{phase}] {entry.Key.NetworkManager.name} moved {entry.Value} backwards along X while the " +
                    $"authority only ever moved forwards. Interpolation cannot overshoot, so this motion was " +
                    $"introduced by the half float position encoding rather than reproduced from the authority.");
            }
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
        /// Moves an object away from its base position and then moves it forward in very small steps, requiring
        /// every non-authority instance to follow without ever moving backwards.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the test coroutine.</returns>
        [UnityTest]
        public IEnumerator HalfFloatPrecisionDoesNotInvertMotion()
        {
            m_AuthorityNetworkManager = GetAuthorityNetworkManager();
            m_AuthorityInstance = SpawnObject(m_TestPrefab, m_AuthorityNetworkManager).GetComponent<NetworkTransform>();

            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthorityInstance.gameObject);
            AssertOnTimeout($"Not all clients spawned {m_AuthorityInstance.name}!");

            var travelTicks = (int)(k_TravelDistance / k_TravelStep);
            yield return DriveAuthority(k_TravelStep, travelTicks);

            yield return WaitForConditionOrTimeOut(AllInstancesCaughtUp);
            AssertOnTimeout("Non-authority instances did not catch up to the authority after the travel phase.");

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
            for (var i = 0; i < 30; i++)
            {
                SampleForRegression();
                yield return null;
            }

            AssertNoRegression("creep");

            // Small movements still have to arrive rather than be discarded.
            yield return WaitForConditionOrTimeOut(AllInstancesCaughtUp);
            AssertOnTimeout($"Non-authority instances did not converge on the authority position " +
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
            m_AuthorityNetworkManager = GetAuthorityNetworkManager();
            m_AuthorityInstance = SpawnObject(m_TestPrefab, m_AuthorityNetworkManager).GetComponent<NetworkTransform>();

            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthorityInstance.gameObject);
            AssertOnTimeout($"Not all clients spawned {m_AuthorityInstance.name}!");

            var travelTicks = (int)(k_TravelDistance / k_TravelStep);
            yield return DriveAuthority(k_TravelStep, travelTicks);

            // A position that a half float happens to represent exactly leaves no rounding loss behind, and with
            // no rounding loss there is nothing that could move the object. Offsetting by less than the encoding
            // can represent guarantees there is some, which is the state a settling object is normally left in.
            yield return DriveAuthority(k_UnrepresentableOffset, 1);

            yield return WaitForConditionOrTimeOut(AllInstancesCaughtUp);
            AssertOnTimeout("Non-authority instances did not catch up to the authority after the travel phase.");

            // Nothing moves for the rest of the test, so the authority's last direction was forwards. Checking for
            // backwards movement rather than for drift from a starting point means the instances are still free to
            // finish interpolating towards the authority without that counting against them.
            BeginSampling();
            for (var i = 0; i < 120; i++)
            {
                SampleForRegression();
                yield return null;
            }

            AssertNoRegression("stationary");
        }
    }

    /// <summary>
    /// Branch coverage for <see cref="NetworkDeltaPosition"/>'s encoding math.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="NetworkTransformHalfFloatPrecisionTests"/> because none of this needs a
    /// session, and that fixture would run it twice over two topologies.
    /// <br /><br />
    /// A value that is exactly representable as a half float carries no rounding loss, so a test built on
    /// one cannot observe the behavior checked here and will pass against broken code. Keep the constants
    /// below off the lattice, and derive expected encodings with <see cref="math.half(float)"/> rather than
    /// writing them out as literals.
    /// </remarks>
    internal class NetworkDeltaPositionTests
    {
        private const int k_Tick = 100;

        // Lossy as a half float, and two of them still fit under the collapse threshold.
        private const float k_LossyStep = 0.7f;

        // Past the threshold and exactly representable, so the collapse cannot hinge on rounding.
        private const float k_CollapsingStep = NetworkDeltaPosition.MaxDeltaBeforeAdjustment + 0.5f;

        // Off the half float lattice on every axis, so each conversion leaves rounding loss behind.
        private static readonly Vector3 k_Base = new Vector3(30.0007f, -12.0003f, 5.0009f);

        private static Vector3 Offset(float amount)
        {
            return k_Base + new Vector3(amount, amount, amount);
        }

        // The transmitted form, so comparisons are against what actually goes on the wire.
        private static ushort[] Encoded(NetworkDeltaPosition deltaPosition)
        {
            return new[]
            {
                deltaPosition.HalfVector3.Axis.x.value,
                deltaPosition.HalfVector3.Axis.y.value,
                deltaPosition.HalfVector3.Axis.z.value,
            };
        }

        [Test]
        public void ConstructorOverloadsProduceTheSameInitialState()
        {
            var position = k_Base;
            var allAxes = math.bool3(true);

            var instances = new[]
            {
                new NetworkDeltaPosition(position, k_Tick),
                new NetworkDeltaPosition(position, k_Tick, allAxes),
                new NetworkDeltaPosition(position.x, position.y, position.z, k_Tick),
                new NetworkDeltaPosition(position.x, position.y, position.z, k_Tick, allAxes),
            };

            foreach (var instance in instances)
            {
                Assert.AreEqual(position, instance.GetCurrentBasePosition(), "The base position should be where the object started.");
                Assert.AreEqual(Vector3.zero, instance.GetDeltaPosition(), "Nothing has moved yet, so there is no delta.");
                Assert.AreEqual(Vector3.zero, instance.PrecisionLossDelta, "No conversion has lost anything yet.");
                Assert.AreEqual(k_Tick, instance.NetworkTick, "The construction tick should be recorded.");
                Assert.IsFalse(instance.CollapsedDeltaIntoBase, "A zero delta cannot have collapsed.");
                Assert.IsFalse(instance.SynchronizeBase, "The base is only synchronized explicitly.");
                Assert.AreEqual(allAxes, instance.HalfVector3.AxisToSynchronize, "All axes should be synchronized by default.");
            }
        }

        [Test]
        public void AccessorsReportTheUnderlyingState()
        {
            var deltaPosition = new NetworkDeltaPosition(k_Base, k_Tick);
            var moved = Offset(k_LossyStep);
            deltaPosition.UpdateFrom(ref moved, k_Tick + 1);

            Assert.AreEqual(deltaPosition.CurrentBasePosition, deltaPosition.GetCurrentBasePosition());
            Assert.AreEqual(deltaPosition.DeltaPosition, deltaPosition.GetDeltaPosition());
            Assert.AreEqual(deltaPosition.HalfDeltaConvertedBack, deltaPosition.GetConvertedDelta());
            Assert.AreEqual(deltaPosition.CurrentBasePosition + deltaPosition.DeltaPosition, deltaPosition.GetFullPosition());

            Assert.AreNotEqual(deltaPosition.GetDeltaPosition().x, deltaPosition.GetConvertedDelta().x,
                "The converted delta is the lossy one and should not match the full precision delta.");
        }

        [Test]
        public void MovingFoldsThePreviousRoundingLossBackIn()
        {
            var deltaPosition = new NetworkDeltaPosition(k_Base, k_Tick);

            var firstMove = Offset(k_LossyStep);
            deltaPosition.UpdateFrom(ref firstMove, k_Tick + 1);

            var carriedLoss = deltaPosition.PrecisionLossDelta;
            Assert.AreNotEqual(0.0f, carriedLoss.x, "A step off the lattice has to leave rounding loss behind.");

            var basePosition = deltaPosition.GetCurrentBasePosition();
            var secondMove = Offset(k_LossyStep * 2.0f);
            deltaPosition.UpdateFrom(ref secondMove, k_Tick + 2);

            Assert.IsFalse(deltaPosition.CollapsedDeltaIntoBase,
                "Both steps together have to stay under the collapse threshold, or the delta asserted on below is reset to zero.");

            // Folding the loss in is what keeps the average position accurate instead of drifting by a
            // fraction of a step per send.
            var rawDelta = secondMove.x - basePosition.x;
            Assert.AreEqual(rawDelta + carriedLoss.x, deltaPosition.GetDeltaPosition().x, 1e-7f,
                "The delta being sent should have the carried rounding loss added to it.");
            Assert.AreNotEqual(math.half(rawDelta).value, deltaPosition.HalfVector3.Axis.x.value,
                "Folding the loss in has to change the transmitted value, or it would have no effect.");
            Assert.AreNotEqual(carriedLoss.x, deltaPosition.PrecisionLossDelta.x,
                "The carried loss should be recomputed from the conversion that just happened.");
        }

        [Test]
        public void StandingStillDoesNotChangeWhatIsSent()
        {
            var deltaPosition = new NetworkDeltaPosition(k_Base, k_Tick);

            // Arrive off the lattice, which is where a settling object ends up.
            var arrived = Offset(k_LossyStep);
            deltaPosition.UpdateFrom(ref arrived, k_Tick + 1);

            var encodedOnArrival = Encoded(deltaPosition);
            var lossOnArrival = deltaPosition.PrecisionLossDelta;
            Assert.AreNotEqual(0.0f, lossOnArrival.x, "The arrival conversion has to leave rounding loss behind.");

            // Folding the loss back in while stationary is what made resting objects jitter.
            for (var tick = k_Tick + 2; tick <= k_Tick + 5; tick++)
            {
                deltaPosition.UpdateFrom(ref arrived, tick);

                Assert.AreEqual(encodedOnArrival, Encoded(deltaPosition),
                    $"The transmitted delta changed on tick {tick} while the position did not move.");
                Assert.AreEqual(lossOnArrival, deltaPosition.PrecisionLossDelta,
                    $"The carried loss should be untouched on tick {tick} so it still applies once movement resumes.");
            }
        }

        [Test]
        public void DeltaCollapsesIntoTheBaseAtTheThreshold()
        {
            var deltaPosition = new NetworkDeltaPosition(k_Base, k_Tick);
            var originalBase = deltaPosition.GetCurrentBasePosition();

            var moved = Offset(k_CollapsingStep);
            deltaPosition.UpdateFrom(ref moved, k_Tick + 1);

            Assert.IsTrue(deltaPosition.CollapsedDeltaIntoBase, "A delta at the threshold should have been folded into the base.");
            Assert.AreEqual(0.0f, deltaPosition.GetDeltaPosition().x, "The delta should be reset once it is folded in.");
            Assert.AreEqual(0.0f, deltaPosition.GetConvertedDelta().x, "The converted delta should be reset along with it.");
            Assert.AreNotEqual(originalBase.x, deltaPosition.GetCurrentBasePosition().x, "The base should have absorbed the delta.");
            Assert.AreEqual(moved.x, deltaPosition.GetFullPosition().x, 1e-3f,
                "Folding the delta into the base must not move the object it describes.");
        }

        [Test]
        public void ADeltaUnderTheThresholdIsLeftAsADelta()
        {
            var deltaPosition = new NetworkDeltaPosition(k_Base, k_Tick);
            var originalBase = deltaPosition.GetCurrentBasePosition();

            var moved = Offset(k_LossyStep);
            deltaPosition.UpdateFrom(ref moved, k_Tick + 1);

            Assert.IsFalse(deltaPosition.CollapsedDeltaIntoBase, "A delta under the threshold should stay a delta.");
            Assert.AreEqual(originalBase, deltaPosition.GetCurrentBasePosition(), "The base should not move while the delta is small.");
            Assert.AreNotEqual(0.0f, deltaPosition.GetDeltaPosition().x, "The delta should hold the movement.");
        }

        [Test]
        public void UnsynchronizedAxesAreLeftUntouched()
        {
            var deltaPosition = new NetworkDeltaPosition(k_Base, k_Tick, math.bool3(true, false, false));

            var moved = Offset(k_LossyStep);
            deltaPosition.UpdateFrom(ref moved, k_Tick + 1);

            Assert.AreNotEqual(0.0f, deltaPosition.GetDeltaPosition().x, "The synchronized axis should track the movement.");
            Assert.AreEqual(0.0f, deltaPosition.GetDeltaPosition().y, "An unsynchronized axis should not produce a delta.");
            Assert.AreEqual(0.0f, deltaPosition.GetDeltaPosition().z, "An unsynchronized axis should not produce a delta.");

            // A stale reference here would break the comparison if the axis is synchronized later.
            Assert.AreEqual(moved.x, deltaPosition.PreviousPosition.x, "The synchronized axis should record where it was sent from.");
            Assert.AreEqual(k_Base.y, deltaPosition.PreviousPosition.y, "An unsynchronized axis should keep its original reference.");
            Assert.AreEqual(k_Base.z, deltaPosition.PreviousPosition.z, "An unsynchronized axis should keep its original reference.");
        }

        [Test]
        public void DecodingOnTheSameTickDoesNotReadTheEncodedAxes()
        {
            var deltaPosition = new NetworkDeltaPosition(k_Base, k_Tick);
            var moved = Offset(k_LossyStep);
            deltaPosition.UpdateFrom(ref moved, k_Tick + 1);

            var expected = deltaPosition.GetFullPosition();

            // Overwriting the encoded axes proves this path returns the already-decoded value rather than
            // decoding again, which would apply the same delta twice.
            deltaPosition.HalfVector3.Axis = math.half3(new float3(1.9f, 1.9f, 1.9f));

            Assert.AreEqual(expected, deltaPosition.ToVector3(k_Tick + 1),
                "Decoding the tick that was just written should return the position already held.");
        }

        [Test]
        public void DecodingANewTickAppliesTheDelta()
        {
            var authority = new NetworkDeltaPosition(k_Base, k_Tick);
            var moved = Offset(k_LossyStep);
            authority.UpdateFrom(ref moved, k_Tick + 1);

            var receiver = new NetworkDeltaPosition(k_Base, k_Tick)
            {
                HalfVector3 = authority.HalfVector3,
            };

            var decoded = receiver.ToVector3(k_Tick + 1);

            Assert.AreEqual(authority.GetConvertedDelta().x, receiver.GetDeltaPosition().x,
                "The receiver should decode the same delta the authority encoded.");
            Assert.AreEqual(k_Base.x + authority.GetConvertedDelta().x, decoded.x, 1e-4f,
                "The decoded position should be the base plus the transmitted delta.");
        }

        [Test]
        public void DecodingCollapsesIntoTheBaseAtTheThreshold()
        {
            var authority = new NetworkDeltaPosition(k_Base, k_Tick);
            var moved = Offset(k_CollapsingStep);
            authority.UpdateFrom(ref moved, k_Tick + 1);

            // The send side folds the delta into its own base but leaves the encoded axes holding it, so the
            // receiving side has to perform the same fold to end up on the same base.
            var receiver = new NetworkDeltaPosition(k_Base, k_Tick)
            {
                HalfVector3 = authority.HalfVector3,
            };

            var decoded = receiver.ToVector3(k_Tick + 1);

            Assert.AreEqual(0.0f, receiver.GetDeltaPosition().x, "The delta should be reset once it is folded into the base.");
            Assert.AreEqual(0, receiver.HalfVector3.Axis.x.value, "The encoded axis should be cleared along with it.");
            Assert.AreEqual(authority.GetCurrentBasePosition().x, receiver.GetCurrentBasePosition().x, 1e-4f,
                "Both sides must end up on the same base position or they will disagree from here on.");
            Assert.AreEqual(moved.x, decoded.x, 1e-3f, "Folding the delta into the base must not move the object.");
        }

        [Test]
        public void DecodingIgnoresUnsynchronizedAxes()
        {
            var axesToSynchronize = math.bool3(true, false, false);
            var authority = new NetworkDeltaPosition(k_Base, k_Tick, axesToSynchronize);
            var moved = Offset(k_LossyStep);
            authority.UpdateFrom(ref moved, k_Tick + 1);

            var receiver = new NetworkDeltaPosition(k_Base, k_Tick, axesToSynchronize)
            {
                HalfVector3 = authority.HalfVector3,
            };

            var decoded = receiver.ToVector3(k_Tick + 1);

            Assert.AreNotEqual(k_Base.x, decoded.x, "The synchronized axis should have moved.");
            Assert.AreEqual(k_Base.y, decoded.y, "An unsynchronized axis should stay at the base value.");
            Assert.AreEqual(k_Base.z, decoded.z, "An unsynchronized axis should stay at the base value.");
        }

        [Test]
        public void HalfDeltaRoundTripsWhenTheBaseIsNotSynchronized()
        {
            var source = new NetworkDeltaPosition(k_Base, k_Tick);
            var moved = Offset(k_LossyStep);
            source.UpdateFrom(ref moved, k_Tick + 1);

            var result = RoundTrip(source, synchronizeBase: false);

            Assert.AreEqual(Encoded(source), Encoded(result), "The encoded axes should survive the round trip.");

            // Only the half float axes go on the wire here, so the receiver keeps whatever base it had.
            Assert.AreEqual(Vector3.zero, result.GetCurrentBasePosition(), "The base should not be transmitted in this mode.");
        }

        [Test]
        public void FullPrecisionRoundTripsWhenTheBaseIsSynchronized()
        {
            var source = new NetworkDeltaPosition(k_Base, k_Tick);
            var moved = Offset(k_LossyStep);
            source.UpdateFrom(ref moved, k_Tick + 1);

            var result = RoundTrip(source, synchronizeBase: true);

            // Synchronizing sends both values at full precision, so this path has to be lossless.
            Assert.AreEqual(source.GetDeltaPosition(), result.GetDeltaPosition(), "The delta should round trip exactly.");
            Assert.AreEqual(source.GetCurrentBasePosition(), result.GetCurrentBasePosition(), "The base should round trip exactly.");
        }

        [Test]
        public void QuantumIsTheSmallestChangeTheEncodingCanSee()
        {
            // Exactly representable, so "one step away" is unambiguous.
            foreach (var value in new[] { 0.5f, 1.0f, -1.0f, 2.0f, 1024.0f })
            {
                var quantum = NetworkDeltaPosition.HalfPrecisionQuantum(value);
                Assert.Greater(quantum, 0.0f, $"The step size at {value} should be positive.");

                Assert.AreNotEqual(math.half(value).value, math.half(value + quantum).value,
                    $"A full step from {value} should encode differently, or it is not the step size.");
                Assert.AreEqual(math.half(value).value, math.half(value + (quantum * 0.25f)).value,
                    $"A quarter step from {value} should encode identically, or the step size is too large.");
            }
        }

        [Test]
        public void QuantumDropsTheSignBecauseTheLatticeIsSymmetric()
        {
            foreach (var value in new[] { 0.5f, 1.0f, 300.0f, 1024.0f })
            {
                Assert.AreEqual(NetworkDeltaPosition.HalfPrecisionQuantum(value),
                    NetworkDeltaPosition.HalfPrecisionQuantum(-value),
                    $"The step size at {value} and {-value} should be the same.");
            }
        }

        [TestCase(65504.0f, TestName = "QuantumIsGuarded_AtLargestFiniteHalf")]
        [TestCase(-65504.0f, TestName = "QuantumIsGuarded_AtNegativeLargestFiniteHalf")]
        [TestCase(70000.0f, TestName = "QuantumIsGuarded_WhenRoundingToInfinity")]
        [TestCase(float.PositiveInfinity, TestName = "QuantumIsGuarded_AtPositiveInfinity")]
        [TestCase(float.NegativeInfinity, TestName = "QuantumIsGuarded_AtNegativeInfinity")]
        [TestCase(float.NaN, TestName = "QuantumIsGuarded_AtNaN")]
        public void QuantumIsGuardedAtTheTopOfTheRange(float value)
        {
            Assert.AreEqual(NetworkDeltaPosition.MaxDeltaBeforeAdjustment,
                NetworkDeltaPosition.HalfPrecisionQuantum(value),
                $"{value} is at or past the largest finite half float and should fall back to the maximum delta.");
        }

        [Test]
        public void QuantumIsNeverNonFiniteOrZero()
        {
            // Why the guard exists: an infinite step size would make the "has it moved?" comparison in
            // UpdateFrom false for every input, silently stopping the rounding loss from being applied.
            var unguarded = Mathf.HalfToFloat(0x7BFF + 1) - Mathf.HalfToFloat(0x7BFF);
            Assert.IsTrue(float.IsInfinity(unguarded) || float.IsNaN(unguarded),
                "The unguarded computation at the top of the range should be non-finite, which is why the guard exists.");

            var values = new[]
            {
                0.0f, float.Epsilon, 1e-7f, 0.5f, 1.0f, 100.0f, 65503.0f, 65504.0f, -65504.0f, 70000.0f,
                float.PositiveInfinity, float.NegativeInfinity, float.NaN,
            };

            foreach (var value in values)
            {
                var quantum = NetworkDeltaPosition.HalfPrecisionQuantum(value);
                Assert.IsFalse(float.IsNaN(quantum) || float.IsInfinity(quantum), $"The step size at {value} should be finite.");
                Assert.Greater(quantum, 0.0f, $"The step size at {value} should be positive.");
            }
        }

        private static NetworkDeltaPosition RoundTrip(NetworkDeltaPosition source, bool synchronizeBase)
        {
            source.SynchronizeBase = synchronizeBase;

            using var writer = new FastBufferWriter(256, Allocator.Temp);
            var writeSerializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
            source.NetworkSerialize(writeSerializer);

            // Starts from a different state, so a value that failed to arrive shows up as a mismatch.
            var result = new NetworkDeltaPosition(Vector3.zero, 0)
            {
                SynchronizeBase = synchronizeBase,
                HalfVector3 = { AxisToSynchronize = source.HalfVector3.AxisToSynchronize },
            };

            using var reader = new FastBufferReader(writer, Allocator.Temp);
            var readSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
            result.NetworkSerialize(readSerializer);

            return result;
        }
    }
}
