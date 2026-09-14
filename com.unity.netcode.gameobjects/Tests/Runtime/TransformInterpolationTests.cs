using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Drives a continuous world space motion on the authority and samples the resulting world
    /// space position on the non-authority.
    /// </summary>
    /// <remarks>
    /// The moving instance is parented to an object held at <see cref="TransformInterpolationTests.ParentPosition"/>,
    /// so the local and world space representations of the same point differ by ~1000 units. A
    /// non-authority that applies a local space value as a world space value (or the reverse) lands
    /// roughly that far from where it belongs, which is what the sampling below looks for.
    /// </remarks>
    internal class TransformInterpolationObject : NetworkTransform
    {
        // The motion is a sine wave, which keeps the authority's position continuous. A
        // discontinuous motion (i.e. a saw tooth) cannot be told apart from an interpolation fault,
        // since both show up on the non-authority as a large single frame delta.
        public const float Amplitude = 1.0f;
        public const float MotionPeriod = 2.0f;

        // How far outside of the motion's band the non-authority is allowed to sit. Budget:
        // - PositionThreshold, since the authority suppresses updates until it has moved that far.
        // - Interpolation lag, as the non-authority renders several ticks behind the authority and
        //   so trails it by up to (peak speed * that time), the peak speed of the motion above
        //   being Amplitude * 2*pi / MotionPeriod.
        // - A hitched frame under CI load, which adds another frame of trailing distance.
        // A world/local space fault puts the instance ~1000 units out, so this stays three orders
        // of magnitude below a real failure.
        public const float PositionTolerance = 0.25f;

        // Deliberately not a factor of MotionPeriod, so the transitions land on a different point
        // of the motion each time instead of repeatedly hitting the same phase.
        private const float k_ToggleInterval = 0.35f;

        public bool IsMoving;
        public bool IsFixed;
        public bool CheckPosition;

        public int LocalSpaceToggles;
        public float MinimumSampled;
        public float MaximumSampled;
        public float WorstDeviation;
        public int SamplesTaken;

        private bool m_IsToggling;
        private double m_MotionStartTime;
        private double m_NextToggleTime;

        public void StartMoving()
        {
            m_MotionStartTime = NetworkManager.LocalTime.Time;
            IsMoving = true;
        }

        /// <summary>
        /// Kept separate from <see cref="StartMoving"/> so that no transition happens while the
        /// non-authority is still converging on the motion.
        /// </summary>
        public void StartToggling()
        {
            m_NextToggleTime = NetworkManager.LocalTime.Time + k_ToggleInterval;
            m_IsToggling = true;
        }

        public void StartSampling()
        {
            MinimumSampled = float.MaxValue;
            MaximumSampled = float.MinValue;
            WorstDeviation = 0.0f;
            SamplesTaken = 0;
            CheckPosition = true;
        }

        /// <summary>
        /// Invoked by <see cref="NetworkManager"/> during <see cref="NetworkUpdateStage.PreLateUpdate"/>
        /// and only for non-authority instances, so the interpolated position for this frame has
        /// already been applied by the time the sampling below runs.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!CheckPosition)
            {
                return;
            }

            var positionY = transform.position.y;
            MinimumSampled = Mathf.Min(MinimumSampled, positionY);
            MaximumSampled = Mathf.Max(MaximumSampled, positionY);
            WorstDeviation = Mathf.Max(WorstDeviation, Mathf.Abs(positionY) - Amplitude);
            SamplesTaken++;
        }

        /// <summary>
        /// Authority side motion. Authority instances are excluded from the update loop's
        /// NetworkTransform registration, so this cannot live in <see cref="OnUpdate"/>.
        /// </summary>
        /// <remarks>
        /// Never invoke <see cref="OnUpdate"/> from here. It advances the interpolators by
        /// <see cref="Time.deltaTime"/>, which the update loop already does once per frame for a
        /// non-authority instance.
        /// </remarks>
        private void Update()
        {
            if (!IsSpawned || !CanCommitToTransform)
            {
                return;
            }

            if (IsFixed)
            {
                transform.position = TransformInterpolationTests.ParentPosition;
                return;
            }

            if (!IsMoving)
            {
                return;
            }

            var localTime = NetworkManager.LocalTime.Time;

            if (m_IsToggling && localTime >= m_NextToggleTime)
            {
                InLocalSpace = !InLocalSpace;
                LocalSpaceToggles++;
                m_NextToggleTime += k_ToggleInterval;
            }

            var elapsed = (float)(localTime - m_MotionStartTime);
            transform.position = new Vector3(0.0f, Amplitude * Mathf.Sin(elapsed * 2.0f * Mathf.PI / MotionPeriod), 0.0f);
        }
    }

    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class TransformInterpolationTests : IntegrationTestWithApproximation
    {
        internal static readonly Vector3 ParentPosition = new Vector3(1000.0f, 1000.0f, 1000.0f);

        private const int k_TargetLocalSpaceToggles = 10;

        // The non-authority has to cover most of the motion's range for the samples to mean
        // anything. An instance that stopped updating sits at a single value and would otherwise
        // satisfy every bounds check in the test.
        private const float k_MinimumRangeCovered = 0.5f;

        protected override int NumberOfClients => 1;

        private GameObject m_PrefabToSpawn;

        private TransformInterpolationObject m_AuthorityParent;
        private TransformInterpolationObject m_AuthorityChild;
        private TransformInterpolationObject m_NonAuthorityChild;

        public TransformInterpolationTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("InterpTestObject");
            m_PrefabToSpawn.AddComponent<TransformInterpolationObject>();
        }

        private IEnumerator SpawnAndParent()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            m_AuthorityParent = SpawnObject(m_PrefabToSpawn, authority).GetComponent<TransformInterpolationObject>();
            m_AuthorityChild = SpawnObject(m_PrefabToSpawn, authority).GetComponent<TransformInterpolationObject>();

            var parentId = m_AuthorityParent.NetworkObject.NetworkObjectId;
            var childId = m_AuthorityChild.NetworkObject.NetworkObjectId;
            var clientId = nonAuthority.LocalClientId;

            yield return WaitForConditionOrTimeOut(() => s_GlobalNetworkObjects.ContainsKey(clientId) &&
                s_GlobalNetworkObjects[clientId].ContainsKey(parentId) &&
                s_GlobalNetworkObjects[clientId].ContainsKey(childId));
            AssertOnTimeout($"Timed out waiting for the non-authority to spawn both {nameof(NetworkObject)}s!");

            m_NonAuthorityChild = s_GlobalNetworkObjects[clientId][childId].GetComponent<TransformInterpolationObject>();

            Assert.True(m_AuthorityChild.NetworkObject.TrySetParent(m_AuthorityParent.NetworkObject), "Failed to parent the moving instance!");

            yield return WaitForConditionOrTimeOut(() => m_NonAuthorityChild.transform.parent != null);
            AssertOnTimeout("Timed out waiting for the non-authority instance to be parented!");
        }

        [UnityTest]
        public IEnumerator TransformInterpolationTest()
        {
            yield return SpawnAndParent();

            m_AuthorityParent.IsFixed = true;

            // The child's world space position only means anything once the parent has settled on
            // both instances, since the non-authority reconstructs it through its own parent.
            var nonAuthorityParent = m_NonAuthorityChild.transform.parent;
            yield return WaitForConditionOrTimeOut(() => Approximately(m_AuthorityParent.transform.position, ParentPosition) &&
                Approximately(nonAuthorityParent.position, ParentPosition));
            AssertOnTimeout($"Timed out waiting for the parent instances to settle at {ParentPosition}!");

            m_AuthorityChild.StartMoving();

            // The child rides its parent out at ~1000 until the motion starts, so wait for the
            // non-authority to close that distance before sampling. Its approach to the first
            // authoritative position is not what this test measures.
            yield return WaitForConditionOrTimeOut(() => Mathf.Abs(m_NonAuthorityChild.transform.position.y) <= TransformInterpolationObject.Amplitude);
            AssertOnTimeout("Timed out waiting for the non-authority instance to converge on the authority's motion!");

            m_NonAuthorityChild.StartSampling();
            m_AuthorityChild.StartToggling();

            var timeOutHelper = new TimeoutFrameCountHelper(10);
            yield return WaitForConditionOrTimeOut(() => m_AuthorityChild.LocalSpaceToggles >= k_TargetLocalSpaceToggles, timeOutHelper);
            m_NonAuthorityChild.CheckPosition = false;
            AssertOnTimeout($"Failed to reach {k_TargetLocalSpaceToggles} local to world space transitions in the given time!", timeOutHelper);

            VerboseDebug($"[{nameof(TransformInterpolationTest)}] Toggles ({m_AuthorityChild.LocalSpaceToggles}) | Samples ({m_NonAuthorityChild.SamplesTaken}) | " +
                $"Range ({m_NonAuthorityChild.MinimumSampled} to {m_NonAuthorityChild.MaximumSampled}) | Worst deviation ({m_NonAuthorityChild.WorstDeviation}) | " +
                $"Frames ({timeOutHelper.GetFrameCount()}) | Elapsed ({timeOutHelper.GetTimeElapsed()})");

            Assert.Greater(m_NonAuthorityChild.SamplesTaken, 0, "The non-authority instance was never updated!");

            Assert.LessOrEqual(m_NonAuthorityChild.WorstDeviation, TransformInterpolationObject.PositionTolerance,
                $"The non-authority instance left the expected world space band of [+/- {TransformInterpolationObject.Amplitude}] by " +
                $"{m_NonAuthorityChild.WorstDeviation}, which exceeds the tolerance of {TransformInterpolationObject.PositionTolerance}. " +
                $"Sampled range was {m_NonAuthorityChild.MinimumSampled} to {m_NonAuthorityChild.MaximumSampled}.");

            var rangeCovered = (m_NonAuthorityChild.MaximumSampled - m_NonAuthorityChild.MinimumSampled) / (2.0f * TransformInterpolationObject.Amplitude);
            Assert.GreaterOrEqual(rangeCovered, k_MinimumRangeCovered,
                $"The non-authority instance only covered {rangeCovered:P0} of the authority's motion, so it was not tracking it.");
        }
    }
}
