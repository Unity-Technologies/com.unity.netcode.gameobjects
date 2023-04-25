using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Helper component for all NetworkTransformTests
    /// </summary>
    public class NetworkTransformTestComponent : NetworkTransform
    {
        public bool ServerAuthority;
        public bool ReadyToReceivePositionUpdate = false;

        public NetworkTransformState AuthorityLastSentState;
        public bool StatePushed { get; internal set; }

        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            StatePushed = true;
            AuthorityLastSentState = networkTransformState;
            base.OnAuthorityPushTransformState(ref networkTransformState);
        }


        public bool StateUpdated { get; internal set; }
        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            StateUpdated = true;
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
        }

        protected override bool OnIsServerAuthoritative()
        {
            return ServerAuthority;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            ReadyToReceivePositionUpdate = true;
        }

        public void CommitToTransform()
        {
            TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
        }

        public (bool isDirty, bool isPositionDirty, bool isRotationDirty, bool isScaleDirty) ApplyState()
        {
            var transformState = ApplyLocalNetworkState(transform);
            return (transformState.IsDirty, transformState.HasPositionChange, transformState.HasRotAngleChange, transformState.HasScaleChange);
        }
    }

    /// <summary>
    /// Helper component for NetworkTransform parenting tests
    /// </summary>
    public class ChildObjectComponent : NetworkBehaviour
    {
        public static readonly List<ChildObjectComponent> Instances = new List<ChildObjectComponent>();
        public static ChildObjectComponent ServerInstance { get; internal set; }
        public static readonly Dictionary<ulong, NetworkObject> ClientInstances = new Dictionary<ulong, NetworkObject>();

        public static void Reset()
        {
            ServerInstance = null;
            ClientInstances.Clear();
            Instances.Clear();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerInstance = this;
            }
            else
            {
                ClientInstances.Add(NetworkManager.LocalClientId, NetworkObject);
            }
            Instances.Add(this);
            base.OnNetworkSpawn();
        }
    }

    /// <summary>
    /// Integration tests for NetworkTransform that will test both
    /// server and host operating modes and will test both authoritative
    /// models for each operating mode.
    /// </summary>
    [TestFixture(HostOrServer.Host, Authority.ServerAuthority)]
    [TestFixture(HostOrServer.Host, Authority.OwnerAuthority)]
    [TestFixture(HostOrServer.Server, Authority.ServerAuthority)]
    [TestFixture(HostOrServer.Server, Authority.OwnerAuthority)]

    public class NetworkTransformTests : IntegrationTestWithApproximation
    {
        private NetworkObject m_AuthoritativePlayer;
        private NetworkObject m_NonAuthoritativePlayer;
        private NetworkObject m_ChildObjectToBeParented;

        private NetworkTransformTestComponent m_AuthoritativeTransform;
        private NetworkTransformTestComponent m_NonAuthoritativeTransform;
        private NetworkTransformTestComponent m_OwnerTransform;

        private readonly Authority m_Authority;

        public enum Authority
        {
            ServerAuthority,
            OwnerAuthority
        }

        public enum Interpolation
        {
            DisableInterpolate,
            EnableInterpolate
        }

        public enum Precision
        {
            Half,
            Full
        }

        public enum Rotation
        {
            Euler,
            Quaternion
        }

        public enum TransformSpace
        {
            World,
            Local
        }

        public enum OverrideState
        {
            Update,
            CommitToTransform,
            SetState
        }

        public enum Axis
        {
            X,
            Y,
            Z,
            XY,
            XZ,
            YZ,
            XYZ
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testWithHost">Determines if we are running as a server or host</param>
        /// <param name="authority">Determines if we are using server or owner authority</param>
        public NetworkTransformTests(HostOrServer testWithHost, Authority authority)
        {
            m_UseHost = testWithHost == HostOrServer.Host ? true : false;
            m_Authority = authority;
        }

        protected override int NumberOfClients => 1;
        protected override bool m_EnableTimeTravel => true;
        protected override bool m_SetupIsACoroutine => false;
        protected override bool m_TearDownIsACoroutine => false;

        private const int k_TickRate = 60;
        private int m_OriginalTargetFrameRate;
        protected override void OnOneTimeSetup()
        {
            m_OriginalTargetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 120;
            base.OnOneTimeSetup();
        }

        protected override void OnOneTimeTearDown()
        {
            Application.targetFrameRate = m_OriginalTargetFrameRate;
            base.OnOneTimeTearDown();
        }

        protected override void OnInlineSetup()
        {
            m_Precision = Precision.Full;
            ChildObjectComponent.Reset();
        }

        protected override void OnInlineTearDown()
        {
            m_EnableVerboseDebug = false;
            Object.DestroyImmediate(m_PlayerPrefab);
        }

        protected override void OnCreatePlayerPrefab()
        {
            var networkTransformTestComponent = m_PlayerPrefab.AddComponent<NetworkTransformTestComponent>();
            networkTransformTestComponent.ServerAuthority = m_Authority == Authority.ServerAuthority;
        }

        protected override void OnServerAndClientsCreated()
        {
            var childObject = CreateNetworkObjectPrefab("ChildObject");
            childObject.AddComponent<ChildObjectComponent>();
            var childNetworkTransform = childObject.AddComponent<NetworkTransform>();
            childNetworkTransform.InLocalSpace = true;
            m_ChildObjectToBeParented = childObject.GetComponent<NetworkObject>();

            // Now apply local transform values
            m_ChildObjectToBeParented.transform.position = m_ChildObjectLocalPosition;
            var childRotation = m_ChildObjectToBeParented.transform.rotation;
            childRotation.eulerAngles = m_ChildObjectLocalRotation;
            m_ChildObjectToBeParented.transform.rotation = childRotation;
            m_ChildObjectToBeParented.transform.localScale = m_ChildObjectLocalScale;
            if (m_EnableVerboseDebug)
            {
                m_ServerNetworkManager.LogLevel = LogLevel.Developer;
                foreach (var clientNetworkManager in m_ClientNetworkManagers)
                {
                    clientNetworkManager.LogLevel = LogLevel.Developer;
                }
            }

            m_ServerNetworkManager.NetworkConfig.TickRate = k_TickRate;
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.TickRate = k_TickRate;
            }
        }

        protected override void OnTimeTravelServerAndClientsConnected()
        {
            // Get the client player representation on both the server and the client side
            var serverSideClientPlayer = m_ServerNetworkManager.ConnectedClients[m_ClientNetworkManagers[0].LocalClientId].PlayerObject;
            var clientSideClientPlayer = m_ClientNetworkManagers[0].LocalClient.PlayerObject;

            m_AuthoritativePlayer = m_Authority == Authority.ServerAuthority ? serverSideClientPlayer : clientSideClientPlayer;
            m_NonAuthoritativePlayer = m_Authority == Authority.ServerAuthority ? clientSideClientPlayer : serverSideClientPlayer;

            // Get the NetworkTransformTestComponent to make sure the client side is ready before starting test
            m_AuthoritativeTransform = m_AuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();
            m_NonAuthoritativeTransform = m_NonAuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();

            m_OwnerTransform = m_AuthoritativeTransform.IsOwner ? m_AuthoritativeTransform : m_NonAuthoritativeTransform;

            // Wait for the client-side to notify it is finished initializing and spawning.
            var success = WaitForConditionOrTimeOutWithTimeTravel(() => m_NonAuthoritativeTransform.ReadyToReceivePositionUpdate == true);
            Assert.True(success, "Timed out waiting for client-side to notify it is ready!");

            Assert.True(m_AuthoritativeTransform.CanCommitToTransform);
            Assert.False(m_NonAuthoritativeTransform.CanCommitToTransform);
            // Just wait for at least one tick for NetworkTransforms to finish synchronization
            WaitForNextTick();
        }

        /// <summary>
        /// Returns true when the server-host and all clients have
        /// instantiated the child object to be used in <see cref="NetworkTransformParentingLocalSpaceOffsetTests"/>
        /// </summary>
        /// <returns></returns>
        private bool AllChildObjectInstancesAreSpawned()
        {
            if (ChildObjectComponent.ServerInstance == null)
            {
                return false;
            }

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (!ChildObjectComponent.ClientInstances.ContainsKey(clientNetworkManager.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AllChildObjectInstancesHaveChild()
        {
            foreach (var instance in ChildObjectComponent.ClientInstances.Values)
            {
                if (instance.transform.parent == null)
                {
                    return false;
                }
            }
            return true;
        }

        // To test that local position, rotation, and scale remain the same when parented.
        private Vector3 m_ChildObjectLocalPosition = new Vector3(5.0f, 0.0f, -5.0f);
        private Vector3 m_ChildObjectLocalRotation = new Vector3(-35.0f, 90.0f, 270.0f);
        private Vector3 m_ChildObjectLocalScale = new Vector3(0.1f, 0.5f, 0.4f);

        /// <summary>
        /// A wait condition specific method that assures the local space coordinates
        /// are not impacted by NetworkTransform when parented.
        /// </summary>
        private bool AllInstancesKeptLocalTransformValues()
        {
            foreach (var childInstance in ChildObjectComponent.Instances)
            {
                var childLocalPosition = childInstance.transform.localPosition;
                var childLocalRotation = childInstance.transform.localRotation.eulerAngles;
                var childLocalScale = childInstance.transform.localScale;

                if (!Approximately(childLocalPosition, m_ChildObjectLocalPosition))
                {
                    return false;
                }
                if (!ApproximatelyEuler(childLocalRotation, m_ChildObjectLocalRotation))
                {
                    return false;
                }
                if (!Approximately(childLocalScale, m_ChildObjectLocalScale))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Handles validating the local space values match the original local space values.
        /// If not, it generates a message containing the axial values that did not match
        /// the target/start local space values.
        /// </summary>
        private void WaitForAllChildrenLocalTransformValuesToMatch()
        {
            var success = WaitForConditionOrTimeOutWithTimeTravel(AllInstancesKeptLocalTransformValues);
            var infoMessage = string.Empty;
            if (s_GlobalTimeoutHelper.TimedOut)
            {
                foreach (var childInstance in ChildObjectComponent.Instances)
                {
                    var childLocalPosition = childInstance.transform.localPosition;
                    var childLocalRotation = childInstance.transform.localRotation.eulerAngles;
                    var childLocalScale = childInstance.transform.localScale;

                    if (!Approximately(childLocalPosition, m_ChildObjectLocalPosition))
                    {
                        infoMessage += $"[{childInstance.name}] Child's Local Position ({childLocalPosition}) | Original Local Position ({m_ChildObjectLocalPosition})\n";
                    }
                    if (!ApproximatelyEuler(childLocalRotation, m_ChildObjectLocalRotation))
                    {
                        infoMessage += $"[{childInstance.name}] Child's Local Rotation ({childLocalRotation}) | Original Local Rotation ({m_ChildObjectLocalRotation})\n";
                    }
                    if (!Approximately(childLocalScale, m_ChildObjectLocalScale))
                    {
                        infoMessage += $"[{childInstance.name}] Child's Local Scale ({childLocalScale}) | Original Local Rotation ({m_ChildObjectLocalScale})\n";
                    }
                }
                Assert.True(success, $"Timed out waiting for all children to have the correct local space values:\n {infoMessage}");
            }
        }

        /// <summary>
        /// Validates that local space transform values remain the same when a NetworkTransform is
        /// parented under another NetworkTransform
        /// </summary>
        [Test]
        public void NetworkTransformParentedLocalSpaceTest([Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            var authoritativeChildObject = SpawnObject(m_ChildObjectToBeParented.gameObject, m_AuthoritativeTransform.NetworkManager);

            // Assure all of the child object instances are spawned
            var success = WaitForConditionOrTimeOutWithTimeTravel(AllChildObjectInstancesAreSpawned);
            Assert.True(success, "Timed out waiting for all child instances to be spawned!");
            // Just a sanity check as it should have timed out before this check
            Assert.IsNotNull(ChildObjectComponent.ServerInstance, $"The server-side {nameof(ChildObjectComponent)} instance is null!");

            // This determines which parent on the server side should be the parent
            if (m_AuthoritativeTransform.IsServerAuthoritative())
            {
                Assert.True(ChildObjectComponent.ServerInstance.NetworkObject.TrySetParent(m_AuthoritativeTransform.transform, false), "[Authoritative] Failed to parent the child object!");
            }
            else
            {
                Assert.True(ChildObjectComponent.ServerInstance.NetworkObject.TrySetParent(m_NonAuthoritativeTransform.transform, false), "[Non-Authoritative] Failed to parent the child object!");
            }

            // This waits for all child instances to be parented
            success = WaitForConditionOrTimeOutWithTimeTravel(AllChildObjectInstancesHaveChild);
            Assert.True(success, "Timed out waiting for all instances to have parented a child!");

            // This validates each child instance has preserved their local space values
            WaitForAllChildrenLocalTransformValuesToMatch();
        }

        /// <summary>
        /// Validates that moving, rotating, and scaling the authority side with a single
        /// tick will properly synchronize the non-authoritative side with the same values.
        /// </summary>
        private void MoveRotateAndScaleAuthority(Vector3 position, Vector3 rotation, Vector3 scale, OverrideState overrideState)
        {
            switch (overrideState)
            {
                case OverrideState.SetState:
                    {
                        var authoritativeRotation = m_AuthoritativeTransform.GetSpaceRelativeRotation();
                        authoritativeRotation.eulerAngles = rotation;
                        if (m_Authority == Authority.OwnerAuthority)
                        {
                            // Under the scenario where the owner is not the server, and non-auth is the server we set the state from the server
                            // to be updated to the owner.
                            if (m_AuthoritativeTransform.IsOwner && !m_AuthoritativeTransform.IsServer && m_NonAuthoritativeTransform.IsServer)
                            {
                                m_NonAuthoritativeTransform.SetState(position, authoritativeRotation, scale);
                            }
                            else
                            {
                                m_AuthoritativeTransform.SetState(position, authoritativeRotation, scale);
                            }
                        }
                        else
                        {
                            m_AuthoritativeTransform.SetState(position, authoritativeRotation, scale);
                        }

                        break;
                    }
                case OverrideState.Update:
                default:
                    {
                        m_AuthoritativeTransform.transform.position = position;

                        var authoritativeRotation = m_AuthoritativeTransform.GetSpaceRelativeRotation();
                        authoritativeRotation.eulerAngles = rotation;
                        m_AuthoritativeTransform.transform.rotation = authoritativeRotation;
                        m_AuthoritativeTransform.transform.localScale = scale;
                        break;
                    }
            }
        }

        /// <summary>
        /// Waits until the next tick
        /// </summary>
        private void WaitForNextTick()
        {
            var currentTick = m_AuthoritativeTransform.NetworkManager.LocalTime.Tick;
            while (m_AuthoritativeTransform.NetworkManager.LocalTime.Tick == currentTick)
            {
                var frameRate = Application.targetFrameRate;
                if (frameRate <= 0)
                {
                    frameRate = 60;
                }
                var frameDuration = 1f / frameRate;
                TimeTravel(frameDuration, 1);
            }
        }

        // The number of iterations to change position, rotation, and scale for NetworkTransformMultipleChangesOverTime
        // Note: this was reduced from 8 iterations to 3 due to the number of tests based on all of the various parameter combinations
        private const int k_PositionRotationScaleIterations = 3;

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.Prefabs = m_ServerNetworkManager.NetworkConfig.Prefabs;
            networkManager.NetworkConfig.TickRate = k_TickRate;
            base.OnNewClientCreated(networkManager);
        }

        private Precision m_Precision = Precision.Full;
        private float m_CurrentHalfPrecision = 0.0f;
        private const float k_HalfPrecisionPosScale = 0.03f;
        private const float k_HalfPrecisionRot = 0.725f;

        protected override float GetDeltaVarianceThreshold()
        {
            if (m_Precision == Precision.Half)
            {
                return m_CurrentHalfPrecision;
            }
            return base.GetDeltaVarianceThreshold();
        }


        private Axis m_CurrentAxis;
        /// <summary>
        /// This validates that multiple changes can occur within the same tick or over
        /// several ticks while still keeping non-authoritative instances synchronized.
        /// </summary>
        [Test]
        public void NetworkTransformMultipleChangesOverTime([Values] TransformSpace testLocalTransform, [Values] OverrideState overideState,
            [Values] Precision precision, [Values] Rotation rotationSynch, [Values] Axis axis)
        {
            // In the name of reducing the very long time it takes to interpolate and run all of the possible combinations,
            // we only interpolate when the second client joins
            m_AuthoritativeTransform.Interpolate = false;
            m_AuthoritativeTransform.InLocalSpace = testLocalTransform == TransformSpace.Local;
            bool axisX = axis == Axis.X || axis == Axis.XY || axis == Axis.XZ || axis == Axis.XYZ;
            bool axisY = axis == Axis.Y || axis == Axis.XY || axis == Axis.YZ || axis == Axis.XYZ;
            bool axisZ = axis == Axis.Z || axis == Axis.XZ || axis == Axis.YZ || axis == Axis.XYZ;
            m_CurrentAxis = axis;
            // Authority dictates what is synchronized and what the precision is going to be
            // so we only need to set this on the authoritative side.
            m_AuthoritativeTransform.UseHalfFloatPrecision = precision == Precision.Half;
            m_AuthoritativeTransform.UseQuaternionSynchronization = rotationSynch == Rotation.Quaternion;
            m_Precision = precision;

            m_AuthoritativeTransform.SyncPositionX = axisX;
            m_AuthoritativeTransform.SyncPositionY = axisY;
            m_AuthoritativeTransform.SyncPositionZ = axisZ;

            if (!m_AuthoritativeTransform.UseQuaternionSynchronization)
            {
                m_AuthoritativeTransform.SyncRotAngleX = axisX;
                m_AuthoritativeTransform.SyncRotAngleY = axisY;
                m_AuthoritativeTransform.SyncRotAngleZ = axisZ;
            }
            else
            {
                // This is not required for usage (setting the value should not matter when quaternion synchronization is enabled)
                // but is required for this test so we don't get a failure on an axis that is marked to not be synchronized when
                // validating the authority's values on non-authority instances.
                m_AuthoritativeTransform.SyncRotAngleX = true;
                m_AuthoritativeTransform.SyncRotAngleY = true;
                m_AuthoritativeTransform.SyncRotAngleZ = true;
            }

            m_AuthoritativeTransform.SyncScaleX = axisX;
            m_AuthoritativeTransform.SyncScaleY = axisY;
            m_AuthoritativeTransform.SyncScaleZ = axisZ;


            var positionStart = GetRandomVector3(0.25f, 1.75f);
            var rotationStart = GetRandomVector3(1f, 15f);
            var scaleStart = GetRandomVector3(0.25f, 2.0f);
            var position = positionStart;
            var rotation = rotationStart;
            var scale = scaleStart;
            m_AuthoritativeTransform.StatePushed = false;
            // Wait for the deltas to be pushed
            WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed);
            // Allow the precision settings to propagate first as changing precision
            // causes a teleport event to occur
            WaitForNextTick();

            // Move and rotate within the same tick, validate the non-authoritative instance updates
            // to each set of changes.  Repeat several times.
            for (int i = 0; i < k_PositionRotationScaleIterations; i++)
            {
                m_NonAuthoritativeTransform.StateUpdated = false;
                m_AuthoritativeTransform.StatePushed = false;
                position = positionStart * i;
                rotation = rotationStart * i;
                scale = scaleStart * i;

                // Apply delta between ticks
                MoveRotateAndScaleAuthority(position, rotation, scale, overideState);

                // Wait for the deltas to be pushed
                Assert.True(WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed && m_NonAuthoritativeTransform.StateUpdated), $"[Non-Interpolate {i}] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed}) or state to be updated ({m_NonAuthoritativeTransform.StateUpdated})!");

                // Wait for deltas to synchronize on non-authoritative side
                var success = WaitForConditionOrTimeOutWithTimeTravel(PositionRotationScaleMatches);
                // Provide additional debug info about what failed (if it fails)
                if (!success)
                {
                    m_EnableVerboseDebug = true;
                    PositionRotationScaleMatches();
                    m_EnableVerboseDebug = false;
                }
                Assert.True(success, $"[Non-Interpolate {i}] Timed out waiting for non-authority to match authority's position or rotation");
            }

            // Only enable interpolation when all axis are set (to reduce the test times)
            if (axis == Axis.XYZ)
            {
                // Now, enable interpolation
                m_AuthoritativeTransform.Interpolate = true;
                m_NonAuthoritativeTransform.StateUpdated = false;
                m_AuthoritativeTransform.StatePushed = false;
                // Wait for the delta (change in interpolation) to be pushed
                var success = WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed && m_NonAuthoritativeTransform.StateUpdated);
                Assert.True(success, $"[Interpolation Enable] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed}) or state to be updated ({m_NonAuthoritativeTransform.StateUpdated})!");

                // Continue for one more update with interpolation enabled
                // Note: We are just verifying one update with interpolation enabled due to the number of tests this integration test has to run
                // and since the NestedNetworkTransformTests already tests interpolation under the same number of conditions (excluding Axis).
                // This is just to verify selecting specific axis doesn't cause issues when interpolating as well.
                m_NonAuthoritativeTransform.StateUpdated = false;
                m_AuthoritativeTransform.StatePushed = false;
                position = positionStart * k_PositionRotationScaleIterations;
                rotation = rotationStart * k_PositionRotationScaleIterations;
                scale = scaleStart * k_PositionRotationScaleIterations;
                MoveRotateAndScaleAuthority(position, rotation, scale, overideState);

                // Wait for the deltas to be pushed and updated
                success = WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed && m_NonAuthoritativeTransform.StateUpdated);
                Assert.True(success, $"[Interpolation {k_PositionRotationScaleIterations}] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed}) or state to be updated ({m_NonAuthoritativeTransform.StateUpdated})!");

                success = WaitForConditionOrTimeOutWithTimeTravel(PositionRotationScaleMatches, 120);

                // Provide additional debug info about what failed (if it fails)
                if (!success)
                {
                    m_EnableVerboseDebug = true;
                    PositionRotationScaleMatches();
                    m_EnableVerboseDebug = false;
                }
                Assert.True(success, $"[Interpolation {k_PositionRotationScaleIterations}] Timed out waiting for non-authority to match authority's position or rotation");
            }
        }

        /// <summary>
        /// Checks scale of a late joining client for all instances of the late joining client's player
        /// </summary>
        [Test]
        public void LateJoiningPlayerInitialScaleValues([Values] TransformSpace testLocalTransform, [Values] Interpolation interpolation, [Values] OverrideState overideState)
        {
            var overrideUpdate = overideState == OverrideState.CommitToTransform;
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthoritativeTransform.InLocalSpace = testLocalTransform == TransformSpace.Local;

            var position = GetRandomVector3(0.25f, 1.75f);
            var rotation = GetRandomVector3(1f, 45f);
            var scale = GetRandomVector3(0.25f, 2.0f);

            // Make some changes to the currently connected clients
            m_NonAuthoritativeTransform.StateUpdated = false;
            m_AuthoritativeTransform.StatePushed = false;
            MoveRotateAndScaleAuthority(position, rotation, scale, overideState);

            // Wait for the deltas to be pushed and updated
            var success = WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed && m_NonAuthoritativeTransform.StateUpdated);
            Assert.True(success, $"[Interpolation {k_PositionRotationScaleIterations}] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed}) or state to be updated ({m_NonAuthoritativeTransform.StateUpdated})!");

            WaitForConditionOrTimeOutWithTimeTravel(PositionRotationScaleMatches);

            // Validate the use of the prefab's transform values as opposed to the replicated state (which now is only the last deltas)
            CreateAndStartNewClientWithTimeTravel();
            var newClientNetworkManager = m_ClientNetworkManagers[NumberOfClients];
            foreach (var playerRelativeEntry in m_PlayerNetworkObjects)
            {
                foreach (var playerInstanceEntry in playerRelativeEntry.Value)
                {
                    var playerInstance = playerInstanceEntry.Value;
                    if (newClientNetworkManager.LocalClientId == playerInstance.OwnerClientId)
                    {
                        Assert.IsTrue(Approximately(m_PlayerPrefab.transform.localScale, playerInstance.transform.localScale), $"{playerInstance.name}'s cloned instance's scale does not match original scale!\n" +
                            $"[ClientId-{playerRelativeEntry.Key} Relative] Player-{playerInstance.OwnerClientId}'s LocalScale ({playerInstance.transform.localScale}) vs Target Scale ({m_PlayerPrefab.transform.localScale})");
                    }
                }
            }
        }

        /// <summary>
        /// Tests changing all axial values one at a time.
        /// These tests are performed:
        /// - While in local space and world space
        /// - While interpolation is enabled and disabled
        /// - Using the TryCommitTransformToServer "override" that can be used
        /// from a child derived or external class.
        /// </summary>
        [Test]
        public void TestAuthoritativeTransformChangeOneAtATime([Values] TransformSpace testLocalTransform, [Values] Interpolation interpolation, [Values] OverrideState overideState)
        {
            var overrideUpdate = overideState == OverrideState.CommitToTransform;
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;

            m_AuthoritativeTransform.InLocalSpace = testLocalTransform == TransformSpace.Local;

            // test position
            var authPlayerTransform = overrideUpdate ? m_OwnerTransform.transform : m_AuthoritativeTransform.transform;

            Assert.AreEqual(Vector3.zero, m_NonAuthoritativeTransform.transform.position, "server side pos should be zero at first"); // sanity check

            m_AuthoritativeTransform.StatePushed = false;
            var nextPosition = GetRandomVector3(2f, 30f);
            m_AuthoritativeTransform.transform.position = nextPosition;
            if (overideState != OverrideState.SetState)
            {
                authPlayerTransform.position = nextPosition;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(nextPosition, null, null, m_AuthoritativeTransform.Interpolate);
            }

            bool success;
            if (overideState != OverrideState.Update)
            {
                // Wait for the deltas to be pushed
                success = WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed);
                Assert.True(success, $"[Position] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed})!");
            }

            success = WaitForConditionOrTimeOutWithTimeTravel(() => PositionsMatch());
            Assert.True(success, $"Timed out waiting for positions to match {m_AuthoritativeTransform.transform.position} | {m_NonAuthoritativeTransform.transform.position}");

            // test rotation
            Assert.AreEqual(Quaternion.identity, m_NonAuthoritativeTransform.transform.rotation, "wrong initial value for rotation"); // sanity check

            m_AuthoritativeTransform.StatePushed = false;
            var nextRotation = Quaternion.Euler(GetRandomVector3(5, 60)); // using euler angles instead of quaternions directly to really see issues users might encounter
            if (overideState != OverrideState.SetState)
            {
                authPlayerTransform.rotation = nextRotation;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(null, nextRotation, null, m_AuthoritativeTransform.Interpolate);
            }
            if (overideState != OverrideState.Update)
            {
                // Wait for the deltas to be pushed
                success = WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed);
                Assert.True(success, $"[Rotation] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed})!");
            }

            // Make sure the values match
            success = WaitForConditionOrTimeOutWithTimeTravel(() => RotationsMatch());
            Assert.True(success, $"Timed out waiting for rotations to match");

            m_AuthoritativeTransform.StatePushed = false;
            var nextScale = GetRandomVector3(1, 6);
            if (overrideUpdate)
            {
                authPlayerTransform.localScale = nextScale;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(null, null, nextScale, m_AuthoritativeTransform.Interpolate);
            }
            if (overideState != OverrideState.Update)
            {
                // Wait for the deltas to be pushed
                success = WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed);
                Assert.True(success, $"[Rotation] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed})!");
            }

            // Make sure the scale values match
            success = WaitForConditionOrTimeOutWithTimeTravel(() => ScaleValuesMatch());
            Assert.True(success, $"Timed out waiting for scale values to match");
        }

        /// <summary>
        /// Test to verify nonAuthority cannot change the transform directly
        /// </summary>
        [Test]
        public void VerifyNonAuthorityCantChangeTransform([Values] Interpolation interpolation, [Values] Precision precision)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthoritativeTransform.UseHalfFloatPrecision = precision == Precision.Half;
            m_AuthoritativeTransform.UseQuaternionSynchronization = true;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.UseHalfFloatPrecision = precision == Precision.Half;
            m_NonAuthoritativeTransform.UseQuaternionSynchronization = true;


            Assert.AreEqual(Vector3.zero, m_NonAuthoritativeTransform.transform.position, "other side pos should be zero at first"); // sanity check

            m_NonAuthoritativeTransform.transform.position = new Vector3(4, 5, 6);

            WaitForNextTick();
            WaitForNextTick();

            Assert.AreEqual(Vector3.zero, m_NonAuthoritativeTransform.transform.position, "[Position] NonAuthority was able to change the position!");

            var nonAuthorityRotation = m_NonAuthoritativeTransform.transform.rotation;
            var originalNonAuthorityEulerRotation = nonAuthorityRotation.eulerAngles;
            var nonAuthorityEulerRotation = originalNonAuthorityEulerRotation;
            // Verify rotation is not marked dirty when rotated by half of the threshold
            nonAuthorityEulerRotation.y += 20.0f;
            nonAuthorityRotation.eulerAngles = nonAuthorityEulerRotation;
            m_NonAuthoritativeTransform.transform.rotation = nonAuthorityRotation;
            WaitForNextTick();
            var nonAuthorityCurrentEuler = m_NonAuthoritativeTransform.transform.rotation.eulerAngles;
            Assert.True(originalNonAuthorityEulerRotation.Equals(nonAuthorityCurrentEuler), "[Rotation] NonAuthority was able to change the rotation!");

            var nonAuthorityScale = m_NonAuthoritativeTransform.transform.localScale;
            m_NonAuthoritativeTransform.transform.localScale = nonAuthorityScale * 100;

            WaitForNextTick();

            Assert.True(nonAuthorityScale.Equals(m_NonAuthoritativeTransform.transform.localScale), "[Scale] NonAuthority was able to change the scale!");
        }

        /// <summary>
        /// Validates that rotation checks don't produce false positive
        /// results when rolling over between 0 and 360 degrees
        /// </summary>
        [Test]
        public void TestRotationThresholdDeltaCheck([Values] Interpolation interpolation, [Values] Precision precision)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthoritativeTransform.UseHalfFloatPrecision = precision == Precision.Half;
            m_AuthoritativeTransform.UseQuaternionSynchronization = true;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.UseHalfFloatPrecision = precision == Precision.Half;
            m_NonAuthoritativeTransform.UseQuaternionSynchronization = true;
            m_NonAuthoritativeTransform.RotAngleThreshold = m_AuthoritativeTransform.RotAngleThreshold = 5.0f;

            var halfThreshold = m_AuthoritativeTransform.RotAngleThreshold * 0.5001f;
            var authorityRotation = m_AuthoritativeTransform.transform.rotation;
            var authorityEulerRotation = authorityRotation.eulerAngles;

            // Apply the current state which assures all bitset flags are updated
            var results = m_AuthoritativeTransform.ApplyState();

            // Verify rotation is not marked dirty when rotated by half of the threshold
            authorityEulerRotation.y += halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();
            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {m_AuthoritativeTransform.RotAngleThreshold} degrees and only adjusted by {halfThreshold} degrees!");
            WaitForNextTick();

            // Verify rotation is marked dirty when rotated by another half threshold value
            authorityEulerRotation.y += halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();
            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by the threshold value: {m_AuthoritativeTransform.RotAngleThreshold} degrees!");
            WaitForNextTick();

            //Reset rotation back to zero on all axis
            authorityRotation.eulerAngles = authorityEulerRotation = Vector3.zero;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            WaitForNextTick();

            // Rotate by 360 minus halfThreshold (which is really just negative halfThreshold) and verify rotation is not marked dirty
            authorityEulerRotation.y = 360 - halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();

            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {m_AuthoritativeTransform.RotAngleThreshold} degrees and only adjusted by " +
                $"{Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");

            authorityEulerRotation.y -= halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();

            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by {Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");

            //Reset rotation back to zero on all axis
            authorityRotation.eulerAngles = authorityEulerRotation = Vector3.zero;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            WaitForNextTick();

            authorityEulerRotation.y -= halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();
            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {m_AuthoritativeTransform.RotAngleThreshold} degrees and only adjusted by " +
                $"{Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");

            authorityEulerRotation.y -= halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();

            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by {Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");
        }

        private bool ValidateBitSetValues(NetworkTransform.NetworkTransformState serverState, NetworkTransform.NetworkTransformState clientState)
        {
            if (serverState.HasPositionX == clientState.HasPositionX && serverState.HasPositionY == clientState.HasPositionY && serverState.HasPositionZ == clientState.HasPositionZ &&
                serverState.HasRotAngleX == clientState.HasRotAngleX && serverState.HasRotAngleY == clientState.HasRotAngleY && serverState.HasRotAngleZ == clientState.HasRotAngleZ &&
                serverState.HasScaleX == clientState.HasScaleX && serverState.HasScaleY == clientState.HasScaleY && serverState.HasScaleZ == clientState.HasScaleZ)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Test to make sure that the bitset value is updated properly
        /// </summary>
        [Test]
        public void TestBitsetValue([Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.RotAngleThreshold = m_AuthoritativeTransform.RotAngleThreshold = 0.1f;
            WaitForNextTick();

            m_AuthoritativeTransform.transform.rotation = Quaternion.Euler(1, 2, 3);
            var serverLastSentState = m_AuthoritativeTransform.AuthorityLastSentState;
            var clientReplicatedState = m_NonAuthoritativeTransform.ReplicatedNetworkState.Value;
            var success = WaitForConditionOrTimeOutWithTimeTravel(() => ValidateBitSetValues(serverLastSentState, clientReplicatedState));
            Assert.True(success, $"Timed out waiting for Authoritative Bitset state to equal NonAuthoritative replicated Bitset state!");

            success = WaitForConditionOrTimeOutWithTimeTravel(() => RotationsMatch());
            Assert.True(success, $"[Timed-Out] Authoritative rotation {m_AuthoritativeTransform.transform.rotation.eulerAngles} != Non-Authoritative rotation {m_NonAuthoritativeTransform.transform.rotation.eulerAngles}");
        }

        private float m_DetectedPotentialInterpolatedTeleport;

        /// <summary>
        /// The tests teleporting with and without interpolation
        /// </summary>
        [Test]
        public void TeleportTest([Values] Interpolation interpolation, [Values] Precision precision)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthoritativeTransform.UseHalfFloatPrecision = precision == Precision.Half;
            m_Precision = precision;
            var authTransform = m_AuthoritativeTransform.transform;
            var nonAuthPosition = m_NonAuthoritativeTransform.transform.position;
            var currentTick = m_AuthoritativeTransform.NetworkManager.ServerTime.Tick;
            m_DetectedPotentialInterpolatedTeleport = 0.0f;
            var teleportDestination = GetRandomVector3(50.0f, 200.0f);
            m_NonAuthoritativeTransform.StateUpdated = false;
            m_AuthoritativeTransform.StatePushed = false;
            m_AuthoritativeTransform.Teleport(teleportDestination, authTransform.rotation, authTransform.localScale);

            // Wait for the deltas to be pushed and updated
            var success = WaitForConditionOrTimeOutWithTimeTravel(() => m_AuthoritativeTransform.StatePushed && m_NonAuthoritativeTransform.StateUpdated);
            Assert.True(success, $"[Teleport] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed}) or state to be updated ({m_NonAuthoritativeTransform.StateUpdated})!");

            SimulateOneFrame();
            Assert.True(TeleportPositionMatches(nonAuthPosition), $"NonAuthoritative position ({m_NonAuthoritativeTransform.GetSpaceRelativePosition()}) is not the same as the destination position {teleportDestination}!");

            var targetDistance = 0.0f;
            if (!Approximately(m_DetectedPotentialInterpolatedTeleport, 0.0f))
            {
                targetDistance = Mathf.Abs(Vector3.Distance(nonAuthPosition, teleportDestination));
            }
            Assert.IsTrue(Approximately(m_DetectedPotentialInterpolatedTeleport, 0.0f), $"Detected possible interpolation on non-authority side! NonAuthority distance: {m_DetectedPotentialInterpolatedTeleport} | Target distance: {targetDistance}");
        }

        /// <summary>
        /// This test validates the <see cref="NetworkTransform.SetState(Vector3?, Quaternion?, Vector3?, bool)"/> method
        /// usage for the non-authoritative side.  It will either be the owner or the server making/requesting state changes.
        /// This validates that:
        /// - The owner authoritative mode can still be controlled by the server (i.e. owner authoritative with server authority override capabilities)
        /// - The server authoritative mode can still be directed by the client owner.
        /// </summary>
        /// <remarks>
        /// This also tests that the original server authoritative model with client-owner driven NetworkTransforms is preserved.
        /// </remarks>
        [Test]
        public void NonAuthorityOwnerSettingStateTest([Values] Interpolation interpolation)
        {
            var interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthoritativeTransform.Interpolate = interpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolate;
            m_NonAuthoritativeTransform.RotAngleThreshold = m_AuthoritativeTransform.RotAngleThreshold = 0.1f;

            // Test one parameter at a time first
            var newPosition = new Vector3(125f, 35f, 65f);
            var newRotation = Quaternion.Euler(1, 2, 3);
            var newScale = new Vector3(2.0f, 2.0f, 2.0f);
            m_NonAuthoritativeTransform.SetState(newPosition, null, null, interpolate);
            var success = WaitForConditionOrTimeOutWithTimeTravel(() => PositionsMatchesValue(newPosition));
            Assert.True(success, $"Timed out waiting for non-authoritative position state request to be applied!");
            Assert.True(Approximately(newPosition, m_AuthoritativeTransform.transform.position), "Authoritative position does not match!");
            Assert.True(Approximately(newPosition, m_NonAuthoritativeTransform.transform.position), "Non-Authoritative position does not match!");

            m_NonAuthoritativeTransform.SetState(null, newRotation, null, interpolate);
            success = WaitForConditionOrTimeOutWithTimeTravel(() => RotationMatchesValue(newRotation.eulerAngles));
            Assert.True(success, $"Timed out waiting for non-authoritative rotation state request to be applied!");
            Assert.True(Approximately(newRotation.eulerAngles, m_AuthoritativeTransform.transform.rotation.eulerAngles), "Authoritative rotation does not match!");
            Assert.True(Approximately(newRotation.eulerAngles, m_NonAuthoritativeTransform.transform.rotation.eulerAngles), "Non-Authoritative rotation does not match!");

            m_NonAuthoritativeTransform.SetState(null, null, newScale, interpolate);
            success = WaitForConditionOrTimeOutWithTimeTravel(() => ScaleMatchesValue(newScale));
            Assert.True(success, $"Timed out waiting for non-authoritative scale state request to be applied!");
            Assert.True(Approximately(newScale, m_AuthoritativeTransform.transform.localScale), "Authoritative scale does not match!");
            Assert.True(Approximately(newScale, m_NonAuthoritativeTransform.transform.localScale), "Non-Authoritative scale does not match!");

            // Test all parameters at once
            newPosition = new Vector3(55f, 95f, -25f);
            newRotation = Quaternion.Euler(20, 5, 322);
            newScale = new Vector3(0.5f, 0.5f, 0.5f);

            m_NonAuthoritativeTransform.SetState(newPosition, newRotation, newScale, interpolate);
            success = WaitForConditionOrTimeOutWithTimeTravel(() => PositionRotationScaleMatches(newPosition, newRotation.eulerAngles, newScale));
            Assert.True(success, $"Timed out waiting for non-authoritative position, rotation, and scale state request to be applied!");
            Assert.True(Approximately(newPosition, m_AuthoritativeTransform.transform.position), "Authoritative position does not match!");
            Assert.True(Approximately(newPosition, m_NonAuthoritativeTransform.transform.position), "Non-Authoritative position does not match!");
            Assert.True(Approximately(newRotation.eulerAngles, m_AuthoritativeTransform.transform.rotation.eulerAngles), "Authoritative rotation does not match!");
            Assert.True(Approximately(newRotation.eulerAngles, m_NonAuthoritativeTransform.transform.rotation.eulerAngles), "Non-Authoritative rotation does not match!");
            Assert.True(Approximately(newScale, m_AuthoritativeTransform.transform.localScale), "Authoritative scale does not match!");
            Assert.True(Approximately(newScale, m_NonAuthoritativeTransform.transform.localScale), "Non-Authoritative scale does not match!");
        }

        private const float k_AproximateDeltaVariance = 0.025f;
        private bool PositionsMatchesValue(Vector3 positionToMatch)
        {
            var authorityPosition = m_AuthoritativeTransform.transform.position;
            var nonAuthorityPosition = m_NonAuthoritativeTransform.transform.position;
            var auhtorityIsEqual = Approximately(authorityPosition, positionToMatch);
            var nonauthorityIsEqual = Approximately(nonAuthorityPosition, positionToMatch);

            if (!auhtorityIsEqual)
            {
                VerboseDebug($"Authority position {authorityPosition} != position to match: {positionToMatch}!");
            }
            if (!nonauthorityIsEqual)
            {
                VerboseDebug($"NonAuthority position {nonAuthorityPosition} != position to match: {positionToMatch}!");
            }
            return auhtorityIsEqual && nonauthorityIsEqual;
        }

        private bool RotationMatchesValue(Vector3 rotationEulerToMatch)
        {
            var authorityRotationEuler = m_AuthoritativeTransform.transform.rotation.eulerAngles;
            var nonAuthorityRotationEuler = m_NonAuthoritativeTransform.transform.rotation.eulerAngles;
            var auhtorityIsEqual = Approximately(authorityRotationEuler, rotationEulerToMatch);
            var nonauthorityIsEqual = Approximately(nonAuthorityRotationEuler, rotationEulerToMatch);

            if (!auhtorityIsEqual)
            {
                VerboseDebug($"Authority rotation {authorityRotationEuler} != rotation to match: {rotationEulerToMatch}!");
            }
            if (!nonauthorityIsEqual)
            {
                VerboseDebug($"NonAuthority rotation {nonAuthorityRotationEuler} != rotation to match: {rotationEulerToMatch}!");
            }
            return auhtorityIsEqual && nonauthorityIsEqual;
        }

        private bool ScaleMatchesValue(Vector3 scaleToMatch)
        {
            var authorityScale = m_AuthoritativeTransform.transform.localScale;
            var nonAuthorityScale = m_NonAuthoritativeTransform.transform.localScale;
            var auhtorityIsEqual = Approximately(authorityScale, scaleToMatch);
            var nonauthorityIsEqual = Approximately(nonAuthorityScale, scaleToMatch);

            if (!auhtorityIsEqual)
            {
                VerboseDebug($"Authority scale {authorityScale} != scale to match: {scaleToMatch}!");
            }
            if (!nonauthorityIsEqual)
            {
                VerboseDebug($"NonAuthority scale {nonAuthorityScale} != scale to match: {scaleToMatch}!");
            }
            return auhtorityIsEqual && nonauthorityIsEqual;
        }

        private bool TeleportPositionMatches(Vector3 nonAuthorityOriginalPosition)
        {
            var nonAuthorityPosition = m_NonAuthoritativeTransform.transform.position;
            var authorityPosition = m_AuthoritativeTransform.transform.position;
            var targetDistance = Mathf.Abs(Vector3.Distance(nonAuthorityOriginalPosition, authorityPosition));
            var nonAuthorityCurrentDistance = Mathf.Abs(Vector3.Distance(nonAuthorityPosition, nonAuthorityOriginalPosition));
            // If we are not within our target distance range
            if (!Approximately(targetDistance, nonAuthorityCurrentDistance))
            {
                // Apply the non-authority's distance that is checked at the end of the teleport test
                m_DetectedPotentialInterpolatedTeleport = nonAuthorityCurrentDistance;
                return false;
            }
            else
            {
                // Otherwise, if we are within our target distance range then reset any already set value
                m_DetectedPotentialInterpolatedTeleport = 0.0f;
            }
            var xIsEqual = Approximately(authorityPosition.x, nonAuthorityPosition.x);
            var yIsEqual = Approximately(authorityPosition.y, nonAuthorityPosition.y);
            var zIsEqual = Approximately(authorityPosition.z, nonAuthorityPosition.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"[{m_AuthoritativeTransform.gameObject.name}] Authority position {authorityPosition} != [{m_NonAuthoritativeTransform.gameObject.name}] NonAuthority position {nonAuthorityPosition}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool PositionRotationScaleMatches(Vector3 position, Vector3 eulerRotation, Vector3 scale)
        {
            return PositionsMatchesValue(position) && RotationMatchesValue(eulerRotation) && ScaleMatchesValue(scale);
        }

        private bool PositionRotationScaleMatches()
        {
            return RotationsMatch() && PositionsMatch() && ScaleValuesMatch();
        }

        private void PrintPositionRotationScaleDeltas()
        {
            RotationsMatch(true);
            PositionsMatch(true);
            ScaleValuesMatch(true);
        }

        private bool RotationsMatch(bool printDeltas = false)
        {
            m_CurrentHalfPrecision = k_HalfPrecisionRot;
            var authorityEulerRotation = m_AuthoritativeTransform.GetSpaceRelativeRotation().eulerAngles;
            var nonAuthorityEulerRotation = m_NonAuthoritativeTransform.GetSpaceRelativeRotation().eulerAngles;
            var xIsEqual = ApproximatelyEuler(authorityEulerRotation.x, nonAuthorityEulerRotation.x) || !m_AuthoritativeTransform.SyncRotAngleX;
            var yIsEqual = ApproximatelyEuler(authorityEulerRotation.y, nonAuthorityEulerRotation.y) || !m_AuthoritativeTransform.SyncRotAngleY;
            var zIsEqual = ApproximatelyEuler(authorityEulerRotation.z, nonAuthorityEulerRotation.z) || !m_AuthoritativeTransform.SyncRotAngleZ;
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"[{m_AuthoritativeTransform.gameObject.name}][X-{xIsEqual} | Y-{yIsEqual} | Z-{zIsEqual}][{m_CurrentAxis}]" +
                    $"[Sync: X-{m_AuthoritativeTransform.SyncRotAngleX} |  X-{m_AuthoritativeTransform.SyncRotAngleY} |  X-{m_AuthoritativeTransform.SyncRotAngleZ}] Authority rotation {authorityEulerRotation} != [{m_NonAuthoritativeTransform.gameObject.name}] NonAuthority rotation {nonAuthorityEulerRotation}");
            }
            if (printDeltas)
            {
                Debug.Log($"[Rotation Match] Euler Delta {EulerDelta(authorityEulerRotation, nonAuthorityEulerRotation)}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool PositionsMatch(bool printDeltas = false)
        {
            m_CurrentHalfPrecision = k_HalfPrecisionPosScale;
            var authorityPosition = m_AuthoritativeTransform.GetSpaceRelativePosition();
            var nonAuthorityPosition = m_NonAuthoritativeTransform.GetSpaceRelativePosition();
            var xIsEqual = Approximately(authorityPosition.x, nonAuthorityPosition.x) || !m_AuthoritativeTransform.SyncPositionX;
            var yIsEqual = Approximately(authorityPosition.y, nonAuthorityPosition.y) || !m_AuthoritativeTransform.SyncPositionY;
            var zIsEqual = Approximately(authorityPosition.z, nonAuthorityPosition.z) || !m_AuthoritativeTransform.SyncPositionZ;
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"[{m_AuthoritativeTransform.gameObject.name}] Authority position {authorityPosition} != [{m_NonAuthoritativeTransform.gameObject.name}] NonAuthority position {nonAuthorityPosition}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool ScaleValuesMatch(bool printDeltas = false)
        {
            m_CurrentHalfPrecision = k_HalfPrecisionPosScale;
            var authorityScale = m_AuthoritativeTransform.transform.localScale;
            var nonAuthorityScale = m_NonAuthoritativeTransform.transform.localScale;
            var xIsEqual = Approximately(authorityScale.x, nonAuthorityScale.x) || !m_AuthoritativeTransform.SyncScaleX;
            var yIsEqual = Approximately(authorityScale.y, nonAuthorityScale.y) || !m_AuthoritativeTransform.SyncScaleY;
            var zIsEqual = Approximately(authorityScale.z, nonAuthorityScale.z) || !m_AuthoritativeTransform.SyncScaleZ;
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"[{m_AuthoritativeTransform.gameObject.name}] Authority scale {authorityScale} != [{m_NonAuthoritativeTransform.gameObject.name}] NonAuthority scale {nonAuthorityScale}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }
    }
}
