using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkTransformNonAuthorityTests : IntegrationTestWithApproximation
    {
        private const int k_NumberOfPasses = 3;
        private const float k_LerpTime = 0.1f;
        protected override int NumberOfClients => 2;

        private StringBuilder m_ErrorMsg = new StringBuilder();

        private GameObject m_PrefabToSpawn;

        private NetworkObject m_AuthorityInstance;

        public NetworkTransformNonAuthorityTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        /// <summary>
        /// The NetworkTransform testing component used for this test
        /// </summary>
        public class NetworkTransformTestComponent : NetworkTransform, INetworkUpdateSystem
        {
            public static NetworkTransformTestComponent AuthorityInstance;
            public static readonly List<NetworkTransformTestComponent> AllInstances = new List<NetworkTransformTestComponent>();

            public static bool VerboseDebug;

            public static void Reset()
            {
                AllInstances.Clear();
            }

            /// <summary>
            /// All of the below bools are set when the non-synchronized axis
            /// have reached their target values.
            /// </summary>
            public bool NonSynchronizedPositionReached { get; private set; }
            public bool NonSynchronizedRotationReached { get; private set; }
            public bool NonSynchronizedScaleReached { get; private set; }

            private bool m_UpdateNonSynchronizedAxis;
            private float m_StartMotionTime;
            private float m_Lerp;

            /// <summary>
            /// The below <see cref="Vector3"/> properties are used to
            /// lerp from the current non-synchronized axis values to
            /// the target non-synchronized axis values.
            /// </summary>
            private Vector3 m_OriginalPosition;
            private Vector3 m_OriginalRotation;
            private Vector3 m_OriginalScale;

            /// <summary>
            /// The below <see cref="Vector3"/> properties are the
            /// target non-synchronized axis values.
            /// </summary>
            private Vector3 m_TargetPosition;
            private Vector3 m_TargetRotation;
            private Vector3 m_TargetScale;

            private void Log(string msg)
            {
                if (!VerboseDebug)
                {
                    return;
                }
                Debug.Log(msg);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool Approximately(Vector3 a, Vector3 b, float deltaVariance = 0.0001f)
            {
                return System.Math.Round(Mathf.Abs(a.x - b.x), 4) <= deltaVariance &&
                    System.Math.Round(Mathf.Abs(a.y - b.y), 4) <= deltaVariance &&
                    System.Math.Round(Mathf.Abs(a.z - b.z), 4) <= deltaVariance;
            }

            /// <summary>
            /// For debugging
            /// </summary>
            public void GetUnSynchronizedTargetInfo(StringBuilder builder)
            {
                if (!NonSynchronizedPositionReached)
                {
                    builder.Append($"[Position] Current: {GetNonSynchronizedPosition(transform.position)} | Target: {m_TargetPosition}");
                }
                if (!NonSynchronizedRotationReached)
                {
                    builder.Append($"[Rotation] Current: {GetNonSynchronizedRotation(transform.rotation.eulerAngles)} | Target: {m_TargetRotation}");
                }
                if (!NonSynchronizedScaleReached)
                {
                    builder.Append($"[Scale] Current: {GetNonSynchronizedScale(transform.localScale)} | Target: {m_TargetScale}");
                }

                builder.Append("\n");
            }

            public void ShouldMove(bool shouldMove = false)
            {
                m_UpdateNonSynchronizedAxis = shouldMove;
                if (m_UpdateNonSynchronizedAxis)
                {
                    m_StartMotionTime = Time.realtimeSinceStartup;
                    m_Lerp = 0.0f;
                }
            }

            public bool HasCompletedMotion()
            {
                return NonSynchronizedPositionReached && NonSynchronizedRotationReached && NonSynchronizedScaleReached;
            }

            #region Generate Random Non-Synchronized Axis Values
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float GenerateRandom(float range)
            {
                var random = Random.Range(-range, range);
                var negMult = random < 0 ? -1 : 1;
                random = Mathf.Clamp(Mathf.Abs(random), range * 0.10f, range) * negMult;
                return random;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 SetRandomNonSynchPosition(float range)
            {
                SetNonSynchPositionTarget(GetNonSynchronizedPosition(Vector3.one) * GenerateRandom(range));
                return m_TargetPosition;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetNonSynchPositionTarget(Vector3 target)
            {
                m_OriginalPosition = GetNonSynchronizedPosition(transform.position);
                m_TargetPosition = GetNonSynchronizedPosition(target);
                NonSynchronizedPositionReached = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 SetRandomNonSynchRotation(float range)
            {
                m_OriginalRotation = GetNonSynchronizedRotation(transform.rotation.eulerAngles);
                SetNonSynchRotationTarget(GetNonSynchronizedRotation(Vector3.one) * GenerateRandom(range));
                return m_TargetRotation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetNonSynchRotationTarget(Vector3 target)
            {
                m_TargetRotation = GetNonSynchronizedRotation(target);
                NonSynchronizedRotationReached = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 SetRandomNonSynchScale(float range)
            {
                SetNonSynchScaleTarget(GetNonSynchronizedScale(Vector3.one) * GenerateRandom(range));
                return m_TargetScale;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetNonSynchScaleTarget(Vector3 target)
            {
                m_OriginalScale = GetNonSynchronizedScale(transform.localScale);
                m_TargetScale = target;
                NonSynchronizedScaleReached = false;
            }
            #endregion

            #region Update Synchronized Axis Values
            public Vector3 MovePosition(Vector3 position)
            {
                if (!CanCommitToTransform)
                {
                    return Vector3.zero;
                }

                transform.position += GetSynchronizedPosition(position);
                return transform.position;
            }

            public Vector3 MoveRotation(Vector3 eulerAngles)
            {
                if (!CanCommitToTransform)
                {
                    return Vector3.zero;
                }
                var rotation = transform.rotation;
                rotation.eulerAngles += GetSynchronizedRotation(eulerAngles);
                transform.rotation = rotation;
                return rotation.eulerAngles;
            }

            public Vector3 MoveScale(Vector3 scale)
            {
                if (!CanCommitToTransform)
                {
                    return Vector3.zero;
                }

                transform.localScale += GetSynchronizedScale(scale);
                return transform.localScale;
            }
            #endregion

            #region Methods to Get Synchronized and Non-Synchronized Values
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Vector3 GetSynchronizedPosition(Vector3 position)
            {
                position.x *= SyncPositionX ? 1 : 0;
                position.y *= SyncPositionY ? 1 : 0;
                position.z *= SyncPositionZ ? 1 : 0;
                return position;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Vector3 GetNonSynchronizedPosition(Vector3 position)
            {
                position.x *= !SyncPositionX ? 1 : 0;
                position.y *= !SyncPositionY ? 1 : 0;
                position.z *= !SyncPositionZ ? 1 : 0;
                return position;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Vector3 GetSynchronizedRotation(Vector3 rotation)
            {
                rotation.x *= SyncRotAngleX ? 1 : 0;
                rotation.y *= SyncRotAngleY ? 1 : 0;
                rotation.z *= SyncRotAngleZ ? 1 : 0;
                return rotation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Vector3 GetNonSynchronizedRotation(Vector3 rotation)
            {

                rotation.x *= !SyncRotAngleX ? 1 : 0;
                rotation.y *= !SyncRotAngleY ? 1 : 0;
                rotation.z *= !SyncRotAngleZ ? 1 : 0;

                return rotation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Vector3 GetSynchronizedScale(Vector3 scale)
            {
                scale.x *= SyncScaleX ? 1 : 0;
                scale.y *= SyncScaleY ? 1 : 0;
                scale.z *= SyncScaleZ ? 1 : 0;
                return scale;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Vector3 GetNonSynchronizedScale(Vector3 scale)
            {
                scale.x *= !SyncScaleX ? 1 : 0;
                scale.y *= !SyncScaleY ? 1 : 0;
                scale.z *= !SyncScaleZ ? 1 : 0;
                return scale;
            }
            #endregion

            #region Spawn, Despawn, and Update Methods
            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();

                if (CanCommitToTransform)
                {
                    NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
                }
                AllInstances.Add(this);
            }

            public override void OnNetworkDespawn()
            {
                NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
                base.OnNetworkDespawn();
            }

            public void NetworkUpdate(NetworkUpdateStage updateStage)
            {
                if (updateStage == NetworkUpdateStage.PreUpdate)
                {
                    UpdateNonSynchronizedAxis();
                }
            }

            public override void OnUpdate()
            {
                UpdateNonSynchronizedAxis();
                base.OnUpdate();
            }

            /// <summary>
            /// Updates the non-synchronized axis values
            /// </summary>
            private void UpdateNonSynchronizedAxis()
            {
                if (!m_UpdateNonSynchronizedAxis || HasCompletedMotion())
                {
                    return;
                }

                // Calculate the lerp factor based on when we started motion vs the time to
                // finish lerping.
                var lerpComplete = m_Lerp >= 1.0f;
                if (!lerpComplete)
                {
                    var deltaTime = Time.realtimeSinceStartup - m_StartMotionTime;
                    if (deltaTime > 0.0f)
                    {
                        m_Lerp = Mathf.Clamp(deltaTime / k_LerpTime, 0.001f, 1.0f);
                    }
                }

                // Handle non-synchronized position axis updates
                if (!NonSynchronizedPositionReached)
                {
                    m_OriginalPosition = Vector3.Lerp(m_OriginalPosition, m_TargetPosition, m_Lerp);
                    // Get the sum of the synchronized value with the lerped un-synchronized value and apply the new position.
                    transform.position = GetSynchronizedPosition(transform.position) + m_OriginalPosition;
                    NonSynchronizedPositionReached = Approximately(m_OriginalPosition, m_TargetPosition);
                    if (NonSynchronizedPositionReached || lerpComplete)
                    {
                        Log($"[{name}][Position] Current: {transform.position} | Current-NonSync: {GetNonSynchronizedPosition(transform.position)} | Original: {m_OriginalPosition} Target: {m_TargetPosition}");
                    }
                }

                // Handle non-synchronized rotation axis updates
                if (!NonSynchronizedRotationReached)
                {
                    var rotation = transform.rotation;
                    m_OriginalRotation = Vector3.Lerp(m_OriginalRotation, m_TargetRotation, m_Lerp);
                    rotation.eulerAngles = GetSynchronizedRotation(rotation.eulerAngles) + m_OriginalRotation;
                    transform.rotation = rotation;
                    NonSynchronizedRotationReached = Approximately(m_OriginalRotation, m_TargetRotation);
                    if (NonSynchronizedRotationReached || lerpComplete)
                    {
                        Log($"[{name}][Rotation] Current: {transform.rotation.eulerAngles} | Current-NonSync: {GetNonSynchronizedRotation(transform.rotation.eulerAngles)} | Target: {m_TargetRotation}");
                    }
                }

                // Handle non-synchronized scale axis updates
                if (!NonSynchronizedScaleReached)
                {
                    m_OriginalScale = Vector3.Lerp(m_OriginalScale, m_TargetScale, m_Lerp);
                    transform.localScale = GetSynchronizedScale(transform.localScale) + m_OriginalScale;
                    NonSynchronizedScaleReached = Approximately(m_OriginalScale, m_TargetScale);
                    if (NonSynchronizedScaleReached || lerpComplete)
                    {
                        Log($"[{name}][Scale] Current: {transform.localScale} | Current-NonSync: {GetNonSynchronizedScale(transform.localScale)} | Target: {m_TargetScale}");
                    }
                }
            }
            #endregion
        }

        protected override IEnumerator OnSetup()
        {
            NetworkTransformTestComponent.Reset();
            return base.OnSetup();
        }

        /// <summary>
        /// All of the below versions of <see cref="ShouldSyncAxis"/>
        /// assure that at least 1 axis is disabled and/or 1 axis is enabled
        /// </summary>
        /// <returns></returns>
        private bool ShouldSyncAxis()
        {
            return ShouldSyncAxis(true, true, false);
        }

        private bool ShouldSyncAxis(bool first)
        {
            return ShouldSyncAxis(first, true, false);
        }

        private bool ShouldSyncAxis(bool first, bool second, bool lastValue)
        {
            // Increase chances to not synchronize based on previous values
            var start = 0;
            if (first)
            {
                start += 20;
            }
            if (second)
            {
                start += 30;
            }

            // If we are on the last axis value, then
            // we want to check for the previous two
            // being both enabled or disabled in order
            // to assure there is at least one axis that
            // is enabled and at least one axis that is
            // disabled.
            if (lastValue)
            {
                if (first && second)
                {
                    // If the previous two are enabled, then
                    // make the last one disabled.
                    return false;
                }
                else
                if (!first && !second)
                {
                    // If both are disabled, then make the
                    // last one enabled.
                    return true;
                }
            }
            return Random.Range(start, 100) >= 50 ? false : true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("TestObject");
            var networkTransform = m_PrefabToSpawn.AddComponent<NetworkTransformTestComponent>();

            // Randomly select one or more axis to disable
            networkTransform.SyncPositionX = ShouldSyncAxis();
            networkTransform.SyncPositionY = ShouldSyncAxis(networkTransform.SyncPositionX);
            networkTransform.SyncPositionZ = ShouldSyncAxis(networkTransform.SyncPositionX, networkTransform.SyncPositionY, true);
            networkTransform.SyncRotAngleX = ShouldSyncAxis();
            networkTransform.SyncRotAngleY = ShouldSyncAxis(networkTransform.SyncRotAngleX);
            networkTransform.SyncRotAngleZ = ShouldSyncAxis(networkTransform.SyncRotAngleX, networkTransform.SyncRotAngleY, true);
            networkTransform.SyncScaleX = ShouldSyncAxis();
            networkTransform.SyncScaleY = ShouldSyncAxis(networkTransform.SyncScaleX);
            networkTransform.SyncScaleZ = ShouldSyncAxis(networkTransform.SyncScaleX, networkTransform.SyncScaleY, true);
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Conditional to verify that all spawned instances' transform values match
        /// </summary>
        private bool AllTransformsAreApproximatelyTheSame()
        {
            m_ErrorMsg.Clear();
            var authorityInstance = m_AuthorityInstance.GetComponent<NetworkTransformTestComponent>();

            foreach (var instance in NetworkTransformTestComponent.AllInstances)
            {
                if (instance == authorityInstance)
                {
                    continue;
                }
                if (!Approximately(instance.transform.position, authorityInstance.transform.position))
                {
                    m_ErrorMsg.AppendLine($"[{instance.name}] Position ({instance.transform.position}) is not " +
                        $"equal to authority's ({authorityInstance.transform.position})! ");
                }
                if (!ApproximatelyEuler(instance.transform.rotation.eulerAngles, authorityInstance.transform.rotation.eulerAngles))
                {
                    m_ErrorMsg.AppendLine($"[{instance.name}] Rotation ({instance.transform.rotation.eulerAngles}) is not " +
                        $"equal to authority's ({authorityInstance.transform.rotation.eulerAngles})! ");
                }
                if (!Approximately(instance.transform.localScale, authorityInstance.transform.localScale))
                {
                    m_ErrorMsg.AppendLine($"[{instance.name}] Scale ({instance.transform.localScale}) is not " +
                        $"equal to authority's ({authorityInstance.transform.localScale})! ");
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        /// <summary>
        /// Conditional to verify that all spawned instances' finished their local
        /// non-synchronized axis motion.
        /// </summary>
        private bool AllNonSynchronizedMotionCompleted()
        {
            m_ErrorMsg.Clear();
            foreach (var instance in NetworkTransformTestComponent.AllInstances)
            {
                if (!instance.HasCompletedMotion())
                {
                    m_ErrorMsg.Append($"[{instance.name}] Has not completed local motion!\n");
                    instance.GetUnSynchronizedTargetInfo(m_ErrorMsg);
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        /// <summary>
        /// Conditional to verify that all clients have spawned an instance of the test object.
        /// </summary>
        private bool AllClientsSpawnedObject()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthorityInstance.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Validates that a non-authority instances can apply changes to any non-synchronized
        /// axis value when using a NetworkTransform.
        /// </summary>
        [UnityTest]
        public IEnumerator NonAuthorityUpdateNonSynchronizedAxis()
        {
            var authority = GetNonAuthorityNetworkManager();
            m_AuthorityInstance = SpawnObject(m_PrefabToSpawn, authority).GetComponent<NetworkObject>();
            NetworkTransformTestComponent.AuthorityInstance = m_AuthorityInstance.GetComponent<NetworkTransformTestComponent>();
            yield return WaitForConditionOrTimeOut(AllClientsSpawnedObject);
            AssertOnTimeout($"All clients did not spawn {m_AuthorityInstance.name}!");

            var authorityComponent = m_AuthorityInstance.GetComponent<NetworkTransformTestComponent>();
            for (int i = 0; i < k_NumberOfPasses; i++)
            {
                // Start moving the authority on the axis being synchronized
                var movePosition = NetworkTransformTestComponent.AuthorityInstance.MovePosition(GetRandomVector3(-4, 4));
                var moveRotation = NetworkTransformTestComponent.AuthorityInstance.MoveRotation(GetRandomVector3(-20, 20));
                var moveScale = NetworkTransformTestComponent.AuthorityInstance.MoveScale(GetRandomVector3(-2, 2));

                // Set the non-synchronized axis delta on the authority and preserve each axis delta
                // to be applied to all other non-authority instances.
                var positionDelta = authorityComponent.SetRandomNonSynchPosition(4);
                var rotationDelta = authorityComponent.SetRandomNonSynchRotation(20);
                var scaleDelta = authorityComponent.SetRandomNonSynchScale(2);

                var builder = new StringBuilder();
                builder.AppendLine($"[Iteration-{i}]Final Expected Position: {movePosition + positionDelta} | Non-Synch: {positionDelta}");
                VerboseDebug(builder.ToString());
                foreach (var testTransform in NetworkTransformTestComponent.AllInstances)
                {
                    // We only need to start the authority instance moving
                    // for the non-synchronized axis
                    if (testTransform == authorityComponent)
                    {
                        testTransform.ShouldMove(true);
                        continue;
                    }
                    // Apply the non-synchronized axis deltas to each cloned instance
                    // and start the local motion.
                    testTransform.SetNonSynchPositionTarget(positionDelta);
                    testTransform.SetNonSynchRotationTarget(rotationDelta);
                    testTransform.SetNonSynchScaleTarget(scaleDelta);
                    testTransform.ShouldMove(true);
                }

                // Wait for all instances to finish their local, non-synchronized, axis changes
                yield return WaitForConditionOrTimeOut(AllNonSynchronizedMotionCompleted);
                AssertOnTimeout($"[Iteration: {i}] Not all instances completed local motion! {m_ErrorMsg}");

                // Verify that upon completing motion, all instances' transforms match
                yield return WaitForConditionOrTimeOut(AllTransformsAreApproximatelyTheSame);

                // For debugging purposes
                if (s_GlobalTimeoutHelper.HasTimedOut())
                {
                    builder.Clear();
                    builder.AppendLine($"Final Expected Position: {movePosition + positionDelta}");
                    builder.AppendLine($"Final Expected Rotation: {moveRotation + rotationDelta}");
                    builder.AppendLine($"Final Expected Scale: {moveScale + scaleDelta}");
                    foreach (var testTransform in NetworkTransformTestComponent.AllInstances)
                    {
                        builder.AppendLine($"[Client-{testTransform.NetworkManager.LocalClientId}] " +
                            $"Position: {testTransform.transform.position}" +
                            $"Rotation: {testTransform.transform.rotation.eulerAngles}" +
                            $"Scale: {testTransform.transform.localScale}");
                    }
                    Debug.Log(builder.ToString());
                }
                AssertOnTimeout($"[Iteration: {i}] Not all instances' transforms match! {m_ErrorMsg}");
            }
        }
    }
}
